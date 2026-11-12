using System.Net;
using System.Text;

namespace PortfolioManager.Tests.MathModule;

/// <summary>What the stub saw: the body is read when the call arrives, because the request is disposed afterwards.</summary>
public sealed record RecordedRequest(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string? Body);

/// <summary>
/// Stands in for the math service: no socket is ever opened. Answers come from a queue; the last one repeats,
/// so "always 500" is a single answer.
/// </summary>
public sealed class StubHandler : HttpMessageHandler
{
    private readonly Queue<Func<CancellationToken, Task<HttpResponseMessage>>> _answers = new();
    private Func<CancellationToken, Task<HttpResponseMessage>>? _last;
    private readonly List<RecordedRequest> _calls = new();

    public IReadOnlyList<RecordedRequest> Calls => _calls;

    public RecordedRequest Single => Assert.Single(_calls);

    public StubHandler Json(HttpStatusCode status, string json) =>
        Answer(_ => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        }));

    public StubHandler Text(HttpStatusCode status, string text, string mediaType = "text/html") =>
        Answer(_ => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(text, Encoding.UTF8, mediaType)
        }));

    public StubHandler Error(HttpStatusCode status, string code, string message) =>
        Json(status, $$"""{"code":"{{code}}","message":"{{message}}"}""");

    public StubHandler Throw(Exception exception) => Answer(_ => Task.FromException<HttpResponseMessage>(exception));

    /// <summary>Never answers; ends only when the request is cancelled.</summary>
    public StubHandler Hang() => Answer(async cancellationToken =>
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.OK);
    });

    public StubHandler Answer(Func<CancellationToken, Task<HttpResponseMessage>> answer)
    {
        _answers.Enqueue(answer);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value),
            StringComparer.OrdinalIgnoreCase);
        _calls.Add(new RecordedRequest(request.Method, request.RequestUri!, headers, body));

        if (_answers.Count > 0)
            _last = _answers.Dequeue();

        return await (_last ?? throw new InvalidOperationException("The stub has no answer queued."))(cancellationToken);
    }
}

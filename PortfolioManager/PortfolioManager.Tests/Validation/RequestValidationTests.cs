using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using PortfolioManager.Common.DTO;
using PortfolioManager.Tests.Auth;

namespace PortfolioManager.Tests.Validation;

/// <summary>
/// The rules on the request bodies, checked with the same DataAnnotations validator MVC runs for [ApiController].
/// The clock is fixed at 2026-01-01 12:00 UTC.
/// </summary>
public class RequestValidationTests
{
    private static readonly TestClock Clock = new();

    private static List<ValidationResult> Validate(object instance)
    {
        var services = new ServiceCollection().AddSingleton<TimeProvider>(Clock).BuildServiceProvider();
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance, services, null), results, validateAllProperties: true);
        return results;
    }

    private static CreateStockDTO ValidStock() => new()
    {
        Quantity = 10, Symbol = "AAPL", EntryPrice = 150.25m, EntryDate = new DateTime(2024, 5, 6)
    };

    private static IEnumerable<string> FailedMembers(object instance) =>
        Validate(instance).SelectMany(result => result.MemberNames).Distinct();

    [Fact]
    public void A_correct_position_passes()
    {
        Assert.Empty(Validate(ValidStock()));
    }

    // ---- quantity ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void A_quantity_of_zero_or_less_is_refused(int quantity)
    {
        var stock = ValidStock();
        stock.Quantity = quantity;

        Assert.Equal(new[] { nameof(CreateStockDTO.Quantity) }, FailedMembers(stock));
    }

    // ---- price ------------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(0.00009)] // below the smallest price the decimal(18,4) column keeps: it would be stored as 0
    public void A_price_of_zero_or_less_or_too_small_to_store_is_refused(double price)
    {
        var stock = ValidStock();
        stock.EntryPrice = (decimal)price;

        Assert.Equal(new[] { nameof(CreateStockDTO.EntryPrice) }, FailedMembers(stock));
    }

    [Theory]
    [InlineData(0.0001)] // a penny stock
    [InlineData(1)]
    [InlineData(123456.78)]
    public void Ordinary_prices_pass(double price)
    {
        var stock = ValidStock();
        stock.EntryPrice = (decimal)price;

        Assert.Empty(Validate(stock));
    }

    // ---- symbol -----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("AAPL")]
    [InlineData("aapl")]
    [InlineData("BRK-B")]
    [InlineData("BRK.B")]
    [InlineData("0700.HK")]
    [InlineData("A")]
    [InlineData("ABCDEFGHIJ")] // ten, the width of the column
    public void Real_looking_tickers_pass(string symbol)
    {
        var stock = ValidStock();
        stock.Symbol = symbol;

        Assert.Empty(Validate(stock));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A B")]
    [InlineData("AAPL;DROP")]
    [InlineData("<script>")]
    [InlineData("-AAPL")]
    [InlineData(".AAPL")]
    [InlineData("AAPL\n")] // a line break at the end
    [InlineData("ABCDEFGHIJK")] // eleven
    [InlineData("АААА")] // Cyrillic letters that look Latin
    public void Anything_else_is_refused_as_a_ticker(string symbol)
    {
        var stock = ValidStock();
        stock.Symbol = symbol;

        Assert.Equal(new[] { nameof(CreateStockDTO.Symbol) }, FailedMembers(stock));
    }

    // ---- entry date -------------------------------------------------------------------------------------------

    [Fact]
    public void Today_passes()
    {
        var stock = ValidStock();
        stock.EntryDate = Clock.Now.UtcDateTime;

        Assert.Empty(Validate(stock));
    }

    [Fact]
    public void Tomorrow_in_UTC_passes_because_the_user_may_be_ahead_of_UTC()
    {
        var stock = ValidStock();
        stock.EntryDate = Clock.Now.UtcDateTime.Date.AddDays(1);

        Assert.Empty(Validate(stock));
    }

    [Fact]
    public void A_date_further_in_the_future_is_refused()
    {
        var stock = ValidStock();
        stock.EntryDate = Clock.Now.UtcDateTime.Date.AddDays(2);

        var error = Assert.Single(Validate(stock));
        Assert.Equal(new[] { nameof(CreateStockDTO.EntryDate) }, error.MemberNames);
        Assert.Equal("Дата купівлі не може бути в майбутньому.", error.ErrorMessage);
    }

    [Fact]
    public void A_missing_date_arrives_as_year_one_and_is_refused()
    {
        var stock = ValidStock();
        stock.EntryDate = default;

        var error = Assert.Single(Validate(stock));
        Assert.Equal("Вкажіть дату купівлі.", error.ErrorMessage);
    }

    // ---- changing a position ----------------------------------------------------------------------------------

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    public void A_new_quantity_above_zero_passes(int quantity)
    {
        Assert.Empty(Validate(new UpdateStockDTO { Quantity = quantity }));
    }

    [Theory]
    [InlineData(0)] // also what a missing quantity arrives as
    [InlineData(-3)]
    public void A_new_quantity_of_zero_or_less_is_refused(int quantity)
    {
        Assert.Equal(new[] { nameof(UpdateStockDTO.Quantity) }, FailedMembers(new UpdateStockDTO { Quantity = quantity }));
    }

    // ---- portfolio --------------------------------------------------------------------------------------------

    [Theory]
    [InlineData("Дивідендний")]
    [InlineData("A")]
    public void A_portfolio_with_a_name_passes(string name)
    {
        Assert.Empty(Validate(new CreatePortfolioDTO { Name = name }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void A_portfolio_needs_a_name(string name)
    {
        var error = Assert.Single(Validate(new CreatePortfolioDTO { Name = name }));

        Assert.Equal("Вкажіть назву портфеля.", error.ErrorMessage);
    }

    [Fact]
    public void A_portfolio_name_of_more_than_100_characters_is_refused()
    {
        Assert.Empty(Validate(new CreatePortfolioDTO { Name = new string('a', 100) }));
        Assert.Single(Validate(new CreatePortfolioDTO { Name = new string('a', 101) }));
    }
}

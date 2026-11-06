import { act, renderHook, waitFor } from '@testing-library/react';
import { useAsyncData } from './useAsyncData';

function deferred<T>() {
    let resolve!: (value: T) => void;
    let reject!: (reason: unknown) => void;
    const promise = new Promise<T>((res, rej) => {
        resolve = res;
        reject = rej;
    });
    return { promise, resolve, reject };
}

test('is loading first and then holds the result', async () => {
    const { result } = renderHook(() => useAsyncData(() => Promise.resolve('готово'), []));

    expect(result.current.loading).toBe(true);
    await waitFor(() => expect(result.current.data).toBe('готово'));
    expect(result.current.loading).toBe(false);
    expect(result.current.error).toBeNull();
});

test('keeps the error of a failed attempt', async () => {
    const failure = new Error('немає зв’язку');
    const { result } = renderHook(() => useAsyncData(() => Promise.reject(failure), []));

    await waitFor(() => expect(result.current.error).toBe(failure));
    expect(result.current.loading).toBe(false);
    expect(result.current.data).toBeUndefined();
});

test('reload fetches again and keeps the old data on screen while it waits', async () => {
    let calls = 0;
    const second = deferred<number>();
    const { result } = renderHook(() =>
        useAsyncData(() => (++calls === 1 ? Promise.resolve(1) : second.promise), [])
    );
    await waitFor(() => expect(result.current.data).toBe(1));

    act(() => result.current.reload());

    await waitFor(() => expect(result.current.refreshing).toBe(true));
    expect(result.current.data).toBe(1);
    expect(result.current.loading).toBe(false);

    await act(async () => second.resolve(2));
    expect(result.current.data).toBe(2);
    expect(result.current.refreshing).toBe(false);
});

test('changed inputs clear the old result and ignore a late answer for the old input', async () => {
    const first = deferred<string>();
    const second = deferred<string>();
    const { result, rerender } = renderHook(
        ({ id }) => useAsyncData(() => (id === 1 ? first.promise : second.promise), [id]),
        { initialProps: { id: 1 } }
    );

    rerender({ id: 2 });
    await act(async () => first.resolve('портфель 1'));

    expect(result.current.data).toBeUndefined();
    expect(result.current.loading).toBe(true);

    await act(async () => second.resolve('портфель 2'));
    expect(result.current.data).toBe('портфель 2');
});

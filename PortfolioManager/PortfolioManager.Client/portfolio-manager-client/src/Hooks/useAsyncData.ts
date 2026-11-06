import { DependencyList, useCallback, useEffect, useRef, useState } from 'react';

export interface AsyncData<T> {
    /** The last successful result. It stays while the data is being reloaded, so tables do not flash empty. */
    data: T | undefined;
    /** The error of the last attempt; cleared when an attempt succeeds. */
    error: unknown;
    /** True until the first attempt after mount (or after `deps` change) finishes. */
    loading: boolean;
    /** True while `reload()` is fetching again. */
    refreshing: boolean;
    reload: () => void;
}

/**
 * Loads data when the component mounts and whenever `deps` change; `reload()` fetches again
 * without clearing what is on screen. A response that arrives after `deps` changed or the
 * component unmounted is ignored.
 */
export function useAsyncData<T>(load: () => Promise<T>, deps: DependencyList): AsyncData<T> {
    const [data, setData] = useState<T>();
    const [error, setError] = useState<unknown>(null);
    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    const [attempt, setAttempt] = useState(0);

    const loadRef = useRef(load);
    loadRef.current = load;

    // Different inputs mean different data: drop the old result before fetching the new one.
    useEffect(() => {
        setData(undefined);
        setError(null);
        setLoading(true);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, deps);

    useEffect(() => {
        let ignore = false;
        setRefreshing(true);

        loadRef.current().then(
            (result) => {
                if (ignore) return;
                setData(result);
                setError(null);
                setLoading(false);
                setRefreshing(false);
            },
            (failure) => {
                if (ignore) return;
                setError(failure);
                setLoading(false);
                setRefreshing(false);
            }
        );

        return () => {
            ignore = true;
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [...deps, attempt]);

    const reload = useCallback(() => setAttempt((n) => n + 1), []);

    return { data, error, loading, refreshing, reload };
}

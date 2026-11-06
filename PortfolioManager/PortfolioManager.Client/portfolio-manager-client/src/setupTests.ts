// Adds matchers such as toBeInTheDocument() to Vitest's expect.
import '@testing-library/jest-dom/vitest';
import { configure } from '@testing-library/react';

// The chart pages are loaded on demand, and on a cold run their code takes longer than the default 1 second to
// be transformed. Waiting longer never slows a passing test; it only stops a slow machine from failing it.
configure({ asyncUtilTimeout: 8000 });

// jsdom has no layout, so it also lacks ResizeObserver, which Recharts' ResponsiveContainer needs.
class ResizeObserverStub {
    observe() {}
    unobserve() {}
    disconnect() {}
}
if (!('ResizeObserver' in globalThis)) {
    (globalThis as unknown as { ResizeObserver: typeof ResizeObserverStub }).ResizeObserver = ResizeObserverStub;
}

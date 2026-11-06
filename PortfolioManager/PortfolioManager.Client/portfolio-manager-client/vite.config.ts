/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: [
            // The icon files in the package root are CommonJS; Vite 8 then gives a default import the whole
            // module object instead of the component. The package's own ESM copy has no such problem.
            { find: /^@mui\/icons-material\/(?!esm\/)(.+)$/, replacement: '@mui/icons-material/esm/$1' },
        ],
    },
    server: {
        // 3000 is the origin the API allows in its CORS settings (Cors__Origins__0); PORT overrides it.
        port: Number(process.env.PORT) || 3000,
        strictPort: true,
    },
    build: {
        outDir: 'dist',
        sourcemap: false,
    },
    test: {
        environment: 'jsdom',
        globals: true,
        setupFiles: './src/setupTests.ts',
        css: false,
    },
});

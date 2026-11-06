# Portfolio Manager: web client

React 18 + TypeScript + MUI 5 + Recharts, built with Vite and tested with Vitest. The interface is in Ukrainian.

## Commands

| Command | What it does |
| --- | --- |
| `npm install` | Installs the dependencies. Needs Node.js 22.12 or newer. |
| `npm run dev` | Starts the dev server on <http://localhost:3000> (set `PORT` to use another port). |
| `npm test` | Runs the tests once (`npm run test:watch` keeps them running). |
| `npm run typecheck` | Checks the types without building. |
| `npm run build` | Checks the types, then builds the app into `dist/`. |
| `npm run preview` | Serves the built `dist/` locally. |

## Settings

The address of the API is read **when the app is built**, from `VITE_API_URL` (with a trailing slash):

- `.env` holds the address of the deployed API and is committed.
- `.env.local` overrides it on your machine and is ignored by git. For a local API:
  `VITE_API_URL=http://localhost:5080/api/`.

The API must list the client's origin in its CORS settings (`Cors__Origins__0=http://localhost:3000`).

## Deploying to Azure

The build output is static files in `dist/` (it used to be `build/`).

1. Build with the address of the deployed API: `npm run build` (it takes `VITE_API_URL` from `.env`, or from the environment if set).
2. Upload the **contents of `dist/`** to the `portfoliomanagerclient` App Service.
3. Routes such as `/portfolios/3` must open the app on reload, so the web server has to return `index.html` for any path that is not a file.
4. Add the client's address to `Cors__Origins__N` in the API's App Service settings.

## Folders

- `src/Auth`: the session (access token in memory, refresh token in localStorage), `AuthProvider`, route guards.
- `src/Services`: the shared axios instance (`http.ts`, adds the token and refreshes it on 401) and one file per API area.
- `src/Hooks`, `src/Domain`: data loading and portfolio calculations.
- `src/Components`: pages and shared components.

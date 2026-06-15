# Prompt feedback: Unified SPA + API hosting

## Original prompt

> Make the same site serve the frontend and the API of the application so I don't have to publish two different sites and configure CORS.

## What was clear

- Goal: single origin, no CORS.
- Two separate deployments was the pain point.

## What was ambiguous

- **Dev vs. prod scope**: The prompt could mean "fix the publish pipeline" (already worked) or "also fix the dev workflow". Had to infer both were intended.
- **CORS was mentioned but no CORS code exists**: The existing project had no CORS middleware. The real problem was the two-server dev workflow and potentially the publish not being used.

## Suggestions for better prompts

- Specify the environment: "I have to run two servers in development" or "my deployed frontend and API are on different domains".
- Describe the current pain: "when I run `npm run dev` and `dotnet run`, the React app makes cross-origin API calls" vs. "when I `dotnet publish`, the frontend is separate".
- Describe the goal state: "I want a single `dotnet run` command that serves both" or "I want a single `dotnet publish` output with no CORS headers needed".

## Improved prompt example

> Currently in development I run `dotnet run` (port 5110) and `npm run dev` (port 5173) separately. I want a single `dotnet run` command that starts Vite automatically and serves everything on port 5110, with no CORS needed. Production publish should bundle the React SPA into the .NET output.

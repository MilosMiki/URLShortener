# Frontend

- Uses React
- Run `npm start` to start on localhost:3000
- Implements login using Supabase's authentication

# Backend

- Uses .net
- Run `dotnet run` to start on localhost:5000
- Implements endpoints: 

## `/{shortCode}` (GET)
Redirects any user (incl. unauthenticated) to the target URL. Logs this visit in a counter stored in a Postgres database. Limits visits by IP address to 3 times per minute (returns 429 - Too many requests, if above the limit).
## `/api/my-urls` (GET)
Shows an authenticated user the URLs he shortened. Uses JWT to extract the user email, then to filter the database (by created_by). Limits visits by email to 3 times per minute (returns 429 - Too many requests, if above the limit).
## `/api/shorten` (POST)
Adds a new unique entry to the shortened URLs. Requires authentication via JWT, otherwise returns 401 - Unauthorized.

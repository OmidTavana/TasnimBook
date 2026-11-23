# TasnimBook deployment

## Database

Apply `20261124120000_CentralizedAppReleases` before deploying the new API. It only extends the existing `appversion` table and preserves all rows. Use either EF migrations or the idempotent SQL script in `TasnimBook.Data/Migrations/20261124120000_CentralizedAppReleases.sql`.

## Configuration

- API: provide production connection strings, JWT secret, API keys and administrator bootstrap values through environment variables or the server secret store. Do not keep production secrets in committed `appsettings.json` files.
- Admin: set `Api__OnlineBaseAddress` and `Api__ApiKey` to the public HTTPS API values.
- Landing: set `Api__BaseAddress` to the API URL ending in `/api/`, and `Site__PublicUrl` to the canonical public URL.
- MAUI: configure the existing `ApiConfiguration` production base address before signing release packages.

## IIS

Install the .NET 10 Hosting Bundle, use separate application pools with **No Managed Code**, bind HTTPS certificates, and deploy API, Admin and Web as three applications/sites. API and Admin include a 501 MB IIS upload limit for application packages. Grant the API application-pool identity modify access only to `WWWROOT/Upload/releases`; grant read access to the rest of the deployment.

When a reverse proxy or load balancer is used, forward `X-Forwarded-For` and `X-Forwarded-Proto`. Keep API and Landing URLs HTTPS to avoid mixed content. Back up the database and release storage together.

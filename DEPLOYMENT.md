# Production Meeting — IIS Deployment Guide

## 1. Prerequisites on the target Windows Server

- **IIS** with the **ASP.NET Core Hosting Bundle** installed (Web Platform Installer or
  direct download from https://dotnet.microsoft.com/download/dotnet/9.0 — get the
  "Hosting Bundle" for ASP.NET Core Runtime 9.x, not just the SDK).
- **SQL Server** (Express, Standard, or a shared instance) reachable from the web server.
- The **Windows Authentication** IIS role service:
  Server Manager → Add Roles and Features → Web Server (IIS) → Web Server →
  Security → **Windows Authentication**.
- The app pool identity (or the accounts of everyone who will use the app) must be
  domain accounts if you want Windows Authentication to work across machines — local
  accounts only authenticate correctly when client and server are the same machine or
  in the same workgroup.

## 2. Publish the app

From the project folder:

```powershell
dotnet publish -c Release -o C:\publish\ProductionMeeting
```

This produces a self-contained deployment folder including an auto-generated
`web.config` (IIS uses this to launch the ASP.NET Core Module). Copy the contents of
`C:\publish\ProductionMeeting` to the IIS site's physical path, e.g.
`C:\inetpub\ProductionMeeting`.

## 3. Configure appsettings for this environment

**Do not** commit real connection strings or secrets into `appsettings.json`. Set them
on the server instead, either:

- **IIS Manager → Configuration Editor** (`system.webServer/aspNetCore` → environmentVariables),
  or
- Environment variables on the App Pool / machine:
  - `ConnectionStrings__DefaultConnection` = `Server=YOUR_SQL_SERVER;Database=ProductionMeetingDb;Trusted_Connection=True;TrustServerCertificate=True`
  - `ASPNETCORE_ENVIRONMENT` = `Production`

The app throws a clear startup error if `ConnectionStrings:DefaultConnection` is empty,
by design — this is a deliberate guard against accidentally deploying with no database
configured.

## 4. Create the IIS site and Application Pool

1. **Application Pool**: create a new pool, .NET CLR Version = **No Managed Code**
   (ASP.NET Core doesn't use the CLR-hosted pipeline), Managed Pipeline Mode = Integrated.
2. **Site**: point the physical path at the published folder, bind HTTPS (443) with a
   valid server certificate — see §6.
3. **Identity**: run the App Pool under a domain service account (or `ApplicationPoolIdentity`
   if only local accounts are needed) with read access to the site folder and permission
   to reach SQL Server.

## 5. Enable Windows Authentication correctly (important)

This app uses the `Microsoft.AspNetCore.Authentication.Negotiate` package, which manages
the entire NTLM/Kerberos handshake **inside the app itself** rather than relying on IIS
to do it. For that to work under IIS:

- **Enable** "Anonymous Authentication" on the site.
- **Disable** "Windows Authentication" on the site.

This is the opposite of the classic ASP.NET (Framework) setup, and it's intentional:
if IIS's own Windows Authentication module is also enabled, it intercepts the handshake
before the app sees it and the two auth layers conflict. Letting IIS pass every request
through anonymously and having the app's Negotiate handler issue the `401 WWW-Authenticate: Negotiate`
challenge itself is the officially supported model for this package.

## 6. HTTPS

- Bind a real certificate (internal CA or public) to port 443 in IIS.
- The app's `Program.cs` calls `UseHsts()` and `UseHttpsRedirection()` in non-Development
  environments, so plain HTTP requests are redirected to HTTPS automatically.
- Cookies used elsewhere in the app (antiforgery) are marked `Secure` — they require HTTPS
  to be issued back to the browser, so don't run this in production over plain HTTP.

## 7. Database migration on the target server

The app runs `db.Database.Migrate()` automatically on startup (see `Program.cs`), so the
first time it starts against a fresh database it creates the schema and seed data itself
— no manual `dotnet ef database update` step is required on the server. Make sure the
App Pool identity (or whatever account the connection string authenticates as) has
`db_owner` (or at least `db_ddladmin` + `db_datawriter` + `db_datareader`) on the
target database the first time it runs.

## 8. Folder permissions

Grant the App Pool identity **Read & Execute** on the published folder. No write access
is needed anywhere under the app folder itself — all persistent state lives in SQL Server.

## 9. First run / first Admin

The very first person to successfully sign in (via Windows Authentication) is
automatically provisioned as **Admin** (see `WindowsUserClaimsTransformation`). Make sure
the first real visit to the site is made by whoever should hold that role, then use
**User Management** in the app to add everyone else with the correct role and
plant/line access.

## 10. Verifying the deployment

1. Browse to the site over HTTPS from a domain-joined machine.
2. Confirm you're not prompted for credentials (Windows Authentication should pass
   your logged-in Windows identity through silently) and you land on the Dashboard.
3. Check **Master Data** and **KPI Master** show the seeded Plant/Line/Indicators/Units/KPIs.
4. Create a test Production Meeting entry, save it, and confirm it appears under **Reports**.

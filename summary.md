# Migration Summary: .NET Framework 4.8 → .NET 10

## Status
✅ **Build succeeds with 0 errors, 0 warnings** (`dotnet build BobsBookstoreClassic.sln --verbosity minimal`)

---

## Changes Made

### Project Files

| Project | Before | After | Notes |
|---------|--------|-------|-------|
| `Bookstore.Web` | Legacy MSBuild XML (net4.8) | SDK-style `Microsoft.NET.Sdk.Web`, `net10.0` | Full rewrite |
| `Bookstore.Data` | SDK `netstandard2.0` + EF6 | SDK `net10.0` + EF Core 10 | Dropped netstandard2.0 (EF Core incompatible, see below) |
| `Bookstore.Domain` | SDK `netstandard2.0` | SDK `net10.0;netstandard2.0` | Multi-targeted, no EF dependency |
| `Bookstore.Common` | SDK `netstandard2.0` | Unchanged | No changes needed |
| `Bookstore.Cdk` | SDK `net10.0` | `net10.0` + updated CDK/Constructs | Minor CDK package update |

### Key Code Migrations

#### Bookstore.Web
- **Removed**: `Global.asax.cs`, `Startup.cs` (OWIN), `App_Start/DependencyInjectionSetup.cs` (Autofac), `App_Start/RouteConfig.cs`, `App_Start/BundleConfig.cs`, `Areas/Admin/AdminAreaRegistration.cs`, `Properties/AssemblyInfo.cs`, `Web.config`, `Views/Web.config`
- **Created**: `Program.cs` (ASP.NET Core entry point with built-in DI, routing, auth), `appsettings.json`, `appsettings.Development.json`
- **Migrated**: All controllers — `System.Web.Mvc.Controller` → `Microsoft.AspNetCore.Mvc.Controller`, `ActionResult` → `IActionResult`
- **Migrated**: `AuthenticationSetup.cs` — OWIN OpenIdConnect → ASP.NET Core `AddOpenIdConnect` + `AddCookie`
- **Migrated**: `LocalAuthenticationMiddleware.cs` — `OwinMiddleware` → `IMiddleware` (ASP.NET Core)
- **Migrated**: `HttpContextExtensions.cs` — `HttpContextBase` → `HttpContext` (ASP.NET Core), `HttpCookie` → `CookieOptions`
- **Migrated**: `MvcHelpers.cs` — `HtmlHelper` → `IHtmlHelper`; `IOwinRequestExtensions.cs` — `IOwinRequest` → `HttpRequest`
- **Migrated**: `ImageTypesAttribute.cs`, `MaxFileSizeAttribute.cs` — `HttpPostedFileBase` → `IFormFile`
- **Migrated**: All view models with `System.Web.Mvc.SelectListItem` → `Microsoft.AspNetCore.Mvc.Rendering.SelectListItem`
- **Migrated**: `InventoryCreateUpdateViewModel.cs` — `HttpPostedFileBase CoverImage` → `IFormFile? CoverImage`
- **Fixed**: Admin area views — `Html.EnumDropDownListFor` (MVC5-only) → `Html.DropDownListFor` + `Html.GetEnumSelectList<T>()`
- **Created**: Admin `[Area("Admin")]` via `AdminAreaControllerBase`, area routing in `Program.cs`
- **LoggingSetup**: NLog retained; integrated via `NLog.Extensions.Logging` into `ILoggingBuilder`
- **ConfigurationSetup**: AWS SSM Parameter Store loading retained; adapted to use async `GetAwaiter().GetResult()` at startup
- **BookstoreConfiguration**: Added `Initialize(IConfiguration)` static method to populate from `appsettings.json` for ASP.NET Core; legacy `ConfigurationManager` code removed (net10.0 only target)

#### Bookstore.Data
- **Migrated**: `ApplicationDbContext` — EF6 (`System.Data.Entity.DbContext`) → EF Core 10 (`Microsoft.EntityFrameworkCore.DbContext`)
  - `DbModelBuilder` → `ModelBuilder`
  - `HasRequired(...).WillCascadeOnDelete(false)` → `HasOne(...).OnDelete(DeleteBehavior.NoAction)`
  - `HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)` → `ValueGeneratedOnAdd()`
  - Constructor: `(string connectionString)` → `(DbContextOptions<ApplicationDbContext> options)`
- **Removed**: `BookstoreDbInitializer.cs` (EF6 `DropCreateDatabaseIfModelChanges` — not applicable to EF Core)
- **Migrated**: All repositories — `System.Data.Entity` → `Microsoft.EntityFrameworkCore`; nested `Include(x => x.Coll.Select(...))` → `Include(x => x.Coll).ThenInclude(...)`
- **Migrated**: `PaginatedList.cs` — `System.Data.Entity.DbExtensions` → `Microsoft.EntityFrameworkCore`
- **Migrated**: `BookstoreConfiguration.cs` — static singleton; added `Initialize(IConfiguration)` for ASP.NET Core startup

#### Bookstore.Domain
- Multi-targeted `net10.0;netstandard2.0` — no EF dependency (clean)
- No code changes needed

### Package Changes

| Old Package | New Package | Notes |
|-------------|-------------|-------|
| `EntityFramework 6.5.1` | `Microsoft.EntityFrameworkCore 10.0.0` + `SqlServer` | EF6 → EF Core 10 |
| `Autofac.Integration.Mvc` + `Autofac.Owin` | Removed | Replaced by built-in ASP.NET Core DI |
| `Microsoft.Owin.*` + `Owin` | Removed | Replaced by ASP.NET Core middleware |
| `Microsoft.Owin.Security.OpenIdConnect` | `Microsoft.AspNetCore.Authentication.OpenIdConnect 10.0.0` | Cognito auth |
| `Microsoft.AspNet.Mvc 5.x` | Built-in framework (Web SDK) | ASP.NET Core MVC |
| `System.Web.Optimization` / `WebGrease` / `Antlr` | Removed | Bundling removed; static files served directly |
| `Microsoft.Web.Infrastructure` | Removed | Not needed |
| `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` | Removed | SDK Roslyn compiler used |
| `NLog 5.4.0` | `NLog 5.4.0` + `NLog.Extensions.Logging 5.4.0` | Added NLog→MEL bridge |
| `Magick.NET-Q8-AnyCPU 14.6.0` | `14.17.2` | Security vulnerability fixes |

---

## Architecture Notes

### Dependency Injection
Migrated from Autofac (`ContainerBuilder`, `RegisterType`, etc.) to ASP.NET Core built-in DI (`builder.Services.Add*`) in `Program.cs`. All service/repository registrations are now in `Program.cs`.

### Configuration
- `Web.config` → `appsettings.json` + `appsettings.Development.json`
- `BookstoreConfiguration` updated with `Initialize(IConfiguration)` method called at startup
- Runtime AWS SSM parameter loading retained in `ConfigurationSetup.ConfigureConfiguration()`

### Authentication
- **Local mode**: `LocalAuthenticationMiddleware` (ASP.NET Core `IMiddleware`) + Cookie auth
- **AWS Cognito**: `AddOpenIdConnect` with `TokenValidated` event for customer sync

### Routing
- Area routes registered in `Program.cs` with `MapControllerRoute("areas", "{area:exists}/...")`
- Default route: `{controller=Home}/{action=Index}/{id?}`
- `[Area("Admin")]` attribute on `AdminAreaControllerBase`

### Static Files
- Bundling removed — static assets (Bootstrap via CDN, jQuery in `/Scripts`, CSS in `/Content`) served directly via `app.UseStaticFiles()`
- Views already used CDN for Bootstrap; local scripts served as static files

### Database Migration (EF Core)
The EF Core model maps to the existing SQL Server schema (created by `db-scripts/bobs-used-bookstore-classic-db.sql`). No EF Core migrations are pre-created — the existing database schema is used directly.

---

## Bookstore.Data netstandard2.0 Removal

**Reason**: `Microsoft.EntityFrameworkCore` 10.0.0 targets `net8.0+` and has no `netstandard2.0` support. Multi-targeting `Bookstore.Data` with EF Core for `net10.0` and EF6 for `netstandard2.0` was attempted but EF6 itself has no true `netstandard2.0` target (only `net45`/`net40` assets via NuGet fallback). Compilation failed because `System.Data.Entity` GAC types are unavailable on a .NET SDK netstandard2.0 build.

Per the multi-targeting escape hatch rule (KB 01-project-file-migration.md): `Bookstore.Data` collapsed to single `net10.0` TFM. Any `.NET Framework` consumer of `Bookstore.Data` would need to continue using EF6 directly or upgrade to EF Core.

---

## Next Steps

1. **Database schema validation**: Run `dotnet ef dbcontext validate` to confirm the EF Core model matches the existing SQL Server schema. If there are mismatches, create EF Core migrations or adjust `OnModelCreating`.

2. **Database seeding**: The EF6 `DropCreateDatabaseIfModelChanges` initializer was removed. For a fresh database, use the `db-scripts/bobs-used-bookstore-classic-db.sql` script.

3. **Amazon.CDK.Lib vulnerability**: `Bookstore.Cdk` uses `Amazon.CDK.Lib 2.272.0` — a low-severity vulnerability advisory exists. Updating requires aligning `Cdklabs.CdkNag` which may introduce breaking API changes in CloudFrontWebDistribution (CS0618 obsolete warning already present).

4. **Magick.NET `IImageResizeService` note**: The `Magick.NET-Q8-AnyCPU` package is now at 14.17.2 which resolves all known CVEs. However, a newer 14.x major API change may require testing the `ImageResizeService`.

5. **HTTPS / OIDC redirect**: The `LocalAuthenticationMiddleware` redirects to `/` after login. For Cognito auth, the redirect URI (`/signin-oidc`) must be registered in the Cognito app client. The ECS/Fargate deployment uses HTTP — Cognito auth requires HTTPS (or the App Runner client ID), as noted in the original README.

6. **Anti-forgery tokens**: `AutoValidateAntiforgeryTokenAttribute` added globally in `FilterConfig`. Ensure all POST forms include `@Html.AntiForgeryToken()` or use the `asp-antiforgery` tag helper.

7. **Session/TempData provider**: TempData uses cookie-based storage by default in ASP.NET Core. If `Notification` TempData values aren't persisting across redirects, configure `AddRazorPages()` or `AddSession()`.

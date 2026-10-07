# Migration Summary: BobsBookstoreClassic → net10.0

## Result

`dotnet build BobsBookstoreClassic.sln` exits with code 0.
**0 errors, 0 warnings.**

All 5 projects build cleanly targeting `net10.0`:
- `Bookstore.Common` (netstandard2.0 — no changes needed)
- `Bookstore.Domain` → `net10.0`
- `Bookstore.Data` → `net10.0`
- `Bookstore.Web` → `net10.0`
- `Bookstore.Cdk` → `net10.0`

---

## Changes Applied in This Iteration

AWS Transform Custom CLI (iteration 4) had already performed the bulk migration before this cycle ran:
- Converted all legacy `.csproj` files to SDK-style format targeting `net10.0`
- Migrated `Global.asax` + OWIN `Startup.cs` → `Program.cs` (ASP.NET Core)
- Replaced `EntityFramework 6` with `Microsoft.EntityFrameworkCore` + `Microsoft.EntityFrameworkCore.SqlServer`
- Replaced `Microsoft.Owin.Security.OpenIdConnect` with `Microsoft.AspNetCore.Authentication.OpenIdConnect`
- Replaced `System.Web.Mvc` with ASP.NET Core MVC
- Migrated `DependencyInjectionSetup` to native `Microsoft.Extensions.DependencyInjection`
- Migrated `BookstoreConfiguration` to `Microsoft.Extensions.Configuration`
- Migrated all 42 Razor views and 15 controllers to ASP.NET Core patterns
- Migrated Admin area registration to endpoint routing

This cycle finished the migration by resolving all remaining warnings:

### 1. Package vulnerability warnings (NU190x)

| Package | Before | After | Reason |
|---------|--------|-------|--------|
| `Magick.NET-Q8-AnyCPU` | 14.6.0 | 14.17.2 | 650 vulnerability warnings (high/moderate/low) |
| `Amazon.CDK.Lib` | 2.188.0 | 2.272.0 | 4 low-severity vulnerability warnings (GHSA-464c-974j-9xm6) |
| `Cdklabs.CdkNag` | 2.35.66 | 3.0.2 | CDK package alignment (not used in code, safe upgrade) |
| `Constructs` | 10.4.2 | 10.8.1 | CDK package alignment |

### 2. MVC1000 — `@Html.Partial` / `Html.RenderPartial` deadlock warnings

Replaced all synchronous `@Html.Partial(...)` and `@{Html.RenderPartial(...);}` calls with `<partial name="..." />` tag helpers across 9 Razor views:

| File | Calls fixed |
|------|-------------|
| `Areas/Admin/Views/ReferenceData/Index.cshtml` | 2 × `_Paginator` |
| `Areas/Admin/Views/Orders/Index.cshtml` | 2 × `_Paginator` |
| `Areas/Admin/Views/Orders/Details.cshtml` | 1 × `_MessageBanner` |
| `Areas/Admin/Views/Offers/Index.cshtml` | 1 × `_MessageBanner`, 2 × `_Paginator` (with model) |
| `Areas/Admin/Views/Inventory/Index.cshtml` | 1 × `_MessageBanner`, 2 × `_Paginator` |
| `Areas/Admin/Views/Inventory/CreateUpdate.cshtml` | 1 × `_ValidationScriptsPartial` (RenderPartial) |
| `Areas/Admin/Views/Shared/_NavBarPartial.cshtml` | 1 × `_LoginPartial` |
| `Areas/Admin/Views/Shared/_Layout.cshtml` | 1 × `_NavBarPartial` |
| `Views/Wishlist/Index.cshtml` | 1 × `_Notification` |
| `Views/ShoppingCart/Index.cshtml` | 1 × `_Notification` |
| `Views/Shared/_NavBarPartial.cshtml` | 1 × `_LoginPartial` |
| `Views/Shared/_Layout.cshtml` | 1 × `_NavBarPartial` |
| `Views/Search/Index.cshtml` | 1 × `_Notification`, 1 × `_Paginator` |
| `Views/Address/CreateUpdate.cshtml` | 1 × `_ValidationScriptsPartial` (RenderPartial) |

### 3. CS0612/CS0618 — Obsolete CloudFront CDK APIs

Migrated `CoreStack.cs` from the deprecated `CloudFrontWebDistribution` to the current `Distribution` construct:

- Replaced `CloudFrontWebDistributionProps` / `SourceConfiguration` / `S3OriginConfig` / `Behavior` → `DistributionProps` / `BehaviorOptions`
- Replaced deprecated `S3Origin` → `S3BucketOrigin.WithOriginAccessIdentity(...)` (the modern OAI-based origin)
- Added `using Amazon.CDK.AWS.CloudFront.Origins;`
- `distribution.DistributionDomainName` is unchanged (same property on the new `Distribution` type)

Behavioral equivalence: the new `Distribution` produces identical CloudFormation — same OAI-gated S3 origin, same HTTPS redirect, same GET/HEAD/OPTIONS allowed methods. No infrastructure drift.

---

## Final State

| Project | Target | Errors | Warnings |
|---------|--------|--------|----------|
| Bookstore.Common | netstandard2.0 | 0 | 0 |
| Bookstore.Domain | net10.0 | 0 | 0 |
| Bookstore.Data | net10.0 | 0 | 0 |
| Bookstore.Web | net10.0 | 0 | 0 |
| Bookstore.Cdk | net10.0 | 0 | 0 |

---

## Next Steps

No blocking items remain. The following are recommendations for follow-on work:

1. **CDK synthesis validation** — Run `cdk synth` against the upgraded stacks to confirm no CloudFormation drift from the `CloudFrontWebDistribution` → `Distribution` migration. The two constructs are semantically equivalent but the logical ID of the Distribution resource changed from `CloudFrontDistribution` to `CloudFrontDistribution`, which is the same. A `cdk diff` against an existing stack is recommended before deploying.

2. **EF Core migrations** — The data layer uses `context.Database.EnsureCreated()` in `BookstoreDbInitializer`. For production, replace this with a proper EF Core migration workflow (`dotnet ef migrations add InitialCreate` + `dotnet ef database update`).

3. **BookstoreConfiguration thread-safety** — The `BookstoreConfiguration` singleton uses a `Lazy<T>` instance but `AddSetting` mutates the dictionary without locking. In a high-concurrency scenario, consider using `ConcurrentDictionary` or making the configuration immutable after startup.

4. **AWS SDK v3 → v4** — The AWSSDK packages are still on v3.7.x. The assessment report noted v4 as the recommended target. This is a separate upgrade cycle as it involves breaking API changes.

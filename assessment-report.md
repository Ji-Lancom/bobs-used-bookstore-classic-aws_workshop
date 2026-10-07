# Assessment Report: BobsBookstoreClassic

## Solution Overview

| Attribute | Value |
|-----------|-------|
| **Solution Name** | BobsBookstoreClassic |
| **Total Projects** | 5 |
| **Target Framework** | net10.0 |
| **Total Lines of Code** | 8592 |
| **Overall Complexity** | High |
| **Total NuGet Packages** | 62 (across all projects) |
| **Incompatible Packages** | 12 |
| **.NET Core Readiness** | Not Ready |
| **Linux Readiness** | Partial |

## Executive Summary

**Solution Migration Mode: COMPLEX**

BobsBookstoreClassic is a layered ASP.NET MVC 5 e-commerce application running on .NET Framework 4.8. The architecture follows a classic multi-tier pattern: **Bookstore.Web** (MVC 5 front-end with OWIN authentication) → **Bookstore.Data** (EF6 data access + AWS services) → **Bookstore.Domain** (domain models and service interfaces) → **Bookstore.Common** (shared constants, netstandard2.0). A separate **Bookstore.Cdk** project (already on net6.0 SDK-style) provisions AWS infrastructure via CDK.

The solution is classified as COMPLEX because it contains OWIN middleware (Microsoft.Owin.Security with OpenID Connect authentication pipeline, cookie auth, and Autofac DI integration via OWIN). The web project uses Global.asax for application startup, BundleConfig for asset bundling, RouteConfig for MVC routing, and an AreaRegistration for the Admin area — all of which require migration to ASP.NET Core equivalents.

- **2 Low-complexity projects** (Bookstore.Domain, Bookstore.Cdk) — Bookstore.Domain is a pure domain model library with zero NuGet packages requiring only SDK-style conversion; Bookstore.Cdk is already SDK-style and needs only a TFM bump from net6.0 to net10.0
- **1 Medium-complexity project** (Bookstore.Data) — EF6 data access layer with AWS SDK dependencies, old-style .csproj requiring SDK conversion and EF6-to-EF Core migration
- **1 High-complexity project** (Bookstore.Web) — MVC 5 web application with 54 packages (12 incompatible), OWIN authentication pipeline, Autofac DI, area registration, bundling, and 42 Razor views
- **1 No-migration project** (Bookstore.Common) — Already targets netstandard2.0, cross-platform compatible, requires no changes

The primary transformation challenge is the **Bookstore.Web** project, which requires a complete rewrite of the OWIN-based OpenID Connect authentication pipeline to ASP.NET Core authentication middleware, migration from Autofac.Mvc5/Autofac.Owin to Autofac.Extensions.DependencyInjection (or native ASP.NET Core DI), conversion of Global.asax/Startup.cs to a unified Program.cs, replacement of BundleConfig with static file serving, migration of all 42 Razor views to use Tag Helpers, and migration from Entity Framework 6 to EF Core across the data layer.

### Key Statistics

| Metric | Count |
|--------|-------|
| Projects requiring format conversion | 3 (Bookstore.Domain, Bookstore.Data, Bookstore.Web — legacy to SDK-style) |
| Blocking issues | 0 |
| Razor views to migrate | 42 |
| Controllers to migrate | 15 |
| Total estimated changes | 72 |

## Project Analysis Table

| Project | Current Framework | Target | LOC | Packages | Incompatible | Complexity |
|---------|-------------------|--------|-----|----------|--------------|------------|
| Bookstore.Common | netstandard2.0 | netstandard2.0 | 6 | 0 | 0 | Low |
| Bookstore.Domain | net4.8 | net10.0 | 1813 | 0 | 0 | Low |
| Bookstore.Data | net4.8 | net10.0 | 1042 | 4 | 0 | Medium |
| Bookstore.Cdk | net6.0 | net10.0 | 595 | 4 | 0 | Low |
| Bookstore.Web | net4.8 | net10.0 | 5136 | 54 | 12 | High |

## Cross-Project Package Summary

| Package | Used By | Version(s) | Compatible | Notes |
|---------|---------|------------|------------|-------|
| EntityFramework | Bookstore.Web, Bookstore.Data | 6.5.1 | Yes | Targets netstandard2.1; migrate to EF Core for full modern .NET support |
| AWSSDK.S3 | Bookstore.Web, Bookstore.Data | 3.7.416.5 | Yes | Upgrade to 4.x (latest 4.0.104.1) |
| AWSSDK.Rekognition | Bookstore.Web, Bookstore.Data | 3.7.400.129 | Yes | Upgrade to 4.x (latest 4.0.101.1) |
| AWSSDK.Core | Bookstore.Web, Bookstore.Data | 3.7.402.35 | Yes | Upgrade to 4.x (latest 4.0.102.8); transitive dependency of other AWS packages |
| Microsoft.AspNet.Mvc | Bookstore.Web | 5.3.0 | No | Replace with ASP.NET Core MVC (built into Microsoft.NET.Sdk.Web) |
| Microsoft.Owin.Security.OpenIdConnect | Bookstore.Web | 4.2.2 | No | Replace with Microsoft.AspNetCore.Authentication.OpenIdConnect |
| Autofac.Mvc5 | Bookstore.Web | 6.1.0 | No | Replace with Autofac.Extensions.DependencyInjection |
| Autofac | Bookstore.Web | 8.2.1 | Yes | Upgrade to 9.3.4; use with Autofac.Extensions.DependencyInjection for ASP.NET Core |
| NLog | Bookstore.Web | 5.4.0 | Yes | Upgrade to 6.2.1; add NLog.Web.AspNetCore for ASP.NET Core integration |
| Newtonsoft.Json | Bookstore.Web | 13.0.3 | Yes | Upgrade to 13.0.4 |

## Cross-Project Dependencies

Bookstore.Web (High)
  - Bookstore.Common (Low) [no migration needed]
  - Bookstore.Data (Medium)
    - Bookstore.Domain (Low)
  - Bookstore.Domain (Low)

Bookstore.Cdk (Low)
  - Bookstore.Common (Low) [no migration needed]

### Recommended Transformation Order (Dependency-First)

1. **Bookstore.Domain** — Leaf library with zero NuGet dependencies; convert to SDK-style and retarget to net10.0 first to unblock Bookstore.Data and Bookstore.Web
2. **Bookstore.Data** — Depends only on Bookstore.Domain; convert to SDK-style, retarget, upgrade AWS SDK packages, and plan EF6-to-EF Core migration
3. **Bookstore.Cdk** — Depends only on Bookstore.Common (no migration needed); bump TFM from net6.0 to net10.0 and upgrade CDK packages
4. **Bookstore.Web** — Root web project depending on Domain, Data, and Common; migrate last after all dependencies are on net10.0

## Key Findings

1. **ASP.NET MVC 5 to ASP.NET Core MVC migration**: The Bookstore.Web project is a full ASP.NET MVC 5 application with 15 controllers (including an Admin area with 7 controllers), 42 Razor views, OWIN middleware pipeline, and Autofac DI container integration. All System.Web.Mvc namespaces, routing, filters, and bundling must be replaced with ASP.NET Core equivalents.
2. **OWIN authentication pipeline requires complete rewrite**: The application uses Microsoft.Owin.Security with OpenID Connect (Microsoft.Owin.Security.OpenIdConnect 4.2.2) and cookie authentication (Microsoft.Owin.Security.Cookies 4.2.2) configured through an IAppBuilder Startup class. This must be replaced with ASP.NET Core's authentication middleware (Microsoft.AspNetCore.Authentication.OpenIdConnect + Cookie authentication) in Program.cs.
3. **Entity Framework 6 to EF Core migration**: Both Bookstore.Data and Bookstore.Web reference EntityFramework 6.5.1. While EF6 targets netstandard2.1 and is technically compatible, the recommended path is migration to EF Core for full modern .NET support, performance improvements, and continued maintenance. This affects ApplicationDbContext, repository implementations, and the database initializer.
4. **Three projects require legacy-to-SDK-style project format conversion**: Bookstore.Domain, Bookstore.Data, and Bookstore.Web all use old-style .csproj with `<Import>` directives, `<Compile Include>` item lists, and HintPath references. These must be converted to SDK-style format.
5. **Autofac DI container integration must be modernized**: The web project uses Autofac.Mvc5 (6.1.0) and Autofac.Owin (7.1.0), both targeting only .NETFramework. These must be replaced with Autofac.Extensions.DependencyInjection for ASP.NET Core, and the DependencyInjectionSetup class rewritten for the ASP.NET Core service provider pattern.
6. **AWS SDK packages need major version upgrade**: All AWSSDK packages (S3, Rekognition, CloudWatchLogs, SimpleSystemsManagement, Core) are on v3.7.x and should be upgraded to v4.x for the latest features and .NET 10 optimization.
7. **Bookstore.Common requires no migration**: Already targets netstandard2.0 with SDK-style project format and zero NuGet dependencies — it is cross-platform compatible as-is.
8. **Admin area registration must be converted to ASP.NET Core area routing**: The AdminAreaRegistration class uses the legacy AreaRegistration pattern which does not exist in ASP.NET Core. Area routing must be configured via endpoint routing in Program.cs.

## External Dependencies

| Dependency | Type | Impact |
|------------|------|--------|
| AWS S3 | Cloud Service | Used for file storage (S3FileService); AWSSDK.S3 upgrade from v3 to v4 required |
| AWS Rekognition | Cloud Service | Used for image validation (RekognitionImageValidationService); AWSSDK.Rekognition upgrade required |
| AWS CloudWatch Logs | Cloud Service | Used for centralized logging via NLog (AWS.Logger.NLog); upgrade to v5.x required |
| AWS Systems Manager | Cloud Service | Used for configuration/secrets (AWSSDK.SimpleSystemsManagement); upgrade required |
| AWS CDK | Cloud Service | Infrastructure-as-code (Bookstore.Cdk); Amazon.CDK.Lib upgrade from 2.188.0 to latest |
| OpenID Connect Provider | Identity | OWIN-based OpenID Connect authentication must be migrated to ASP.NET Core auth middleware |
| SQL Server | Database | Entity Framework 6 data access layer; migration to EF Core requires Microsoft.EntityFrameworkCore.SqlServer |

## Actionable Next Steps

1. **Phase 1 — Library migration** (Low risk): Convert Bookstore.Domain to SDK-style .csproj targeting net10.0. No packages to change, pure project format conversion.
2. **Phase 2 — Data layer migration** (Medium risk): Convert Bookstore.Data to SDK-style, retarget to net10.0, upgrade AWSSDK.S3 and AWSSDK.Rekognition to v4.x, upgrade Magick.NET to latest, and migrate EntityFramework 6 to EF Core (Microsoft.EntityFrameworkCore.SqlServer). Rewrite ApplicationDbContext, repositories, and database initializer for EF Core APIs.
3. **Phase 3 — CDK project upgrade** (Low risk): Bump Bookstore.Cdk from net6.0 to net10.0, upgrade Amazon.CDK.Lib, Cdklabs.CdkNag, and Constructs to latest versions.
4. **Phase 4 — Web application migration** (High risk): Convert Bookstore.Web to SDK-style ASP.NET Core MVC project. Replace OWIN with ASP.NET Core middleware, migrate OpenID Connect auth, replace Autofac.Mvc5 with Autofac.Extensions.DependencyInjection, convert Global.asax/Startup.cs to Program.cs, remove BundleConfig (use static files), migrate RouteConfig to endpoint routing, convert Admin AreaRegistration, update all 42 Razor views for Tag Helpers, and upgrade all compatible packages.
5. **Phase 5 — Validation and testing** (Medium risk): End-to-end validation of authentication flows, data access, file upload/image validation, admin area functionality, and AWS service integrations.

---

## Per-Project Assessment Details

### Bookstore.Domain

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net4.8 |
| **Lines of Code** | 1813 |
| **NuGet Packages** | 0 |
| **Project References** | 0 |
| **Complexity** | Low |
| **Estimated Changes** | 1 |

#### Migration Analysis

##### Migration Strategy

1. Convert the old-style `.csproj` (with `<Import>`, `<Compile Include>` items, and MSBuild targets) to SDK-style `<Project Sdk="Microsoft.NET.Sdk">` format. Remove all explicit `<Compile Include>` entries (SDK-style uses glob patterns by default), remove `<Import>` of `Microsoft.Common.props` and `Microsoft.CSharp.targets`, and remove legacy `<PropertyGroup>` sections for Configuration/Platform.
2. Change `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>` to `<TargetFramework>net10.0</TargetFramework>`. Preserve `<RootNamespace>Bookstore.Domain</RootNamespace>` and `<AssemblyName>Bookstore.Domain</AssemblyName>`.
3. Delete `Properties/AssemblyInfo.cs` and set `<GenerateAssemblyInfo>true</GenerateAssemblyInfo>` (SDK-style default). Migrate any custom assembly attributes to csproj properties if needed.
4. Verify that all domain model classes (entities, DTOs, services, interfaces across Addresses, Books, Carts, Customers, Offers, Orders, ReferenceData) compile cleanly against net10.0 — these use only `System.ComponentModel.DataAnnotations` and standard BCL types which are fully available.

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| Assembly reference removal | Low | Old-style csproj has explicit `<Reference Include="System.ComponentModel.DataAnnotations" />` etc.; SDK-style includes these implicitly for net10.0 |

Low risk overall: this is a pure domain model library with zero NuGet dependencies, no System.Web references, and only standard BCL types. The only change is project format conversion.

##### Recommendations

1. Migrate this project first as it is the leaf dependency for both Bookstore.Data and Bookstore.Web — unblocking downstream migrations.
2. After conversion, verify that all 43 source files (models, DTOs, services, interfaces) compile without changes under net10.0.

##### Cross-Project Impact

Bookstore.Domain is referenced by Bookstore.Data (direct ProjectReference) and Bookstore.Web (direct ProjectReference). It must be migrated and verified before either dependent project can be converted. As a leaf library with no dependencies of its own, it has no upstream constraints.

---

### Bookstore.Data

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net4.8 |
| **Lines of Code** | 1042 |
| **NuGet Packages** | 4 |
| **Project References** | 1 |
| **Complexity** | Medium |
| **Estimated Changes** | 5 |

#### Package Compatibility (4 packages, 0 incompatible)

| Package | Version | Compatibility | Recommendation |
|---------|---------|---------------|----------------|
| AWSSDK.Rekognition | 3.7.400.129 | COMPATIBLE | UpgradePackage |
| AWSSDK.S3 | 3.7.416.5 | COMPATIBLE | UpgradePackage |
| EntityFramework | 6.5.1 | COMPATIBLE | KeepPackage |
| Magick.NET-Q8-AnyCPU | 14.6.0 | COMPATIBLE | UpgradePackage |

#### Project Dependencies (1)

- Bookstore.Domain

#### Legacy Files Inventory (1 files across 1 kinds)

| Kind | Files |
|------|------:|
| `.config` | 1 |

#### Migration Analysis

##### Migration Strategy

1. Convert the old-style `.csproj` to SDK-style `<Project Sdk="Microsoft.NET.Sdk">`. Remove all `<Compile Include>` entries, `<Import>` directives, `<Reference Include="...">` with HintPaths, and `<NuGetPackageImportStamp>`. Retain `<PackageReference>` elements (already present in this hybrid csproj).
2. Change `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>` to `<TargetFramework>net10.0</TargetFramework>`. Preserve `<RootNamespace>Bookstore.Data</RootNamespace>` and `<AssemblyName>Bookstore.Data</AssemblyName>`.
3. Delete `Properties/AssemblyInfo.cs` and rely on SDK auto-generation.
4. Upgrade AWSSDK.Rekognition from 3.7.400.129 to latest 4.x (4.0.101.1), AWSSDK.S3 from 3.7.416.5 to 4.x (4.0.104.1), and Magick.NET-Q8-AnyCPU from 14.6.0 to latest (14.17.2). The AWS SDK v4 upgrade may require namespace/API adjustments.
5. Migrate EntityFramework 6.5.1 to Microsoft.EntityFrameworkCore.SqlServer. Rewrite `ApplicationDbContext` (change base class from `DbContext` (EF6) to EF Core `DbContext`), update `BookstoreConfiguration` (EF6 `DbConfiguration` → EF Core configuration in `OnConfiguring`/`OnModelCreating`), replace `BookstoreDbInitializer` (EF6 `IDatabaseInitializer` → EF Core migrations or `EnsureCreated`), and update all 7 repository classes to use EF Core LINQ/async patterns.
6. Migrate `App.config` connection string and EF configuration to `appsettings.json` / DI-injected `IConfiguration` (coordinated with the web project's configuration setup).
7. Replace `System.Configuration` reference with `Microsoft.Extensions.Configuration` if `ConfigurationManager` is used in `BookstoreConfiguration.cs`.

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| EF6 to EF Core API differences | Medium | HasRequired/HasOptional, Database.SetInitializer, lazy loading behavior changes require careful review |
| AWS SDK v3 → v4 breaking changes | Medium | AWSSDK v4 has namespace and API changes; S3FileService and RekognitionImageValidationService must be updated |
| System.Configuration dependency | Low | BookstoreConfiguration uses ConfigurationManager; replace with IConfiguration injection |

##### Recommendations

1. Migrate EF6 to EF Core incrementally: convert DbContext first, then update repositories one at a time, verifying query behavior at each step.
2. Update AWS SDK to v4 and test S3 file operations and Rekognition image validation independently before integrating with the web project.
3. Ensure Magick.NET cross-platform native binaries are included for Linux deployment (the package includes linux-x64 runtimes automatically).

##### Cross-Project Impact

Bookstore.Data is referenced by Bookstore.Web. It depends on Bookstore.Domain (must be migrated first). The EF Core migration in this project will require corresponding changes in Bookstore.Web's `DependencyInjectionSetup.cs` for DbContext registration and in `ConfigurationSetup.cs` for connection string handling.

---

### Bookstore.Cdk

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net6.0 |
| **Lines of Code** | 595 |
| **NuGet Packages** | 4 |
| **Project References** | 1 |
| **Complexity** | Low |
| **Estimated Changes** | 4 |

#### Package Compatibility (4 packages, 0 incompatible)

| Package | Version | Compatibility | Recommendation |
|---------|---------|---------------|----------------|
| Amazon.CDK.Lib | 2.188.0 | COMPATIBLE | UpgradePackage |
| Cdklabs.CdkNag | 2.35.66 | COMPATIBLE | UpgradePackage |
| Constructs | 10.4.2 | COMPATIBLE | UpgradePackage |
| Amazon.Jsii.Analyzers | * | COMPATIBLE | KeepPackage |

#### Project Dependencies (1)

- Bookstore.Common

#### Migration Analysis

##### Migration Strategy

1. Update `<TargetFramework>net6.0</TargetFramework>` to `<TargetFramework>net10.0</TargetFramework>` in the SDK-style csproj (already SDK-style, no format conversion needed). The `<RollForward>Major</RollForward>` setting can be removed since net10.0 will be the direct target.
2. Upgrade Amazon.CDK.Lib from 2.188.0 to latest (2.272.0), Cdklabs.CdkNag from 2.35.66 to latest (3.0.2), and Constructs from 10.4.2 to latest (10.8.1).
3. Review CDK stack definitions (CoreStack, DatabaseStack, EcsStack, NetworkStack) for any deprecated CDK constructs introduced between v2.188 and v2.272. Cdklabs.CdkNag 3.x may have breaking changes from 2.x — verify NagPack/rule configuration.
4. Verify the project builds and `cdk synth` produces valid CloudFormation output after the upgrades.

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| Cdklabs.CdkNag major version jump (2.x → 3.x) | Low | May require minor API adjustments for nag rule configuration |
| CDK construct deprecations | Low | Some constructs may be deprecated between CDK v2.188 and v2.272 |

##### Recommendations

1. Run `cdk diff` after upgrading to verify no unintended infrastructure changes.
2. Upgrade Amazon.CDK.Lib incrementally if the jump from 2.188.0 to 2.272.0 introduces significant construct changes.

##### Cross-Project Impact

Bookstore.Cdk depends on Bookstore.Common (netstandard2.0, no migration needed). It has no downstream dependents. It can be migrated in parallel with Bookstore.Data since they share no direct dependency chain (both depend on leaf libraries only).

---

### Bookstore.Web

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | net4.8 |
| **Lines of Code** | 5136 |
| **NuGet Packages** | 54 |
| **Project References** | 3 |
| **Complexity** | High |
| **Estimated Changes** | 62 |

#### Package Compatibility (54 packages, 12 incompatible)

| Package | Version | Compatibility | Recommendation |
|---------|---------|---------------|----------------|
| Antlr | 3.5.0.2 | COMPATIBLE | ReplacePackage |
| Autofac | 8.2.1 | COMPATIBLE | UpgradePackage |
| Autofac.Mvc5 | 6.1.0 | INCOMPATIBLE | ReplacePackage |
| Autofac.Owin | 7.1.0 | INCOMPATIBLE | ReplacePackage |
| AWS.Logger.Core | 3.3.3 | COMPATIBLE | UpgradePackage |
| AWS.Logger.NLog | 3.3.4 | COMPATIBLE | UpgradePackage |
| AWSSDK.CloudWatchLogs | 3.7.410.17 | COMPATIBLE | UpgradePackage |
| AWSSDK.Core | 3.7.402.35 | COMPATIBLE | UpgradePackage |
| AWSSDK.Rekognition | 3.7.400.129 | COMPATIBLE | UpgradePackage |
| AWSSDK.S3 | 3.7.416.5 | COMPATIBLE | UpgradePackage |
| AWSSDK.SimpleSystemsManagement | 3.7.404.10 | COMPATIBLE | UpgradePackage |
| EntityFramework | 6.5.1 | COMPATIBLE | KeepPackage |
| jQuery | 3.7.1 | COMPATIBLE | ReplacePackage |
| jQuery.Validation | 1.21.0 | COMPATIBLE | ReplacePackage |
| Microsoft.AspNet.Mvc | 5.3.0 | INCOMPATIBLE | ReplacePackage |
| Microsoft.AspNet.Razor | 3.3.0 | INCOMPATIBLE | ReplacePackage |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | INCOMPATIBLE | ReplacePackage |
| Microsoft.AspNet.WebPages | 3.3.0 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Bcl.AsyncInterfaces | 9.0.3 | COMPATIBLE | ReplacePackage |
| Microsoft.Bcl.Memory | 9.0.3 | COMPATIBLE | ReplacePackage |
| Microsoft.Bcl.TimeProvider | 9.0.3 | COMPATIBLE | ReplacePackage |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 4.1.0 | COMPATIBLE | ReplacePackage |
| Microsoft.Extensions.DependencyInjection.Abstractions | 9.0.3 | COMPATIBLE | ReplacePackage |
| Microsoft.Extensions.Logging.Abstractions | 9.0.3 | COMPATIBLE | ReplacePackage |
| Microsoft.IdentityModel.Abstractions | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.IdentityModel.JsonWebTokens | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.IdentityModel.Logging | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.IdentityModel.Protocols | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.IdentityModel.Tokens | 8.7.0 | COMPATIBLE | KeepPackage |
| Microsoft.jQuery.Unobtrusive.Validation | 4.0.0 | COMPATIBLE | ReplacePackage |
| Microsoft.Owin | 4.2.2 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Owin.Host.SystemWeb | 4.2.2 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Owin.Security | 4.2.2 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Owin.Security.Cookies | 4.2.2 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Owin.Security.OpenIdConnect | 4.2.2 | INCOMPATIBLE | ReplacePackage |
| Microsoft.Web.Infrastructure | 2.0.1 | COMPATIBLE | ReplacePackage |
| Modernizr | 2.8.3 | COMPATIBLE | ReplacePackage |
| Newtonsoft.Json | 13.0.3 | COMPATIBLE | UpgradePackage |
| NLog | 5.4.0 | COMPATIBLE | UpgradePackage |
| Owin | 1.0 | INCOMPATIBLE | ReplacePackage |
| System.Buffers | 4.6.1 | COMPATIBLE | ReplacePackage |
| System.Diagnostics.DiagnosticSource | 9.0.3 | COMPATIBLE | ReplacePackage |
| System.IdentityModel.Tokens.Jwt | 8.7.0 | COMPATIBLE | KeepPackage |
| System.IO.Pipelines | 9.0.3 | COMPATIBLE | ReplacePackage |
| System.Memory | 4.6.3 | COMPATIBLE | ReplacePackage |
| System.Numerics.Vectors | 4.6.1 | COMPATIBLE | ReplacePackage |
| System.Runtime.CompilerServices.Unsafe | 6.1.2 | COMPATIBLE | ReplacePackage |
| System.Text.Encoding | 4.3.0 | COMPATIBLE | ReplacePackage |
| System.Text.Encodings.Web | 9.0.3 | COMPATIBLE | ReplacePackage |
| System.Text.Json | 9.0.3 | COMPATIBLE | ReplacePackage |
| System.Threading.Tasks.Extensions | 4.6.3 | COMPATIBLE | ReplacePackage |
| System.ValueTuple | 4.6.1 | COMPATIBLE | ReplacePackage |
| WebGrease | 1.6.0 | COMPATIBLE | ReplacePackage |

#### Project Dependencies (3)

- Bookstore.Common
- Bookstore.Data
- Bookstore.Domain

#### Legacy Files Inventory (49 files across 3 kinds)

| Kind | Files |
|------|------:|
| `.asax` | 1 |
| `.config` | 6 |
| `.cshtml` | 42 |

#### Migration Analysis

##### Migration Strategy

1. **Project format conversion**: Convert the old-style `.csproj` (with `ProjectTypeGuids`, `<Import>` targets for `Microsoft.WebApplication.targets` and `Microsoft.CSharp.targets`, `<Compile Include>` items, `<Content Include>` items, `<Reference Include>` with HintPaths) to SDK-style `<Project Sdk="Microsoft.NET.Sdk.Web">`. Set `<TargetFramework>net10.0</TargetFramework>`. Remove `packages.config` and convert all entries to `<PackageReference>`.
2. **Remove incompatible packages**: Remove all 12 INCOMPATIBLE packages (Microsoft.AspNet.Mvc, Microsoft.AspNet.Razor, Microsoft.AspNet.WebPages, Microsoft.AspNet.Web.Optimization, Microsoft.Owin.*, Owin, Autofac.Mvc5, Autofac.Owin). Remove all BCL shim packages (System.Buffers, System.Memory, System.ValueTuple, etc.) and framework-provided packages (System.Text.Json, System.IO.Pipelines, etc.) that are built into the net10.0 runtime. Remove content/tooling packages (jQuery, Modernizr, WebGrease, Antlr, Microsoft.CodeDom.Providers.DotNetCompilerPlatform, Microsoft.Web.Infrastructure).
3. **Replace OWIN authentication with ASP.NET Core middleware**: Rewrite `Startup.cs` (IAppBuilder-based OWIN pipeline) and `AuthenticationSetup.cs` into `Program.cs` using `builder.Services.AddAuthentication().AddOpenIdConnect().AddCookie()`. Migrate `LocalAuthenticationMiddleware` (OWIN middleware) to ASP.NET Core middleware. Remove `IOwinRequestExtensions.cs` and `HttpContextExtensions.cs` that depend on `Microsoft.Owin.IOwinContext`.
4. **Replace Autofac.Mvc5/Autofac.Owin with ASP.NET Core DI**: Add `Autofac.Extensions.DependencyInjection` package. Rewrite `DependencyInjectionSetup.cs` from `DependencyResolver.SetResolver(new AutofacDependencyResolver(container))` to `builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())`. Upgrade Autofac from 8.2.1 to 9.3.4.
5. **Migrate Global.asax to Program.cs**: Consolidate `Global.asax.cs` (Application_Start), `BundleConfig.cs`, `FilterConfig.cs`, `RouteConfig.cs`, and `ConfigurationSetup.cs` into a unified `Program.cs`. Replace `RouteConfig.RegisterRoutes(RouteTable.Routes)` with `app.MapControllerRoute(...)` endpoint routing. Replace `BundleConfig` with `app.UseStaticFiles()` and move Content/Scripts to `wwwroot/`. Replace `FilterConfig.RegisterGlobalFilters` with ASP.NET Core filter registration.
6. **Migrate Admin area**: Replace `AdminAreaRegistration.cs` (`AreaRegistration`) with ASP.NET Core area routing via `[Area("Admin")]` attribute on controllers and `app.MapAreaControllerRoute(...)` in Program.cs.
7. **Migrate Razor views**: Update all 42 `.cshtml` views to use ASP.NET Core Tag Helpers instead of `@Html.ActionLink`, `@Html.BeginForm`, `@Html.TextBoxFor`, `@Html.ValidationMessageFor`, etc. Add `@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers` to `_ViewImports.cshtml`. Replace `@Scripts.Render("~/bundles/...")` and `@Styles.Render("~/Content/...")` with direct `<script src="...">` and `<link href="...">` references. Update `_Layout.cshtml` files.
8. **Migrate static assets**: Move `Content/` and `Scripts/` directories to `wwwroot/`. Deliver jQuery, jquery-validate, and jquery.validate.unobtrusive via LibMan or direct inclusion in wwwroot (remove NuGet package references for client-side assets).
9. **Migrate Web.config to appsettings.json**: Extract `<appSettings>`, `<connectionStrings>`, and custom configuration sections from `Web.config` into `appsettings.json`. Remove `Web.config`, `Web.Debug.config`, `Web.Release.config`, `Views/Web.config`, and `Areas/Admin/Views/web.config` (ASP.NET Core uses `_ViewImports.cshtml` instead of Views/Web.config).
10. **Migrate logging**: Upgrade NLog from 5.4.0 to 6.2.1, upgrade AWS.Logger.NLog from 3.3.4 to 5.0.1, and configure NLog through `NLog.Web.AspNetCore` in Program.cs. Rewrite `LoggingSetup.cs` for ASP.NET Core hosting.
11. **Upgrade remaining packages**: Upgrade AWSSDK.* packages to v4.x, Newtonsoft.Json to 13.0.4, and coordinate EntityFramework migration with Bookstore.Data.
12. **Delete Properties/AssemblyInfo.cs** and rely on SDK auto-generation.

##### Risks & Architectural Concerns

| Risk | Severity | Notes |
|------|----------|-------|
| OWIN → ASP.NET Core auth migration | High | OpenID Connect authentication flow must be fully replicated; token validation, claims transformation, and cookie configuration must match |
| Autofac DI container migration | Medium | DependencyInjectionSetup registers services via Autofac ContainerBuilder; must be adapted to ASP.NET Core's IServiceCollection + AutofacServiceProviderFactory |
| MVC 5 → ASP.NET Core MVC namespace changes | Medium | All 15 controllers use System.Web.Mvc (Controller, ActionResult, HttpPostedFileBase, SelectList, etc.); must be updated to Microsoft.AspNetCore.Mvc equivalents |
| Bundling removal | Medium | BundleConfig registers script/style bundles; removal requires manual static file references in views and potential build-time minification setup |
| Admin area routing | Low | AreaRegistration pattern must be replaced with attribute-based area routing |
| HttpContext.Current usage | Medium | Any use of HttpContext.Current in helpers (HttpContextExtensions, ClaimsPrincipalExtensions) must be replaced with IHttpContextAccessor injection |
| EF6 shared with Data project | Medium | EntityFramework migration must be coordinated between Web and Data projects |

##### Recommendations

1. Migrate authentication first and validate the OpenID Connect flow end-to-end before proceeding with other changes, as it is the highest-risk component.
2. Use `Autofac.Extensions.DependencyInjection` to preserve existing Autofac registrations while adopting the ASP.NET Core hosting model — this minimizes DI rewrites.
3. Replace `@Html.Action`/`@Html.RenderAction` calls (if any) with ViewComponents before converting views to Tag Helpers.
4. Set up a `wwwroot/` directory structure early and copy static assets before removing bundling references from views.
5. Keep `Newtonsoft.Json` for now (upgrade to 13.0.4) rather than migrating to `System.Text.Json`, to minimize serialization behavior changes.

##### Cross-Project Impact

Bookstore.Web is the root project and depends on Bookstore.Common (no migration needed), Bookstore.Domain, and Bookstore.Data. It must be migrated last, after Bookstore.Domain and Bookstore.Data are on net10.0. The EF Core migration in Bookstore.Data directly affects this project's DbContext registration in DependencyInjectionSetup.cs and the configuration setup. No other project depends on Bookstore.Web.

---

### Bookstore.Common

#### Project Metrics

| Metric | Value |
|--------|-------|
| **Framework** | netstandard2.0 |
| **Lines of Code** | 6 |
| **NuGet Packages** | 0 |
| **Project References** | 0 |
| **Complexity** | Low |
| **Estimated Changes** | 0 |

#### Migration Analysis

##### Migration Strategy

1. **No migration needed.** Bookstore.Common already targets `netstandard2.0` with an SDK-style project file and zero NuGet dependencies. It is cross-platform compatible and can be consumed by any net10.0 project without changes.
2. The project contains a single file (`Constants.cs`, 6 lines) defining shared constants. No code changes are required.

##### Risks & Architectural Concerns

No risks. This project is already cross-platform compatible via netstandard2.0, which is supported by all modern .NET runtimes (net5.0 through net10.0). The project has zero NuGet dependencies and no framework-specific code.

##### Recommendations

1. Leave this project unchanged. It serves as a shared constants library consumed by Bookstore.Cdk and Bookstore.Web.
2. Optionally retarget to net10.0 in the future for access to newer BCL APIs, but this is not required for the current migration.

##### Cross-Project Impact

Bookstore.Common is referenced by Bookstore.Cdk and Bookstore.Web. Since it requires no migration, it imposes no ordering constraint on other projects. It is ready to be consumed by net10.0 projects immediately.

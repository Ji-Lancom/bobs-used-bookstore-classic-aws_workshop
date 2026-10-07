using Amazon.Rekognition;
using Amazon.S3;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using BobsBookstoreClassic.Data;
using Bookstore.Data;
using Bookstore.Data.FileServices;
using Bookstore.Data.ImageResizeService;
using Bookstore.Data.ImageValidationServices;
using Bookstore.Data.Repositories;
using Bookstore.Domain;
using Bookstore.Domain.Addresses;
using Bookstore.Domain.Books;
using Bookstore.Domain.Carts;
using Bookstore.Domain.Customers;
using Bookstore.Domain.Offers;
using Bookstore.Domain.Orders;
using Bookstore.Domain.ReferenceData;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NLog.AWS.Logger;
using NLog.Config;
using NLog.Targets;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Bootstrap BookstoreConfiguration from IConfiguration
InitializeBookstoreConfiguration(builder.Configuration);

// Configure NLog
ConfigureLogging(builder.Configuration);

// Use Autofac as DI container
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Add ASP.NET Core MVC
builder.Services.AddControllersWithViews();

// Add EF Core
var connectionString = BookstoreConfiguration.GetConnectionString("BookstoreDatabaseConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure authentication
var authMode = BookstoreConfiguration.GetSetting("Services/Authentication");
if (authMode == "aws")
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.ClientId = BookstoreConfiguration.GetSetting("Authentication/Cognito/LocalClientId");
        options.MetadataAddress = BookstoreConfiguration.GetSetting("Authentication/Cognito/MetadataAddress");
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.SaveTokens = true;
        options.UseTokenLifetime = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "cognito:username",
            RoleClaimType = "cognito:groups"
        };
        options.Events = new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                context.ProtocolMessage.RedirectUri = GetReturnUrl(context.Request);
                return System.Threading.Tasks.Task.CompletedTask;
            },
            OnAuthorizationCodeReceived = context =>
            {
                context.TokenEndpointRequest!.RedirectUri = GetReturnUrl(context.Request);
                return System.Threading.Tasks.Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var service = context.HttpContext.RequestServices.GetRequiredService<ICustomerService>();
                var identity = context.Principal!.Identity as ClaimsIdentity;

                var dto = new CreateOrUpdateCustomerDto(
                    identity!.GetSub()!,
                    identity!.Name!,
                    identity.FindFirst(c => c.Type.Contains("givenname"))?.Value ?? string.Empty,
                    identity.FindFirst(c => c.Type.Contains("surname"))?.Value ?? string.Empty);

                await service.CreateOrUpdateCustomerAsync(dto);
            }
        };
    });
}
else
{
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Authentication/Login";
        });
}

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterType<BookService>().As<IBookService>();
    containerBuilder.RegisterType<OrderService>().As<IOrderService>();
    containerBuilder.RegisterType<ReferenceDataService>().As<IReferenceDataService>();
    containerBuilder.RegisterType<OfferService>().As<IOfferService>();
    containerBuilder.RegisterType<CustomerService>().As<ICustomerService>();
    containerBuilder.RegisterType<AddressService>().As<IAddressService>();
    containerBuilder.RegisterType<ShoppingCartService>().As<IShoppingCartService>();
    containerBuilder.RegisterType<ImageResizeService>().As<IImageResizeService>();

    containerBuilder.RegisterType<CustomerRepository>().As<ICustomerRepository>();
    containerBuilder.RegisterType<AddressRepository>().As<IAddressRepository>();
    containerBuilder.RegisterType<BookRepository>().As<IBookRepository>();
    containerBuilder.RegisterType<OfferRepository>().As<IOfferRepository>();
    containerBuilder.RegisterType<ShoppingCartRepository>().As<IShoppingCartRepository>();
    containerBuilder.RegisterType<OrderRepository>().As<IOrderRepository>();
    containerBuilder.RegisterType<ReferenceDataRepository>().As<IReferenceDataRepository>();

    containerBuilder.RegisterGeneric(typeof(PaginatedList<>)).As(typeof(IPaginatedList<>)).InstancePerLifetimeScope();

    if (BookstoreConfiguration.GetSetting("Services/FileService") == "aws")
    {
        containerBuilder.RegisterType<AmazonS3Client>().As<IAmazonS3>();
        containerBuilder.RegisterType<S3FileService>().As<IFileService>();
    }
    else
    {
        var env = builder.Environment;
        var webRootPath = Path.Combine(env.WebRootPath ?? env.ContentRootPath, "Content");
        containerBuilder.RegisterInstance(new LocalFileService(webRootPath)).As<IFileService>();
    }

    if (BookstoreConfiguration.GetSetting("Services/ImageValidationService") == "aws")
    {
        containerBuilder.RegisterType<AmazonRekognitionClient>().As<IAmazonRekognition>();
        containerBuilder.RegisterType<RekognitionImageValidationService>().As<IImageValidationService>();
    }
    else
    {
        containerBuilder.RegisterType<LocalImageValidationService>().As<IImageValidationService>();
    }

    if (BookstoreConfiguration.GetSetting("Services/Authentication") != "aws")
    {
        containerBuilder.RegisterType<LocalAuthenticationMiddleware>();
    }
});

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    BookstoreDbInitializer.Seed(dbContext);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/Index/500");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

if (authMode != "aws")
{
    app.UseMiddleware<LocalAuthenticationMiddleware>();
}

app.MapControllerRoute(
    name: "Admin_default",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" },
    constraints: null,
    dataTokens: new { area = "Admin" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void InitializeBookstoreConfiguration(IConfiguration configuration)
{
    var appSettings = new Dictionary<string, string?>();
    var connectionStrings = new Dictionary<string, string?>();

    foreach (var section in configuration.GetSection("Services").GetChildren())
    {
        appSettings[$"Services/{section.Key}"] = section.Value;
    }

    foreach (var section in configuration.GetSection("Authentication").GetChildren())
    {
        foreach (var child in section.GetChildren())
        {
            appSettings[$"Authentication/{section.Key}/{child.Key}"] = child.Value;
        }
    }

    foreach (var section in configuration.GetSection("Files").GetChildren())
    {
        appSettings[$"Files/{section.Key}"] = section.Value;
    }

    foreach (var section in configuration.GetSection("ConnectionStrings").GetChildren())
    {
        connectionStrings[section.Key] = section.Value;
    }

    // Also check for environment variables that override settings
    foreach (var kvp in appSettings)
    {
        var envKey = kvp.Key.Replace("/", "__");
        var envValue = Environment.GetEnvironmentVariable(envKey);
        if (envValue != null)
            appSettings[kvp.Key] = envValue;
    }

    BookstoreConfiguration.Initialize(
        new Dictionary<string, string?>(appSettings, StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, string?>(connectionStrings, StringComparer.OrdinalIgnoreCase));
}

static void ConfigureLogging(IConfiguration configuration)
{
    var config = new LoggingConfiguration();
    Target loggingTarget;

    if (BookstoreConfiguration.GetSetting("Services/LoggingService") == "aws")
    {
        loggingTarget = new AWSTarget { LogGroup = Bookstore.Common.Constants.AppName };
    }
    else
    {
        loggingTarget = new DebuggerTarget();
    }

    config.AddTarget("logging", loggingTarget);
    config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Info, loggingTarget));

    LogManager.Configuration = config;
}

static string GetReturnUrl(Microsoft.AspNetCore.Http.HttpRequest request)
{
    return $"{request.Scheme}://{request.Host}/signin-oidc";
}

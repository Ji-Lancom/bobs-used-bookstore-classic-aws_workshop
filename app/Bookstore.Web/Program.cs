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
using Bookstore.Web;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NLog.Extensions.Logging;
using Amazon.S3;
using Amazon.Rekognition;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Initialize the legacy BookstoreConfiguration from IConfiguration
// This populates the static configuration store used throughout the Data layer.
BookstoreConfiguration.Initialize(builder.Configuration);

// Optionally pull configuration from AWS SSM Parameter Store at startup
ConfigurationSetup.ConfigureConfiguration();

// Add ASP.NET Core MVC with views
builder.Services.AddControllersWithViews();

// Configure EF Core
var connectionString = BookstoreConfiguration.GetConnectionString("BookstoreDatabaseConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register domain services
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReferenceDataService, ReferenceDataService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IImageResizeService, ImageResizeService>();

// Register repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
builder.Services.AddScoped(typeof(IPaginatedList<>), typeof(PaginatedList<>));

// Register file and image services
if (BookstoreConfiguration.GetSetting("Services/FileService") == "aws")
{
    builder.Services.AddSingleton<IAmazonS3, AmazonS3Client>();
    builder.Services.AddScoped<IFileService, S3FileService>();
}
else
{
    builder.Services.AddSingleton<IFileService>(provider =>
    {
        var env = provider.GetRequiredService<IWebHostEnvironment>();
        return new LocalFileService(env.WebRootPath);
    });
}

if (BookstoreConfiguration.GetSetting("Services/ImageValidationService") == "aws")
{
    builder.Services.AddSingleton<IAmazonRekognition, AmazonRekognitionClient>();
    builder.Services.AddScoped<IImageValidationService, RekognitionImageValidationService>();
}
else
{
    builder.Services.AddScoped<IImageValidationService, LocalImageValidationService>();
}

// Register local authentication middleware
if (BookstoreConfiguration.GetSetting("Services/Authentication") != "aws")
{
    builder.Services.AddScoped<LocalAuthenticationMiddleware>();
}

// Configure authentication
AuthenticationSetup.ConfigureAuthentication(builder.Services);

// Configure NLog logging
builder.Logging.ClearProviders();
builder.Logging.AddNLog(LoggingSetup.CreateNLogConfiguration());

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Local authentication middleware (only in non-AWS auth mode)
if (BookstoreConfiguration.GetSetting("Services/Authentication") != "aws")
{
    app.UseMiddleware<LocalAuthenticationMiddleware>();
}

// Area route must be registered before the default route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

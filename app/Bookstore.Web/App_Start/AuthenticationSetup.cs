using BobsBookstoreClassic.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Bookstore.Domain.Customers;
using Bookstore.Web.Helpers;

namespace Bookstore.Web
{
    public static class AuthenticationSetup
    {
        public static void ConfigureAuthentication(IServiceCollection services)
        {
            if (BookstoreConfiguration.GetSetting("Services/Authentication") == "aws")
            {
                ConfigureCognitoAuthentication(services);
            }
            else
            {
                // Local auth uses a custom middleware; add cookie auth so ClaimsPrincipal persists between requests
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.LoginPath = "/Authentication/Login";
                        options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    });

                services.AddAuthorization(options =>
                {
                    options.AddPolicy("Administrators", policy => policy.RequireRole("Administrators"));
                });
            }
        }

        private static void ConfigureCognitoAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(options =>
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
                options.UsePkce = true;
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
                        var request = context.HttpContext.Request;
                        context.ProtocolMessage.RedirectUri = $"{request.Scheme}://{request.Host}/signin-oidc";
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var service = context.HttpContext.RequestServices.GetRequiredService<ICustomerService>();

                        var identity = context.Principal?.Identity as ClaimsIdentity;
                        if (identity == null) return;

                        var dto = new CreateOrUpdateCustomerDto(
                            identity.GetSub() ?? string.Empty,
                            identity.Name ?? string.Empty,
                            identity.FindFirst(c => c.Type.Contains("givenname"))?.Value ?? string.Empty,
                            identity.FindFirst(c => c.Type.Contains("surname"))?.Value ?? string.Empty);

                        await service.CreateOrUpdateCustomerAsync(dto);
                    }
                };
            });

            services.AddAuthorization();
        }
    }
}

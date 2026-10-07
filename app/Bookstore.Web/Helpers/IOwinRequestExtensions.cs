namespace Bookstore.Web.Helpers
{
    /// <summary>
    /// Extension methods for ASP.NET Core HttpRequest to build redirect URIs.
    /// Replaces the legacy IOwinRequest extension.
    /// </summary>
    public static class HttpRequestExtensions
    {
        public static string GetReturnUrl(this HttpRequest request)
        {
            return $"{request.Scheme}://{request.Host}/signin-oidc";
        }
    }
}

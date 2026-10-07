namespace Bookstore.Web.Helpers
{
    public static class HttpContextExtensions
    {
        private const string CookieKey = "ShoppingCartId";

        public static string GetShoppingCartCorrelationId(this HttpContext context)
        {
            string? shoppingCartClientId = context.Request.Cookies[CookieKey];

            if (string.IsNullOrWhiteSpace(shoppingCartClientId))
            {
                shoppingCartClientId = context.User?.Identity?.IsAuthenticated == true
                    ? context.User.GetSub()
                    : Guid.NewGuid().ToString();
            }

            context.Response.Cookies.Append(CookieKey, shoppingCartClientId ?? Guid.NewGuid().ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddYears(1),
                Path = "/",
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

            return shoppingCartClientId ?? Guid.NewGuid().ToString();
        }
    }
}

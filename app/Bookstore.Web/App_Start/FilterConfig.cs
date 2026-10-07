using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(MvcOptions filters)
        {
            filters.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        }
    }
}

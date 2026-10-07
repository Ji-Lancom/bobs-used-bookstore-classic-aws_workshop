using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Web.Areas.Admin.Controllers
{
    public class ErrorController : AdminAreaControllerBase
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

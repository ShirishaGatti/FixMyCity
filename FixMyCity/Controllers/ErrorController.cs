using System.Web.Mvc;

namespace FixMyCity.Controllers
{
    // Referenced directly by Web.config:
    //   <customErrors defaultRedirect="~/Error/General"> / <error statusCode="404" redirect="~/Error/NotFound"/>
    //   <httpErrors>   path="/Error/General" / path="/Error/NotFound"
    // Both entry points must exist and must NOT themselves require auth or
    // throw — this is the last line of defense, it has nowhere further to
    // redirect to if it fails.
    public class ErrorController : Controller
    {
        [HttpGet]
        public ActionResult General()
        {
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true; // avoid IIS re-wrapping our own error view
            // Reuses the scaffolded Views/Shared/Error.cshtml you already have —
            // no new view needed. Swap for a dedicated Views/Error/General.cshtml later if you want custom styling.
            return View("Error");
        }

        [HttpGet]
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            return View("Error");
        }
    }
}
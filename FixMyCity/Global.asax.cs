using FixMyCity.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace FixMyCity
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            //GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configure(App_Start.WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AntiForgeryConfig.UniqueClaimTypeIdentifier = "ConsumerId";
        }
        protected void Application_PostAuthorizeRequest()
        {
            if (HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath
                    .StartsWith("~/api/", StringComparison.OrdinalIgnoreCase))
            {
                HttpContext.Current.SetSessionStateBehavior(
                    System.Web.SessionState.SessionStateBehavior.Required);
            }
        }
        protected void Application_EndRequest()
        {
            var path = Request.Path;
            if (Response.StatusCode == 302 &&
                path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                var location = Response.Headers["Location"] ?? "";
                if (location.IndexOf("/Account/Login", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Response.ClearContent();
                    Response.StatusCode = 401;
                    Response.Headers.Remove("Location");
                    Response.ContentType = "application/json";
                    Response.Write("{\"success\":false,\"message\":\"Session expired. Please log in.\"}");
                }
            }
        }
    }
}

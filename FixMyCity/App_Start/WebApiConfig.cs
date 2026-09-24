using System.Web.Http;
using System.Web.Http.Cors;

namespace FixMyCity.App_Start
{
    public class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Enable CORS
            var cors = new EnableCorsAttribute(
                "http://localhost:4200",
                "*",
                "*"
            );

            config.EnableCors(cors);

            // Attribute routing
            config.MapHttpAttributeRoutes();

            // Conventional routing
            config.Routes.MapHttpRoute(
                name: "DefaultActionApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new
                {
                    id = RouteParameter.Optional
                }
            );
        }
    }
}
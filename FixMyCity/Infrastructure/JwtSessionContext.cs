using System.Security.Claims;
using System.Web;
namespace FixMyCity.Infrastructure
{
    // Abstraction over "who is the current user" so controllers/services
    // never touch Session/Cookies/Claims directly — same role this played
    // in FoodOrderingSystem (there was even a Session-based version before
    // the JWT migration; keeping the interface stable let the swap happen
    // without touching every controller).
    public interface ISessionContext
    {
        int ConsumerId { get; }
        int RoleId { get; }
        string Email { get; }
        bool IsLoggedIn { get; }
    }


        public class JwtSessionContext : ISessionContext
        {
            // Read HttpContext.Current.User lazily instead of re-parsing the
            // raw cookie in the constructor. RoleAuthorizeAttribute runs
            // *before* the action (and after this controller is constructed)
            // and may silently rotate an expired access token — re-deriving
            // from the original cookie here would miss that rotation and
            // read a stale/expired token for the rest of the request.
            private ClaimsPrincipal Principal => HttpContext.Current?.User as ClaimsPrincipal;

            public int ConsumerId
            {
                get
                {
                    var claim = Principal?.FindFirst("ConsumerId");
                    return claim != null ? int.Parse(claim.Value) : 0;
                }
            }

            public int RoleId
            {
                get
                {
                    var claim = Principal?.FindFirst("RoleId");
                    return claim != null ? int.Parse(claim.Value) : 0;
                }
            }

            public string Email
            {
                get
                {
                    var claim = Principal?.FindFirst("Email");
                    return claim?.Value ?? "";
                }
            }

            public bool IsLoggedIn => ConsumerId > 0;
        }
    }

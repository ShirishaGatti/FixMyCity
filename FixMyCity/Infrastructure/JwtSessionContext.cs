using System.Security.Claims;

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

    // Reads identity off the validated JWT (set onto HttpContext.User by
    // RoleAuthorizeAttribute) instead of Session — this is what makes the
    // "rotating JWT" auth stateless across web-farm nodes.
    public class JwtSessionContext : ISessionContext
    {
        private readonly ClaimsPrincipal _principal;

        public JwtSessionContext()
        {
            var token = JwtHelper.GetTokenFromRequest();
            _principal = token != null ? JwtHelper.ValidateToken(token) : null;
        }

        public int ConsumerId
        {
            get
            {
                var claim = _principal?.FindFirst("ConsumerId");
                return claim != null ? int.Parse(claim.Value) : 0;
            }
        }

        public int RoleId
        {
            get
            {
                var claim = _principal?.FindFirst("RoleId");
                return claim != null ? int.Parse(claim.Value) : 0;
            }
        }

        public string Email
        {
            get
            {
                var claim = _principal?.FindFirst("Email");
                return claim?.Value ?? "";
            }
        }

        public bool IsLoggedIn => ConsumerId > 0;
    }
}
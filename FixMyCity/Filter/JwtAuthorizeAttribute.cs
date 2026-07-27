using System;
using System.Linq;
using System.Web.Mvc;
using ComplaintSystem.Security;

namespace ComplaintSystem.Filters
{
    // System.Web.Mvc.IAuthorizationFilter (NOT the ASP.NET Core one - different interface signature)
    // This runs BEFORE the action executes, in the authorization stage of the filter pipeline.
    public class JwtAuthorizeAttribute : FilterAttribute, IAuthorizationFilter
    {
        // Usage: [JwtAuthorize] -> any authenticated user
        //        [JwtAuthorize(Roles = "Admin")] -> Admin only
        //        [JwtAuthorize(Roles = "Admin,SupportExecutive")] -> either role
        public string Roles { get; set; }

        public void OnAuthorization(AuthorizationContext filterContext)
        {
            // 1. Extract token - here from a cookie; could also read from Authorization header
            //    if this were a pure API consumed by JS/mobile clients.
            var tokenCookie = filterContext.HttpContext.Request.Cookies["auth_token"];

            if (tokenCookie == null || string.IsNullOrWhiteSpace(tokenCookie.Value))
            {
                DenyAccess(filterContext, isUnauthenticated: true);
                return;
            }

            var claims = JwtTokenHelper.ValidateToken(tokenCookie.Value);

            if (claims == null)
            {
                // token missing, tampered, or expired
                DenyAccess(filterContext, isUnauthenticated: true);
                return;
            }

            // 2. Stash claims on HttpContext.Items so controllers/actions can read
            //    the current user without re-parsing the token (like HttpContext.User in Core)
            filterContext.HttpContext.Items["UserId"] = Convert.ToInt32(claims["sub"]);
            filterContext.HttpContext.Items["Email"] = claims["email"].ToString();
            filterContext.HttpContext.Items["Role"] = claims["role"].ToString();

            // 3. If no specific roles required, being authenticated is enough
            if (string.IsNullOrWhiteSpace(Roles))
            {
                return;
            }

            // 4. RBAC check - does the user's role match any of the allowed roles?
            string currentRole = claims["role"].ToString();
            var allowedRoles = Roles.Split(',').Select(r => r.Trim());

            if (!allowedRoles.Contains(currentRole, StringComparer.OrdinalIgnoreCase))
            {
                DenyAccess(filterContext, isUnauthenticated: false);
            }
        }

        private void DenyAccess(AuthorizationContext filterContext, bool isUnauthenticated)
        {
            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.Result = new HttpStatusCodeResult(
                    isUnauthenticated ? 401 : 403,
                    isUnauthenticated ? "Unauthorized" : "Forbidden");
            }
            else if (isUnauthenticated)
            {
                filterContext.Result = new RedirectResult("~/Account/Login");
            }
            else
            {
                filterContext.Result = new ViewResult { ViewName = "~/Views/Shared/AccessDenied.cshtml" };
            }
        }
    }
}

using FixMyCity.Infrastructure;
using FixMyCity.Service;
using System;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace FixMyCity.Filters
{
    // RBAC gate for MVC actions/controllers.
    //
    // Usage:
    //   [RoleAuthorize]                                   -> any logged-in Consumer
    //   [RoleAuthorize(RoleIds.Admin)]                     -> Admin only
    //   [RoleAuthorize(RoleIds.SupportExecutive, RoleIds.Admin)]  -> either role
    //
    // Also performs the silent-refresh dance: if the short-lived access
    // token has expired but a valid, non-revoked refresh token cookie is
    // present, it transparently rotates both tokens and lets the request
    // proceed — the caller never sees a 401 unless the refresh token is
    // itself invalid/expired/revoked.
    public class RoleAuthorizeAttribute : FilterAttribute, IAuthorizationFilter
    {
        private readonly int[] _allowedRoles;

        public RoleAuthorizeAttribute(params int[] allowedRoles)
        {
            _allowedRoles = allowedRoles ?? new int[0];
        }

        public void OnAuthorization(AuthorizationContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var response = filterContext.HttpContext.Response;

            // ── 1. Try the access token cookie ──────────────────────────
            var accessCookie = request.Cookies["jwt_token"];
            ClaimsPrincipal principal = null;

            if (accessCookie != null && !string.IsNullOrEmpty(accessCookie.Value))
                principal = JwtHelper.ValidateToken(accessCookie.Value);

            // ── 2. Access token missing/expired — try silent refresh ───
            if (principal == null)
            {
                var refreshCookie = request.Cookies["refresh_token"];

                if (refreshCookie != null && !string.IsNullOrEmpty(refreshCookie.Value))
                {
                    var authService = new AuthService();
                    var tokens = authService.TryRefresh(refreshCookie.Value);

                    if (tokens != null)
                    {
                        response.Cookies.Add(new HttpCookie("jwt_token", tokens.AccessToken)
                        {
                            HttpOnly = true,
                            Secure = request.IsSecureConnection,
                            Expires = DateTime.UtcNow.AddMinutes(JwtHelper.GetAccessExpiryMinutes())
                        });
                        response.Cookies.Add(new HttpCookie("refresh_token", tokens.RefreshToken)
                        {
                            HttpOnly = true,
                            Secure = request.IsSecureConnection,
                            Expires = tokens.TrustExpiresAt
                        });

                        principal = JwtHelper.ValidateToken(tokens.AccessToken);
                    }
                }
            }

            // ── 3. Both failed → not authenticated ──────────────────────
            if (principal == null)
            {
                filterContext.Result = Unauthorized(request);
                return;
            }

            // ── 4. Role check ────────────────────────────────────────────
            int roleId = int.Parse(principal.FindFirst("RoleId").Value);
            if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(roleId))
            {
                filterContext.Result = Forbidden(request);
                return;
            }

            filterContext.HttpContext.User = principal;
            System.Web.HttpContext.Current.User = principal;
            System.Threading.Thread.CurrentPrincipal = principal;
        }

        private ActionResult Unauthorized(HttpRequestBase request)
        {
            return request.IsAjaxRequest()
                ? (ActionResult)new JsonResult
                {
                    Data = new
                    {
                        success = false,
                        message = "Session expired. Please log in.",
                        redirectUrl = "/Account/Login"
                    },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                }
                : new RedirectResult("/Account/Login");
        }

        private ActionResult Forbidden(HttpRequestBase request)
        {
            return request.IsAjaxRequest()
                ? (ActionResult)new JsonResult
                {
                    Data = new { success = false, message = "You don't have permission to perform this action." },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                }
                : new HttpStatusCodeResult(403, "Forbidden");
        }
    }
}
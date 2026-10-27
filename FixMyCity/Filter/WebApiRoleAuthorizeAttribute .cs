using FixMyCity.Infrastructure;
using FixMyCity.Service;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace FixMyCity.Filters
{
    public class WebApiRoleAuthorizeAttribute
        : AuthorizationFilterAttribute
    {
        private readonly int[] _allowedRoles;

        public WebApiRoleAuthorizeAttribute(params int[] allowedRoles)
        {
            _allowedRoles = allowedRoles ?? new int[0];
        }

        public override void OnAuthorization(
            HttpActionContext actionContext)
        {
            var context = HttpContext.Current;

            if (context == null)
            {
                Reject(actionContext, HttpStatusCode.Unauthorized,
                    "Authentication required.");
                return;
            }

            var request = context.Request;
            var principal = ValidateAccessToken(
                request.Cookies["jwt_token"]?.Value);

            // Attempt silent refresh when the access token is invalid.
            if (principal == null)
            {
                var refreshToken =
                    request.Cookies["refresh_token"]?.Value;

                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    var authService = new AuthService();
                    var tokens = authService.TryRefresh(refreshToken);

                    if (tokens != null)
                    {
                        context.Response.Cookies.Add(
                            new HttpCookie("jwt_token", tokens.AccessToken)
                            {
                                HttpOnly = true,
                                Secure = request.IsSecureConnection,
                                Expires = DateTime.UtcNow.AddMinutes(
                                    JwtHelper.GetAccessExpiryMinutes())
                            });

                        context.Response.Cookies.Add(
                            new HttpCookie("refresh_token",
                                tokens.RefreshToken)
                            {
                                HttpOnly = true,
                                Secure = request.IsSecureConnection,
                                Expires = tokens.TrustExpiresAt
                            });

                        principal = ValidateAccessToken(tokens.AccessToken);
                    }
                }
            }

            // Reject requests without a valid access token.
            if (principal == null)
            {
                Reject(actionContext, HttpStatusCode.Unauthorized,
                    "Session expired. Please log in.");
                return;
            }

            // Extract the authenticated user's role safely.
            var roleClaim = principal.FindFirst("RoleId");
            int roleId;

            if (roleClaim == null ||
                !int.TryParse(roleClaim.Value, out roleId))
            {
                Reject(actionContext, HttpStatusCode.Unauthorized,
                    "Invalid authentication claims.");
                return;
            }

            // Reject authenticated users with the wrong role.
            if (_allowedRoles.Length > 0 &&
                !_allowedRoles.Contains(roleId))
            {
                Reject(actionContext, HttpStatusCode.Forbidden,
                    "You don't have permission to access this resource.");
                return;
            }

            // Make the authenticated principal available to Web API.
            actionContext.RequestContext.Principal = principal;
            context.User = principal;
            Thread.CurrentPrincipal = principal;
        }

        private static ClaimsPrincipal ValidateAccessToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            try
            {
                return JwtHelper.ValidateToken(token);
            }
            catch
            {
                return null;
            }
        }


        private static void Reject(
            HttpActionContext actionContext,
            HttpStatusCode statusCode,
            string message)
        {
            var context = HttpContext.Current;

            if (context != null)
            {
                context.Response.SuppressFormsAuthenticationRedirect = true;
                context.Response.TrySkipIisCustomErrors = true;
            }

            actionContext.Response =
                actionContext.Request.CreateResponse(
                    statusCode,
                    new
                    {
                        success = false,
                        message = message,
                        redirectUrl = "/login"
                    });
        }

    }
}
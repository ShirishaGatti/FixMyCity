
using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
using FixMyCity.Model;
using FixMyCity.Service;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace FixMyCity.Controllers
{
    [RoutePrefix("api/auth")]
    [EnableCors(
        origins: "http://localhost:4200",
        headers: "content-type,authorization",
        methods: "GET,POST",
        SupportsCredentials = true)]
    public class AuthApiController : ApiController
    {
        private readonly IAuthService _authService;
        private readonly IMailService _mailService;

        public AuthApiController()
            : this(new AuthService(), new MailService())
        {
        }

        public AuthApiController(
            IAuthService authService,
            IMailService mailService)
        {
            _authService = authService;
            _mailService = mailService;
        }

        private HttpContext Context
        {
            get { return HttpContext.Current; }
        }

        // GET api/auth/cities
        [HttpGet]
        [Route("cities")]
        public IHttpActionResult GetCities()
        {
            try
            {
                return Ok(_authService.GetCities());
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // GET api/auth/wards/5
        [HttpGet]
        [Route("wards/{cityId:int}")]
        public IHttpActionResult GetWards(int cityId)
        {
            if (cityId <= 0)
                return BadRequest("Invalid city ID.");

            try
            {
                var wards = _authService.GetWardsByCity(cityId)
                    .Select(w => new
                    {
                        id = w.WardId,
                        name = w.WardName
                    });

                return Ok(wards);
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // POST api/auth/register
        [HttpPost]
        [Route("register")]
        public IHttpActionResult Register(RegisterViewModel vm)
        {
            if (vm == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                _authService.Register(vm);

                return Ok(new
                {
                    success = true,
                    message = "Registration successful. Please log in."
                });
            }
            catch (BusinessException ex)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    message = ex.Message,
                    code = ex.ErrorCode
                });
            }
            catch (DataAccessException)
            {
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    message = "Unable to register at this time."
                });
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }


        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(LoginViewModel vm)
        {
            if (vm == null || !ModelState.IsValid)
                return BadRequest("Please enter your email and password.");

            try
            {
                // Validate credentials.
                LoginViewModel result = _authService.Login(vm);

                // Persist the OTP challenge in the database.
                LoginChallengeResult challenge =
                    _authService.CreateLoginChallenge(
                        result.ConsumerId,
                        vm.RememberMe);

                // Send OTP only after the challenge is persisted.
                _mailService.SendOtpEmail(vm.Email, challenge.Otp);

                return Ok(new
                {
                    success = true,
                    requiresOtp = true,
                    pendingToken = challenge.PendingToken,
                    message = "Verification code sent to your email."
                });
            }
            catch (BusinessException ex)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    message = ex.Message,
                    code = ex.ErrorCode
                });
            }
            catch (DataAccessException)
            {
                // Log the actual exception internally.
                return Content(
                    HttpStatusCode.InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unable to process login right now."
                    });
            }
            catch (Exception)
            {
                // Log the actual exception internally.
                return Content(
                    HttpStatusCode.InternalServerError,
                    new
                    {
                        success = false,
                        message = "An unexpected error occurred."
                    });
            }
        }


        [HttpPost]
        [Route("verify-otp")]
        public IHttpActionResult VerifyOtp(VerifyOtpRequest req)
        {
            if (req == null || !ModelState.IsValid)
                return BadRequest("Please enter the verification code.");

            try
            {
                // Verify the OTP using the database-backed challenge.
                // The challenge must be consumed atomically on success.
                LoginChallenge challenge =
                    _authService.VerifyLoginChallenge(
                        req.PendingToken,
                        req.EnteredOtp);

                // Reload trusted user details from the database.
                Consumer consumer =
                    _authService.GetConsumerById(challenge.ConsumerId);

                if (consumer == null)
                {
                    return Content(HttpStatusCode.Unauthorized, new
                    {
                        success = false,
                        message = "Unable to complete login."
                    });
                }

                // Preserve your existing token-issuing implementation.
                TokenPair tokens = _authService.IssueTokenPair(
                    consumer.ConsumerId,
                    consumer.RoleId,
                    consumer.Email,
                    challenge.RememberMe);

                // Preserve your existing secure cookie implementation.
                WriteTokenCookies(tokens);

                string redirectUrl;

                switch (consumer.RoleId)
                {
                    case RoleIds.Citizen:
                        redirectUrl = "/dashboard";
                        break;

                    case RoleIds.SupportExecutive:
                        //redirectUrl = "/officer/complaints";
                        redirectUrl = "/complaints";
                        break;

                    case RoleIds.Admin:
                        redirectUrl = "/admin/dashboard";
                        break;

                    default:
                        redirectUrl = "/login";
                        break;
                }

                return Ok(new
                {
                    success = true,
                    message = "Login successful.",
                    roleId = consumer.RoleId,
                    redirectUrl = redirectUrl
                });
            }
            catch (BusinessException ex)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    message = ex.Message,
                    code = ex.ErrorCode
                });
            }
            catch (DataAccessException)
            {
                return Content(
                    HttpStatusCode.InternalServerError,
                    new
                    {
                        success = false,
                        message = "Unable to verify the code right now."
                    });
            }
            catch (Exception)
            {
                // Log the actual exception internally.
                return Content(
                    HttpStatusCode.InternalServerError,
                    new
                    {
                        success = false,
                        message = "An unexpected error occurred."
                    });
            }
        }


        // POST api/auth/refresh
        [HttpPost]
        [Route("refresh")]
        public IHttpActionResult Refresh()
        {
            var cookie = Context.Request.Cookies["refresh_token"];
            string rawToken = cookie == null ? null : cookie.Value;

            try
            {
                TokenPair tokens = _authService.TryRefresh(rawToken);

                if (tokens == null)
                {
                    ClearTokenCookies();

                    return Content(HttpStatusCode.Unauthorized, new
                    {
                        success = false,
                        message = "Session expired. Please log in again."
                    });
                }

                WriteTokenCookies(tokens);

                return Ok(new { success = true });
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // POST api/auth/logout
        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout()
        {
            var cookie = Context.Request.Cookies["refresh_token"];
            string rawToken = cookie == null ? null : cookie.Value;

            try
            {
                if (!string.IsNullOrEmpty(rawToken))
                    _authService.RevokeByRawRefreshToken(rawToken);

                ClearTokenCookies();

                return Ok(new
                {
                    success = true,
                    message = "Logged out successfully."
                });
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // POST api/auth/forgot-password
        [HttpPost]
        [Route("forgot-password")]
        public IHttpActionResult ForgotPassword(
            ForgotPasswordViewModel vm)
        {
            if (vm == null || !ModelState.IsValid)
                return BadRequest("Please enter a valid email.");

            try
            {
                int? consumerId =
                    _authService.GetConsumerIdByEmail(vm.Email);

                if (consumerId.HasValue)
                {
                    string otp = _authService.CreateOtp(
                        consumerId.Value, "PASSWORD_RESET");

                    _mailService.SendOtpEmail(vm.Email, otp);
                }

                Context.Session["ResetEmail"] = vm.Email;

                return Ok(new
                {
                    success = true,
                    message = "If that email is registered, a reset code has been sent."
                });
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        // POST api/auth/reset-password
        [HttpPost]
        [Route("reset-password")]
        public IHttpActionResult ResetPassword(
            ResetPasswordViewModel vm)
        {
            if (vm == null || !ModelState.IsValid)
                return BadRequest(ModelState);

            string email = Context.Session["ResetEmail"] as string;

            if (string.IsNullOrEmpty(email))
                return Content(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    message = "Session expired. Please request a new code."
                });

            try
            {
                int? consumerId = _authService.GetConsumerIdByEmail(email);

                if (!consumerId.HasValue)
                {
                    Context.Session.Remove("ResetEmail");
                    return BadRequest("Session expired. Please request a new code.");
                }

                _authService.ValidateOtp(
                    consumerId.Value, vm.EnteredOtp, "PASSWORD_RESET");

                _authService.ResetPassword(
                    consumerId.Value, vm.NewPassword, vm.ConfirmPassword);

                Context.Session.Remove("ResetEmail");

                return Ok(new
                {
                    success = true,
                    message = "Password reset successfully."
                });
            }
            catch (BusinessException ex)
            {
                return Content(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    message = ex.Message,
                    code = ex.ErrorCode
                });
            }
            catch (Exception)
            {
                return InternalServerError();
            }
        }

        private void WriteTokenCookies(TokenPair tokens)
        {
            var request = Context.Request;
            var response = Context.Response;

            var accessCookie = new HttpCookie(
                "jwt_token", tokens.AccessToken)
            {
                HttpOnly = true,
                Secure = request.IsSecureConnection,
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(
                    JwtHelper.GetAccessExpiryMinutes())
            };

            var refreshCookie = new HttpCookie(
                "refresh_token", tokens.RefreshToken)
            {
                HttpOnly = true,
                Secure = request.IsSecureConnection,
                Path = "/",
                SameSite = SameSiteMode.Lax,
                Expires = tokens.TrustExpiresAt
            };

            response.Cookies.Add(accessCookie);
            response.Cookies.Add(refreshCookie);
        }

        private void ClearTokenCookies()
        {
            var response = Context.Response;

            response.Cookies.Add(new HttpCookie("jwt_token", "")
            {
                HttpOnly = true,
                Secure = Context.Request.IsSecureConnection,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            });

            response.Cookies.Add(new HttpCookie("refresh_token", "")
            {
                HttpOnly = true,
                Secure = Context.Request.IsSecureConnection,
                Path = "/",
                Expires = DateTime.UtcNow.AddDays(-1)
            });
        }
    }
}

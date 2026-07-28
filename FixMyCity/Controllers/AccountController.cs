
using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
using FixMyCity.Model;
using FixMyCity.Models;
using FixMyCity.Service;
using FixMyCityModel;
using FixMyCityModel.ViewModel;
using System;
using System.Web;
using System.Web.Mvc;

namespace FixMyCity.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly FixMyCity.Repository.IAuthRepository _authRepo;
        private readonly IMailService _emailService;

        public AccountController() : this(new AuthService(), new FixMyCity.Repository.AuthRepository(), new MailService()) { }
        public AccountController(IAuthService authService, FixMyCity.Repository.IAuthRepository authRepo, IMailService emailService)
        {
            _authService = authService;
            _authRepo = authRepo;
            _emailService = emailService;
        }

        /* private void PopulateCitiesAndWards(int? selectedCityId = null, int? selectedWardId = null)
         {
             var cities = _authRepo.GetCities();
             ViewBag.Cities = new SelectList(cities, "CityId", "CityName", selectedCityId);

             int cityId = selectedCityId ?? (cities.Count > 0 ? cities[0].CityId : 1);
             var wards = _authRepo.GetWardsByCity(cityId);
             ViewBag.Wards = new SelectList(wards, "WardId", "WardName", selectedWardId);
         }*/
        private void PopulateCitiesAndWards(RegisterViewModel vm)
        {
            var cities = _authRepo.GetCities();
            vm.Cities = new SelectList(cities, "CityId", "CityName", vm.CityId);

            int cityId = vm.CityId ?? (cities.Count > 0 ? cities[0].CityId : 1);
            vm.Wards = new SelectList(_authRepo.GetWardsByCity(cityId), "WardId", "WardName", vm.WardId);
        }

        // ===========================
        // Register
        // ===========================
        [HttpGet]
        public ActionResult Register()
        {
            PopulateCitiesAndWards();
            return View(new RegisterViewModel());
        }

        [HttpGet]
        public JsonResult GetWards(int cityId)
        {
            var wards = _authRepo.GetWardsByCity(cityId);
            var result = System.Linq.Enumerable.Select(wards, w => new { id = w.WardId, name = w.WardName });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Please correct the highlighted fields." });
                PopulateCitiesAndWards(vm.CityId, vm.WardId);
                return View(vm);
            }

            try
            {
                _authService.Register(vm);
                if (Request.IsAjaxRequest())
                    return Json(new { success = true, message = "Registration successful! Please log in to continue.", redirectUrl = Url.Action("Login", "Account") });

                TempData["SuccessMessage"] = "Registration successful. Please log in.";
                return RedirectToAction("Login", "Account");
            }
            catch (BusinessException ex)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = ex.Message, code = ex.ErrorCode });
                ModelState.AddModelError("", ex.Message);
                PopulateCitiesAndWards(vm.CityId, vm.WardId);
                return View(vm);
            }
            catch (DataAccessException ex)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = ex.Message });
                ModelState.AddModelError("", ex.Message);
                PopulateCitiesAndWards(vm.CityId, vm.WardId);
                return View(vm);
            }
            catch (Exception)
            {
                string msg = "An unexpected error occurred during registration. Please check your details and try again.";
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = msg });
                ModelState.AddModelError("", msg);
                PopulateCitiesAndWards(vm.CityId, vm.WardId);
                return View(vm);
            }
        }

        // ===========================
        // Login (step 1: password)
        // ===========================
        [HttpGet]
        public ActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "Please enter your email and password." });
                return View(vm);
            }

            try
            {
                LoginViewModel result = _authService.Login(vm); // throws BusinessException on bad credentials

                string otp = _authService.CreateOtp(result.ConsumerId, "LOGIN");
                _emailService.SendOtpEmail(vm.Email, otp);

                Session["PendingConsumerId"] = result.ConsumerId;
                Session["PendingRoleId"] = result.RoleId;
                Session["PendingEmail"] = vm.Email;
                Session["PendingRememberMe"] = vm.RememberMe;

                string redirectUrl = Url.Action("VerifyOtp", "Account");
                if (Request.IsAjaxRequest())
                    return Json(new { success = true, redirectUrl = redirectUrl });

                return RedirectToAction("VerifyOtp", "Account");
            }
            catch (BusinessException ex)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = ex.Message, code = ex.ErrorCode });
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
            catch (DataAccessException ex)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = ex.Message });
                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
            catch (Exception ex)
            {
                string msg = "Invalid email or password. Please try again.";
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = msg });
                ModelState.AddModelError("", msg);
                return View(vm);
            }
        }

        // ===========================
        // Login (step 2: OTP)
        // ===========================
        [HttpGet]
        public ActionResult VerifyOtp()
        {
            if (Session["PendingConsumerId"] == null)
                return RedirectToAction("Login");

            return View(new VerifyOtpViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyOtp(VerifyOtpViewModel vm)
        {
            int consumerId = Session["PendingConsumerId"] as int? ?? 0;

            try
            {
                _authService.ValidateOtp(consumerId, vm.EnteredOtp, "LOGIN");
            }
            catch (BusinessException ex)
            {
                return Json(new { success = false, message = ex.Message, code = ex.ErrorCode });
            }

            int roleId = Session["PendingRoleId"] as int? ?? 0;
            string email = Session["PendingEmail"] as string;
            bool rememberMe = Session["PendingRememberMe"] as bool? ?? false;

            TokenPair tokens = _authService.IssueTokenPair(consumerId, roleId, email, rememberMe);
            WriteTokenCookies(tokens);

            Session.Remove("PendingConsumerId");
            Session.Remove("PendingRoleId");
            Session.Remove("PendingEmail");
            Session.Remove("PendingRememberMe");

            string redirectUrl;
            switch (roleId)
            {
                case RoleIds.Citizen: redirectUrl = Url.Action("MyComplaints", "Complaint"); break;
                case RoleIds.SupportExecutive: redirectUrl = Url.Action("Queue", "Complaint"); break;
                case RoleIds.Admin: redirectUrl = Url.Action("Dashboard", "Admin"); break;
                default: redirectUrl = Url.Action("Login", "Account"); break;
            }

            return Json(new { success = true, redirectUrl });
        }

        // ── Called silently by RoleAuthorizeAttribute; also exposed here
        //    in case the front end wants to proactively refresh. ──────────
        [HttpPost]
        public ActionResult Refresh()
        {
            var rawRefreshToken = Request.Cookies["refresh_token"]?.Value;
            var tokens = _authService.TryRefresh(rawRefreshToken);

            if (tokens == null)
            {
                ClearTokenCookies();
                return Json(new { success = false, redirectUrl = Url.Action("Login", "Account") });
            }

            WriteTokenCookies(tokens);
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            var rawRefreshToken = Request.Cookies["refresh_token"]?.Value;
            if (!string.IsNullOrEmpty(rawRefreshToken))
                _authService.RevokeByRawRefreshToken(rawRefreshToken);

            ClearTokenCookies();
            return RedirectToAction("Login", "Account");
        }

        // ===========================
        // Forgot / reset password
        // ===========================
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ForgotPassword(ForgotPasswordViewModel vm)
        {
            // Always return the same generic response whether or not the
            // email exists — don't let this endpoint be used to enumerate
            // registered accounts.
            int? consumerId = _authService.GetConsumerIdByEmail(vm.Email);
            if (consumerId.HasValue)
            {
                string otp = _authService.CreateOtp(consumerId.Value, "PASSWORD_RESET");
                // new MailService().SendOtpEmail(vm.Email, otp);   // wire up your mail service here
            }

            Session["ResetEmail"] = vm.Email;

            return Json(new
            {
                success = true,
                message = "If that email is registered, a reset code has been sent.",
                redirectUrl = Url.Action("ResetPassword", "Account")
            });
        }

        [HttpGet]
        public ActionResult ResetPassword()
        {
            if (Session["ResetEmail"] == null)
                return RedirectToAction("ForgotPassword");

            return View(new ResetPasswordViewModel { Email = Session["ResetEmail"].ToString() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordViewModel vm)
        {
            try
            {
                var email = Session["ResetEmail"] as string;
                int? consumerId = _authService.GetConsumerIdByEmail(email);

                if (!consumerId.HasValue)
                    throw new BusinessException("Session expired. Please request a new code.", "SESSION_EXPIRED");

                _authService.ValidateOtp(consumerId.Value, vm.EnteredOtp, "PASSWORD_RESET");
                _authService.ResetPassword(consumerId.Value, vm.NewPassword, vm.ConfirmPassword);

                Session.Remove("ResetEmail");

                return Json(new
                {
                    success = true,
                    message = "Password reset successfully.",
                    redirectUrl = Url.Action("Login", "Account")
                });
            }
            catch (BusinessException ex)
            {
                if (ex.ErrorCode == "SESSION_EXPIRED")
                    Session.Remove("ResetEmail");

                return Json(new { success = false, message = ex.Message, errorCode = ex.ErrorCode });
            }
        }

        // ===========================
        // Cookie helpers
        // ===========================
        private void WriteTokenCookies(TokenPair tokens)
        {
            Response.Cookies.Add(new HttpCookie("jwt_token", tokens.AccessToken)
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                Expires = DateTime.UtcNow.AddMinutes(Infrastructure.JwtHelper.GetAccessExpiryMinutes())
            });

            Response.Cookies.Add(new HttpCookie("refresh_token", tokens.RefreshToken)
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection,
                Expires = tokens.TrustExpiresAt
            });
        }

        private void ClearTokenCookies()
        {
            Response.Cookies.Add(new HttpCookie("jwt_token", "") { HttpOnly = true, Expires = DateTime.UtcNow.AddDays(-1) });
            Response.Cookies.Add(new HttpCookie("refresh_token", "") { HttpOnly = true, Expires = DateTime.UtcNow.AddDays(-1) });
        }
    }
}
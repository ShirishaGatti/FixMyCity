using System;
using System.Web;
using System.Web.Mvc;
using ComplaintSystem.Services;
using ComplaintSystem.ViewModels;
using ComplaintSystem.Filters;

namespace ComplaintSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        // In real project, inject via Unity/constructor DI instead of manual instantiation
        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = _authService.Register(model);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = _authService.Login(model);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            // Store JWT in an httpOnly cookie - JS can't read it, mitigates XSS token theft
            var cookie = new HttpCookie("auth_token", result.Token)
            {
                HttpOnly = true,
                Secure = Request.IsSecureConnection, // true once you're on HTTPS
                Expires = DateTime.UtcNow.AddMinutes(60)
            };
            Response.Cookies.Add(cookie);

            // Redirect based on role
            switch (result.Role)
            {
                case "Admin": return RedirectToAction("Index", "AdminDashboard");
                case "SupportExecutive": return RedirectToAction("Index", "OfficerDashboard");
                default: return RedirectToAction("Index", "ComplaintDashboard");
            }
        }

        [JwtAuthorize] // any authenticated user can logout
        public ActionResult Logout()
        {
            Response.Cookies.Add(new HttpCookie("auth_token", "") { Expires = DateTime.UtcNow.AddDays(-1) });
            return RedirectToAction("Login");
        }
    }
}

// Usage examples elsewhere in the app:
//
// [JwtAuthorize(Roles = "Admin")]
// public class UserManagementController : Controller { ... }
//
// [JwtAuthorize(Roles = "Admin,SupportExecutive")]
// public ActionResult UpdateStatus(int complaintId, int statusId) { ... }
//
// [JwtAuthorize] // Employee, SupportExecutive, or Admin - just needs to be logged in
// public ActionResult MyComplaints() { ... }
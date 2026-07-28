using FixMyCity.Filters;
using FixMyCity.Infrastructure;
using FixMyCity.Service;
using FixMyCity.ViewModel;
using System.Web.Mvc;

namespace FixMyCity.Controllers
{
    // Everything here requires an authenticated Admin. RoleAuthorizeAttribute
    // (Filter/RoleAuthorizeAttribute.cs) handles the silent-refresh + 401/403
    // logic the same way it does for every other protected controller.
    [RoleAuthorize(RoleIds.Admin)]
    public class AdminController : Controller
    {
        private readonly IAuthService _authService;

        public AdminController() : this(new AuthService()) { }
        public AdminController(IAuthService authService)
        {
            _authService = authService;
        }

        // Admin's own landing page — referenced by AccountController.VerifyOtp's
        // role-based redirect switch. Replace with your real dashboard view.
        public ActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public ActionResult CreateStaff()
        {
            return View(new StaffRegisterViewModel { RoleId = RoleIds.SupportExecutive });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStaff(StaffRegisterViewModel vm)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Please correct the highlighted fields." });

            // Throws BusinessException on bad input — GlobalMvcExceptionFilter
            // turns that into { success:false, message } for this ajax call,
            // exactly like AccountController.Register does. This only works
            // once GlobalMvcExceptionFilter is actually registered in
            // FilterConfig.cs — see the fixed version of that file.
            _authService.RegisterStaff(vm);
            return Json(new { success = true, message = "Staff account created." });
        }
    }
}
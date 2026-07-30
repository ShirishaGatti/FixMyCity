using FixMyCity.Exceptions;
using FixMyCity.Filters;
using FixMyCity.Infrastructure;
using FixMyCity.service;
using FixMyCity.Service;
using FixMyCityModel.ViewModel;
using System.Web.Mvc;

namespace FixMyCity.Controllers
{
    // Admin console — dashboard, master data, users, complaints, officers.
    // RoleAuthorize does the silent-refresh + 401/403 dance identically to
    // every other protected controller. AuthService kept only for the
    // existing CreateStaff endpoint; new work goes through AdminService.
    [RoleAuthorize(RoleIds.Admin)]
    public class AdminController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IAdminService _adminService;
        private readonly ISessionContext _session;

        public AdminController() : this(new AuthService(), new AdminService(), new JwtSessionContext()) { }
        public AdminController(IAuthService authService, IAdminService adminService, ISessionContext session)
        {
            _authService = authService;
            _adminService = adminService;
            _session = session;
        }

        // ────────────────────────────────────────────
        // Dashboard
        // ────────────────────────────────────────────
        public ActionResult Dashboard()
        {
            var vm = _adminService.GetDashboard();
            return View(vm);
        }

        // ────────────────────────────────────────────
        // Master Data (single page, 6 modal partials)
        // ────────────────────────────────────────────
        [HttpGet]
        public ActionResult MasterData()
        {
            return View(_adminService.GetMasterData());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveMaster(MasterEntitySaveViewModel vm)
        {
            try
            {
                int newId = _adminService.SaveMaster(vm, _session.ConsumerId);
                return Json(new { success = true, message = "Saved successfully.", id = newId, entityType = vm.EntityType });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message, code = ex.ErrorCode }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public JsonResult GetDistricts(int? stateId)
        {
            var list = _adminService.GetDistricts(stateId);
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        // ────────────────────────────────────────────
        // Users
        // ────────────────────────────────────────────
        [HttpGet]
        public ActionResult Users()
        {

            var vm = _adminService.ListUsers(new AdminUserListFilterViewModel { RoleId = 0 });
            return View(vm);
        }

        [HttpPost]
        public ActionResult GetUsers(AdminUserListFilterViewModel filter)
        {
            filter = filter ?? new AdminUserListFilterViewModel();
            filter.RoleId = 0; // Users tab = all roles; Officers tab uses its own action
            var vm = _adminService.ListUsers(filter);
            return PartialView("_UserTable", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateUser(int consumerId, int roleId, int? deptId, int? wardId, string designation)
        {
            try
            {
                _adminService.UpdateUserRole(consumerId, roleId, deptId, wardId, designation, _session.ConsumerId);
                return Json(new { success = true, message = "User updated." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            try
            {
                _adminService.DeleteUser(id);
                return Json(new { success = true, message = "User deleted." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // ────────────────────────────────────────────
        // Complaints
        // ────────────────────────────────────────────
        [HttpGet]
        public ActionResult Complaints()
        {
            var vm = _adminService.ListComplaints(new AdminComplaintListFilterViewModel());
            return View(vm);
        }

        [HttpPost]
        public ActionResult GetComplaints(AdminComplaintListFilterViewModel filter)
        {
            var vm = _adminService.ListComplaints(filter ?? new AdminComplaintListFilterViewModel());
            return PartialView("_ComplaintTable", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo)
        {
            try
            {
                _adminService.UpdateComplaint(complaintId, categoryId, priorityId, statusId, assignedTo, _session.ConsumerId);
                return Json(new { success = true, message = "Complaint updated." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteComplaint(int id)
        {
            try
            {
                _adminService.DeleteComplaint(id);
                return Json(new { success = true, message = "Complaint deleted." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // ────────────────────────────────────────────
        // Officers  (RoleId = SupportExecutive)
        // ────────────────────────────────────────────
        [HttpGet]
        public ActionResult Officers()
        {
            var vm = _adminService.ListUsers(new AdminUserListFilterViewModel { RoleId = RoleIds.SupportExecutive });
            return View(vm);
        }

        [HttpPost]
        public ActionResult GetOfficers(AdminUserListFilterViewModel filter)
        {
            filter = filter ?? new AdminUserListFilterViewModel();
            filter.RoleId = RoleIds.SupportExecutive;
            var vm = _adminService.ListUsers(filter);
            return PartialView("_OfficerTable", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOfficer(int consumerId, string designation, int? wardId, int? deptId)
        {
            try
            {
                _adminService.UpdateOfficer(consumerId, designation, wardId, deptId, _session.ConsumerId);
                return Json(new { success = true, message = "Officer updated." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteOfficer(int id)
        {
            try
            {
                _adminService.DeleteUser(id); // Officer is a Consumer; delete is same as user delete
                return Json(new { success = true, message = "Officer deleted." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // ────────────────────────────────────────────
        // Legacy staff-create (kept intact from original controller)
        // ────────────────────────────────────────────
        [HttpGet]
        public ActionResult CreateStaff()
        {
            return View(new FixMyCityModel.ViewModel.StaffRegisterViewModel { RoleId = RoleIds.SupportExecutive });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStaff(FixMyCityModel.ViewModel.StaffRegisterViewModel vm)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Please correct the highlighted fields." });
            _authService.RegisterStaff(vm);
            return Json(new { success = true, message = "Staff account created." });
        }
    }
}

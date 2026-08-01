using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
using FixMyCity.Filters;
using FixMyCity.service;
using FixMyCity.Service;
using FixMyCityModel.ViewModel;
using System;
using System.Web.Mvc;

namespace FixMyCity.Controllers
{
    [RoleAuthorize(RoleIds.Admin)]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ISessionContext _session;

        public AdminController()
     : this(new AdminService(), new JwtSessionContext())
        {
        }

        public AdminController(IAdminService service, ISessionContext session)
        {
            _adminService = service;
            _session = session;
        }
        // Wherever the logged-in admin's id actually comes from in your auth setup
        // (claims/session) — swap this out for the real accessor.
        private int CurrentActorId => _session.ConsumerId;
        private int roleId => _session.RoleId;

        // ============================================================
        // USERS
        // ============================================================

        // Full page — first load only. Renders the shell + the partial once.
         public ActionResult Dashboard()
        {
            var vm = _adminService.GetDashboard();
            return View(vm);
        }
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
                int newId = _adminService.SaveMaster(vm, CurrentActorId);
                return Json(new { success = true, message = "Saved successfully.", id = newId, entityType = vm.EntityType });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message, code = ex.ErrorCode }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }
        [HttpGet]
        public JsonResult GetMasterList(string entityType, int? parentId, bool includeInactive = false)
        {
            try
            {
                var list = _adminService.GetMasterList(entityType, parentId, includeInactive);
                return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
            }
            catch (BusinessException ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
            catch (DataAccessException ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult GetDistricts(int? stateId)
        {
            var list = _adminService.GetDistricts(stateId);
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Users(AdminUserListFilterViewModel filter)
        {
            var vm = _adminService.ListUsers(filter);
            return View(vm);
        }

        // AJAX target for search / sort / page. Returns ONLY the table+pagination
        // markup, no full layout — this is what stops the whole page reloading.
        [HttpGet]
        public ActionResult UsersGrid(AdminUserListFilterViewModel filter)
        {
            var vm = _adminService.ListUsers(filter);
            return PartialView("_UsersGrid", vm);
        }

        [HttpPost]
        public JsonResult DeleteUser(int id)
        {
            try
            {
                _adminService.DeleteUser(id, CurrentActorId);
                return Json(new { success = true });
            }
            catch (BusinessException ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult UpdateUserStatus(int id, bool isActive)
        {
            try
            {
                _adminService.UpdateUserStatus(id, isActive, CurrentActorId);
                return Json(new { success = true });
            }
            catch (BusinessException ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult EditUser(int id)
        {
            var vm = _adminService.GetUserById(id);
            return PartialView("_EditUserModal", vm);
        }

        [HttpPost]
        public JsonResult SaveUserRole(int consumerId, int roleId, int? deptId, int? wardId, string designation)
        {
            try
            {
                _adminService.UpdateUserRole(consumerId, roleId, deptId, wardId, designation, CurrentActorId);
                return Json(new { success = true });
            }
            catch (BusinessException ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ============================================================
        // COMPLAINTS
        // ============================================================

        public ActionResult Complaints(AdminComplaintListFilterViewModel filter)
        {
            var vm = _adminService.ListComplaints(filter);
            return View(vm);
        }

        [HttpGet]
        public ActionResult ComplaintsGrid(AdminComplaintListFilterViewModel filter)
        {
            var vm = _adminService.ListComplaints(filter);
            return PartialView("_ComplaintsGrid", vm);
        }

        [HttpPost]
        public JsonResult DeleteComplaint(int id)
        {
            try
            {
                _adminService.DeleteComplaint(id, CurrentActorId);
                return Json(new { success = true });
            }
            catch (BusinessException ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public ActionResult EditComplaint(int id)
        {
            var vm = _adminService.GetComplaintById(id);
            return PartialView("_EditComplaintModal", vm);
        }

        [HttpPost]
        public JsonResult SaveComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo)
        {
            try
            {
                _adminService.UpdateComplaint(complaintId, categoryId, priorityId, statusId, assignedTo, CurrentActorId,roleId);
                return Json(new { success = true });
            }
            catch (BusinessException ex)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = ex.Message });
            }
        }
        /*  [HttpPost]
        public ActionResult GetOfficers(AdminUserListFilterViewModel filter)
        {
            filter = filter ?? new AdminUserListFilterViewModel();
           // filter.RoleId = RoleIds.SupportExecutive;
            var vm = _adminService.GetOfficers(filter);
            return PartialView("_OfficerTable", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOfficer(int consumerId, string designation, int? wardId, int? deptId)
        {
            try
            {
                _adminService.UpdateOfficer(consumerId, designation, wardId, deptId, CurrentActorId);
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
                _adminService.DeleteUser(id, CurrentActorId); // Officer is a Consumer; delete is same as user delete
                return Json(new { success = true, message = "Officer deleted." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }*/


    }
}
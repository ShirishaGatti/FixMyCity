using FixMyCity.Exceptions;
using FixMyCity.Infrastructure;
using FixMyCity.Filters;
using FixMyCity.service;
using FixMyCity.Service;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FixMyCity.Controllers
{
    [RoleAuthorize(RoleIds.Admin)]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ISessionContext _session;
        private readonly IAuthService _authService;
        private readonly IMailService _mailService;
        private readonly IConsumerService _consumerService;

        public AdminController()
     : this(new AdminService(), new JwtSessionContext(), new AuthService(), new MailService(), new ConsumerService())
        {
        }

        public AdminController(IAdminService service, ISessionContext session, IAuthService authService, IMailService mailService, IConsumerService consumerService)
        {
            _adminService = service;
            _session = session;
            _authService = authService;
            _mailService = mailService;
            _consumerService = consumerService;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ImportMasterData(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                return Json(new
                {
                    success = false,
                    message = "No file uploaded."
                });
            }

            try
            {
                string json;

                using (var reader = new StreamReader(file.InputStream))
                {
                    json = reader.ReadToEnd();
                }

                var items = JsonConvert.DeserializeObject<List<MasterImportViewModel>>(json);

                if (items == null || items.Count == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Invalid JSON format."
                    });
                }

                var results = new List<string>();

                foreach (var item in items)
                {
                    try
                    {
                        var model = new MasterEntitySaveViewModel
                        {
                            EntityType = item.EntityType,
                            Name = item.Name,
                            IsActive = true
                        };

                        // Pass DepartmentId for categories
                        if (!string.IsNullOrWhiteSpace(item.EntityType) &&
                            item.EntityType.Equals(
                                "category",
                                StringComparison.OrdinalIgnoreCase) &&
                            item.DepartmentId.HasValue)
                        {
                            model.DepartmentId = item.DepartmentId.Value;
                        }

                        // Pass WardNo for wards
                        if (!string.IsNullOrWhiteSpace(item.EntityType) &&
                            item.EntityType.Equals(
                                "ward",
                                StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(item.WardNo))
                        {
                            model.WardNo = item.WardNo;
                        }

                        // Pass ParentId for:
                        // District -> State
                        // City     -> District
                        // Ward     -> City
                        if (!string.IsNullOrWhiteSpace(item.EntityType) &&
                            (
                                item.EntityType.Equals(
                                    "district",
                                    StringComparison.OrdinalIgnoreCase) ||

                                item.EntityType.Equals(
                                    "city",
                                    StringComparison.OrdinalIgnoreCase) ||

                                item.EntityType.Equals(
                                    "ward",
                                    StringComparison.OrdinalIgnoreCase)
                            ) &&
                            item.ParentId.HasValue)
                        {
                            model.ParentId = item.ParentId.Value;
                        }

                        int newId = _adminService.SaveMaster(
                            model,
                            CurrentActorId
                        );

                        results.Add(
                            string.Format(
                                "SUCCESS {0} (ID: {1})",
                                item.Name,
                                newId
                            )
                        );
                    }
                    catch (BusinessException ex)
                    {
                        results.Add(
                            string.Format(
                                "FAILED {0} - Validation: {1}",
                                item.Name,
                                ex.Message
                            )
                        );
                    }
                    catch (Exception ex)
                    {
                        results.Add(
                            string.Format(
                                "FAILED {0} - Error: {1}",
                                item.Name,
                                ex.Message
                            )
                        );
                    }
                }

                return Json(new
                {
                    success = true,
                    total = items.Count,
                    imported = results.Count(
                        r => r.StartsWith("SUCCESS")),

                    failed = results.Count(
                        r => r.StartsWith("FAILED")),

                    results = results
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public JsonResult GetMasterList(
            string entityType,
            int? parentId,
            bool includeInactive = false)
        {
            try
            {
                var data = _adminService.GetMasterList(
                    entityType,
                    parentId,
                    includeInactive
                );

                return Json(
                    new
                    {
                        success = true,
                        data = data
                    },
                    JsonRequestBehavior.AllowGet
                );
            }
            catch (BusinessException ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message
                    },
                    JsonRequestBehavior.AllowGet
                );
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message
                    },
                    JsonRequestBehavior.AllowGet
                );
            }
        }


        //[HttpGet]
        //public JsonResult GetMasterList(string entityType, int? parentId, bool includeInactive = false)
        //{
        //    try
        //    {
        //        var list = _adminService.GetMasterList(entityType, parentId, includeInactive);
        //        return Json(new { success = true, data = list }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (BusinessException ex)
        //    {
        //        return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (DataAccessException ex)
        //    {
        //        return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}
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
                return Json(new { success = false, message = ex.Message, code = ex.ErrorCode });
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
                return Json(new { success = false, message = ex.Message, code = ex.ErrorCode });
            }
        }

        [HttpGet]
        public ActionResult EditUser(int id)
        {
            var vm = _adminService.GetUserById(id);
            return PartialView("_EditUserModal", vm);
        }

        [HttpPost]
        public JsonResult SaveUserRole(int consumerId, int roleId, int? deptId)
        {
            try
            {
                var previous = _adminService.GetUserById(consumerId);
                _adminService.UpdateUser(consumerId, roleId, deptId, CurrentActorId);

                var updated = _adminService.GetUserById(consumerId);
                if (previous != null && updated != null &&
                    previous.RoleId != RoleIds.SupportExecutive &&
                    updated.RoleId == RoleIds.SupportExecutive &&
                    !string.IsNullOrWhiteSpace(updated.Email))
                {
                    SendRoleChangedEmail(updated);
                }

                return Json(new { success = true });
            }
            catch (BusinessException ex)
            {
                return Json(new { success = false, message = ex.Message, code = ex.ErrorCode });
            }
        }

        private void SendRoleChangedEmail(AdminUserEditViewModel user)
        {
            try
            {
                string roleName = user.Roles?.FirstOrDefault(r => r.RoleId == user.RoleId)?.RoleName ?? "Officer";
                string deptName = user.Departments?.FirstOrDefault(d => d.DepartmentId == user.DeptId)?.DepartmentName;
                _mailService.SendRoleChangedEmail(user.Email, user.Name, roleName, deptName);
            }
            catch (Exception ex)
            {
                FixMyCity.Infrastructure.FileLogger.Log(ex, "AdminController.SendRoleChangedEmail");
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
        [ValidateAntiForgeryToken]

        public JsonResult DeleteComplaint(int id)
        {
            try
            {
                _adminService.DeleteComplaint(id, CurrentActorId);
                return Json(new { success = true });
            }
            catch (BusinessException ex)
            {
                return Json(new { success = false, message = ex.Message, code = ex.ErrorCode });
            }
        }

        [HttpGet]
        public ActionResult AssignComplaintModal(int id)
        {
            var vm = _adminService.GetComplaintById(id);
            return PartialView("_AssignComplaintModal", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveAssignment(int complaintId, int? assignedTo)
        {
            try
            {
                var previous = _adminService.GetComplaintById(complaintId);

                _adminService.AssignComplaint(complaintId, assignedTo, CurrentActorId);

                // Notify the officer and citizen when (and only when) the assignment actually changed.
                if (assignedTo.HasValue && assignedTo.Value != previous?.AssignedTo)
                {
                    SendAssignmentOtp(assignedTo.Value, previous?.PriorityId ?? 0, previous);
                    SendCitizenAssignedEmail(previous, assignedTo.Value);
                }

                return Json(new { success = true });
            }
            catch (BusinessException ex)
            {
                return Json(new { success = false, message = ex.Message, code = ex.ErrorCode });
            }
        }

        private void SendCitizenAssignedEmail(AdminComplaintEditViewModel complaint, int officerConsumerId)
        {
            try
            {
                if (complaint == null || string.IsNullOrWhiteSpace(complaint.RaisedByEmail)) return;

                var officer = _adminService.GetUserById(officerConsumerId);
                if (officer == null || string.IsNullOrWhiteSpace(officer.Name)) return;

                string number = complaint.ComplaintNumber ?? $"#{complaint.ComplaintId}";
                string title = complaint.Title ?? "Complaint";

                _mailService.SendComplaintAssignedEmail(complaint.RaisedByEmail, complaint.RaisedByName, number, title, officer.Name);
            }
            catch (Exception ex)
            {
                FixMyCity.Infrastructure.FileLogger.Log(ex, "AdminController.SendCitizenAssignedEmail");
            }
        }

        private void SendAssignmentOtp(int officerConsumerId, int priorityId, AdminComplaintEditViewModel complaint)
        {
            try
            {
                var officer = _adminService.GetUserById(officerConsumerId);
                if (officer == null || string.IsNullOrWhiteSpace(officer.Email)) return;

                //string otp = _authService.CreateOtp(officerConsumerId, "COMPLAINT_ASSIGNED");

                string number = complaint?.ComplaintNumber ?? $"#{complaint?.ComplaintId}";
                string title = complaint?.Title ?? "Complaint";
                string priority = complaint?.Priorities?.FirstOrDefault(p => p.PriorityId == priorityId)?.PriorityName
                                  ?? complaint?.PriorityName ?? "Medium";

                _mailService.SendAssignmentOtpEmail(officer.Email, officer.Name, number, title, priority);
            }
            catch (Exception ex)
            {
                // Best-effort — a mail failure must not fail the assignment itself.
                FixMyCity.Infrastructure.FileLogger.Log(ex, "AdminController.SendAssignmentOtp");
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
        /*
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

        // ============================================================
        // PROFILE
        // ============================================================

        [HttpGet]
        public ActionResult Profile()
        {
            Consumer consumer;
            try
            {
                consumer = _consumerService.GetProfile(_session.ConsumerId);
            }
            catch (NotFoundException)
            {
                return RedirectToAction("Login", "Account");
            }

            var vm = new ProfileViewModel
            {
                ConsumerId = consumer.ConsumerId,
                Name = consumer.Name,
                Email = consumer.Email,
                Contact = consumer.Contact,
                DOB = consumer.DOB,
                AddressLine = consumer.AddressLine,
                CityId = consumer.CityId,
                WardId = consumer.WardId,
                Designation = consumer.Designation
            };

            PopulateProfileDropdowns(vm);
            ViewBag.ActivePage = "Profile";
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileViewModel vm)
        {
            ViewBag.ActivePage = "Profile";
            string dobError;
            if (!vm.ValidateDob(out dobError))
            {
                ModelState.AddModelError("DOB", dobError);
            }
            if (!ModelState.IsValid)
            {
                PopulateProfileDropdowns(vm);
                return View(vm);
            }

            try
            {
                _consumerService.UpdateProfile(_session.ConsumerId, vm.Name, vm.Contact,
                    vm.DOB, vm.AddressLine, vm.CityId, vm.WardId, vm.Designation);
                TempData["Success"] = "Profile updated successfully.";
                return RedirectToAction("Profile");
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError("", ex.Message);
                PopulateProfileDropdowns(vm);
                return View(vm);
            }
            catch (DataAccessException ex)
            {
                ModelState.AddModelError("", ex.Message);
                PopulateProfileDropdowns(vm);
                return View(vm);
            }
        }

        private void PopulateProfileDropdowns(ProfileViewModel vm)
        {
            var cities = _consumerService.GetCities();
            vm.Cities = cities;
            int cityId = vm.CityId ?? (cities.Count > 0 ? cities[0].CityId : 1);
            vm.Wards = _consumerService.GetWardsByCity(cityId);
        }

    }
}
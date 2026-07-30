/*using FixMyCity.Exceptions;
using FixMyCity.Filters;
using FixMyCity.Infrastructure;
using FixMyCity.Repository;
using FixMyCity.service;
using FixMyCity.Service;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Configuration;
using System.Web.Mvc;

namespace FixMyCity.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly IConsumerService _consumerService;
        private readonly IComplaintService _complaintService;
        private readonly ISessionContext _session;

        public ComplaintController() {
            _consumerService=new ConsumerService();
           _complaintService=new ComplaintService();
            _session = new JwtSessionContext();
        }        



        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public ActionResult MyComplaints()
        {
            //ViewBag.ConsumerName = _session.Email;
            var vm = _complaintService.GetMyComplaints(_session.ConsumerId);
            vm.AllowedExtensionsCsv = ConfigurationManager.AppSettings["AllowedAttachmentExtensions"] ?? ".jpg,.jpeg,.png,.pdf,.doc,.docx";
            vm.MaxAttachmentSizeMB = Convert.ToInt32(ConfigurationManager.AppSettings["MaxAttachmentSizeMB"] ?? "5");
            return View(vm);
        }

   

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public ActionResult Home()
        {
          //  ViewBag.ConsumerName = _session.Email;
            return View();
        }

        // Any authenticated role can view/edit their own profile
        [RoleAuthorize]
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
                Designation = consumer.Designation,
            };
           // ViewBag.ConsumerName = _session.Email;
            PopulateProfileDropdowns(vm);
            return View(vm);
        }

        [RoleAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileViewModel vm)
        {
          //  ViewBag.ConsumerName = _session.Email;
            if (!ModelState.IsValid)
            {
                PopulateProfileDropdowns(vm);
                return View(vm);
            }

            try
            {
                // Always use the session-derived id — never trust the
                // posted ConsumerId, it's a hidden field a client can edit.
                _consumerService.UpdateProfile(_session.ConsumerId, vm.Name, vm.Contact,
                    vm.DOB, vm.AddressLine, vm.CityId, vm.WardId, vm.Designation);

                TempData["SuccessMessage"] = "Profile updated successfully.";
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
        [RoleAuthorize(RoleIds.Citizen)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FileComplaint(FileComplaintViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                string firstError = "Please correct the highlighted fields.";
                foreach (var state in ModelState.Values)
                    foreach (var err in state.Errors)
                    { firstError = err.ErrorMessage; goto done; }
                done:
                return Json(new { success = false, message = firstError });
            }

            int newId;
            try
            {
                newId = _complaintService.FileComplaint(vm, _session.ConsumerId);
            }
            catch (DataAccessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            if (vm.Attachments != null)
            {
                try
                {
                    _complaintService.UploadAttachments(newId, _session.ConsumerId, vm.Attachments);
                }
                catch (BusinessException ex)
                {
                    // Complaint already filed successfully — don't lose it over an
                    // attachment problem, just tell the citizen what went wrong.
                    return Json(new { success = true, message = "Complaint filed, but: " + ex.Message, complaintId = newId });
                }
            }

            return Json(new { success = true, message = "Complaint filed successfully.", complaintId = newId });
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public ActionResult ComplaintDetails(int id)
        {
            Complaint complaint;
            try
            {
                complaint = _complaintService.GetComplaintDetails(id, _session.ConsumerId);
            }
            catch (NotFoundException)
            {
                return RedirectToAction("MyComplaints");
            }

            var vm = new ComplaintDetailsViewModel
            {
                Complaint = complaint,
                Attachments = _complaintService.GetAttachments(id, _session.ConsumerId)
            };
            return View(vm);
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public ActionResult DownloadAttachment(int id)
        {
            Attachment attachment;
            try
            {
                attachment = _complaintService.GetAttachmentForDownload(id, _session.ConsumerId);
            }
            catch (NotFoundException)
            {
                return HttpNotFound();
            }

            string physicalPath = _complaintService.GetPhysicalPath(attachment);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound();

            string contentType = string.IsNullOrEmpty(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType;
            return File(physicalPath, contentType, attachment.FileName);
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAttachment(int id)
        {
            try
            {
                _complaintService.DeleteAttachment(id, _session.ConsumerId);
                return Json(new { success = true, message = "Attachment removed." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (NotFoundException) { return Json(new { success = false, message = "Attachment not found." }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }
        private void PopulateProfileDropdowns(ProfileViewModel vm)
        {
            var cities = _consumerService.GetCities();
            vm.Cities = cities;
            int cityId = vm.CityId ?? (cities.Count > 0 ? cities[0].CityId : 1);
            vm.Wards = _consumerService.GetWardsByCity(cityId);
        }
    }
}*/
    using FixMyCity.Exceptions;
using FixMyCity.Filters;
using FixMyCity.Infrastructure;
using FixMyCity.Repository;
using FixMyCity.service;
using FixMyCity.Service;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Configuration;
using System.Web.Mvc;

namespace FixMyCity.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly IConsumerService _consumerService;
        private readonly IComplaintService _complaintService;
        private readonly ISessionContext _session;

        public ComplaintController()
        {
            _consumerService = new ConsumerService();
            _complaintService = new ComplaintService();
            _session = new JwtSessionContext();
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public ActionResult MyComplaints(ComplaintListFilterViewModel filter)
        {
            filter = filter ?? new ComplaintListFilterViewModel();
            if (filter.PageNumber < 1) filter.PageNumber = 1;
            if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 10;

            var vm = _complaintService.Search(_session.ConsumerId, filter);
            vm.AllowedExtensionsCsv = ConfigurationManager.AppSettings["AllowedAttachmentExtensions"] ?? ".jpg,.jpeg,.png,.pdf,.doc,.docx";
            vm.MaxAttachmentSizeMB = Convert.ToInt32(ConfigurationManager.AppSettings["MaxAttachmentSizeMB"] ?? "5");
            return View(vm);
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveComplaint(FileComplaintViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                string firstError = "Please correct the highlighted fields.";
                foreach (var state in ModelState.Values)
                    foreach (var err in state.Errors)
                    { firstError = err.ErrorMessage; goto done; }
                done:
                return Json(new { success = false, message = firstError });
            }

            int savedId;
            try
            {
                savedId = _complaintService.SaveComplaint(vm, _session.ConsumerId);
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }

            if (vm.Attachments != null)
            {
                try { _complaintService.UploadAttachments(savedId, _session.ConsumerId, vm.Attachments); }
                catch (BusinessException ex)
                {
                    return Json(new { success = true, message = "Complaint saved, but: " + ex.Message, complaintId = savedId });
                }
            }

            string verb = vm.ComplaintId.HasValue ? "updated" : "filed";
            return Json(new { success = true, message = $"Complaint {verb} successfully.", complaintId = savedId });
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public JsonResult GetComplaintForEdit(int id)
        {
            try
            {
                var c = _complaintService.GetComplaintDetails(id, _session.ConsumerId);
                if (c.StatusName != "Open")
                    return Json(new { success = false, message = "Only Open complaints can be edited." }, JsonRequestBehavior.AllowGet);

                return Json(new
                {
                    success = true,
                    complaintId = c.ComplaintId,
                    title = c.Title,
                    description = c.Description,
                    categoryId = c.CategoryId,
                    priorityId = c.PriorityId,
                    cityId = c.CityId,
                    wardId = c.WardId,
                    addressLine = c.AddressLine,
                    landmark = c.Landmark
                }, JsonRequestBehavior.AllowGet);
            }
            catch (NotFoundException)
            {
                return Json(new { success = false, message = "Complaint not found." }, JsonRequestBehavior.AllowGet);
            }
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteComplaint(int id)
        {
            try
            {
                _complaintService.DeleteComplaint(id, _session.ConsumerId);
                return Json(new { success = true, message = "Complaint deleted." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public ActionResult ExportComplaintPdf(int id)
        {
            ComplaintExportViewModel export;
            try { export = _complaintService.GetComplaintForExport(id, _session.ConsumerId); }
            catch (NotFoundException) { return HttpNotFound(); }

            byte[] pdf = ComplaintPdfBuilder.Build(export);
            string fileName = $"Complaint_{export.Complaint.ComplaintNumber}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public ActionResult ComplaintDetailsPartial(int id)
        {
            try
            {
                var vm = new ComplaintDetailsViewModel
                {
                    Complaint = _complaintService.GetComplaintDetails(id, _session.ConsumerId),
                    Attachments = _complaintService.GetAttachments(id, _session.ConsumerId)
                };
                return PartialView("_ComplaintDetailsCard", vm);
            }
            catch (NotFoundException)
            {
                return Content("<div class='p-4'>Complaint not found.</div>");
            }
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public ActionResult DownloadAttachment(int id)
        {
            Attachment attachment;
            try { attachment = _complaintService.GetAttachmentForDownload(id, _session.ConsumerId); }
            catch (NotFoundException) { return HttpNotFound(); }

            string physicalPath = _complaintService.GetPhysicalPath(attachment);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound();

            string contentType = string.IsNullOrEmpty(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType;
            return File(physicalPath, contentType, attachment.FileName);
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAttachment(int id)
        {
            try
            {
                _complaintService.DeleteAttachment(id, _session.ConsumerId);
                return Json(new { success = true, message = "Attachment removed." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (NotFoundException) { return Json(new { success = false, message = "Attachment not found." }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public ActionResult Home()
        {
            return View();
        }

        [RoleAuthorize]
        [HttpGet]
        public ActionResult Profile()
        {
            Consumer consumer;
            try { consumer = _consumerService.GetProfile(_session.ConsumerId); }
            catch (NotFoundException) { return RedirectToAction("Login", "Account"); }

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
            return View(vm);
        }

        [RoleAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                PopulateProfileDropdowns(vm);
                return View(vm);
            }

            try
            {
                _consumerService.UpdateProfile(_session.ConsumerId, vm.Name, vm.Contact,
                    vm.DOB, vm.AddressLine, vm.CityId, vm.WardId, vm.Designation);
                TempData["SuccessMessage"] = "Profile updated successfully.";
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
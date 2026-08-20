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
using System.Web.Services.Description;
using System.Web.UI.WebControls;

namespace FixMyCity.Controllers
{
    [RoleAuthorize(RoleIds.Citizen)]
    public class CitizenController : Controller
    {
        private readonly IConsumerService _consumerService;
        private readonly IComplaintService _complaintService;
        private readonly ISessionContext _session;
        private readonly IComplaintChatService _chatService;

        public CitizenController()
        {
            _consumerService = new ConsumerService();
            _complaintService = new ComplaintService();
            _chatService = new ComplaintChatService();      // NEW

            _session = new JwtSessionContext();
        }
        private int CurrentActorId => _session.ConsumerId;
        private int roleId => _session.RoleId;


        [HttpGet]
        public ActionResult MyComplaints(ComplaintListFilterViewModel filter)
        {
            filter = filter ?? new ComplaintListFilterViewModel();
            if (filter.PageNumber < 1) filter.PageNumber = 1;
            if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 10;
            string dateErr;
            if (!filter.ValidateDates(out dateErr))
            {
                TempData["Error"] = dateErr;
            }

            var vm = _complaintService.Search(CurrentActorId, filter);
            vm.AllowedExtensionsCsv = ConfigurationManager.AppSettings["AllowedAttachmentExtensions"] ?? ".jpg,.jpeg,.png,.pdf,.doc,.docx";
            vm.MaxAttachmentSizeMB = Convert.ToInt32(ConfigurationManager.AppSettings["MaxAttachmentSizeMB"] ?? "5");
            if (Request.IsAjaxRequest())
            {
                return PartialView("_MyComplaintList", vm);
            }
            return View(vm);
        }

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
                savedId = _complaintService.SaveComplaint(vm, CurrentActorId, roleId);
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }

            if (vm.Attachments != null)
            {
                try { _complaintService.UploadAttachments(savedId, CurrentActorId, vm.Attachments); }
                catch (BusinessException ex)
                {
                    return Json(new { success = false, message = ex.Message, complaintId = savedId });
                }
                catch (DataAccessException ex)
                {
                    return Json(new { success = false, message = ex.Message, complaintId = savedId });
                }
                catch (Exception)
                {
                    return Json(new { success = false, message = "Complaint saved, but document upload failed. Please try again.", complaintId = savedId });
                }
            }

            string verb = vm.ComplaintId.HasValue ? "updated" : "filed";
            return Json(new { success = true, message = $"Complaint {verb} successfully.", complaintId = savedId });
        }

        [HttpGet]
        public JsonResult GetComplaintForEdit(int id)
        {
            try
            {
                var c = _complaintService.GetComplaintDetails(id, CurrentActorId);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteComplaint(int id)
        {
            try
            {
                _complaintService.DeleteComplaint(id, CurrentActorId);
                return Json(new { success = true, message = "Complaint deleted." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // Resolution-confirmation workflow � the citizen's half. Confirm closes
        // the complaint outright; Reject sends it back to the officer as
        // Reopened. Both only succeed (SP-enforced) while the complaint is
        // Awaiting Customer Confirmation and belongs to the caller.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmResolution(int id)
        {
            try
            {
                _complaintService.ConfirmResolution(id, CurrentActorId);
                return Json(new { success = true, message = "Thanks for confirming the complaint has been closed." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (Exception) { return Json(new { success = false, message = "Unable to confirm the resolution. Please try again." }); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectResolution(int id, string reason)
        {
            try
            {
                _complaintService.RejectResolution(id, CurrentActorId, reason);
                return Json(new { success = true, message = "The complaint has been reopened and sent back to the officer." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (Exception) { return Json(new { success = false, message = "Unable to reject the resolution. Please try again." }); }
        }

        [HttpGet]
        public ActionResult ExportComplaintPdf(int id)
        {
            ComplaintExportViewModel export;
            try { export = _complaintService.GetComplaintForExport(id, CurrentActorId); }
            catch (NotFoundException) { return HttpNotFound(); }

            byte[] pdf = ComplaintPdfBuilder.Build(export);
            string fileName = $"Complaint_{export.Complaint.ComplaintNumber}.pdf";
            return File(pdf, "application/pdf", fileName);
        }

        [HttpGet]
        public ActionResult ComplaintDetailsPartial(int id)
        {
            try
            {
                var vm = new ComplaintDetailsViewModel
                {
                    Complaint = _complaintService.GetComplaintDetails(id, CurrentActorId),
                    Attachments = _complaintService.GetAttachments(id, CurrentActorId),
                    Chat = _chatService.GetThread(id, CurrentActorId, roleId, 0)
                };

                return PartialView("ComplaintDetails", vm);
            }
            catch (NotFoundException)
            {
                return Content("<div class='p-4'>Complaint not found.</div>");
            }
        }

        [HttpGet]
        public ActionResult DownloadAttachment(int id)
        {
            Attachment attachment;
            try { attachment = _complaintService.GetAttachmentForDownload(id, CurrentActorId); }
            catch (NotFoundException) { return HttpNotFound(); }

            string physicalPath = _complaintService.GetPhysicalPath(attachment);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound();

            string contentType = string.IsNullOrEmpty(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType;
            return File(physicalPath, contentType, attachment.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAttachment(int id)
        {
            try
            {
                _complaintService.DeleteAttachment(id, CurrentActorId);
                return Json(new { success = true, message = "Attachment removed." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (NotFoundException) { return Json(new { success = false, message = "Attachment not found." }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        [HttpGet]
        public ActionResult Home()
        {
            var filter = new ComplaintListFilterViewModel { PageSize = 100 };
            var vm = _complaintService.Search(CurrentActorId, filter);
            return View(vm);
        }

        [HttpGet]
        public ActionResult Profile()
        {
            Consumer consumer;
            try { consumer = _consumerService.GetProfile(CurrentActorId); }
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
        //Support executives expect a "Queue" landing page after login.Redirect to the officer complaints UI which implements the queue.


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileViewModel vm)
        {
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
                _consumerService.UpdateProfile(CurrentActorId, vm.Name, vm.Contact,
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
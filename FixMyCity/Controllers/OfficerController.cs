using FixMyCity.Exceptions;
using FixMyCity.Filters;
using FixMyCity.Infrastructure;
using FixMyCity.service;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Linq;
using System.Web.Mvc;
using FixMyCity.Service;

namespace FixMyCity.Controllers
{
    [RoleAuthorize(RoleIds.SupportExecutive)]
    public class OfficerController : Controller
    {
        private readonly IComplaintService _complaintService;
        private readonly IConsumerService _consumerService;
        private readonly ISessionContext _session;
        private readonly IMailService _mailService;
        private readonly IComplaintChatService _chatService;

        // Default constructor — used by MVC framework; delegates to the injectable one.
        public OfficerController()
            : this(new ComplaintService(), new ConsumerService(), new JwtSessionContext(), new MailService(), new ComplaintChatService())
        {
        }

        // Parameterized constructor — used by unit tests or a DI container.
        public OfficerController(
            IComplaintService complaintService,
            IConsumerService consumerService,
            ISessionContext session,
            IMailService mailService,
            IComplaintChatService chatService)
        {
            _complaintService = complaintService;
            _consumerService = consumerService;
            _session = session;
            _mailService = mailService;
            _chatService = chatService;
        }

        private int CurrentActorId => _session.ConsumerId;
        private int roleId => _session.RoleId;
        public ActionResult Dashboard()
        {
            ViewBag.ActivePage = "Home";
            var vm = _complaintService.GetOfficerDashboard(_session.ConsumerId, _session.RoleId);
            return View(vm);
        }

        public ActionResult Complaints(OfficerComplaintsQuery query)
        {
            ViewBag.ActivePage = "Complaints";
            var vm = _complaintService.GetOfficerComplaints(_session.ConsumerId, query);
            return View(vm);
        }

        [HttpGet]
        public PartialViewResult ComplaintList(OfficerComplaintsQuery query)
        {
            var vm = _complaintService.GetOfficerComplaints(_session.ConsumerId, query);
            return PartialView("_OfficerComplaintTable", vm);
        }
        [HttpGet]
        public ActionResult ComplaintDetailsPartial(int id)
        {
            try
            {
                Complaint complaint = null;
                
                // Try to get complaint assigned to the current officer using RoleId
                if (roleId == (int)RoleIds.SupportExecutive)
                {
                    // For officers, try to get the complaint using the ConsumerId from session
                    // to look up assigned complaint
                    var consumerId = CurrentActorId;
                    if (consumerId > 0)
                    {
                        try
                        {
                            complaint = _complaintService.GetAssignedComplaint(consumerId, id);
                        }
                        catch (NotFoundException)
                        {
                            // ConsumerId didn't work, try getting all officer complaints
                            var officerComplaints = _complaintService.GetOfficerComplaints(CurrentActorId, new OfficerComplaintsQuery());
                            if (officerComplaints.Complaints.Any(c => c.ComplaintId == id))
                            {
                                complaint = officerComplaints.Complaints.First(c => c.ComplaintId == id);
                            }
                        }
                    }
                    // If still not found, try without officer filter
                    if (complaint == null)
                    {
                        var allComplaints = _complaintService.GetOfficerComplaints(CurrentActorId, new OfficerComplaintsQuery());
                        if (allComplaints.Complaints.Any(c => c.ComplaintId == id))
                        {
                            complaint = allComplaints.Complaints.First(c => c.ComplaintId == id);
                        }
                    }
                }
                else
                {
                    // For other roles, use original approach
                    complaint = _complaintService.GetAssignedComplaint(CurrentActorId, id);
                }
                
                if (complaint == null)
                {
                    return Content("<div class='p-4'>Complaint not found or not assigned to you.</div>");
                }

                var raiserName = "";
                if (complaint.RaisedBy > 0)
                {
                    try
                    {
                        var raiser = _consumerService.GetProfile(complaint.RaisedBy);
                        raiserName = raiser != null ? raiser.Name : "Deleted User";
                    }
                    catch (NotFoundException)
                    {
                        // Citizen account was deactivated/deleted after filing — complaint
                        // itself is still valid and must remain viewable by the officer.
                        raiserName = "Deleted User";
                    }
                }
                ViewData["RaiseByName"] = raiserName;
                var vm = new ComplaintDetailsViewModel
                {
                    Complaint = complaint,
                    RaiseByName = raiserName,
                    Attachments = _complaintService.GetAttachments(id, complaint.RaisedBy),
                   // Chat = _chatService.GetThread(id, CurrentActorId, roleId, 0)
                };

                return PartialView("OfficerComplaintDetails", vm);
            }
            catch (NotFoundException)
            {
                return Content("<div class='p-4'>Complaint not found.</div>");
            }
        }
      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = string.IsNullOrWhiteSpace(errors) ? "Please correct the highlighted fields." : errors });
            }

            try
            {
                var previous = _complaintService.GetAssignedComplaint(CurrentActorId, complaintId);
                _complaintService.UpdateComplaint(complaintId, categoryId, priorityId, statusId, assignedTo, CurrentActorId, roleId);

                // Notify the citizen when the officer changes the complaint's progress.
                if (previous != null && previous.StatusId != statusId)
                {
                    var current = _complaintService.GetAssignedComplaint(CurrentActorId, complaintId) ?? previous;
                    SendCitizenProgressEmail(current, previous.StatusName, current.StatusName ?? "Updated");
                }

                return Json(new { success = true, message = "Complaint updated successfully." });
            }
            catch (BusinessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (DataAccessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Unable to update complaint. Please try again." });
            }
        }

        // Dedicated transition for the resolution-confirmation workflow.
        // Deliberately separate from UpdateComplaint: an officer may only ever
        // push a complaint into "Awaiting Customer Confirmation" via this
        // action — Closed/Reopened only ever happen through the citizen's
        // Confirm/Reject actions or the 7-day auto-expiry.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResolveComplaint(int complaintId)
        {
            try
            {
                var previous = _complaintService.GetAssignedComplaint(CurrentActorId, complaintId);
                _complaintService.ResolveComplaint(complaintId, CurrentActorId);

                // Resolving moves the complaint into "Awaiting Customer Confirmation" — notify the citizen.
                var current = _complaintService.GetAssignedComplaint(CurrentActorId, complaintId);
                if (previous != null && current != null && previous.StatusId != current.StatusId)
                {
                    SendCitizenProgressEmail(current, previous.StatusName, current.StatusName);
                }

                return Json(new { success = true, message = "Complaint marked Resolved. Awaiting the citizen's confirmation." });
            }
            catch (BusinessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (DataAccessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Unable to mark the complaint as resolved. Please try again." });
            }
        }

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

        // Previously missing: the Profile view posts here on Save Changes.
        // Mirrors ComplaintController's Profile(POST) so officers can
        // actually persist edits instead of hitting a 404.
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

        private void SendCitizenProgressEmail(Complaint complaint, string oldStatusName, string newStatusName)
        {
            try
            {
                if (complaint == null || complaint.RaisedBy <= 0) return;

                var citizen = _consumerService.GetProfile(complaint.RaisedBy);
                if (citizen == null || string.IsNullOrWhiteSpace(citizen.Email)) return;

                string number = complaint.ComplaintNumber ?? $"#{complaint.ComplaintId}";
                string title = complaint.Title ?? "Complaint";

                _mailService.SendComplaintProgressEmail(citizen.Email, citizen.Name, number, title, oldStatusName, newStatusName);
            }
            catch (Exception ex)
            {
                // Best-effort — a mail failure must not fail the officer's update.
                FixMyCity.Infrastructure.FileLogger.Log(ex, "OfficerController.SendCitizenProgressEmail");
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
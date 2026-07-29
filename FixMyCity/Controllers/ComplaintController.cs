using FixMyCity.Exceptions;
using FixMyCity.Filters;
using FixMyCity.Infrastructure;
using FixMyCity.Repository;
using FixMyCity.service;
using FixMyCity.Service;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
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

  /*      public ComplaintController(
            ISessionContext session,
            IConsumerService consumerService,
            IComplaintService complaintService)
        {
            _session = session;
            _consumerService = consumerService;
            _complaintService = complaintService;
        }*/

        [RoleAuthorize(RoleIds.Citizen)]
        [HttpGet]
        public ActionResult MyComplaints()
        {
            //ViewBag.ConsumerName = _session.Email;
            var vm = _complaintService.GetMyComplaints(_session.ConsumerId);
            return View(vm);
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

            try
            {
                int newId = _complaintService.FileComplaint(vm, _session.ConsumerId);
                return Json(new { success = true, message = "Complaint filed successfully.", complaintId = newId });
            }
            catch (DataAccessException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
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

        private void PopulateProfileDropdowns(ProfileViewModel vm)
        {
            var cities = _consumerService.GetCities();
            vm.Cities = cities;
            int cityId = vm.CityId ?? (cities.Count > 0 ? cities[0].CityId : 1);
            vm.Wards = _consumerService.GetWardsByCity(cityId);
        }
    }
}
using FixMyCity.Exceptions;
using FixMyCity.Filters;
using FixMyCity.Infrastructure;
using FixMyCity.service;
using FixMyCity.Service;
using FixMyCityModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace FixMyCity.Controllers
{
        [WebApiRoleAuthorize(RoleIds.Citizen)]
     //   [EnableCors(origins: "*", headers: "*", methods: "*")]
        [RoutePrefix("api/citizen")]
        public class ComplaintApiController : ApiController
        {
            private readonly IComplaintService _complaintService;
            private readonly IConsumerService _consumerService;
            private readonly ISessionContext _session;

            public ComplaintApiController()
                : this(new ComplaintService(), new JwtSessionContext(),new ConsumerService())
            {
            }

            public ComplaintApiController(
                IComplaintService complaintService,
                JwtSessionContext session,
                ConsumerService consumerService)
            {
                _complaintService = complaintService;
                _session = session;
            _consumerService = consumerService;
            }

        //[HttpGet]
        //[Route("complaints")]
        //public IHttpActionResult GetComplaints(int pageNumber = 1,int pageSize = 10)
        //{
        //    try
        //    {
        //    var filter = new ComplaintListFilterViewModel
        //    {
        //        PageNumber = pageNumber,
        //        PageSize = pageSize,
        //        SortField = "CreatedAt",
        //        SortDirection = "DESC"
        //    };

        //        var vm = _complaintService.Search(
        //            7,
        //            filter
        //        );

        //        return Ok(vm);
        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}

        [HttpGet]
        [Route("complaints")]
        public IHttpActionResult GetComplaintList(
        [FromUri] ComplaintListFilterViewModel filter)
        {
            try
            {
                if (filter == null)
                    filter = new ComplaintListFilterViewModel();
                var vm = _complaintService.Search(7, filter);
                return Ok(vm);
            }catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpGet]
        [Route("filters")]
        public IHttpActionResult GetList()
        {
            try
            {
                var vm = _complaintService.GetList(7);
                return Ok(vm);
            }
            catch(Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpGet]
        [Route("complaintDetail/{complaintId:int}")]
        public IHttpActionResult ComplaintDetailsPartial(int complaintId)
        {
            try
            {
                var vm = new ComplaintDetailsViewModel
                {
                    Complaint = _complaintService.GetComplaintDetails(complaintId, 7),
                    Attachments = _complaintService.GetAttachments(complaintId, 7)
                    //  Chat = _chatService.GetThread(id, CurrentActorId, roleId, 0)
                };

                return Ok(vm);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpGet]
        [Route("wards/{cityId:int}")]
        public IHttpActionResult GetWards(int cityId)
        {
            try
            {
                var wards = _consumerService.GetWardsByCity(cityId);

                return Ok(wards);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpDelete]
        [Route("deleteComplaint/{id:int}")]
        public IHttpActionResult DeleteComplaint(int id)
        {
            try
            {
                _complaintService.DeleteComplaint(id, 7);
                return Json(new { success = true, message = "Complaint deleted." });
            }
            catch (BusinessException ex) { return Json(new { success = false, message = ex.Message }); }
            catch (DataAccessException ex) { return Json(new { success = false, message = ex.Message }); }
        }
        [HttpPost]
        [Route("saveComplaint")]
        public IHttpActionResult SaveComplaint()
        {
            try
            {
                var request = HttpContext.Current.Request;

                var vm = new FileComplaintViewModel
                {
                    ComplaintId = string.IsNullOrEmpty(request.Form["ComplaintId"])
                        ? (int?)null
                        : Convert.ToInt32(request.Form["ComplaintId"]),

                    Title = request.Form["Title"],
                    Description = request.Form["Description"],
                    CategoryId = Convert.ToInt32(request.Form["CategoryId"]),
                    PriorityId = Convert.ToInt32(request.Form["PriorityId"]),
                    CityId = Convert.ToInt32(request.Form["CityId"]),
                    WardId = Convert.ToInt32(request.Form["WardId"]),
                    AddressLine = request.Form["AddressLine"],
                    Landmark = request.Form["Landmark"]
                };

                int actorId = 7; // replace with session ConsumerId later
                int roleId = 2;

                int savedId = _complaintService.SaveComplaint(
                    vm,
                    actorId,
                    roleId
                );

                //if (request.Files.Count > 0)
                //{
                //    var attachments = new HttpPostedFileBase[request.Files.Count];

                //    for (int i = 0; i < request.Files.Count; i++)
                //    {
                //        attachments[i] = request.Files[i];
                //    }

                //    _complaintService.UploadAttachments(
                //        savedId,
                //        actorId,
                //        attachments
                //    );
                //}

                string verb = vm.ComplaintId.HasValue
                    ? "updated"
                    : "filed";

                return Ok(new
                {
                    success = true,
                    message = $"Complaint {verb} successfully.",
                    complaintId = savedId
                });
            }
            catch (BusinessException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (DataAccessException ex)
            {
                return InternalServerError(ex);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

    }
    }

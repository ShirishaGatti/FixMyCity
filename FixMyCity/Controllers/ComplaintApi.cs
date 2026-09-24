using FixMyCity.Filters;
using FixMyCity.Infrastructure;
using FixMyCity.service;
using FixMyCityModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace FixMyCity.Controllers
{
        //[RoleAuthorize(RoleIds.Citizen)]
        [EnableCors(origins: "*", headers: "*", methods: "*")]
        [RoutePrefix("api/citizen")]
        public class ComplaintApiController : ApiController
        {
            private readonly IComplaintService _complaintService;
            private readonly ISessionContext _session;

            public ComplaintApiController()
                : this(new ComplaintService(), new JwtSessionContext())
            {
            }

            public ComplaintApiController(
                IComplaintService complaintService,
                JwtSessionContext session)
            {
                _complaintService = complaintService;
                _session = session;
            }

            [HttpGet]
            [Route("complaints")]
            public IHttpActionResult GetComplaints()
            {
                try
                {
                    var filter = new ComplaintListFilterViewModel
                    {
                        PageNumber = 1,
                        PageSize = 100
                    };

                    var vm = _complaintService.Search(
                        7,
                        filter
                    );

                    return Ok(vm);
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }
        }
    }

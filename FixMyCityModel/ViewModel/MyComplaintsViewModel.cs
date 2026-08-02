using FixMyCityModel.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixMyCityModel.ViewModel
{ 

        public class MyComplaintsViewModel : BaseViewModel
        {
        public string AllowedExtensionsCsv;
        public object MaxAttachmentSizeMB;

        public List<Complaint> Complaints { get; set; }
            public List<ComplaintCategory> Categories { get; set; }
            public List<ComplaintPriority> Priorities { get; set; }
            public List<City> Cities { get; set; }
        public ComplaintListFilterViewModel Filter { get; set; } = new ComplaintListFilterViewModel();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public string SearchTerm { get; set; }
        public int? StatusId { get; set; }
        public int? PriorityId { get; set; }
        public int? CategoryId { get; set; }
        public string SortColumn { get; set; }
        public string SortDirection { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public List<Complaint> AssignedComplaints { get; set; } = new List<Complaint>();
        public List<ComplaintStatus> Statuses { get; set; }
    }
    }


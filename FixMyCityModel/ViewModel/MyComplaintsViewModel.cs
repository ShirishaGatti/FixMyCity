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
        public List<ComplaintStatus> Statuses { get; set; }
    }
    }


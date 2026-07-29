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
            public List<Complaint> Complaints { get; set; }
            public List<ComplaintCategory> Categories { get; set; }
            public List<ComplaintPriority> Priorities { get; set; }
            public List<City> Cities { get; set; }
        }
    }


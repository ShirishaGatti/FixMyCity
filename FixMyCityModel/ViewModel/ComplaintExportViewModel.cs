using FixMyCityModel.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixMyCityModel.ViewModel
{
    // New: ComplaintExportViewModel.cs
    public class ComplaintExportViewModel
    {
        public Complaint Complaint { get; set; }
        public Consumer Citizen { get; set; }
        public List<AttachmentViewModel> Attachments { get; set; }
    }
}

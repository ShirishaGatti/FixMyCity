using FixMyCityModel.Model;
using System.Collections.Generic;

namespace FixMyCityModel.ViewModel
{
    public class AdminComplaintDetailsViewModel
    {
        public Complaint Complaint { get; set; }
        public List<AttachmentViewModel> Attachments { get; set; }
        public ComplaintChatViewModel Chat { get; set; }
        public string RaiseByName { get; set; }

    }
}
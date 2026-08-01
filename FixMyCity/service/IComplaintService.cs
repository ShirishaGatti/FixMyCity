using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FixMyCity.service
{
    public interface IComplaintService
    {
        MyComplaintsViewModel GetMyComplaints(int consumerId);
        int FileComplaint(FileComplaintViewModel vm, int consumerId);
        List<AttachmentViewModel> UploadAttachments(int complaintId, int consumerId, IEnumerable<HttpPostedFileBase> files);
        List<AttachmentViewModel> GetAttachments(int complaintId, int consumerId);
        Complaint GetComplaintDetails(int complaintId, int consumerId);
        Attachment GetAttachmentForDownload(int attachmentId, int consumerId);
        string GetPhysicalPath(Attachment attachment);
        void DeleteAttachment(int attachmentId, int consumerId);
        // Interface additions
        int SaveComplaint(FileComplaintViewModel vm, int consumerId,int roleId);              // upsert — Complaint.ComplaintId null/0 = create
        void DeleteComplaint(int complaintId, int consumerId);
        MyComplaintsViewModel Search(int consumerId, ComplaintListFilterViewModel filter);
        ComplaintExportViewModel GetComplaintForExport(int complaintId, int consumerId);
    }
}
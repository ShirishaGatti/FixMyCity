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
        MyComplaintsViewModel GetComplaints(int consumerId, int assignedTo, int roleId);
        int FileComplaint(FileComplaintViewModel vm, int consumerId);
        List<AttachmentViewModel> UploadAttachments(int complaintId, int consumerId, IEnumerable<HttpPostedFileBase> files);
        List<AttachmentViewModel> GetAttachments(int complaintId, int consumerId);
        Complaint GetComplaintDetails(int complaintId, int consumerId);
        Attachment GetAttachmentForDownload(int attachmentId, int consumerId);
        string GetPhysicalPath(Attachment attachment);
        void UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo, int actorId, int roleId);

        void DeleteAttachment(int attachmentId, int consumerId);
        // Interface additions
        OfficerDashboardViewModel GetOfficerDashboard(int officerId, int roleId);
        MyComplaintsViewModel GetOfficerComplaints(int officerId, OfficerComplaintsQuery query);
        //Complaint GetAssignedComplaint(int officerId, int complaintId);
        int SaveComplaint(FileComplaintViewModel vm, int consumerId, int roleId);              // upsert — Complaint.ComplaintId null/0 = create
        void DeleteComplaint(int complaintId, int consumerId);
        MyComplaintsViewModel Search(int consumerId, ComplaintListFilterViewModel filter);
        ComplaintExportViewModel GetComplaintForExport(int complaintId, int consumerId);

        // Resolution-confirmation workflow
        void ResolveComplaint(int complaintId, int officerId);
        void ConfirmResolution(int complaintId, int consumerId);
        void RejectResolution(int complaintId, int consumerId, string reason);
    }
}
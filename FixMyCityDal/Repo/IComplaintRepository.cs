using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System.Collections.Generic;

namespace FixMyCity.Repository
{
    public interface IComplaintRepository
    {
        List<Complaint> GetComplaints(int? complaintId, int? consumerId, int roleId);
        int Create(Complaint complaint);
        Complaint GetById(int complaintId, int consumerId);
        List<ComplaintCategory> GetCategories();
        List<ComplaintPriority> GetPriorities();
        int CreateAttachment(int complaintId, string fileName, string contentType, long fileSizeBytes, int uploadedBy);
        List<Attachment> GetAttachmentsByComplaintId(int complaintId, int consumerId);
        Attachment GetAttachmentById(int attachmentId, int consumerId);
        void DeleteAttachment(int attachmentId, int consumerId);
        // Interface additions
        int SaveComplaint(Complaint complaint,int roleId,int consumerId);
        // upsert — Complaint.ComplaintId null/0 = create
        bool UpdateComplaint(int complaintId, int categoryId, int priorityId, int statusId, int? assignedTo, int actorId, int roleId);


        bool DeleteComplaint(int complaintId, int consumerId);
        ComplaintSearchResult Search(int consumerId, ComplaintListFilterViewModel filter); List<ComplaintStatus> GetStatuses();
        List<Complaint> GetAssignedByOfficerId(int officerId);
        //Complaint GetAssignedComplaintById(int complaintId, int officerId);
    }
}
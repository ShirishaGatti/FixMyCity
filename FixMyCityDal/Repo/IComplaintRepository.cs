using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System.Collections.Generic;

namespace FixMyCity.Repository
{
    public interface IComplaintRepository
    {
        List<Complaint> GetByConsumerId(int consumerId);
        Complaint GetById(int complaintId, int consumerId);
        int Create(Complaint complaint);
        List<ComplaintCategory> GetCategories();
        List<ComplaintPriority> GetPriorities();
        int CreateAttachment(int complaintId, string fileName, string contentType, long fileSizeBytes, int uploadedBy);
        List<Attachment> GetAttachmentsByComplaintId(int complaintId, int consumerId);
        Attachment GetAttachmentById(int attachmentId, int consumerId);
        void DeleteAttachment(int attachmentId, int consumerId);
        // Interface additions
        int SaveComplaint(Complaint complaint,int roleId);              // upsert — Complaint.ComplaintId null/0 = create
        bool DeleteComplaint(int complaintId, int consumerId);
        ComplaintSearchResult Search(int consumerId, ComplaintListFilterViewModel filter); List<ComplaintStatus> GetStatuses();
    }
}
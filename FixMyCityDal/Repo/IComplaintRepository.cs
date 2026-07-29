using FixMyCityModel.Model;
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
    }
}
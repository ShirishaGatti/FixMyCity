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
    }
}
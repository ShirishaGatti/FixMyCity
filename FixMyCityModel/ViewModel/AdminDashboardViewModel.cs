using FixMyCityModel.Model;
using System.Collections.Generic;

namespace FixMyCityModel.ViewModel
{
    // Admin landing page: aggregate KPIs shown on cards + top categories mini-chart.
    // AdminRepository fills these fields in one Admin_GetDashboardStats sproc call.
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCitizens { get; set; }
        public int TotalOfficers { get; set; }
        public int TotalAdmins { get; set; }

        public int TotalComplaints { get; set; }
        public int OpenComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int ClosedComplaints { get; set; }

        public int TotalCities { get; set; }
        public int TotalWards { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalCategories { get; set; }

        // { CategoryName -> Count } for the top-5 categories chart on the dashboard.
        public List<CategoryCount> TopCategories { get; set; } = new List<CategoryCount>();

        // Last 7 recent complaints for the activity feed.
        public List<Complaint> RecentComplaints { get; set; } = new List<Complaint>();
    }

    public class CategoryCount
    {
        public string CategoryName { get; set; }
        public int Count { get; set; }
    }
}

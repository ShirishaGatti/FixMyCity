using FixMyCityModel.Model;
using System;
using System.Collections.Generic;

namespace FixMyCityModel.ViewModel
{
    // Admin-view row for a complaint. Names denormalised for display.
    public class AdminComplaintRow
    {
        public int ComplaintId { get; set; }
        public string ComplaintNumber { get; set; }
        public string Title { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int PriorityId { get; set; }
        public string PriorityName { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public string RaisedByName { get; set; }
        public string AssigneeName { get; set; }
        public int? AssignedTo { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public int WardId { get; set; }
        public string WardName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminComplaintListFilterViewModel
    {
        public int? CategoryId { get; set; }
        public int? CityId { get; set; }
        public int? WardId { get; set; }

        // "ComplaintId", "CreatedAt", "CategoryName", "StatusName", "PriorityName"
        public string SortBy { get; set; } = "ComplaintId";
        public string SortDir { get; set; } = "DESC";

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class AdminComplaintListViewModel
    {
        public List<AdminComplaintRow> Rows { get; set; } = new List<AdminComplaintRow>();
        public int TotalCount { get; set; }
        public AdminComplaintListFilterViewModel Filter { get; set; } = new AdminComplaintListFilterViewModel();

        public List<ComplaintCategory> Categories { get; set; } = new List<ComplaintCategory>();
        public List<ComplaintPriority> Priorities { get; set; } = new List<ComplaintPriority>();
        public List<ComplaintStatus> Statuses { get; set; } = new List<ComplaintStatus>();
        public List<City> Cities { get; set; } = new List<City>();
        public List<Ward> Wards { get; set; } = new List<Ward>();
    }
}

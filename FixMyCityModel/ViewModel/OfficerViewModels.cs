using FixMyCityModel.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FixMyCityModel.ViewModel
{
    public class OfficerDashboardViewModel : BaseViewModel
    {
        public int TotalAssigned { get; set; }
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ClosedCount { get; set; }
        public int TodayCount { get; set; }
        public int WeeklyCount { get; set; }
        public int MonthlyCount { get; set; }
        public List<ValueCount> PriorityBreakdown { get; set; } = new List<ValueCount>();
        public List<Complaint> RecentComplaints { get; set; } = new List<Complaint>();
    }

    public class OfficerComplaintsQuery
    {
        public string SearchTerm { get; set; }
        public int? StatusId { get; set; }
        public int? PriorityId { get; set; }
        public int? CategoryId { get; set; }
        public string SortColumn { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class OfficerComplaintsViewModel : BaseViewModel
    {
        public string SearchTerm { get; set; }
        public int? StatusId { get; set; }
        public int? PriorityId { get; set; }
        public int? CategoryId { get; set; }
        public string SortColumn { get; set; }
        public string SortDirection { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public List<Complaint> AssignedComplaints { get; set; } = new List<Complaint>();
        public List<ComplaintStatus> Statuses { get; set; } = new List<ComplaintStatus>();
        public List<ComplaintCategory> Categories { get; set; } = new List<ComplaintCategory>();
        public List<ComplaintPriority> Priorities { get; set; } = new List<ComplaintPriority>();
    }

    public class OfficerComplaintUpdateViewModel
    {
        public int ComplaintId { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public int PriorityId { get; set; }
        [Required]
        public int StatusId { get; set; }
        public string Remarks { get; set; }
        public string ResolutionNotes { get; set; }
    }

    public class ValueCount
    {
        public string Label { get; set; }
        public int Count { get; set; }
    }
}

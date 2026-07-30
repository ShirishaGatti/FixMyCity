using FixMyCityModel.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixMyCityModel.ViewModel
{
    // New: ComplaintListFilterViewModel.cs
    public class ComplaintListFilterViewModel
    {
        public string Title { get; set; }
        public int? CategoryId { get; set; }
        public int? StatusId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string SortField { get; set; } = "CreatedAt";
        public string SortDirection { get; set; } = "DESC";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class ComplaintSearchResult
    {
        public List<Complaint> Complaints { get; set; }
        public int TotalCount { get; set; }
    }
}

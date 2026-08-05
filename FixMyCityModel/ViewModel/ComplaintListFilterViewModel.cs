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

        public bool ValidateDates(out string errorMessage)
        {
            errorMessage = null;
            if (DateFrom.HasValue && DateFrom.Value.Date > DateTime.Today)
            {
                errorMessage = "From Date cannot be a future date.";
                DateFrom = DateTime.Today;
                return false;
            }
            if (DateFrom.HasValue && DateTo.HasValue && DateFrom.Value.Date > DateTo.Value.Date)
            {
                errorMessage = "From Date must be on or before To Date.";
                DateTo = DateFrom;
                return false;
            }
            return true;
        }
    }
    public class ComplaintSearchResult
    {
        public List<Complaint> Complaints { get; set; }
        public int TotalCount { get; set; }
    }
}

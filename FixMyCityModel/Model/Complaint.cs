using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FixMyCityModel.Model
{
    // Maps Complaint.Complaint table
    public class Complaint
    {
        public int ComplaintId { get; set; }
        public string ComplaintNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public int PriorityId { get; set; }
        public int StatusId { get; set; }
        public int RaisedBy { get; set; }
        public int? AssignedTo { get; set; }
        public string AddressLine { get; set; }
        public string Landmark { get; set; }
        public int WardId { get; set; }
        public int CityId { get; set; }
        public DateTime? ResolvedDate { get; set; }
        public DateTime? ClosedDate { get; set; }
        public int ReopenCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Denormalised for display — populated by SP joins, not tracked by ORM
        public string CategoryName { get; set; }
        public string PriorityName { get; set; }
        public string StatusName { get; set; }
        public string WardName { get; set; }
        public string CityName { get; set; }
        public string AssignedName { get; set; }

        // Resolution-confirmation workflow — computed, not stored.
        // The citizen has 7 days from ResolvedDate to confirm/reject
        // before Complaint_AutoExpireResolutions closes it for them.
        public bool IsAwaitingConfirmation =>
            string.Equals(StatusName, "Awaiting Customer Confirmation", StringComparison.OrdinalIgnoreCase);

        public DateTime? ResolutionDeadlineUtc => ResolvedDate?.AddDays(7);

        public int? DaysLeftToRespond =>
            ResolutionDeadlineUtc.HasValue
                ? (int?)Math.Max(0, (int)Math.Ceiling((ResolutionDeadlineUtc.Value - DateTime.UtcNow).TotalDays))
                : null;
    }
}

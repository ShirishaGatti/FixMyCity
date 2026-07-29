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
        public string AssigneeName { get; set; }
    }
}

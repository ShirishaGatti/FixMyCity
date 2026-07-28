using System;

namespace FixMyCity.Model
{
    // Maps Complaint.Consumer
    // "Consumer" here is the single user table for the whole system —
    // citizens, support executives, and admins are all rows here,
    // distinguished by RoleId (and DeptId when relevant).
    public class Consumer
    {
        public int ConsumerId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }
        public DateTime? DOB { get; set; }
        public string AddressLine { get; set; }
        public int? CityId { get; set; }
        public int? WardId { get; set; }
        public int RoleId { get; set; }
        public int? DeptId { get; set; }          // relevant for SupportExecutive / Admin
        public string Designation { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}

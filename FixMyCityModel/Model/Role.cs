using System;

namespace FixMyCity.Model
{
    // Maps Complaint.Role
    // Seed rows expected: 1 = Citizen, 2 = SupportExecutive, 3 = Admin
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public bool IsActive { get; set; }
    }
}

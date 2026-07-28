using System;

namespace FixMyCity.Model
{
    // Maps Complaint.RefreshToken (see SQL/Auth_Schema_Additions.sql —
    // this table is NOT in the original complaints.sql and had to be added,
    // same shape as the rotating-refresh design already used in Foodies).
    public class RefreshToken
    {
        public int Id { get; set; }
        public string TokenHash { get; set; }
        public int ConsumerId { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public bool RememberMe { get; set; }
        public DateTime TrustExpiresAt { get; set; }
    }
}

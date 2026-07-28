namespace FixMyCity.Infrastructure
{
    // Keep in sync with the seeded rows in Complaint.Role.
    // Centralising these avoids magic numbers scattered across
    // [RoleAuthorize(...)] attributes and controller switches.
    public static class RoleIds
    {
        public const int Citizen = 2;
        public const int SupportExecutive = 3;
        public const int Admin = 1;
    }
}

using System;

namespace FixMyCityModel.Model
{
    // Maps Complaint.ConsumerCredential
    // NOTE: unlike a design with a separate OTP table, this schema keeps
    // OTP state (OTPHash / ValidTill / IsUsed / AttemptCount) on the SAME
    // row as the password hash — one row per Consumer. The repository
    // updates just the OTP columns for OTP flows and just the password /
    // lockout columns for login flows.
    public class ConsumerCredential
    {
        public int ConsumerCredId { get; set; }
        public int ConsumerId { get; set; }

        public byte[] PassHash { get; set; }
        public byte[] PassSalt { get; set; }

        public int FailedLoginCount { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime PassChangedAt { get; set; }

        public byte[] OTPHash { get; set; }
        public DateTime ValidTill { get; set; }
        public bool IsUsed { get; set; }
        public int AttemptCount { get; set; }
        public DateTime OtpCreatedDate { get; set; }

        public bool IsActive { get; set; }

        // Convenience — populated by the repo on login look-ups so the
        // service layer doesn't need a second round-trip to Consumer.
        public int RoleId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
    }
}

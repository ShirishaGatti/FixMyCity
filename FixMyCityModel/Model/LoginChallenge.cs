using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FixMyCity.Model
    {
        public class LoginChallenge
        {
            public int ChallengeId { get; set; }
            public string TokenHash { get; set; }      // SHA-256 hex of the pendingToken
            public string OtpHash { get; set; }        // SHA-256 hex of the OTP
            public int ConsumerId { get; set; }
            public string Purpose { get; set; }        // "LOGIN"
            public bool RememberMe { get; set; }
            public byte AttemptCount { get; set; }
            public DateTime ExpiresAt { get; set; }    // UTC
            public DateTime? ConsumedAt { get; set; }  // UTC
            public DateTime CreatedAt { get; set; }    // UTC
        }

        public class LoginChallengeResult
        {
            public string PendingToken { get; set; }
            public string Otp { get; set; }
        }

        public class VerifyOtpRequest
        {
            [Required]
            public string PendingToken { get; set; }

            [Required]
            [StringLength(6, MinimumLength = 6)]
            public string EnteredOtp { get; set; }
        }
    }


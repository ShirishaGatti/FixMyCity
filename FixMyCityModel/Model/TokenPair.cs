using System;

namespace FixMyCityModel.Model
{
    // Returned by the service after a successful login/refresh.
    // RefreshToken here is the RAW value — only ever exists in memory
    // long enough to be written into the cookie; the DB only ever
    // stores its hash.
    public class TokenPair
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ConsumerId { get; set; }
        public string Email { get; set; }
        public int RoleId { get; set; }
        public bool RememberMe { get; set; }
        public DateTime TrustExpiresAt { get; set; }
    }
}

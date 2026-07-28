using FixMyCity.Infrastructure;
using FixMyCity.Exceptions;
using FixMyCity.Model;
using FixMyCity.Repository;

using FixMyCity.ViewModel;
using FixMyCityModel;
using FixMyCityModel.ViewModel;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FixMyCity.Service
{
    // Business rules only — zero knowledge of Session/Cookies/HttpContext.
    // Controller owns all of that; this class is unit-testable in isolation.
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repo;

        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;
        private const int OtpExpiryMinutes = 5;
        private const int MaxOtpAttempts = 5;
        private const int RememberMeDays = 30;

        // NOTE on password hashing: the SQL schema stores PassHash and
        // PassSalt as two separate VARBINARY columns rather than one
        // self-salting BCrypt string, so this uses PBKDF2 (Rfc2898) with
        // a random 128-bit salt and 100k iterations to match that shape.
        // If you'd rather standardise on BCrypt like the rest of the
        // Foodies codebase, drop PassSalt and store BCrypt's own string
        // output in PassHash instead — flagging this as a deliberate
        // deviation, not an oversight.
        private const int Pbkdf2Iterations = 100000;
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;

        public AuthService() : this(new AuthRepository()) { }

        public AuthService(IAuthRepository repo)
        {
            _repo = repo;
        }

        // ===========================
        // Register (public, self-service — Citizen only)
        // ===========================
        public int Register(RegisterViewModel vm)
        {
            ValidateName(vm.Name);
            ValidateEmail(vm.Email);
            ValidateNewPassword(vm.Password);

            if (string.IsNullOrWhiteSpace(vm.Contact))
                throw new BusinessException("Contact number is required.", "CONTACT_REQUIRED");

            // Self-registration is restricted to Citizen; SupportExecutive/Admin
            // accounts go through RegisterStaff() instead, which is only reachable
            // from an endpoint gated by [RoleAuthorize(RoleIds.Admin)].
            vm.RoleId = RoleIds.Citizen;
            vm.DepartmentId = null;
            vm.Designation = null;

            if (EmailExists(vm.Email))
                throw new BusinessException("An account with this email already exists.", "EMAIL_EXISTS");

            byte[] salt = GenerateSalt();
            byte[] hash = HashPassword(vm.Password, salt);

            return _repo.Register(vm, hash, salt);
        }

        // ===========================
        // Register (Admin-only — SupportExecutive / Admin accounts)
        // ===========================
        // Deliberately a separate method rather than a "which role" flag on
        // Register(), for two reasons:
        //   1. RegisterViewModel and StaffRegisterViewModel are different
        //      shapes (StaffRegisterViewModel legitimately exposes RoleId/
        //      DeptId/Designation as bindable — RegisterViewModel must not).
        //   2. It keeps the public, anonymous Register() path simple to audit:
        //      there is no code path in it that can ever produce anything but
        //      a Citizen, no matter what a caller posts.
        // The repository call is reused because the underlying insert
        // (Auth_Register sproc) doesn't care which controller action got it
        // here — only the validation rules above it differ.
        public int RegisterStaff(StaffRegisterViewModel vm)
        {
            ValidateName(vm.Name);
            ValidateEmail(vm.Email);
            ValidateNewPassword(vm.Password);

            if (string.IsNullOrWhiteSpace(vm.Contact))
                throw new BusinessException("Contact number is required.", "CONTACT_REQUIRED");

            if (vm.RoleId != RoleIds.SupportExecutive && vm.RoleId != RoleIds.Admin)
                throw new BusinessException("Role must be Support Executive or Admin.", "INVALID_ROLE");

            if (vm.DeptId <= 0)
                throw new BusinessException("Department is required for staff accounts.", "DEPT_REQUIRED");

            if (EmailExists(vm.Email))
                throw new BusinessException("An account with this email already exists.", "EMAIL_EXISTS");

            byte[] salt = GenerateSalt();
            byte[] hash = HashPassword(vm.Password, salt);

            var registerVm = new RegisterViewModel
            {
                Name = vm.Name,
                Email = vm.Email,
                Password = vm.Password,
                Contact = vm.Contact,
                RoleId = vm.RoleId,
                DepartmentId = vm.DeptId,
                Designation = vm.Designation
            };

            return _repo.Register(registerVm, hash, salt);
        }

        public bool EmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                ValidateEmail(email);
                return _repo.GetCredentialByEmail(email) != null;
            }
            catch
            {
                return false;
            }
        }

        // Used by forgot-password: looks a consumer up by email WITHOUT
        // touching the password/lockout logic in Login(). Deliberately
        // returns null instead of throwing on a not-found email — callers
        // must not let this be used to enumerate registered accounts.
        public int? GetConsumerIdByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                return null;

            var cred = _repo.GetCredentialByEmail(email);
            return cred?.ConsumerId;
        }

        // ===========================
        // Login (password step)
        // ===========================
        public LoginViewModel Login(LoginViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Email) || string.IsNullOrWhiteSpace(vm.Password))
                throw new BusinessException("Email and password are required.", "MISSING_CREDENTIALS");

            var cred = _repo.GetCredentialByEmail(vm.Email);
            if (cred == null)
                throw new BusinessException("User does not exist. Kindly sign up.", "USER_NOT_FOUND");

            if (!cred.IsActive)
                throw new BusinessException("Your account has been deactivated.", "ACCOUNT_INACTIVE");

            if (cred.IsLocked)
            {
                if (cred.LockedUntil.HasValue && cred.LockedUntil.Value > DateTime.UtcNow)
                {
                    throw new BusinessException(
                        string.Format("Account locked. Try again after {0:HH:mm} UTC.", cred.LockedUntil.Value),
                        "ACCOUNT_LOCKED");
                }

                // Lock window has passed — clear it before checking the password.
                _repo.UpdateLoginState(cred.ConsumerCredId, 0, false, null, cred.LastLoginAt);
                cred.IsLocked = false;
                cred.FailedLoginCount = 0;
            }

            bool passwordOk = VerifyPassword(vm.Password, cred.PassHash, cred.PassSalt);

            if (!passwordOk)
            {
                int newFails = cred.FailedLoginCount + 1;
                bool lockNow = newFails >= MaxFailedAttempts;

                _repo.UpdateLoginState(
                    cred.ConsumerCredId,
                    newFails,
                    lockNow,
                    lockNow ? DateTime.UtcNow.AddMinutes(LockoutMinutes) : (DateTime?)null,
                    cred.LastLoginAt);

                string msg = lockNow
                    ? string.Format("Too many failed attempts. Account locked for {0} minutes.", LockoutMinutes)
                    : "Invalid email or password.";

                throw new BusinessException(msg, "INVALID_CREDENTIALS");
            }

            // Success — reset failure counter, stamp last login.
            _repo.UpdateLoginState(cred.ConsumerCredId, 0, false, null, DateTime.UtcNow);

            return new LoginViewModel
            {
                Success = true,
                ConsumerId = cred.ConsumerId,
                RoleId = cred.RoleId,
                Name = cred.Name
            };
        }

        // ===========================
        // OTP (second factor after password)
        // ===========================
        public string CreateOtp(int consumerId, string purpose)
        {
            if (consumerId <= 0)
                throw new BusinessException("Invalid account.", "INVALID_CONSUMER");

            string otp = GenerateOtp();
            byte[] otpHash = HashOtp(otp);
            DateTime validTill = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);

            _repo.SetOtp(consumerId, otpHash, validTill);

            return otp; // raw OTP only ever returned here, to be emailed — never persisted raw
        }

        public void ValidateOtp(int consumerId, string enteredOtp, string purpose)
        {
            if (consumerId <= 0)
                throw new BusinessException("Session expired. Please log in again.", "SESSION_EXPIRED");

            if (string.IsNullOrWhiteSpace(enteredOtp) || !Regex.IsMatch(enteredOtp, @"^\d{6}$"))
                throw new BusinessException("Enter a valid 6-digit OTP.", "INVALID_OTP_FORMAT");

            var state = _repo.GetOtpState(consumerId);
            if (state == null || state.OTPHash == null)
                throw new BusinessException("No active OTP found. Please request a new one.", "OTP_NOT_FOUND");

            if (state.IsUsed)
                throw new BusinessException("This OTP has already been used. Please request a new one.", "OTP_ALREADY_USED");

            if (state.AttemptCount >= MaxOtpAttempts)
                throw new BusinessException("Too many failed attempts. Please request a new OTP.", "MAX_ATTEMPTS");

            if (DateTime.UtcNow > state.ValidTill)
                throw new BusinessException("OTP expired. Please request a new one.", "OTP_EXPIRED");

            byte[] enteredHash = HashOtp(enteredOtp);

            if (!ConstantTimeEquals(enteredHash, state.OTPHash))
            {
                _repo.IncrementOtpAttempts(consumerId);
                throw new BusinessException("Invalid OTP.", "INVALID_OTP");
            }

            _repo.MarkOtpUsed(consumerId);
        }

        // ===========================
        // Password reset
        // ===========================
        public void ResetPassword(int consumerId, string newPassword, string confirmPassword)
        {
            if (consumerId <= 0)
                throw new BusinessException("Session expired. Please request a new code.", "SESSION_EXPIRED");

            ValidateNewPassword(newPassword);

            if (newPassword != confirmPassword)
                throw new BusinessException("Passwords do not match.", "PASSWORD_MISMATCH");

            byte[] salt = GenerateSalt();
            byte[] hash = HashPassword(newPassword, salt);
            _repo.UpdatePassword(consumerId, hash, salt);
        }

        // ===========================
        // Rotating JWT refresh tokens
        // ===========================
        public TokenPair IssueTokenPair(int consumerId, int roleId, string email, bool rememberMe)
        {
            string accessToken = JwtHelper.GenerateToken(consumerId, roleId, email);
            string rawRefresh = JwtHelper.GenerateRefreshToken();
            string refreshHash = JwtHelper.HashToken(rawRefresh);

            DateTime now = DateTime.UtcNow;
            DateTime expiresAt = now.AddDays(JwtHelper.GetRefreshExpiryDays()); // normal rotation cycle
            DateTime trustExpiresAt = rememberMe ? now.AddDays(RememberMeDays) : expiresAt;

            _repo.CreateRefreshToken(refreshHash, consumerId, email, roleId, expiresAt, rememberMe, trustExpiresAt);

            return new TokenPair
            {
                AccessToken = accessToken,
                RefreshToken = rawRefresh,
                ConsumerId = consumerId,
                Email = email,
                RoleId = roleId,
                RememberMe = rememberMe,
                TrustExpiresAt = trustExpiresAt
            };
        }

        public TokenPair TryRefresh(string rawRefreshToken)
        {
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
                return null;

            string hash = JwtHelper.HashToken(rawRefreshToken);
            var stored = _repo.GetRefreshToken(hash);

            if (stored == null)
                return null;

            // Reuse detection: a REVOKED token being presented again means
            // either a replay of a stale token or a stolen one — nuke every
            // session for this account either way.
            if (stored.IsRevoked)
            {
                _repo.RevokeAllRefreshTokens(stored.ConsumerId);
                return null;
            }

            if (DateTime.UtcNow > stored.ExpiresAt)
                return null;

            // Remember-me gate — independent of the 7-day rotation-cycle
            // expiry above. This is what forces a fresh login + OTP once
            // the 7-day (or 30-day, if remembered) trust window is over,
            // even though rotation kept extending ExpiresAt in the meantime.
            if (DateTime.UtcNow > stored.TrustExpiresAt)
            {
                _repo.RevokeAllRefreshTokens(stored.ConsumerId);
                return null;
            }

            string newRawRefresh = JwtHelper.GenerateRefreshToken();
            string newRefreshHash = JwtHelper.HashToken(newRawRefresh);
            DateTime newExpiry = DateTime.UtcNow.AddDays(JwtHelper.GetRefreshExpiryDays());

            // TrustExpiresAt is carried forward as-is, never recalculated —
            // otherwise "remember me for 30 days" would silently become
            // "30 more days on every visit" and never truly expire.
            _repo.RotateRefreshToken(hash, newRefreshHash, stored.ConsumerId, stored.Email, stored.RoleId,
                                      newExpiry, stored.RememberMe, stored.TrustExpiresAt);

            string newAccessToken = JwtHelper.GenerateToken(stored.ConsumerId, stored.RoleId, stored.Email);

            return new TokenPair
            {
                AccessToken = newAccessToken,
                RefreshToken = newRawRefresh,
                ConsumerId = stored.ConsumerId,
                Email = stored.Email,
                RoleId = stored.RoleId,
                RememberMe = stored.RememberMe,
                TrustExpiresAt = stored.TrustExpiresAt
            };
        }

        public void RevokeByRawRefreshToken(string rawRefreshToken)
        {
            if (string.IsNullOrEmpty(rawRefreshToken)) return;

            string hash = JwtHelper.HashToken(rawRefreshToken);
            var stored = _repo.GetRefreshToken(hash);
            if (stored != null)
                _repo.RevokeAllRefreshTokens(stored.ConsumerId);
        }

        // ===========================
        // Shared validation helpers
        // ===========================
        // Pulled out of Register()/RegisterStaff() so the two entry points
        // can't quietly drift apart on what "valid" means.
        private void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 3)
                throw new BusinessException("Name must contain at least 3 characters.", "INVALID_NAME");
        }

        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                throw new BusinessException("Invalid email address.", "INVALID_EMAIL");
        }

        private void ValidateNewPassword(string password)
        {
            if (!IsValidPassword(password))
            {
                throw new BusinessException(
                    "Password must contain uppercase, lowercase, digit, special character and be at least 8 characters.",
                    "WEAK_PASSWORD");
            }
        }

        // ===========================
        // Crypto / low-level helpers
        // ===========================
        private byte[] GenerateSalt()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] salt = new byte[SaltSizeBytes];
                rng.GetBytes(salt);
                return salt;
            }
        }

        private byte[] HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt))
            {
                pbkdf2.IterationCount = Pbkdf2Iterations;
                return pbkdf2.GetBytes(HashSizeBytes);
            }
        }

        private bool VerifyPassword(string password, byte[] hash, byte[] salt)
        {
            if (hash == null || salt == null) return false;
            byte[] computed = HashPassword(password, salt);
            return ConstantTimeEquals(computed, hash);
        }

        private string GenerateOtp()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                rng.GetBytes(bytes);
                int value = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF;
                return (100000 + (value % 900000)).ToString();
            }
        }

        private byte[] HashOtp(string otp)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(otp));
            }
        }

        private bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];

            return result == 0;
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }

        private bool IsValidPassword(string password)
        {
            return !string.IsNullOrEmpty(password) &&
                   Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&^#()_\-+=]).{8,}$");
        }
    }
}

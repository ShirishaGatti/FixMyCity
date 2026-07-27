using System;
using System.Security.Cryptography;
using ComplaintSystem.Entities;
using ComplaintSystem.Repository;
using ComplaintSystem.Security;
using ComplaintSystem.ViewModels;
using ComplaintSystem.DAL;

namespace ComplaintSystem.Services
{
    public interface IAuthService
    {
        AuthResultViewModel Register(RegisterViewModel model);
        AuthResultViewModel Login(LoginViewModel model);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ApplicationDbContext _context;

        private const int DefaultEmployeeRoleId = 3; // seeded in Role table: 1=Admin, 2=SupportExecutive, 3=Employee
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 15;

        public AuthService(IUserRepository userRepository, ApplicationDbContext context)
        {
            _userRepository = userRepository;
            _context = context;
        }

        public AuthResultViewModel Register(RegisterViewModel model)
        {
            if (_userRepository.EmailExists(model.Email))
            {
                return new AuthResultViewModel { Success = false, Message = "Email is already registered." };
            }

            byte[] salt = GenerateSalt();
            byte[] hash = HashPassword(model.Password, salt);

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                Contact = model.Contact,
                RoleId = DefaultEmployeeRoleId,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _userRepository.Add(user);
            _userRepository.Save(); // SaveChanges - now user.UserId is populated by EF

            var credential = new UserCredential
            {
                UserId = user.UserId,
                PassHash = hash,
                PassSalt = salt,
                PassChangedAt = DateTime.UtcNow
            };

            _context.UserCredentials.Add(credential);
            _context.SaveChanges();

            return new AuthResultViewModel { Success = true, Message = "Registration successful. Please login." };
        }

        public AuthResultViewModel Login(LoginViewModel model)
        {
            var user = _userRepository.GetByEmail(model.Email);

            if (user == null || user.UserCredential == null)
            {
                return new AuthResultViewModel { Success = false, Message = "Invalid email or password." };
            }

            if (!user.IsActive)
            {
                return new AuthResultViewModel { Success = false, Message = "Your account has been deactivated. Contact Admin." };
            }

            var cred = user.UserCredential;

            if (cred.IsLocked && cred.LockedUntil.HasValue && cred.LockedUntil.Value > DateTime.UtcNow)
            {
                return new AuthResultViewModel { Success = false, Message = $"Account locked. Try again after {cred.LockedUntil.Value:HH:mm}." };
            }

            bool passwordValid = VerifyPassword(model.Password, cred.PassHash, cred.PassSalt);

            if (!passwordValid)
            {
                cred.FailedLoginCount++;
                if (cred.FailedLoginCount >= MaxFailedAttempts)
                {
                    cred.IsLocked = true;
                    cred.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                }
                _context.SaveChanges();
                return new AuthResultViewModel { Success = false, Message = "Invalid email or password." };
            }

            // successful login - reset counters
            cred.FailedLoginCount = 0;
            cred.IsLocked = false;
            cred.LockedUntil = null;
            cred.LastLoginAt = DateTime.UtcNow;
            _context.SaveChanges();

            string token = JwtTokenHelper.GenerateToken(user.UserId, user.Email, user.Role.RoleName);

            return new AuthResultViewModel
            {
                Success = true,
                Message = "Login successful.",
                Token = token,
                Role = user.Role.RoleName
            };
        }

        private static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        private static byte[] HashPassword(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000))
            {
                return pbkdf2.GetBytes(32); // 256-bit hash
            }
        }

        private static bool VerifyPassword(string password, byte[] storedHash, byte[] storedSalt)
        {
            byte[] computedHash = HashPassword(password, storedSalt);
            if (computedHash.Length != storedHash.Length) return false;

            int diff = 0;
            for (int i = 0; i < computedHash.Length; i++)
                diff |= computedHash[i] ^ storedHash[i];

            return diff == 0;
        }
    }
}
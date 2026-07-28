
using FixMyCity.Model;
using FixMyCity.ViewModel;
using FixMyCityModel;
using FixMyCityModel.ViewModel;

namespace FixMyCity.Service
{
    public interface IAuthService
    {
        int Register(RegisterViewModel vm);

        // Admin-only provisioning path — see AuthService.RegisterStaff for why
        // this is a separate method rather than a flag on Register().
        int RegisterStaff(StaffRegisterViewModel vm);

        LoginViewModel Login(LoginViewModel vm);
        bool EmailExists(string email);
        int? GetConsumerIdByEmail(string email);

        string CreateOtp(int consumerId, string purpose);
        void ValidateOtp(int consumerId, string enteredOtp, string purpose);

        void ResetPassword(int consumerId, string newPassword, string confirmPassword);

        TokenPair IssueTokenPair(int consumerId, int roleId, string email, bool rememberMe);
        TokenPair TryRefresh(string rawRefreshToken);
        void RevokeByRawRefreshToken(string rawRefreshToken);
    }
}

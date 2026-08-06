
using FixMyCityModel;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System.Collections.Generic;

namespace FixMyCity.Service
{
    public interface IAuthService
    {
        int Register(RegisterViewModel vm);

        LoginViewModel Login(LoginViewModel vm);
        bool EmailExists(string email);
        int? GetConsumerIdByEmail(string email);

        string CreateOtp(int consumerId, string purpose);
        void ValidateOtp(int consumerId, string enteredOtp, string purpose);

        void ResetPassword(int consumerId, string newPassword, string confirmPassword);

        TokenPair IssueTokenPair(int consumerId, int roleId, string email, bool rememberMe);
        TokenPair TryRefresh(string rawRefreshToken);
        void RevokeByRawRefreshToken(string rawRefreshToken);
        List<City> GetCities();

        List<Ward> GetWardsByCity(int cityId);
    }
}

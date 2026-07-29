using FixMyCityModel;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using System;
using System.Collections.Generic;

namespace FixMyCity.Repository
{
    public interface IAuthRepository
    {
        int Register(RegisterViewModel vm, byte[] passHash, byte[] passSalt);
        ConsumerCredential GetCredentialByEmail(string email);
        Consumer GetConsumerById(int consumerId);
        bool UpdateConsumerProfile(int consumerId, string name, string contact, DateTime? dob, string addressLine, int? cityId, int? wardId, string designation);

        List<City> GetCities();
        List<Ward> GetWardsByCity(int cityId);

        bool UpdateLoginState(int consumerCredId, int failedCount, bool isLocked,
                               DateTime? lockedUntil, DateTime? lastLoginAt);

        bool UpdatePassword(int consumerId, byte[] passHash, byte[] passSalt);

        bool SetOtp(int consumerId, byte[] otpHash, DateTime validTill);
        ConsumerCredential GetOtpState(int consumerId);
        bool IncrementOtpAttempts(int consumerId);
        bool MarkOtpUsed(int consumerId);

        bool CreateRefreshToken(string tokenHash, int consumerId, string email, int roleId,
                                 DateTime expiresAt, bool rememberMe, DateTime trustExpiresAt);
        RefreshToken GetRefreshToken(string tokenHash);
        bool RotateRefreshToken(string oldHash, string newHash, int consumerId, string email, int roleId,
                                 DateTime expiresAt, bool rememberMe, DateTime trustExpiresAt);
        bool RevokeAllRefreshTokens(int consumerId);
    }
}

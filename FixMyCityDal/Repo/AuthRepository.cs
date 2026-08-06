using FixMyCity.Exceptions;
using FixMyCityModel;
using FixMyCityModel.Model;
using FixMyCityModel.ViewModel;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace FixMyCity.Repository
{
    /// <summary>
    /// Data-access class for Auth (Consumer credentials, OTP, refresh tokens).
    /// Follows the same Enterprise Library Data Application Block pattern as
    /// the Tags business class: Database field from DatabaseFactory,
    /// GetStoredProcCommand + AddInParameter/AddOutParameter, ExecuteDataSet/
    /// ExecuteNonQuery, try/catch swallowing to a bool/null return.
    ///
    /// Requires in Web.config:
    ///   <connectionStrings>
    ///     <add name="ComplaintDb" connectionString="..." providerName="System.Data.SqlClient" />
    ///   </connectionStrings>
    ///   <dataConfiguration defaultDatabase="ComplaintDb" />
    /// </summary>
    public class AuthRepository : IAuthRepository
    {
        private readonly Database db;

        public AuthRepository()
        {
            db = DatabaseFactory.CreateDatabase();
        }


        /// <summary>
        /// Inserts a new Consumer + ConsumerCredential row for Register.
        /// Saves newly created Id in the returned int.
        /// </summary>
        /// <returns>New ConsumerId if Insert operation is successful; Else 0.</returns>
        public int Register(RegisterViewModel vm, byte[] passHash, byte[] passSalt)
        {
            int newConsumerId = 0;
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.Auth_Register");
                this.db.AddOutParameter(com, "NewConsumerId", DbType.Int32, 4);

                // Nullable string/int parameters collapsed to one-liners.
                // Model validation already enforced required fields; the
                // repository's only job here is correct DB type mapping.
                this.db.AddInParameter(com, "Name",
                    DbType.String, string.IsNullOrEmpty(vm.Name) ? (object)DBNull.Value : vm.Name);
                this.db.AddInParameter(com, "Email",
                    DbType.String, string.IsNullOrEmpty(vm.Email) ? (object)DBNull.Value : vm.Email);
                this.db.AddInParameter(com, "Contact",
                    DbType.String, string.IsNullOrEmpty(vm.Contact) ? (object)DBNull.Value : vm.Contact);
                this.db.AddInParameter(com, "DOB",
                    DbType.DateTime, vm.DOB.HasValue ? (object)vm.DOB.Value : DBNull.Value);
                this.db.AddInParameter(com, "AddressLine",
                    DbType.String, string.IsNullOrEmpty(vm.AddressLine) ? (object)DBNull.Value : vm.AddressLine);
                this.db.AddInParameter(com, "CityId",
                    DbType.Int32, (vm.CityId.HasValue && vm.CityId.Value > 0) ? (object)vm.CityId.Value : DBNull.Value);
                this.db.AddInParameter(com, "WardId",
                    DbType.Int32, (vm.WardId.HasValue && vm.WardId.Value > 0) ? (object)vm.WardId.Value : DBNull.Value);
                this.db.AddInParameter(com, "RoleId", DbType.Int32, vm.RoleId);
                this.db.AddInParameter(com, "DeptId",
                    DbType.Int32, (vm.DepartmentId.HasValue && vm.DepartmentId.Value > 0) ? (object)vm.DepartmentId.Value : DBNull.Value);
                this.db.AddInParameter(com, "Designation",
                    DbType.String, string.IsNullOrEmpty(vm.Designation) ? (object)DBNull.Value : vm.Designation);
                this.db.AddInParameter(com, "PassHash", DbType.Binary, passHash);
                this.db.AddInParameter(com, "PassSalt", DbType.Binary, passSalt);

                this.db.ExecuteNonQuery(com);
                newConsumerId = Convert.ToInt32(this.db.GetParameterValue(com, "NewConsumerId"));
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                    throw new DataAccessException("An account with this email address already exists.", "Auth_Register", ex);
                throw new DataAccessException("Registration failed due to a database error.", "Auth_Register", ex);
            }

            return newConsumerId;
        }

        /// <summary>
        /// Loads the ConsumerCredential (+ Consumer/Role convenience fields) for the given email.
        /// </summary>
        /// <returns>Populated ConsumerCredential if found; Else null.</returns>
        public ConsumerCredential GetCredentialByEmail(string email)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.Auth_GetCredentialByEmail");
                this.db.AddInParameter(com, "Email", DbType.String, email);
                DataSet ds = this.db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return MapCredential(ds.Tables[0].Rows[0]);

                return null; // No row → user doesn't exist (not a DB failure)
            }
            catch (SqlException ex)
            {
                // A real DB outage must not silently collapse into "user not found".
                throw new DataAccessException(
                    "Failed to retrieve credentials. Please try again later.",
                    "Auth_GetCredentialByEmail", ex);
            }
        }

        /// <summary>
        /// Fetches a Consumer's profile row by primary key.
        /// </summary>
        public Consumer GetConsumerById(int consumerId)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.Consumer_GetById");
                this.db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                DataSet ds = this.db.ExecuteDataSet(com);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataRow row = ds.Tables[0].Rows[0];
                    return new Consumer
                    {
                        ConsumerId = Convert.ToInt32(row["ConsumerId"]),
                        Name       = Convert.ToString(row["Name"]),
                        Email      = Convert.ToString(row["Email"]),
                        Contact    = Convert.ToString(row["Contact"]),
                        DOB        = row["DOB"] is DBNull ? (DateTime?)null : Convert.ToDateTime(row["DOB"]),
                        AddressLine= row["AddressLine"] is DBNull ? null : Convert.ToString(row["AddressLine"]),
                        CityId     = row["CityId"] is DBNull ? (int?)null : Convert.ToInt32(row["CityId"]),
                        WardId     = row["WardId"] is DBNull ? (int?)null : Convert.ToInt32(row["WardId"]),
                        RoleId     = Convert.ToInt32(row["RoleId"]),
                        Designation= row["Designation"] is DBNull ? null : Convert.ToString(row["Designation"]),
                        IsActive   = Convert.ToBoolean(row["IsActive"]),
                    };
                }
                return null;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to retrieve consumer profile.", "Consumer_GetById", ex);
            }
        }

        /// <summary>
        /// Updates editable profile fields for a Consumer (name, contact, DOB, address, city, ward, designation).
        /// </summary>
        public bool UpdateConsumerProfile(int consumerId, string name, string contact,
            DateTime? dob, string addressLine, int? cityId, int? wardId, string designation)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.Consumer_UpdateProfile");
                this.db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                this.db.AddInParameter(com, "Name", DbType.String, name);
                this.db.AddInParameter(com, "Contact", DbType.String, contact);
                this.db.AddInParameter(com, "DOB", DbType.Date, dob.HasValue ? (object)dob.Value : DBNull.Value);
                this.db.AddInParameter(com, "AddressLine", DbType.String, (object)addressLine ?? DBNull.Value);
                this.db.AddInParameter(com, "CityId", DbType.Int32, cityId.HasValue ? (object)cityId.Value : DBNull.Value);
                this.db.AddInParameter(com, "WardId", DbType.Int32, wardId.HasValue ? (object)wardId.Value : DBNull.Value);
                this.db.AddInParameter(com, "Designation", DbType.String, (object)designation ?? DBNull.Value);
                this.db.ExecuteNonQuery(com);
                return true;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to update profile.", "Consumer_UpdateProfile", ex);
            }
        }

        /// <summary>
        /// Fetches cities list from database with safe fallback.
        /// </summary>
        public List<City> GetCities()
        {
            var list = new List<City>();
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.CitiesGetAll");
                DataSet ds = this.db.ExecuteDataSet(com);
                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        list.Add(new City
                        {
                            CityId = Convert.ToInt32(row["CityId"]),
                            CityName = Convert.ToString(row["CityName"])
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to load cities list.", "CitiesGetAll", ex);
            }

            return list;
        }

        /// <summary>
        /// Fetches wards list for a given city from database.
        /// </summary>
        public List<Ward> GetWardsByCity(int cityId)
        {
            var list = new List<Ward>();
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.WardsGetByCity");
                this.db.AddInParameter(com, "CityId", DbType.Int32, cityId);
                DataSet ds = this.db.ExecuteDataSet(com);
                if (ds != null && ds.Tables.Count > 0)
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        list.Add(new Ward
                        {
                            WardId = Convert.ToInt32(row["WardId"]),
                            WardName = Convert.ToString(row["WardName"]),
                            CityId = cityId
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to load wards list.", "WardsGetByCity", ex);
            }

            return list;
        }

        /// <summary>
        /// Updates the login/lockout state columns on ConsumerCredential.
        /// </summary>
        /// <returns>True if Update operation is successful; Else False.</returns>
        public bool UpdateLoginState(int consumerCredId, int failedCount, bool isLocked,
                                      DateTime? lockedUntil, DateTime? lastLoginAt)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.Auth_UpdateLoginState");
                this.db.AddInParameter(com, "ConsumerCredId", DbType.Int32, consumerCredId);
                this.db.AddInParameter(com, "FailedLoginCount", DbType.Int32, failedCount);
                this.db.AddInParameter(com, "IsLocked", DbType.Boolean, isLocked);

                if (lockedUntil.HasValue && lockedUntil.Value > DateTime.MinValue)
                {
                    this.db.AddInParameter(com, "LockedUntil", DbType.DateTime, lockedUntil.Value);
                }
                else
                {
                    this.db.AddInParameter(com, "LockedUntil", DbType.DateTime, DBNull.Value);
                }

                if (lastLoginAt.HasValue && lastLoginAt.Value > DateTime.MinValue)
                {
                    this.db.AddInParameter(com, "LastLoginAt", DbType.DateTime, lastLoginAt.Value);
                }
                else
                {
                    this.db.AddInParameter(com, "LastLoginAt", DbType.DateTime, DBNull.Value);
                }

                this.db.ExecuteNonQuery(com);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to update login state.", "Auth_UpdateLoginState", ex);
            }

            return true;
        }

        /// <summary>
        /// Updates the password hash/salt for a Consumer.
        /// </summary>
        /// <returns>True if Update operation is successful; Else False.</returns>
        public bool UpdatePassword(int consumerId, byte[] passHash, byte[] passSalt)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.Auth_UpdatePassword");
                this.db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                this.db.AddInParameter(com, "PassHash", DbType.Binary, passHash);
                this.db.AddInParameter(com, "PassSalt", DbType.Binary, passSalt);
                this.db.ExecuteNonQuery(com);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to update password.", "Auth_UpdatePassword", ex);
            }

            return true;
        }

        /// <summary>
        /// Sets a new OTP hash + expiry on ConsumerCredential.
        /// </summary>
        /// <returns>True if Update operation is successful; Else False.</returns>
        public bool SetOtp(int consumerId, byte[] otpHash, DateTime validTill)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.Otp_Set");
                this.db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                this.db.AddInParameter(com, "OTPHash", DbType.Binary, otpHash);
                this.db.AddInParameter(com, "ValidTill", DbType.DateTime, validTill);
                this.db.ExecuteNonQuery(com);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to set OTP.", "Otp_Set", ex);
            }

            return true;
        }

        /// <summary>
        /// Loads the current OTP state for a Consumer.
        /// </summary>
        /// <returns>Populated ConsumerCredential if found; Else null.</returns>
        public ConsumerCredential GetOtpState(int consumerId)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.Otp_GetState");
                this.db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                DataSet ds = this.db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    return MapCredential(ds.Tables[0].Rows[0]);
                }
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to load OTP state.", "Otp_GetState", ex);
            }

            return null;
        }

        /// <summary>
        /// Increments the failed-OTP-attempt counter for a Consumer.
        /// </summary>
        /// <returns>True if Update operation is successful; Else False.</returns>
        public bool IncrementOtpAttempts(int consumerId)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.Otp_IncrementAttempts");
                this.db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                this.db.ExecuteNonQuery(com);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to increment OTP attempts.", "Otp_IncrementAttempts", ex);
            }

            return true;
        }

        /// <summary>
        /// Marks the current OTP as used for a Consumer.
        /// </summary>
        /// <returns>True if Update operation is successful; Else False.</returns>
        public bool MarkOtpUsed(int consumerId)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.Otp_MarkUsed");
                this.db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                this.db.ExecuteNonQuery(com);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to mark OTP as used.", "Otp_MarkUsed", ex);
            }

            return true;
        }

        /// <summary>
        /// Inserts a new rotating refresh token row.
        /// </summary>
        /// <returns>True if Insert operation is successful; Else False.</returns>
        public bool CreateRefreshToken(string tokenHash, int consumerId, string email, int roleId,
                                        DateTime expiresAt, bool rememberMe, DateTime trustExpiresAt)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.RefreshToken_Create");
                this.db.AddInParameter(com, "TokenHash", DbType.String, tokenHash);
                this.db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                this.db.AddInParameter(com, "Email", DbType.String, email);
                this.db.AddInParameter(com, "RoleId", DbType.Int32, roleId);
                this.db.AddInParameter(com, "ExpiresAt", DbType.DateTime, expiresAt);
                this.db.AddInParameter(com, "RememberMe", DbType.Boolean, rememberMe);
                this.db.AddInParameter(com, "TrustExpiresAt", DbType.DateTime, trustExpiresAt);
                this.db.ExecuteNonQuery(com);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to create refresh token.", "RefreshToken_Create", ex);
            }

            return true;
        }

        /// <summary>
        /// Loads a refresh token row by its hash.
        /// </summary>
        /// <returns>Populated RefreshToken if found; Else null.</returns>
        public RefreshToken GetRefreshToken(string tokenHash)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.RefreshToken_GetByHash");
                this.db.AddInParameter(com, "TokenHash", DbType.String, tokenHash);
                DataSet ds = this.db.ExecuteDataSet(com);
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    DataRow row = ds.Tables[0].Rows[0];
                    return new RefreshToken
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        TokenHash = Convert.ToString(row["TokenHash"]),
                        ConsumerId = Convert.ToInt32(row["ConsumerId"]),
                        Email = Convert.ToString(row["Email"]),
                        RoleId = Convert.ToInt32(row["RoleId"]),
                        ExpiresAt = Convert.ToDateTime(row["ExpiresAt"]),
                        IsRevoked = Convert.ToBoolean(row["IsRevoked"]),
                        RememberMe = Convert.ToBoolean(row["RememberMe"]),
                        TrustExpiresAt = Convert.ToDateTime(row["TrustExpiresAt"])
                    };
                }
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to load refresh token.", "RefreshToken_GetByHash", ex);
            }

            return null;
        }

        /// <summary>
        /// Rotates a refresh token: revokes the old hash, inserts the new one.
        /// </summary>
        /// <returns>True if Update operation is successful; Else False.</returns>
        public bool RotateRefreshToken(string oldHash, string newHash, int consumerId, string email, int roleId,
                                        DateTime expiresAt, bool rememberMe, DateTime trustExpiresAt)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.RefreshToken_Rotate");
                this.db.AddInParameter(com, "OldTokenHash", DbType.String, oldHash);
                this.db.AddInParameter(com, "NewTokenHash", DbType.String, newHash);
                this.db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                this.db.AddInParameter(com, "Email", DbType.String, email);
                this.db.AddInParameter(com, "RoleId", DbType.Int32, roleId);
                this.db.AddInParameter(com, "ExpiresAt", DbType.DateTime, expiresAt);
                this.db.AddInParameter(com, "RememberMe", DbType.Boolean, rememberMe);
                this.db.AddInParameter(com, "TrustExpiresAt", DbType.DateTime, trustExpiresAt);
                this.db.ExecuteNonQuery(com);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to rotate refresh token.", "RefreshToken_Rotate", ex);
            }

            return true;
        }

        /// <summary>
        /// Revokes every refresh token for a Consumer (reuse-detection / sign-out-everywhere).
        /// </summary>
        /// <returns>True if Update operation is successful; Else False.</returns>
        public bool RevokeAllRefreshTokens(int consumerId)
        {
            try
            {
                DbCommand com = this.db.GetStoredProcCommand("FixMyCity.RefreshToken_RevokeAllForConsumer");
                this.db.AddInParameter(com, "ConsumerId", DbType.Int32, consumerId);
                this.db.ExecuteNonQuery(com);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException("Failed to revoke refresh tokens.", "RefreshToken_RevokeAllForConsumer", ex);
            }

            return true;
        }

        /// <summary>
        /// Maps one ConsumerCredential row (from either Auth_GetCredentialByEmail
        /// or Otp_GetState — same column shape) onto the model.
        /// </summary>
        private static ConsumerCredential MapCredential(DataRow row)
        {
            return new ConsumerCredential
            {
                ConsumerCredId = Convert.ToInt32(row["ConsumerCredId"]),
                ConsumerId = Convert.ToInt32(row["ConsumerId"]),
                PassHash = row["PassHash"] as byte[],
                PassSalt = row["PassSalt"] as byte[],
                FailedLoginCount = Convert.ToInt32(row["FailedLoginCount"]),
                IsLocked = Convert.ToBoolean(row["IsLocked"]),
                LockedUntil = row["LockedUntil"] is DBNull ? (DateTime?)null : Convert.ToDateTime(row["LockedUntil"]),
                LastLoginAt = row["LastLoginAt"] is DBNull ? (DateTime?)null : Convert.ToDateTime(row["LastLoginAt"]),
                PassChangedAt = Convert.ToDateTime(row["PassChangedAt"]),
                OTPHash = row["OTPHash"] as byte[],
                ValidTill = row["ValidTill"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(row["ValidTill"]),
                IsUsed = row["IsUsed"] is DBNull ? true : Convert.ToBoolean(row["IsUsed"]),
                AttemptCount = row["AttemptCount"] is DBNull ? 0 : Convert.ToInt32(row["AttemptCount"]),
                OtpCreatedDate = row["OtpCreatedDate"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(row["OtpCreatedDate"]),
                IsActive = Convert.ToBoolean(row["IsActive"]),
                RoleId = Convert.ToInt32(row["RoleId"]),
                Email = Convert.ToString(row["Email"]),
                Name = Convert.ToString(row["Name"])
            };
        }

        /// <summary>
        /// Best-effort file log for the "// To Do: Handle Exception" slots above —
        /// logging must never itself throw and mask the original exception.
        /// </summary>
        //private static void FileLog(Exception ex, string storedProcedure)
        //{
        //    try
        //    {
        //        System.IO.File.AppendAllText(
        //            System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Logs/errors.log"),
        //            String.Format("{0:u} | SP:{1} | {2}\r\n", DateTime.UtcNow, storedProcedure, ex));
        //    }
        //    catch { /* swallow — logging is not allowed to crash the request */ }
        //}

    }
}
use Training_DB_Shirisha_Gatti
SET IDENTITY_INSERT FixMyCity.Role ON;
    INSERT INTO FixMyCity.Role (RoleId, RoleName, IsActive,CreatedDate,LastModifiedAt) VALUES
        (2, 'Citizen', 1,GETDATE(),GETDATE()),
        (3, 'SupportExecutive', 1,GETDATE(),GETDATE()),
        (1, 'Admin', 1,GETDATE(),GETDATE());
    SET IDENTITY_INSERT FixMyCity.Role OFF;
	select * from FixMyCity.role


CREATE or alter PROCEDURE FixMyCity.CitiesGetAll  
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	CitiesGetAll
***********************************************************************************************
*/
AS  
BEGIN  
    SET NOCOUNT ON;  
    SELECT  
        CityId,  
        CityName  
    FROM FixMyCity.City  
    WHERE IsActive = 1          -- only surfaced cities  
    ORDER BY CityName;  
END  

CREATE OR Alter PROCEDURE FixMyCity.WardsGetByCity
    @CityId INT
	/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	WardsGetByCity
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        WardId,
        WardName,
        CityId
    FROM FixMyCity.Ward
    WHERE CityId  = @CityId
      AND IsActive = 1          -- only surfaced wards
    ORDER BY WardName;
END
GO

 CREATE OR ALTER PROCEDURE Register  
    @Name        VARCHAR(100),  
    @Email       VARCHAR(150),  
    @Contact     VARCHAR(15) = NULL,  
    @DOB         DATETIME = NULL,  
    @AddressLine VARCHAR(250) = NULL,  
    @CityId      INT = NULL,  
    @WardId      INT = NULL,  
    @RoleId      INT,  
    @DeptId      INT = NULL,  
    @Designation VARCHAR(100) = NULL,  
    @PassHash    VARBINARY(64),  
    @PassSalt    VARBINARY(32),  
    @NewConsumerId INT OUTPUT  
/*  
***********************************************************************************************  
 Date      Modified By    Purpose of Modification  
1 28jule2026  Shrisha Gatti registeration  
***********************************************************************************************  
*/  
AS  
BEGIN  
    SET NOCOUNT ON;  
    BEGIN TRY  
        BEGIN TRANSACTION;  
   
        INSERT INTO FixMyCity.Consumer (Name, Email, Contact, DOB, AddressLine, CityId, WardId, RoleId, DeptId, Designation,CreatedDate,LastModifiedAt)  
        VALUES (@Name, @Email, @Contact, @DOB, @AddressLine, @CityId, @WardId, @RoleId, @DeptId, @Designation,GETDATE(),GETDATE());  
   
        SET @NewConsumerId = SCOPE_IDENTITY();  
   
        INSERT INTO FixMyCity.ConsumerCredential (ConsumerId, PassHash, PassSalt,CreatedDate,LastModifiedAt)  
        VALUES (@NewConsumerId, @PassHash, @PassSalt,GETDATE(),GETDATE());  
   
        COMMIT TRANSACTION;  
    END TRY  
    BEGIN CATCH  
         IF @@TRANCOUNT > 0         ROLLBACK TRANSACTION;      THROW;  
    END CATCH  
END   

CREATE OR ALTER PROCEDURE FixMyCity.GetCredentialByEmail
    @Email VARCHAR(150)
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	Get Credential By Email
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        cc.ConsumerCredId, cc.ConsumerId, cc.PassHash, cc.PassSalt,
        cc.FailedLoginCount, cc.IsLocked, cc.LockedUntil, cc.LastLoginAt, cc.PassChangedAt,
        cc.OTPHash, cc.ValidTill, cc.IsUsed, cc.AttemptCount, cc.OtpCreatedDate,
        c.IsActive, c.RoleId, c.Email, c.Name
    FROM FixMyCity.ConsumerCredential cc
    INNER JOIN FixMyCity.Consumer c ON c.ConsumerId = cc.ConsumerId
    WHERE c.Email = @Email;
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.Auth_UpdateLoginState
    @ConsumerCredId   INT,
    @FailedLoginCount INT,
    @IsLocked         BIT,
    @LockedUntil      DATETIME = NULL,
    @LastLoginAt      DATETIME = NULL
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	Update Login State
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE FixMyCity.ConsumerCredential
    SET FailedLoginCount = @FailedLoginCount,
        IsLocked          = @IsLocked,
        LockedUntil       = @LockedUntil,
        LastLoginAt       = @LastLoginAt,
		LastModifiedAt=GETDATE()
    WHERE ConsumerCredId = @ConsumerCredId;
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.Auth_UpdatePassword
    @ConsumerId INT,
    @PassHash   VARBINARY(64),
    @PassSalt   VARBINARY(32)
/*   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	Update Password
**********
***********************************************************************************************
	Date*************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE FixMyCity.ConsumerCredential
    SET PassHash      = @PassHash,
        PassSalt       = @PassSalt,
        PassChangedAt  = GETUTCDATE(),
		LastModifiedAt=GETDATE()
    WHERE ConsumerId = @ConsumerId;
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.Otp_Set
    @ConsumerId INT,
    @OTPHash    VARBINARY(32),
    @ValidTill  DATETIME
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	to set otp
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE FixMyCity.ConsumerCredential
    SET OTPHash        = @OTPHash,
        ValidTill       = @ValidTill,
        IsUsed          = 0,
        AttemptCount    = 0,
        OtpCreatedDate  = GETUTCDATE(),
		LastModifiedAt=GETDATE()
    WHERE ConsumerId = @ConsumerId;
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.Otp_GetState
    @ConsumerId INT
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	to get otp state
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        cc.ConsumerCredId, cc.ConsumerId, cc.PassHash, cc.PassSalt,
        cc.FailedLoginCount, cc.IsLocked, cc.LockedUntil, cc.LastLoginAt, cc.PassChangedAt,
        cc.OTPHash, cc.ValidTill, cc.IsUsed, cc.AttemptCount, cc.OtpCreatedDate,
        c.IsActive, c.RoleId, c.Email, c.Name
    FROM FixMyCity.ConsumerCredential cc
    INNER JOIN FixMyCity.Consumer c ON c.ConsumerId = cc.ConsumerId
    WHERE cc.ConsumerId = @ConsumerId;
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.Otp_IncrementAttempts
    @ConsumerId INT
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	inc otp attempts
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE FixMyCity.ConsumerCredential
    SET AttemptCount = AttemptCount + 1
    WHERE ConsumerId = @ConsumerId;
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.Otp_MarkUsed
    @ConsumerId INT
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	mark otp used
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE FixMyCity.ConsumerCredential
    SET IsUsed = 1
    WHERE ConsumerId = @ConsumerId;
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.RefreshToken_Create
    @TokenHash      VARCHAR(128),
    @ConsumerId     INT,
    @Email          VARCHAR(150),
    @RoleId         INT,
    @ExpiresAt      DATETIME,
    @RememberMe     BIT,
    @TrustExpiresAt DATETIME
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	new refresh token
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO FixMyCity.RefreshToken (TokenHash, ConsumerId, Email, RoleId, ExpiresAt, RememberMe, TrustExpiresAt,CreatedDate,LastModifiedAt)
    VALUES (@TokenHash, @ConsumerId, @Email, @RoleId, @ExpiresAt, @RememberMe, @TrustExpiresAt,GETDATE(),
	GETDATE());
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.RefreshToken_GetByHash
    @TokenHash VARCHAR(128)
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	Get refresh token hash
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    -- Deliberately returns the row even if IsRevoked = 1 — the service
    -- layer's reuse-detection logic (TryRefresh) needs to see revoked
    -- tokens being presented again, that's how it detects token theft/replay.
    SELECT TOP 1
        Id, TokenHash, ConsumerId, Email, RoleId, ExpiresAt, IsRevoked, RememberMe, TrustExpiresAt
    FROM FixMyCity.RefreshToken
    WHERE TokenHash = @TokenHash;
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.RefreshToken_Rotate
    @OldTokenHash   VARCHAR(128),
    @NewTokenHash   VARCHAR(128),
    @ConsumerId     INT,
    @Email          VARCHAR(150),
    @RoleId         INT,
    @ExpiresAt      DATETIME,
    @RememberMe     BIT,
    @TrustExpiresAt DATETIME
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	refresh token rotate
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
 
        UPDATE FixMyCity.RefreshToken
        SET IsRevoked = 1
        WHERE TokenHash = @OldTokenHash;
 
        INSERT INTO FixMyCity.RefreshToken (TokenHash, ConsumerId, Email, RoleId, ExpiresAt, RememberMe, TrustExpiresAt,CreatedDate,LastModifiedAt)
        VALUES (@NewTokenHash, @ConsumerId, @Email, @RoleId, @ExpiresAt, @RememberMe, @TrustExpiresAt,GETDATE(),GETDATE());
 
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
    END CATCH
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.RefreshToken_RevokeAllForConsumer
    @ConsumerId INT
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	28jule2026		Shrisha Gatti	refreshtoken revoke
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE FixMyCity.RefreshToken
    SET IsRevoked = 1
    WHERE ConsumerId = @ConsumerId
      AND IsRevoked = 0;
END
GO
 
 use Training_DB_Shirisha_Gatti
 exec City_GetAll

CREATE PROCEDURE FixMyCity.Consumer_UpdateProfile
    @ConsumerId    INT,
    @Name          NVARCHAR(150),
    @Contact       NVARCHAR(20),
    @DOB           DATE          = NULL,
    @AddressLine   NVARCHAR(250) = NULL,
    @CityId        INT           = NULL,
    @WardId        INT           = NULL,
    @Designation   NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE FixMyCity.Consumer
    SET
        Name        = @Name,
        Contact     = @Contact,
        DOB         = @DOB,
        AddressLine = @AddressLine,
        CityId      = @CityId,
        WardId      = @WardId,
        Designation = @Designation,
        LastModifiedAt   = GETUTCDATE()
    WHERE ConsumerId = @ConsumerId;
END
GO
 SELECT * 
FROM sys.tables
WHERE name = 'Consumer';

SELECT s.name AS SchemaName, t.name AS TableName
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name = 'Consumer';
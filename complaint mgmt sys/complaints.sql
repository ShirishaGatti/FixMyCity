use Training_DB_Shirisha_Gatti
/* =============================================================
   FixMyCity database schema � full rebuild from Complaint schema
   Changes made:
     1. Complaint.* -> FixMyCity.* (schema renamed throughout)
     2. CreatedBy and LastModifiedBy made NULLable on every table
   ============================================================= */

CREATE SCHEMA FixMyCity;
GO

CREATE TABLE FixMyCity.Consumer (
    ConsumerId      INT IDENTITY(1,1) PRIMARY KEY,
    Name            VARCHAR(100) NOT NULL,
    Email           VARCHAR(150) NOT NULL UNIQUE,
    Contact         VARCHAR(15) NOT NULL,
    DOB             DATE NULL,
    AddressLine     VARCHAR(250) NULL,
    CityId          INT NULL,
    WardId          INT NULL,
    RoleId          INT NOT NULL,
    DeptId          INT NULL,
    Designation     VARCHAR(100) NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId)
);
GO

CREATE TABLE FixMyCity.Role (
    RoleId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleName        VARCHAR(50) NOT NULL UNIQUE,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId)
);
GO

CREATE TABLE FixMyCity.Department (
    DepartmentId    INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentName  VARCHAR(100) NOT NULL UNIQUE,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId)
);
GO

CREATE TABLE FixMyCity.ComplaintStatus (
    StatusId        INT IDENTITY(1,1) PRIMARY KEY,
    StatusName      VARCHAR(30) NOT NULL UNIQUE,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId)
);
GO

CREATE TABLE FixMyCity.ComplaintPriority (
    PriorityId      INT IDENTITY(1,1) PRIMARY KEY,
    PriorityName    VARCHAR(20) NOT NULL UNIQUE,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId)
);
GO

CREATE TABLE FixMyCity.ComplaintCategory (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName    VARCHAR(100) NOT NULL UNIQUE,
    DepartmentId    INT NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_Category_Dept FOREIGN KEY (DepartmentId) REFERENCES FixMyCity.Department(DepartmentId)
);
GO

CREATE TABLE FixMyCity.State (
    StateId         INT IDENTITY(1,1) PRIMARY KEY,
    StateName       VARCHAR(100) NOT NULL UNIQUE,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId)
);
GO

CREATE TABLE FixMyCity.District (
    DistrictId      INT IDENTITY(1,1) PRIMARY KEY,
    DistrictName    VARCHAR(100) NOT NULL,
    StateId         INT NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_District_State FOREIGN KEY (StateId) REFERENCES FixMyCity.State(StateId),
    CONSTRAINT UQ_District UNIQUE (DistrictName, StateId)
);
GO

CREATE TABLE FixMyCity.City (
    CityId          INT IDENTITY(1,1) PRIMARY KEY,
    CityName        VARCHAR(100) NOT NULL,
    DistrictId      INT NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_City_District FOREIGN KEY (DistrictId) REFERENCES FixMyCity.District(DistrictId),
    CONSTRAINT UQ_City UNIQUE (CityName, DistrictId)
);
GO

CREATE TABLE FixMyCity.Ward (
    WardId          INT IDENTITY(1,1) PRIMARY KEY,
    WardNo          VARCHAR(10) NOT NULL,
    WardName        VARCHAR(100) NULL,
    CityId          INT NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_Ward_City FOREIGN KEY (CityId) REFERENCES FixMyCity.City(CityId),
    CONSTRAINT UQ_Ward UNIQUE (WardNo, CityId)
);
GO

ALTER TABLE FixMyCity.Consumer
ADD CONSTRAINT FK_User_Role FOREIGN KEY (RoleId) REFERENCES FixMyCity.Role(RoleId);
GO

ALTER TABLE FixMyCity.Consumer
ADD CONSTRAINT FK_User_Dept FOREIGN KEY (DeptId) REFERENCES FixMyCity.Department(DepartmentId);
GO

CREATE TABLE FixMyCity.ConsumerCredential (
    ConsumerCredId      INT IDENTITY(1,1) PRIMARY KEY,
    ConsumerId          INT NOT NULL UNIQUE,
    PassHash            VARBINARY(256) NOT NULL,
    PassSalt            VARBINARY(128) NOT NULL,
    FailedLoginCount    INT NOT NULL DEFAULT 0,
    IsLocked            BIT NOT NULL DEFAULT 0,
    LockedUntil         DATETIME NULL,
    LastLoginAt         DATETIME NULL,
    PassChangedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    OTPHash             VARBINARY(256) NOT NULL,
    ValidTill           DATETIME NOT NULL,
    IsUsed              BIT NOT NULL DEFAULT 0,
    AttemptCount        INT NOT NULL DEFAULT 0,
    OtpCreatedDate      DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive            BIT NOT NULL DEFAULT 1,
    CreatedDate         DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy           INT NULL,
    LastModifiedAt      DATE NULL,
    LastModifiedBy       INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_OTP_Consumer FOREIGN KEY (ConsumerId) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT CK_OTP_Attempts CHECK (AttemptCount <= 5)
);
GO

CREATE TABLE FixMyCity.Complaint (
    ComplaintId     INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintNumber VARCHAR(20) NOT NULL UNIQUE,
    Title           VARCHAR(150) NOT NULL,
    Description     VARCHAR(1000) NOT NULL,
    CategoryId      INT NOT NULL,
    PriorityId      INT NOT NULL,
    StatusId        INT NOT NULL,
    RaisedBy        INT NOT NULL,
    AssignedTo      INT NULL,
    AddressLine     VARCHAR(250) NOT NULL,
    Landmark        VARCHAR(150) NULL,
    WardId          INT NOT NULL,
    CityId          INT NOT NULL,
    ResolvedDate    DATETIME NULL,
    ClosedDate      DATETIME NULL,
    ReopenCount     INT NOT NULL DEFAULT 0,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_Complaint_Category FOREIGN KEY (CategoryId) REFERENCES FixMyCity.ComplaintCategory(CategoryId),
    CONSTRAINT FK_Complaint_Priority FOREIGN KEY (PriorityId) REFERENCES FixMyCity.ComplaintPriority(PriorityId),
    CONSTRAINT FK_Complaint_Status FOREIGN KEY (StatusId) REFERENCES FixMyCity.ComplaintStatus(StatusId),
    CONSTRAINT FK_Complaint_RaisedBy FOREIGN KEY (RaisedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_Complaint_AssignedTo FOREIGN KEY (AssignedTo) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_Complaint_Ward FOREIGN KEY (WardId) REFERENCES FixMyCity.Ward(WardId),
    CONSTRAINT FK_Complaint_City FOREIGN KEY (CityId) REFERENCES FixMyCity.City(CityId),
    CONSTRAINT CK_Complaint_Dates CHECK (ResolvedDate IS NULL OR ResolvedDate >= CreatedAt)
);
GO

CREATE INDEX IX_Complaint_Status ON FixMyCity.Complaint(StatusId);
CREATE INDEX IX_Complaint_AssignedTo ON FixMyCity.Complaint(AssignedTo);
CREATE INDEX IX_Complaint_RaisedBy ON FixMyCity.Complaint(RaisedBy);
CREATE INDEX IX_Complaint_Ward ON FixMyCity.Complaint(WardId);
CREATE INDEX IX_Complaint_CreatedDate ON FixMyCity.Complaint(CreatedAt DESC);
GO

CREATE TABLE FixMyCity.ComplaintAttachment (
    AttachmentId    INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId     INT NOT NULL,
    FileName        VARCHAR(255) NOT NULL,
    UploadedBy      INT NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_Attachment_Complaint FOREIGN KEY (ComplaintId) REFERENCES FixMyCity.Complaint(ComplaintId),
    CONSTRAINT FK_Attachment_Consumer FOREIGN KEY (UploadedBy) REFERENCES FixMyCity.Consumer(ConsumerId)
);
GO

CREATE TABLE FixMyCity.ComplaintComment (
    CommentId       INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId     INT NOT NULL,
    ConsumerId      INT NOT NULL,
    Comment         VARCHAR(1000) NOT NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_Comment_Complaint FOREIGN KEY (ComplaintId) REFERENCES FixMyCity.Complaint(ComplaintId),
    CONSTRAINT FK_Comment_Consumer FOREIGN KEY (ConsumerId) REFERENCES FixMyCity.Consumer(ConsumerId)
);
GO

CREATE TABLE FixMyCity.ComplaintHistory (
    HistoryId       INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId     INT NOT NULL,
    FieldChanged    VARCHAR(50) NOT NULL,
    OldValue        VARCHAR(200) NULL,
    NewValue        VARCHAR(200) NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_History_Complaint FOREIGN KEY (ComplaintId) REFERENCES FixMyCity.Complaint(ComplaintId),
    CONSTRAINT FK_History_Consumer FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId)
);
GO

CREATE TABLE FixMyCity.ComplaintFeedback (
    FeedbackId      INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId     INT NOT NULL UNIQUE,
    Rating          TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment         VARCHAR(500) NULL,
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_Feedback_Complaint FOREIGN KEY (ComplaintId) REFERENCES FixMyCity.Complaint(ComplaintId)
);
GO

CREATE TABLE FixMyCity.RefreshToken (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    TokenHash       VARCHAR(128) NOT NULL,
    ConsumerId      INT NOT NULL REFERENCES FixMyCity.Consumer(ConsumerId),
    Email           VARCHAR(150) NOT NULL,
    RoleId          INT NOT NULL,
    ExpiresAt       DATETIME NOT NULL,
    IsRevoked       BIT NOT NULL DEFAULT 0,
    RememberMe      BIT NOT NULL DEFAULT 0,
    TrustExpiresAt  DATETIME NOT NULL,
    CreatedDate     DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT UQ_RefreshToken_TokenHash UNIQUE (TokenHash)
);
GO

CREATE INDEX IX_RefreshToken_ConsumerId ON FixMyCity.RefreshToken(ConsumerId);
GO

-- ==========================
-- Insert into Complaint.State
-- ==========================
INSERT INTO FixMyCity.State
(StateName, IsActive,  LastModifiedAt)
VALUES
('Karnataka', 1,  GETDATE() ),
('Goa', 1,  GETDATE() ),
('Gujarat', 1,  GETDATE() ),
('Telangana', 1,  GETDATE() )

select * from FixMyCity.City
-- =============================
-- Insert into Complaint.District
-- =============================
INSERT INTO FixMyCity.District
(DistrictName, StateId, IsActive,  LastModifiedAt )
VALUES
('Pune',  2, 1, GETDATE() ),
('Nagpur',       2, 1, GETDATE()),
('Bengaluru',    3, 1, GETDATE()),
('Ahmedabad',    5, 1, GETDATE()),
('Hyderabad',    6, 1, GETDATE());

-- =========================
-- Insert into Complaint.City
-- =========================
INSERT INTO FixMyCity.City
(CityName, DistrictId, IsActive,  LastModifiedAt)
VALUES
('Pune',        2,  1, GETDATE()),
('Nagpur',      3,  1, GETDATE()),
('Bengaluru',   4,  1, GETDATE()),
('Ahmedabad',   5,  1, GETDATE()),
('Hyderabad',   6,  1, GETDATE());

-- =========================
-- Insert into Complaint.Ward
-- =========================
INSERT INTO FixMyCity.Ward
(WardNo, WardName, CityId, IsActive,  LastModifiedAt)
VALUES
('1', 'Shivaji Nagar', 1, 1, GETDATE()),
('2', 'Dharampeth',    2, 1, GETDATE()),
('3', 'Indiranagar',   3, 1, GETDATE()),
('4', 'Navrangpura',   4, 1, GETDATE()),
('5', 'Banjara Hills', 5, 1, GETDATE());

INSERT INTO FixMyCity.Consumer
(
    Name,
    Email,
    Contact,
    DOB,
    AddressLine,
    CityId,
    WardId,
    RoleId,
    DeptId,
    IsActive,
    CreatedBy,
    LastModifiedAt,
    LastModifiedBy
)
VALUES
(
    'Admin',
    'admin@test.com',
    '9876543210',
    '2000-01-01',
    'Pune',
    1,
    1,
    1,
    1,
    1,
    1,
    GETDATE(),
    1
);

ALTER TABLE FixMyCity.ConsumerCredential
ALTER COLUMN OTPHash VARBINARY(256) NULL;

ALTER TABLE FixMyCity.ConsumerCredential
ALTER COLUMN ValidTill DATETIME NULL;
ALTER TABLE FixMyCity.ConsumerCredential
ALTER COLUMN OtpCreatedDate DATETIME NULL;

select * from FixMyCity.consumer

exec sp_helptext 'FixMyCity.Admin_DeleteComplaint'
  
CREATE OR ALTER PROCEDURE FixMyCity.ComplaintDelete  
    @ComplaintId INT, @ConsumerId INT  
AS  
BEGIN  
    SET NOCOUNT ON;  
    UPDATE FixMyCity.Complaint  
    SET IsActive = 0,
		LastModifiedAt = GETUTCDATE()  
    WHERE ComplaintId = @ComplaintId 
		AND IsActive = 1  
		AND StatusId = 
			(SELECT StatusId
			 FROM FixMyCity.ComplaintStatus
			 WHERE StatusName = 'Open'); 
	SELECT @@ROWCOUNT;

END  

EXEC sp_rename 'FixMyCity.Complaint_Delete','FixMyCity.ComplaintDelete'
SELECT * FROM FIXMYCITY.Complaint
update fixmycity.complaint set isactive=1

CREATE   OR ALTER PROCEDURE FixMyCity.Complaint_Save  
    @ComplaintId INT = NULL,  
    @Title VARCHAR(150)=NULL,
    @Description VARCHAR(1000)=NULL,
    @CategoryId INT,
    @PriorityId INT,  
    @RaisedBy INT =NULL,
    @AddressLine VARCHAR(250)=NULL,
    @Landmark VARCHAR(150) = NULL,  
    @WardId INT =NULL,
    @CityId INT =NULL,
    @RoleId INT,
    @Status INT =NULL,
    @AssignedTo INT =NULL,
    @SavedComplaintId INT = NULL OUTPUT  
AS  
BEGIN  
    SET NOCOUNT ON; SET XACT_ABORT ON;  
    BEGIN TRY  
        BEGIN TRANSACTION;  
  
        IF @ComplaintId IS NULL OR @ComplaintId = 0  
        BEGIN  
            DECLARE @StatusId INT = (SELECT StatusId FROM FixMyCity.ComplaintStatus WHERE StatusName = 'Open');  
            IF @StatusId IS NULL THROW 51000, 'Open status not configured.', 1;  
  
            INSERT INTO FixMyCity.Complaint  
                (ComplaintNumber, Title, Description, CategoryId, PriorityId, StatusId,  
                 RaisedBy, AddressLine, Landmark, WardId, CityId, CreatedBy)  
            VALUES  
                ('TEMP', @Title, @Description, @CategoryId, @PriorityId, @StatusId,  
                 @RaisedBy, @AddressLine, @Landmark, @WardId, @CityId, @RaisedBy);  
  
            SET @SavedComplaintId = SCOPE_IDENTITY();  
  
            UPDATE FixMyCity.Complaint  
            SET ComplaintNumber = 'FMC' + CONVERT(VARCHAR(4), YEAR(GETDATE()))  
                                 + RIGHT('000000' + CAST(@SavedComplaintId AS VARCHAR(6)), 6)  
            WHERE ComplaintId = @SavedComplaintId;  
  
            INSERT INTO FixMyCity.ComplaintHistory (ComplaintId, FieldChanged, OldValue, NewValue, CreatedBy)  
            VALUES (@SavedComplaintId, 'StatusId', NULL, CAST(@StatusId AS VARCHAR(10)), @RaisedBy);  
        END  
        ELSE  
        BEGIN  
            DECLARE @CurrentStatus VARCHAR(30) = (  
                SELECT st.StatusName FROM FixMyCity.Complaint c  
                JOIN FixMyCity.ComplaintStatus st ON st.StatusId = c.StatusId  
                WHERE c.ComplaintId = @ComplaintId
                AND c.IsActive = 1
                AND
                (
                    (@RoleId = 1)
                    OR
                    (@RoleId = 2 AND c.RaisedBy = @RaisedBy)
                    OR
                    (@RoleId = 3 AND c.AssignedTo = @RaisedBy)
                )
            );  
  
            IF @CurrentStatus IS NULL THROW 51001, 'Complaint not found or you do not have permission to edit it.', 1;  
            IF @RoleId = 2 AND @CurrentStatus <> 'Open'
            BEGIN
                THROW 51002, 'Complaint can only be edited while it is Open.', 1;
            END

            IF @RoleId=2
                UPDATE FixMyCity.Complaint  
                SET Title = @Title, Description = @Description, CategoryId = @CategoryId,  
                    PriorityId = @PriorityId, AddressLine = @AddressLine, Landmark = @Landmark,  
                    WardId = @WardId, CityId = @CityId, LastModifiedAt = GETUTCDATE()  
                WHERE ComplaintId = @ComplaintId AND RaisedBy = @RaisedBy;  
            ELSE
                UPDATE FixMyCity.Complaint  
                SET CategoryId = @CategoryId,  
                    PriorityId = @PriorityId, 
                    StatusId = @Status,
                    AssignedTo = CASE WHEN @RoleId = 1 THEN @AssignedTo ELSE AssignedTo END,
                    LastModifiedAt = GETUTCDATE()  
                WHERE ComplaintId = @ComplaintId ; 
            SET @SavedComplaintId = @ComplaintId;  
  
            INSERT INTO FixMyCity.ComplaintHistory (ComplaintId, FieldChanged, OldValue, NewValue)  
            VALUES (@SavedComplaintId, 'Complaint', 'Edited', 'Details updated');  
        END  
  
        COMMIT TRANSACTION;  
    END TRY  
    BEGIN CATCH  
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;  
        THROW;  
    END CATCH  
END  

use Training_DB_Shirisha_Gatti
select * from FixMyCity.consumer
update FixMyCity.Complaint set IsActive =1 where ComplaintId=6
EXEC SP_HELPTEXT'FIXMYCITY.Complaint_Save'
CREATE OR ALTER PROCEDURE FixMyCity.ComplaintGetById
(
    @RoleId INT,
    @ConsumerId INT = NULL,
    @AssignedTo INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Sql NVARCHAR(MAX);

    SET @Sql = N'
    SELECT
        c.ComplaintId,
        c.ComplaintNumber,
        c.Title,
        c.Description,
        c.CategoryId,
        cat.CategoryName,
        c.PriorityId,
        pr.PriorityName,
        c.StatusId,
        st.StatusName,
        c.RaisedBy,
        c.AssignedTo,
        asg.Name AS AssignedName,
        c.AddressLine,
        c.Landmark,
        c.WardId,
        w.WardName,
        c.CityId,
        ci.CityName,
        c.ResolvedDate,
        c.ClosedDate,
        c.ReopenCount,
        c.CreatedAt
    FROM FixMyCity.Complaint c
        INNER JOIN FixMyCity.ComplaintCategory cat
            ON cat.CategoryId = c.CategoryId
        INNER JOIN FixMyCity.ComplaintPriority pr
            ON pr.PriorityId = c.PriorityId
        INNER JOIN FixMyCity.ComplaintStatus st
            ON st.StatusId = c.StatusId
        INNER JOIN FixMyCity.Ward w
            ON w.WardId = c.WardId
        INNER JOIN FixMyCity.City ci
            ON ci.CityId = c.CityId
        LEFT JOIN FixMyCity.Consumer asg
            ON asg.ConsumerId = c.AssignedTo
    WHERE c.IsActive = 1';

    IF (@RoleId = 2)
    BEGIN
        SET @Sql += N'
        AND c.RaisedBy = @ConsumerId';
    END
    ELSE IF (@RoleId = 3)
    BEGIN
        SET @Sql += N'
        AND c.AssignedTo = @AssignedTo';
    END

    SET @Sql += N'
    ORDER BY c.CreatedAt DESC;';

    EXEC sp_executesql
        @Sql,
        N'@ConsumerId INT, @AssignedTo INT',
        @ConsumerId = @ConsumerId,
        @AssignedTo = @AssignedTo;
END
GO

select * from fixmycity.complaintStatus
select * from fixmycity.complaint
update fixmycity.complaint set assignedto =13 where complaintid=6
/* =====================================================================
   FixMyCity — Complaint Chat (Officer <-> Citizen) feature
   -----------------------------------------------------------------------
   Design notes (matches existing FixMyCity conventions):
     - Schema: FixMyCity (same as everything else)
     - Soft delete via IsActive (never hard-delete a message)
     - CreatedBy/LastModifiedBy nullable INT FK -> Consumer, per house style
     - A message is EITHER text OR a single attachment, never both
       (CK_ChatMessage_Content enforces exactly one of the two is set)
     - Chat is only writable while complaint status is Open or In Progress.
       This is enforced in TWO places (defense in depth):
         1. ComplaintChat_Insert SP refuses the insert if status is
            Resolved/Closed (or complaint inactive) — hard stop even if
            the service layer is ever bypassed or has a bug.
         2. ComplaintChatService (C#) checks the same rule before calling
            the repository, so the user gets a clean business-error
            message instead of a raw SQL exception.
     - Permission (participant-only) is checked INSIDE the SPs, same
       pattern as FixMyCity.ComplaintGetById — never trust the caller.
   ===================================================================== */

-- =====================================================================
-- 1. TABLE
-- =====================================================================
CREATE TABLE FixMyCity.ComplaintChatMessage (
    ChatMessageId   INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId     INT NOT NULL,
    SenderId        INT NOT NULL,               -- Consumer.ConsumerId of author
    SenderRoleId    INT NOT NULL,                -- RoleId at time of send (audit trail; role could change later)
    MessageText     VARCHAR(1000) NULL,
    AttachmentId    INT NULL,                    -- FK to a chat-specific attachment row (see table below)
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       INT NULL,
    LastModifiedAt  DATE NULL,
    LastModifiedBy  INT NULL,
    CONSTRAINT FK_ChatMessage_Complaint FOREIGN KEY (ComplaintId) REFERENCES FixMyCity.Complaint(ComplaintId),
    CONSTRAINT FK_ChatMessage_Sender FOREIGN KEY (SenderId) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_ChatMessage_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_ChatMessage_LastModifiedBy FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    -- Exactly one of MessageText / AttachmentId must be populated (never both, never neither)
    CONSTRAINT CK_ChatMessage_Content CHECK (
        (MessageText IS NOT NULL AND AttachmentId IS NULL)
        OR
        (MessageText IS NULL AND AttachmentId IS NOT NULL)
    )
);
GO

CREATE INDEX IX_ChatMessage_Complaint ON FixMyCity.ComplaintChatMessage(ComplaintId, CreatedAt);
GO

-- Chat attachments are kept in their own table (separate from
-- ComplaintAttachment, which represents evidence filed with the
-- complaint itself, not conversation history). Same shape/fields as
-- ComplaintAttachment for consistency, stored under a separate disk
-- root (App_Data/Uploads/ComplaintChat/{ComplaintId}/) per requirement.
CREATE TABLE FixMyCity.ComplaintChatAttachment (
    ChatAttachmentId INT IDENTITY(1,1) PRIMARY KEY,
    ComplaintId      INT NOT NULL,
    FileName         VARCHAR(255) NOT NULL,
    ContentType      VARCHAR(100) NULL,
    FileSizeBytes    BIGINT NOT NULL,
    UploadedBy       INT NOT NULL,
    IsActive         BIT NOT NULL DEFAULT 1,
    CreatedAt        DATETIME NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy        INT NULL,
    LastModifiedAt   DATE NULL,
    LastModifiedBy   INT NULL,
    CONSTRAINT FK_ChatAttachment_Complaint FOREIGN KEY (ComplaintId) REFERENCES FixMyCity.Complaint(ComplaintId),
    CONSTRAINT FK_ChatAttachment_Consumer FOREIGN KEY (UploadedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_ChatAttachment_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES FixMyCity.Consumer(ConsumerId),
    CONSTRAINT FK_ChatAttachment_LastModifiedBy FOREIGN KEY (LastModifiedBy) REFERENCES FixMyCity.Consumer(ConsumerId)
);
GO

ALTER TABLE FixMyCity.ComplaintChatMessage
ADD CONSTRAINT FK_ChatMessage_Attachment FOREIGN KEY (AttachmentId) REFERENCES FixMyCity.ComplaintChatAttachment(ChatAttachmentId);
GO

-- =====================================================================
-- 2. ComplaintChatAttachment_Create
--    Mirrors ComplaintAttachment_Create exactly (same calling shape as
--    ComplaintRepository.CreateAttachment), so the repository code you
--    already have is trivial to copy/adapt.
-- =====================================================================
CREATE OR ALTER PROCEDURE FixMyCity.ComplaintChatAttachment_Create
    @ComplaintId    INT,
    @FileName       VARCHAR(255),
    @ContentType    VARCHAR(100) = NULL,
    @FileSizeBytes  BIGINT,
    @UploadedBy     INT,
    @NewAttachmentId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Same open/in-progress guard as the message insert — an attachment
    -- is just a message payload, so it must obey the same lifecycle rule.
    IF NOT EXISTS (
        SELECT 1
        FROM FixMyCity.Complaint c
        JOIN FixMyCity.ComplaintStatus st ON st.StatusId = c.StatusId
        WHERE c.ComplaintId = @ComplaintId
          AND c.IsActive = 1
          AND st.StatusName IN ('Open', 'In Progress')
    )
        THROW 52002, 'Chat is closed for this complaint.', 1;

    INSERT INTO FixMyCity.ComplaintChatAttachment
        (ComplaintId, FileName, ContentType, FileSizeBytes, UploadedBy, CreatedBy)
    VALUES
        (@ComplaintId, @FileName, @ContentType, @FileSizeBytes, @UploadedBy, @UploadedBy);

    SET @NewAttachmentId = SCOPE_IDENTITY();
END
GO

-- =====================================================================
-- 3. ComplaintChat_Insert
--    Inserts either a text message or an attachment-message.
--    Permission + lifecycle rules enforced here (defense in depth):
--      - Sender must be the citizen who raised it (RoleId = 2) OR the
--        officer currently assigned to it (RoleId = 3).
--      - Complaint must be IsActive = 1 and status Open/In Progress.
-- =====================================================================
CREATE OR ALTER PROCEDURE FixMyCity.ComplaintChat_Insert
    @ComplaintId    INT,
    @SenderId       INT,
    @SenderRoleId   INT,
    @MessageText    VARCHAR(1000) = NULL,
    @AttachmentId   INT = NULL,
    @NewChatMessageId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;

    IF (@MessageText IS NULL AND @AttachmentId IS NULL)
       OR (@MessageText IS NOT NULL AND @AttachmentId IS NOT NULL)
        THROW 52001, 'A chat message must contain either text or an attachment, not both or neither.', 1;

    DECLARE @IsParticipant BIT = 0;
    DECLARE @IsOpenForChat BIT = 0;

    SELECT
        @IsParticipant = CASE
            WHEN (@SenderRoleId = 2 AND c.RaisedBy = @SenderId) THEN 1
            WHEN (@SenderRoleId = 3 AND c.AssignedTo = @SenderId) THEN 1
            ELSE 0
        END,
        @IsOpenForChat = CASE WHEN st.StatusName IN ('Open', 'In Progress') THEN 1 ELSE 0 END
    FROM FixMyCity.Complaint c
    JOIN FixMyCity.ComplaintStatus st ON st.StatusId = c.StatusId
    WHERE c.ComplaintId = @ComplaintId
      AND c.IsActive = 1;

    IF @IsParticipant IS NULL OR @IsParticipant = 0
        THROW 52000, 'You do not have permission to post in this complaint''s chat.', 1;

    IF @IsOpenForChat = 0
        THROW 52002, 'Chat is closed for this complaint.', 1;

    INSERT INTO FixMyCity.ComplaintChatMessage
        (ComplaintId, SenderId, SenderRoleId, MessageText, AttachmentId, CreatedBy)
    VALUES
        (@ComplaintId, @SenderId, @SenderRoleId, @MessageText, @AttachmentId, @SenderId);

    SET @NewChatMessageId = SCOPE_IDENTITY();
END
GO

-- =====================================================================
-- 4. ComplaintChat_GetByComplaintId
--    Returns full thread (messages + joined attachment + sender info).
--    Permission checked here too — caller must be participant.
--    @SinceMessageId supports lightweight polling: pass the highest
--    ChatMessageId the client already has, get only newer rows back.
-- =====================================================================
CREATE OR ALTER PROCEDURE FixMyCity.ComplaintChat_GetByComplaintId
    @ComplaintId    INT,
    @RequesterId    INT,
    @RequesterRoleId INT,
    @SinceMessageId INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM FixMyCity.Complaint c
        WHERE c.ComplaintId = @ComplaintId
          AND c.IsActive = 1
          AND (
                (@RequesterRoleId = 2 AND c.RaisedBy = @RequesterId)
             OR (@RequesterRoleId = 3 AND c.AssignedTo = @RequesterId)
          )
    )
        THROW 52000, 'You do not have permission to view this complaint''s chat.', 1;

    SELECT
        m.ChatMessageId,
        m.ComplaintId,
        m.SenderId,
        sender.Name        AS SenderName,
        m.SenderRoleId,
        m.MessageText,
        m.AttachmentId,
        att.FileName,
        att.ContentType,
        att.FileSizeBytes,
        m.CreatedAt
    FROM FixMyCity.ComplaintChatMessage m
    JOIN FixMyCity.Consumer sender ON sender.ConsumerId = m.SenderId
    LEFT JOIN FixMyCity.ComplaintChatAttachment att ON att.ChatAttachmentId = m.AttachmentId
    WHERE m.ComplaintId = @ComplaintId
      AND m.IsActive = 1
      AND m.ChatMessageId > @SinceMessageId
    ORDER BY m.CreatedAt ASC, m.ChatMessageId ASC;

    -- Second result set: is the thread still open for writing right now?
    -- Lets the UI disable the composer without a second round trip.
    SELECT CASE WHEN st.StatusName IN ('Open', 'In Progress') THEN 1 ELSE 0 END AS IsChatOpen
    FROM FixMyCity.Complaint c
    JOIN FixMyCity.ComplaintStatus st ON st.StatusId = c.StatusId
    WHERE c.ComplaintId = @ComplaintId;
END
GO

-- =====================================================================
-- 5. ComplaintChatAttachment_GetById
--    For download — permission checked via the parent complaint, same
--    pattern as ComplaintAttachment_GetById.
-- =====================================================================
CREATE OR ALTER PROCEDURE FixMyCity.ComplaintChatAttachment_GetById
    @ChatAttachmentId INT,
    @RequesterId       INT,
    @RequesterRoleId   INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        att.ChatAttachmentId,
        att.ComplaintId,
        att.FileName,
        att.ContentType,
        att.FileSizeBytes,
        att.UploadedBy,
        att.CreatedAt
    FROM FixMyCity.ComplaintChatAttachment att
    JOIN FixMyCity.Complaint c ON c.ComplaintId = att.ComplaintId
    WHERE att.ChatAttachmentId = @ChatAttachmentId
      AND att.IsActive = 1
      AND c.IsActive = 1
      AND (
            (@RequesterRoleId = 2 AND c.RaisedBy = @RequesterId)
         OR (@RequesterRoleId = 3 AND c.AssignedTo = @RequesterId)
      );
END
GO

DECLARE @NewChatMessageId INT;

EXEC FixMyCity.ComplaintChat_Insert
    @ComplaintId = 6,
    @SenderId = 13,
    @SenderRoleId = 3,
    @MessageText = 'Hello, I have reviewed your complaint. Our team will inspect the location today.',
    @AttachmentId = NULL,
    @NewChatMessageId = @NewChatMessageId OUTPUT;

SELECT @NewChatMessageId AS ChatMessageId;

DECLARE @NewChatMessageId INT;

EXEC FixMyCity.ComplaintChat_Insert
    @ComplaintId = 6,
    @SenderId = 13,
    @SenderRoleId = 3,
    @MessageText = 'Please share a clearer photo if possible.',
    @AttachmentId = NULL,
    @NewChatMessageId = @NewChatMessageId OUTPUT;

    SELECT RaisedBy
FROM FixMyCity.Complaint
WHERE ComplaintId = 6;
use Training_DB_Shirisha_Gatti
exec sp_helptext 'FixMyCity.Complaint_Search'
select * from fixmycity.complaint
SELECT
    m.ChatMessageId,
    c.Name AS Sender,
    r.RoleName,
    m.MessageText,
    m.CreatedAt
FROM FixMyCity.ComplaintChatMessage m
JOIN FixMyCity.Consumer c
    ON c.ConsumerId = m.SenderId
JOIN FixMyCity.Role r
    ON r.RoleId = m.SenderRoleId
WHERE m.ComplaintId = 6
ORDER BY m.ChatMessageId;
use Training_DB_Shirisha_Gatti
SELECT
    c.ComplaintNumber,
    c.AssignedTo,
    asg.Name AS AssigneeName
FROM FixMyCity.Complaint c
LEFT JOIN FixMyCity.Consumer asg
    ON asg.ConsumerId = c.AssignedTo;

	exec sp_helptext 'fixmycity.Admin_ListUsers'

	CREATE   PROCEDURE FixMyCity.AdminUserList  
(  
    @Name       NVARCHAR(100) = NULL,  
    @Designation NVARCHAR(100) = NULL,  
    @CityId     INT = NULL,  
    @WardId     INT = NULL,  
    @RoleId     INT = NULL,  
    @SortBy     NVARCHAR(50) = 'ConsumerId',  
    @SortDir    NVARCHAR(4) = 'DESC',  
    @PageNumber INT = 1,  
    @PageSize   INT = 10  
)  
AS  
BEGIN  
    SET NOCOUNT ON;  
  
    IF (@PageNumber < 1) SET @PageNumber = 1;  
    IF (@PageSize < 1 OR @PageSize > 100) SET @PageSize = 10;  
  
    SET @Name = NULLIF(LTRIM(RTRIM(@Name)), '');  
    SET @Designation = NULLIF(LTRIM(RTRIM(@Designation)), '');  
  
    IF @SortBy NOT IN ('ConsumerId', 'Name', 'DOB')  
        SET @SortBy = 'ConsumerId';  
  
    IF UPPER(@SortDir) NOT IN ('ASC', 'DESC')  
        SET @SortDir = 'DESC';  
  
    DECLARE @Sql NVARCHAR(MAX) = N'  
    ;WITH Filtered AS  
    (  
        SELECT  
            c.ConsumerId,  
            c.Name,  
            c.Email,  
            c.Contact,  
            c.DOB,  
            c.RoleId,  
            r.RoleName,  
            c.CityId,  
            ci.CityName,  
            c.WardId,  
            w.WardName,  
            c.DeptId,  
            d.DepartmentName,  
            c.Designation,  
            c.IsActive,  
            c.CreatedDate,  
            TotalCount = COUNT(*) OVER()  
        FROM FixMyCity.Consumer c  
        LEFT JOIN FixMyCity.Role r ON c.RoleId = r.RoleId  
        LEFT JOIN FixMyCity.City ci ON c.CityId = ci.CityId  
        LEFT JOIN FixMyCity.Ward w ON c.WardId = w.WardId  
        LEFT JOIN FixMyCity.Department d ON c.DeptId = d.DepartmentId  
        WHERE c.IsActive = 1  
          AND (@Name IS NULL OR c.Name LIKE ''%'' + @Name + ''%'')  
          AND (@Designation IS NULL OR c.Designation LIKE ''%'' + @Designation + ''%'')  
          AND (@CityId IS NULL OR c.CityId = @CityId)  
          AND (@WardId IS NULL OR c.WardId = @WardId)  
          AND (@RoleId IS NULL OR c.RoleId = @RoleId)  
    )  
    SELECT *  
    FROM Filtered  
    ORDER BY ' + QUOTENAME(@SortBy) + ' ' + @SortDir +  
    CASE WHEN @SortBy <> 'ConsumerId' THEN ', ConsumerId ' + @SortDir ELSE '' END + '  
    OFFSET (@PageNumber - 1) * @PageSize ROWS  
    FETCH NEXT @PageSize ROWS ONLY;';  
  
    EXEC sp_executesql  
        @Sql,  
        N'@Name NVARCHAR(100), @Designation NVARCHAR(100), @CityId INT, @WardId INT, @RoleId INT, @PageNumber INT, @PageSize INT',  
        @Name, @Designation, @CityId, @WardId, @RoleId, @PageNumber, @PageSize;  
END  

use Training_DB_Shirisha_Gatti
CREATE PROCEDURE FixMyCity.ComplaintChatAttachment_Deactivate
    @ChatAttachmentId INT,
    @ModifiedBy       INT = NULL   -- optional: pass the uploader's ConsumerId for audit trail
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE FixMyCity.ComplaintChatAttachment
    SET IsActive       = 0,
        LastModifiedAt = GETUTCDATE(),
        LastModifiedBy = @ModifiedBy
    WHERE ChatAttachmentId = @ChatAttachmentId
      AND IsActive = 1;   -- idempotent: no-op if already deactivated, avoids a redundant write
END
GO
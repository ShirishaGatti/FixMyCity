use Training_DB_Shirisha_Gatti

CREATE OR ALTER PROCEDURE FixMyCity.Admin_GetDashboardStats
AS
BEGIN
    SET NOCOUNT ON;

    /*=========================================================
      Result Set 1 : Dashboard Counts
    =========================================================*/

    SELECT
        TotalUsers =
            (SELECT COUNT(*) FROM FixMyCity.Consumer WHERE IsActive = 1),

        TotalCitizens =
            (
                SELECT COUNT(*)
                FROM FixMyCity.Consumer
                WHERE RoleId = 2
                  AND IsActive = 1
            ),

        TotalOfficers =
            (
                SELECT COUNT(*)
                FROM FixMyCity.Consumer
                WHERE RoleId = 3
                  AND IsActive = 1
            ),

        TotalAdmins =
            (
                SELECT COUNT(*)
                FROM FixMyCity.Consumer
                WHERE RoleId = 1
                  AND IsActive = 1
            ),

        TotalComplaints =
            (
                SELECT COUNT(*)
                FROM FixMyCity.Complaint
                WHERE IsActive = 1
            ),

        OpenComplaints =
            (
                SELECT COUNT(*)
                FROM FixMyCity.Complaint c
                INNER JOIN FixMyCity.ComplaintStatus s
                    ON c.StatusId = s.StatusId
                WHERE s.StatusName = 'Open'
                  AND c.IsActive = 1
            ),

        InProgressComplaints =
            (
                SELECT COUNT(*)
                FROM FixMyCity.Complaint c
                INNER JOIN FixMyCity.ComplaintStatus s
                    ON c.StatusId = s.StatusId
                WHERE s.StatusName = 'In Progress'
                  AND c.IsActive = 1
            ),

        ResolvedComplaints =
            (
                SELECT COUNT(*)
                FROM FixMyCity.Complaint c
                INNER JOIN FixMyCity.ComplaintStatus s
                    ON c.StatusId = s.StatusId
                WHERE s.StatusName = 'Resolved'
                  AND c.IsActive = 1
            ),

        ClosedComplaints =
            (
                SELECT COUNT(*)
                FROM FixMyCity.Complaint c
                INNER JOIN FixMyCity.ComplaintStatus s
                    ON c.StatusId = s.StatusId
                WHERE s.StatusName = 'Closed'
                  AND c.IsActive = 1
            ),

        TotalCities =
            (
                SELECT COUNT(*)
                FROM FixMyCity.City
                WHERE IsActive = 1
            ),

        TotalWards =
            (
                SELECT COUNT(*)
                FROM FixMyCity.Ward
                WHERE IsActive = 1
            ),

        TotalDepartments =
            (
                SELECT COUNT(*)
                FROM FixMyCity.Department
                WHERE IsActive = 1
            ),

        TotalCategories =
            (
                SELECT COUNT(*)
                FROM FixMyCity.ComplaintCategory
                WHERE IsActive = 1
            );


    /*=========================================================
      Result Set 2 : Top Categories
    =========================================================*/

    SELECT TOP (5)
        cc.CategoryName,
        COUNT(*) AS Cnt
    FROM FixMyCity.Complaint c
    INNER JOIN FixMyCity.ComplaintCategory cc
        ON c.CategoryId = cc.CategoryId
    WHERE c.IsActive = 1
    GROUP BY cc.CategoryName
    ORDER BY COUNT(*) DESC;


    /*=========================================================
      Result Set 3 : Recent Complaints
    =========================================================*/

    SELECT TOP (10)
        c.ComplaintId,
        c.ComplaintNumber,
        c.Title,
        s.StatusName,
        c.CreatedAt
    FROM FixMyCity.Complaint c
    INNER JOIN FixMyCity.ComplaintStatus s
        ON c.StatusId = s.StatusId
    WHERE c.IsActive = 1
    ORDER BY c.CreatedAt DESC;

END
GO

create or alter PROCEDURE FixMyCity.Ward_GetAll
   
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        WardId,
        WardName,
        CityId
    FROM FixMyCity.Ward
    WHERE  IsActive = 1          -- only surfaced wards
    ORDER BY WardName;
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.Admin_ListUsers
(
    @Name           NVARCHAR(100) = NULL,
    @Designation    NVARCHAR(100) = NULL,
    @CityId         INT = NULL,
    @WardId         INT = NULL,
    @RoleId         INT = NULL,

    @SortBy         NVARCHAR(50) = 'ConsumerId',
    @SortDir        NVARCHAR(4) = 'DESC',

    @PageNumber     INT = 1,
    @PageSize       INT = 10,

    @TotalCount     INT OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    IF (@PageNumber < 1) SET @PageNumber = 1;
    IF (@PageSize < 1) SET @PageSize = 10;

    -- Whitelist sorting columns
    IF @SortBy NOT IN ('ConsumerId','Name','Designation','CreatedDate','Email')
        SET @SortBy = 'ConsumerId';

    IF UPPER(@SortDir) NOT IN ('ASC','DESC')
        SET @SortDir = 'DESC';

    DECLARE @Where NVARCHAR(MAX) = N'
        WHERE 1=1
        AND (@Name IS NULL OR c.Name LIKE ''%'' + @Name + ''%'')
        AND (@Designation IS NULL OR c.Designation LIKE ''%'' + @Designation + ''%'')
        AND (@CityId IS NULL OR c.CityId = @CityId)
        AND (@WardId IS NULL OR c.WardId = @WardId)
        AND (@RoleId IS NULL OR c.RoleId = @RoleId)';

    ----------------------------------------------------------
    -- Total Count
    ----------------------------------------------------------

    DECLARE @CountSql NVARCHAR(MAX) = N'
    SELECT @TotalCount = COUNT(*)
    FROM FixMyCity.Consumer c
    ' + @Where;

    EXEC sp_executesql
        @CountSql,
        N'@Name NVARCHAR(100),
          @Designation NVARCHAR(100),
          @CityId INT,
          @WardId INT,
          @RoleId INT,
          @TotalCount INT OUTPUT',
        @Name,
        @Designation,
        @CityId,
        @WardId,
        @RoleId,
        @TotalCount OUTPUT;

    ----------------------------------------------------------
    -- Main Query
    ----------------------------------------------------------

    DECLARE @Sql NVARCHAR(MAX) = N'

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
        c.CreatedDate

    FROM FixMyCity.Consumer c

    LEFT JOIN FixMyCity.Role r
        ON c.RoleId = r.RoleId

    LEFT JOIN FixMyCity.City ci
        ON c.CityId = ci.CityId

    LEFT JOIN FixMyCity.Ward w
        ON c.WardId = w.WardId

    LEFT JOIN FixMyCity.Department d
        ON c.DeptId = d.DepartmentId

    ' + @Where + '

    ORDER BY ' + QUOTENAME(@SortBy) + ' ' + @SortDir + '

    OFFSET (@PageNumber-1)*@PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;';

    EXEC sp_executesql
        @Sql,
        N'@Name NVARCHAR(100),
          @Designation NVARCHAR(100),
          @CityId INT,
          @WardId INT,
          @RoleId INT,
          @PageNumber INT,
          @PageSize INT',
        @Name,
        @Designation,
        @CityId,
        @WardId,
        @RoleId,
        @PageNumber,
        @PageSize;
END
GO

exec sp_helptext 'FixMyCity.Admin_ListUsers'
CREATE   PROCEDURE FixMyCity.Admin_ListUsers  
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
USE Training_DB_Shirisha_Gatti
GO

/*===============================================================================================
  These procs are called by the current AdminRepository/AdminService but didn't exist yet in the
  uploaded .sql files. Adjust column names below if your Consumer/Complaint tables differ.
===============================================================================================*/

CREATE OR ALTER PROCEDURE FixMyCity.Admin_GetUserById
    @ConsumerId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        c.ConsumerId, c.Name, c.Email, c.Contact, c.DOB,
        c.RoleId, c.CityId, c.WardId, c.DeptId, c.Designation, c.IsActive
    FROM FixMyCity.Consumer c
    WHERE c.ConsumerId = @ConsumerId;
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.Admin_UpdateUserStatus
    @ConsumerId INT,
    @IsActive   BIT,
    @ActorId    INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE FixMyCity.Consumer
    SET IsActive = @IsActive,
        LastModifiedAt = GETUTCDATE()
    WHERE ConsumerId = @ConsumerId;
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.Admin_DeleteUser
    @ConsumerId INT,
    @ActorId    INT
AS
BEGIN
    SET NOCOUNT ON;
    -- Soft delete: keeps history / FK integrity with Complaint.RaisedBy intact.
    UPDATE FixMyCity.Consumer
    SET IsActive = 0,
        LastModifiedAt = GETUTCDATE()
    WHERE ConsumerId = @ConsumerId;
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.Admin_GetComplaintById
    @ComplaintId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        c.ComplaintId, c.Title, c.Description,
        c.CategoryId, c.PriorityId, c.StatusId, c.AssignedTo,
        c.CityId, c.WardId
    FROM FixMyCity.Complaint c
    WHERE c.ComplaintId = @ComplaintId;
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.Admin_DeleteComplaint
    @ComplaintId INT,
    @ActorId     INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE FixMyCity.Complaint
    SET IsActive = 0
    WHERE ComplaintId = @ComplaintId;
END
GO

CREATE PROCEDURE Admin_UpdateOfficer
(
    @ConsumerId INT,
    @Designation NVARCHAR(100),
    @WardId INT = NULL,
    @DeptId INT = NULL,
    @ActorId INT
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Consumer
    SET
        Designation = @Designation,
        WardId = @WardId,
        DeptId = @DeptId,
        UpdatedBy = @ActorId,
        UpdatedDate = GETDATE()
    WHERE ConsumerId = @ConsumerId;
END
use Training_DB_Shirisha_Gatti
exec sp_helptext Fixmycity.Complaint_Search

CREATE OR ALTER PROCEDURE FixMyCity.State_GetAll
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        Id         = StateId,
        Name       = StateName,
        ParentId   = CAST(NULL AS INT),
        ParentName = CAST(NULL AS NVARCHAR(100)),
        IsActive
    FROM FixMyCity.State
    WHERE @IncludeInactive = 1 OR IsActive = 1
    ORDER BY StateName;
END
GO
 
-- =====================================================================
-- DISTRICT (parent = State)
-- =====================================================================
CREATE OR ALTER PROCEDURE FixMyCity.District_GetAll
    @ParentId        INT = NULL,   -- StateId filter (optional)
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        Id         = d.DistrictId,
        Name       = d.DistrictName,
        ParentId   = d.StateId,
        ParentName = s.StateName,
        IsActive   = d.IsActive
    FROM FixMyCity.District d
    INNER JOIN FixMyCity.State s ON d.StateId = s.StateId
    WHERE (@ParentId IS NULL OR d.StateId = @ParentId)
      AND (@IncludeInactive = 1 OR d.IsActive = 1)
    ORDER BY d.DistrictName;
END
GO
 
-- =====================================================================
-- CITY (parent = District, nullable per your SaveCity signature)
-- =====================================================================
CREATE OR ALTER PROCEDURE FixMyCity.City_GetByDistrict
    @ParentId        INT = NULL,   -- DistrictId filter (optional)
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        Id         = c.CityId,
        Name       = c.CityName,
        ParentId   = c.DistrictId,
        ParentName = d.DistrictName,
        IsActive   = c.IsActive
    FROM FixMyCity.City c
    LEFT JOIN FixMyCity.District d ON c.DistrictId = d.DistrictId
    WHERE (@ParentId IS NULL OR c.DistrictId = @ParentId)
      AND (@IncludeInactive = 1 OR c.IsActive = 1)
    ORDER BY c.CityName;
END
GO

 
-- =====================================================================
-- WARD (parent = City) — replaces your existing Ward_GetAll.
-- Backward compatible: calling it with no args behaves like before
-- (active wards only), it just also now supports filtering by city
-- and optionally including inactive rows for the admin grid.
-- =====================================================================
CREATE OR ALTER PROCEDURE FixMyCity.Ward_GetAll
    @ParentId        INT = NULL,   -- CityId filter (optional)
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        Id         = w.WardId,
        Name       = w.WardName,
        ParentId   = w.CityId,
        ParentName = c.CityName,
        IsActive   = w.IsActive,
		WardNo=w.WardNo
    FROM FixMyCity.Ward w
    INNER JOIN FixMyCity.City c ON w.CityId = c.CityId
    WHERE (@ParentId IS NULL OR w.CityId = @ParentId)
      AND (@IncludeInactive = 1 OR w.IsActive = 1)
    ORDER BY w.WardName;
END
GO
 
-- =====================================================================
-- CATEGORY (no parent) — table is ComplaintCategory, proc name kept
-- as "Category_GetAll" to match your Category_Save naming.
-- =====================================================================
CREATE OR ALTER PROCEDURE FixMyCity.GetCategory
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        Id         = C.CategoryId,
        Name       = C.CategoryName,
        ParentId   = CAST(NULL AS INT),
        ParentName = CAST(NULL AS NVARCHAR(100)),
		DepartmentId= D.DepartmentId,
		DepartmentName = D.DepartmentName,
        IsActive=C.IsActive
    FROM FixMyCity.ComplaintCategory C
	JOIN FixMyCity.Department D
    ON C.DepartmentId = D.DepartmentId
    WHERE @IncludeInactive = 1
       OR C.IsActive = 1
    ORDER BY CategoryName;
END
GO
 
-- =====================================================================
-- DEPARTMENT (no parent)
-- =====================================================================
CREATE OR ALTER PROCEDURE FixMyCity.Department_GetAll
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        Id         = DepartmentId,
        Name       = DepartmentName,
        ParentId   = CAST(NULL AS INT),
        ParentName = CAST(NULL AS NVARCHAR(100)),
        IsActive
    FROM FixMyCity.Department
    WHERE @IncludeInactive = 1 OR IsActive = 1
    ORDER BY DepartmentName;
END
GO
 
-- =====================================================================
-- ROLE (no parent) — NEW: GetAll + Save. Roles didn't have a save
-- path before; this brings it in line with the other 6 masters.
-- Note: RoleId is used all over Consumer/JWT claims (1=Admin,
-- 2=Citizen, 3=Officer per your dashboard SP), so treat editing
-- existing rows here with care — don't let the grid delete/renumber
-- IDs, only rename or toggle IsActive.
-- =====================================================================
CREATE OR ALTER PROCEDURE FixMyCity.Role_GetAll
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        Id         = RoleId,
        Name       = RoleName,
        ParentId   = CAST(NULL AS INT),
        ParentName = CAST(NULL AS NVARCHAR(100)),
        IsActive
    FROM FixMyCity.Role
    WHERE @IncludeInactive = 1 OR IsActive = 1
    ORDER BY RoleName;
END
GO
 
CREATE OR ALTER PROCEDURE FixMyCity.Role_Save
    @Id       INT,
    @Name     NVARCHAR(100),
    @ParentId INT = NULL,      -- unused, kept only so the signature matches
                                -- the shared SaveSimpleMaster() call shape
    @IsActive BIT,
    @ActorId  INT,
    @NewId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
 
    IF @Id = 0 OR @Id IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM FixMyCity.Role WHERE RoleName = @Name)
        BEGIN
            RAISERROR('A role with that name already exists.', 16, 1);
            RETURN;
        END
 
        INSERT INTO FixMyCity.Role (RoleName, IsActive)
        VALUES (@Name, @IsActive);
 
        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE FixMyCity.Role
        SET RoleName = @Name,
            IsActive = @IsActive
        WHERE RoleId = @Id;
 
        SET @NewId = @Id;
    END
END
GO

select * from FixMyCity.state
 
 /*=====================================================================
  STATE
=====================================================================*/
CREATE OR ALTER PROCEDURE FixMyCity.State_Save
    @Id       INT,
    @Name     NVARCHAR(100),
    @ParentId INT = NULL,     -- unused, kept for signature parity
    @IsActive BIT,
    @ActorId  INT,
    @NewId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id = 0 OR @Id IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM FixMyCity.State WHERE StateName = @Name)
        BEGIN
            RAISERROR('A state with that name already exists.', 16, 1);
            RETURN;
        END

        INSERT INTO FixMyCity.State (StateName, IsActive)
        VALUES (@Name, @IsActive);

        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE FixMyCity.State
        SET StateName = @Name,
            IsActive  = @IsActive
        WHERE StateId = @Id;

        SET @NewId = @Id;
    END
END
GO

/*=====================================================================
  DISTRICT (parent = State)
=====================================================================*/
CREATE OR ALTER PROCEDURE FixMyCity.District_Save
    @Id       INT,
    @Name     NVARCHAR(100),
    @ParentId INT,            -- StateId, required
    @IsActive BIT,
    @ActorId  INT,
    @NewId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM FixMyCity.State WHERE StateId = @ParentId)
    BEGIN
        RAISERROR('Selected state does not exist.', 16, 1);
        RETURN;
    END

    IF @Id = 0 OR @Id IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM FixMyCity.District WHERE DistrictName = @Name AND StateId = @ParentId)
        BEGIN
            RAISERROR('A district with that name already exists in this state.', 16, 1);
            RETURN;
        END

        INSERT INTO FixMyCity.District (DistrictName, StateId, IsActive)
        VALUES (@Name, @ParentId, @IsActive);

        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE FixMyCity.District
        SET DistrictName = @Name,
            StateId       = @ParentId,
            IsActive      = @IsActive
        WHERE DistrictId = @Id;

        SET @NewId = @Id;
    END
END
GO

/*=====================================================================
  CITY (parent = District, nullable)
  Save proc stays City_Save per your repo call; list proc matches your
  actual dictionary entry, City_GetByDistrict.
 @ParentId INT = NULL,     -- DistrictId, optional
    =====================================================================*/
CREATE OR ALTER PROCEDURE FixMyCity.City_Save
    @Id       INT,
    @Name     NVARCHAR(100),
	@ParentId INT ,
   @IsActive BIT,
    @ActorId  INT,
    @NewId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ParentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM FixMyCity.District WHERE DistrictId = @ParentId)
    BEGIN
        RAISERROR('Selected district does not exist.', 16, 1);
        RETURN;
    END

    IF @Id = 0 OR @Id IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM FixMyCity.City WHERE CityName = @Name)
        BEGIN
            RAISERROR('A city with that name already exists.', 16, 1);
            RETURN;
        END

        INSERT INTO FixMyCity.City (CityName, DistrictId, IsActive)
        VALUES (@Name, @ParentId, @IsActive);

        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE FixMyCity.City
        SET CityName   = @Name,
            DistrictId = @ParentId,
            IsActive   = @IsActive
        WHERE CityId = @Id;

        SET @NewId = @Id;
    END
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.City_GetByDistrict
    @ParentId        INT = NULL,   -- DistrictId filter, optional
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        Id         = c.CityId,
        Name       = c.CityName,
        ParentId   = c.DistrictId,
        ParentName = d.DistrictName,
        IsActive   = c.IsActive
    FROM FixMyCity.City c
    LEFT JOIN FixMyCity.District d ON c.DistrictId = d.DistrictId
    WHERE (@ParentId IS NULL OR c.DistrictId = @ParentId)
      AND (@IncludeInactive = 1 OR c.IsActive = 1)
    ORDER BY c.CityName;
END
GO

/*=====================================================================
  WARD (parent = City)
=====================================================================*/
CREATE OR ALTER PROCEDURE FixMyCity.Ward_Save
    @Id       INT,
    @Name     NVARCHAR(100),
    @ParentId INT,            -- CityId, required
    @IsActive BIT,
    @ActorId  INT,
    @NewId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @ParentId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM FixMyCity.City WHERE CityId = @ParentId)
    BEGIN
        RAISERROR('Selected city does not exist.', 16, 1);
        RETURN;
    END

    IF @Id = 0 OR @Id IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM FixMyCity.Ward WHERE WardName = @Name AND CityId = @ParentId)
        BEGIN
            RAISERROR('A ward with that name already exists in this city.', 16, 1);
            RETURN;
        END

        INSERT INTO FixMyCity.Ward (WardName, CityId, IsActive)
        VALUES (@Name, @ParentId, @IsActive);

        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE FixMyCity.Ward
        SET WardName = @Name,
            CityId   = @ParentId,
            IsActive = @IsActive
        WHERE WardId = @Id;

        SET @NewId = @Id;
    END
END
GO

/*=====================================================================
  CATEGORY (no parent) — table ComplaintCategory, list proc name kept
  as GetCategory per your dictionary.
=====================================================================*/
use Training_DB_Shirisha_Gatti
CREATE OR ALTER PROCEDURE FixMyCity.Category_Save
    @Id       INT,
    @Name     NVARCHAR(100),
    @ParentId INT = NULL,     -- unused
    @IsActive BIT,
    @ActorId  INT,
	@DepartmentId INT,
    @NewId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id = 0 OR @Id IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM FixMyCity.ComplaintCategory WHERE CategoryName = @Name)
        BEGIN
            RAISERROR('A category with that name already exists.', 16, 1);
            RETURN;
        END

        INSERT INTO FixMyCity.ComplaintCategory (CategoryName,DepartmentId, IsActive)
        VALUES (@Name,@DepartmentId, @IsActive);

        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE FixMyCity.ComplaintCategory
        SET CategoryName = @Name,
            IsActive     = @IsActive
        WHERE CategoryId = @Id;

        SET @NewId = @Id;
    END
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.GetCategory
    @ParentId        INT = NULL,   -- unused, kept for GetMasterList's uniform call shape
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        Id         = CategoryId,
        Name       = CategoryName,
        ParentId   = CAST(NULL AS INT),
        ParentName = CAST(NULL AS NVARCHAR(100)),
        IsActive
    FROM FixMyCity.ComplaintCategory
    WHERE @IncludeInactive = 1 OR IsActive = 1
    ORDER BY CategoryName;
END
GO

/*=====================================================================
  DEPARTMENT (no parent)
=====================================================================*/
CREATE OR ALTER PROCEDURE FixMyCity.Department_Save
    @Id       INT,
    @Name     NVARCHAR(100),
    @ParentId INT = NULL,     -- unused
    @IsActive BIT,
    @ActorId  INT,
    @NewId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id = 0 OR @Id IS NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM FixMyCity.Department WHERE DepartmentName = @Name)
        BEGIN
            RAISERROR('A department with that name already exists.', 16, 1);
            RETURN;
        END

        INSERT INTO FixMyCity.Department (DepartmentName, IsActive)
        VALUES (@Name, @IsActive);

        SET @NewId = SCOPE_IDENTITY();
    END
    ELSE
    BEGIN
        UPDATE FixMyCity.Department
        SET DepartmentName = @Name,
            IsActive       = @IsActive
        WHERE DepartmentId = @Id;

        SET @NewId = @Id;
    END
END
GO

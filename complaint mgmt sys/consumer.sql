-- Seed lookups (run once)
INSERT INTO FixMyCity.ComplaintStatus (StatusName) VALUES ('Open'), ('In Progress'), ('Resolved'), ('Closed');
INSERT INTO FixMyCity.ComplaintPriority (PriorityName) VALUES ('High'), ('Medium'), ('Low');
INSERT INTO FixMyCity.Department (DepartmentName) VALUES ('Roads'), ('Water'), ('Sanitation'), ('Electricity'), ('General');
INSERT INTO FixMyCity.ComplaintCategory (CategoryName, DepartmentId) VALUES
    ('Pothole / Road Damage', 1), ('Water Leakage', 2), ('Garbage Collection', 3),
    ('Streetlight Not Working', 4), ('Other', 5);
GO
use Training_DB_Shirisha_Gatti

CREATE OR ALTER PROCEDURE FixMyCity.Complaint_Create
    @Title VARCHAR(150), @Description VARCHAR(1000), @CategoryId INT, @PriorityId INT,
    @RaisedBy INT, @AddressLine VARCHAR(250), @Landmark VARCHAR(150) = NULL,
    @WardId INT, @CityId INT, @NewComplaintId INT OUTPUT
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	29jule2026		Shrisha Gatti	 save complaint
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    BEGIN TRY
        DECLARE @StatusId INT = (SELECT StatusId FROM FixMyCity.ComplaintStatus WHERE StatusName = 'Open');
        IF @StatusId IS NULL THROW 51000, 'Open status not configured.', 1;

        BEGIN TRANSACTION;

        INSERT INTO FixMyCity.Complaint
            (ComplaintNumber, Title, Description, CategoryId, PriorityId, StatusId,
             RaisedBy, AddressLine, Landmark, WardId, CityId, CreatedBy)
        VALUES
            ('TEMP', @Title, @Description, @CategoryId, @PriorityId, @StatusId,
             @RaisedBy, @AddressLine, @Landmark, @WardId, @CityId, @RaisedBy);

        SET @NewComplaintId = SCOPE_IDENTITY();

        UPDATE FixMyCity.Complaint
        SET ComplaintNumber = 'FMC' + CONVERT(VARCHAR(4), YEAR(GETDATE()))
                             + RIGHT('000000' + CAST(@NewComplaintId AS VARCHAR(6)), 6)
        WHERE ComplaintId = @NewComplaintId;

        INSERT INTO FixMyCity.ComplaintHistory (ComplaintId, FieldChanged, OldValue, NewValue, CreatedBy)
        VALUES (@NewComplaintId, 'StatusId', NULL, CAST(@StatusId AS VARCHAR(10)), @RaisedBy);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.Complaint_GetByConsumerId 
@ConsumerId INT
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	29jule2026		Shrisha Gatti	Get Consumer by id
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.ComplaintId, c.ComplaintNumber, c.Title, c.Description,
           c.CategoryId, cat.CategoryName, c.PriorityId, pr.PriorityName,
           c.StatusId, st.StatusName, c.RaisedBy, c.AssignedTo, asg.Name AS AssigneeName,
           c.AddressLine, c.Landmark, c.WardId, w.WardName, c.CityId, ci.CityName,
           c.ResolvedDate, c.ClosedDate, c.ReopenCount, c.CreatedAt
    FROM FixMyCity.Complaint c
    JOIN FixMyCity.ComplaintCategory cat ON cat.CategoryId = c.CategoryId
    JOIN FixMyCity.ComplaintPriority pr  ON pr.PriorityId  = c.PriorityId
    JOIN FixMyCity.ComplaintStatus st    ON st.StatusId    = c.StatusId
    JOIN FixMyCity.Ward w                ON w.WardId       = c.WardId
    JOIN FixMyCity.City ci               ON ci.CityId      = c.CityId
    LEFT JOIN FixMyCity.Consumer asg     ON asg.ConsumerId = c.AssignedTo
    WHERE c.RaisedBy = @ConsumerId AND c.IsActive = 1
    ORDER BY c.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.Complaint_GetById 
	@ComplaintId INT,
	@ConsumerId INT
/*
***********************************************************************************************
	Date   			Modified By   	Purpose of Modification
1	29jule2026		Shrisha Gatti	get complaint by id 
***********************************************************************************************
*/
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.ComplaintId, c.ComplaintNumber, c.Title, c.Description,
           c.CategoryId, cat.CategoryName, c.PriorityId, pr.PriorityName,
           c.StatusId, st.StatusName, c.RaisedBy, c.AssignedTo, asg.Name AS AssigneeName,
           c.AddressLine, c.Landmark, c.WardId, w.WardName, c.CityId, ci.CityName,
           c.ResolvedDate, c.ClosedDate, c.ReopenCount, c.CreatedAt
    FROM FixMyCity.Complaint c
    JOIN FixMyCity.ComplaintCategory cat ON cat.CategoryId = c.CategoryId
    JOIN FixMyCity.ComplaintPriority pr  ON pr.PriorityId  = c.PriorityId
    JOIN FixMyCity.ComplaintStatus st    ON st.StatusId    = c.StatusId
    JOIN FixMyCity.Ward w                ON w.WardId       = c.WardId
    JOIN FixMyCity.City ci               ON ci.CityId      = c.CityId
    LEFT JOIN FixMyCity.Consumer asg     ON asg.ConsumerId = c.AssignedTo
    -- @ConsumerId scopes the query to the owning citizen — prevents one
    -- citizen from viewing another citizen's complaint by guessing the id
    WHERE c.ComplaintId = @ComplaintId AND c.RaisedBy = @ConsumerId AND c.IsActive = 1;
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.Category_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName FROM FixMyCity.ComplaintCategory WHERE IsActive = 1 ORDER BY CategoryName;
END
GO

CREATE OR ALTER PROCEDURE FixMyCity.Priority_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PriorityId, PriorityName FROM FixMyCity.ComplaintPriority WHERE IsActive = 1
    ORDER BY CASE PriorityName WHEN 'High' THEN 1 WHEN 'Medium' THEN 2 WHEN 'Low' THEN 3 ELSE 4 END;
END
GO


CREATE OR ALTER PROCEDURE FixMyCity.Consumer_GetById
    @ConsumerId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ConsumerId,
        Name,
        Email,
        Contact,
        DOB,
        AddressLine,
        CityId,
        WardId,
        RoleId,
        DeptId,
        Designation,
        IsActive
    FROM FixMyCity.Consumer
    WHERE ConsumerId = @ConsumerId
      AND IsActive = 1;
END
GO
select * from FixMyCity.ConsumerCredential
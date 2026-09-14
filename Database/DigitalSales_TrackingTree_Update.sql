-- Cập nhật CSDL cho cấu trúc Tiến trình & Checklist 4 tầng (DigitalSales)
-- 1. Thêm cột ParentID và DurationDays vào RM_DigitalSalesTracking
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RM_DigitalSalesTracking' AND COLUMN_NAME = 'ParentID')
BEGIN
    ALTER TABLE dbo.RM_DigitalSalesTracking ADD ParentID INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RM_DigitalSalesTracking' AND COLUMN_NAME = 'DurationDays')
BEGIN
    ALTER TABLE dbo.RM_DigitalSalesTracking ADD DurationDays INT NULL;
END
GO

-- 2. Cập nhật Stored Procedure RM_DigitalSalesTracking_GetBySalesID
IF OBJECT_ID('dbo.RM_DigitalSalesTracking_GetBySalesID', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_DigitalSalesTracking_GetBySalesID;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesTracking_GetBySalesID
    @DigitalSalesID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.TrackingID,
        t.DigitalSalesID,
        t.ParentID,
        t.ProcessID,
        ISNULL(p.ProcessName, N'Checklist tiến trình dự án') AS ProcessName,
        p.StatusID,
        st.StatusName AS SalesStatusName,
        (SELECT COUNT(1) FROM dbo.RM_DigitalSalesProcess pr WHERE pr.StatusID = p.StatusID AND pr.IsActive = 1 AND pr.IsDeleted = 0) AS ProcessCountOfStatus,
        t.ProgressID,
        ISNULL(t.TaskName, pg.ProgressName) AS TaskName,
        ISNULL(pg.ProgressName, t.TaskName) AS ProgressName,
        ISNULL(t.DurationDays, ISNULL(pg.DefaultDurationDays, 3)) AS DurationDays,
        ISNULL(pg.DefaultDurationDays, 3) AS DefaultDurationDays,
        t.AssignedUserID,
        u.FullName AS AssignedUserName,
        t.StartDate,
        t.Deadline,
        t.CompletedDate,
        t.Status,
        CASE t.Status 
            WHEN 1 THEN N'Chưa thực hiện' 
            WHEN 2 THEN N'Đang thực hiện' 
            WHEN 3 THEN N'Hoàn thành' 
            WHEN 4 THEN N'Quá hạn' 
            ELSE N'Khác' 
        END AS TaskStatusName,
        CASE 
            WHEN t.Status <> 3 AND t.Deadline < GETDATE() THEN 1 
            ELSE 0 
        END AS IsOverdue,
        t.ResultNote,
        t.AttachmentFile,
        t.IsCustomTask,
        t.SortOrder,
        t.CreatedDate
    FROM dbo.RM_DigitalSalesTracking t
    LEFT JOIN dbo.RM_DigitalSalesProgress pg ON t.ProgressID = pg.ProgressID
    LEFT JOIN dbo.RM_DigitalSalesProcess p ON t.ProcessID = p.ProcessID
    LEFT JOIN dbo.RM_DigitalSalesStatus st ON p.StatusID = st.StatusID
    LEFT JOIN dbo.Sys_Users u ON t.AssignedUserID = u.UserId
    WHERE t.DigitalSalesID = @DigitalSalesID
    ORDER BY ISNULL(p.SortOrder, 999) ASC, ISNULL(pg.SortOrder, 999) ASC, ISNULL(t.ParentID, 0) ASC, t.SortOrder ASC, t.TrackingID ASC;
END
GO

-- 3. Cập nhật Stored Procedure RM_DigitalSalesTracking_Save
IF OBJECT_ID('dbo.RM_DigitalSalesTracking_Save', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_DigitalSalesTracking_Save;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesTracking_Save
    @TrackingID INT,
    @DigitalSalesID INT,
    @ProcessID INT = NULL,
    @ProgressID INT = NULL,
    @TaskName NVARCHAR(500),
    @AssignedUserID INT = NULL,
    @StartDate DATETIME = NULL,
    @Deadline DATETIME = NULL,
    @Status TINYINT = 1,
    @ResultNote NVARCHAR(MAX) = NULL,
    @AttachmentFile NVARCHAR(500) = NULL,
    @IsCustomTask BIT = 1,
    @SortOrder INT = 0,
    @UserName VARCHAR(150),
    @ParentID INT = NULL,
    @DurationDays INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @TrackingID <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesTracking WHERE TrackingID = @TrackingID)
    BEGIN
        INSERT INTO dbo.RM_DigitalSalesTracking
        (
            DigitalSalesID, ParentID, DurationDays, ProcessID, ProgressID, TaskName, AssignedUserID,
            StartDate, Deadline, Status, ResultNote, AttachmentFile, IsCustomTask, SortOrder,
            CreatedDate, CreatedBy
        )
        VALUES
        (
            @DigitalSalesID, @ParentID, @DurationDays, @ProcessID, @ProgressID, @TaskName, @AssignedUserID,
            ISNULL(@StartDate, GETDATE()), @Deadline, ISNULL(@Status, 1), @ResultNote, @AttachmentFile, ISNULL(@IsCustomTask, 1), ISNULL(@SortOrder, 0),
            GETDATE(), @UserName
        );
        DECLARE @NewTrackingID INT = SCOPE_IDENTITY();
        SELECT @NewTrackingID;
        RETURN @NewTrackingID;
    END
    ELSE
    BEGIN
        UPDATE dbo.RM_DigitalSalesTracking
        SET
            ParentID = CASE WHEN @ParentID IS NOT NULL THEN @ParentID ELSE ParentID END,
            DurationDays = CASE WHEN @DurationDays IS NOT NULL THEN @DurationDays ELSE DurationDays END,
            ProcessID = CASE WHEN @ProcessID IS NOT NULL THEN @ProcessID ELSE ProcessID END,
            TaskName = ISNULL(@TaskName, TaskName),
            AssignedUserID = ISNULL(@AssignedUserID, AssignedUserID),
            StartDate = ISNULL(@StartDate, StartDate),
            Deadline = ISNULL(@Deadline, Deadline),
            Status = ISNULL(@Status, Status),
            CompletedDate = CASE WHEN @Status = 3 THEN GETDATE() ELSE CompletedDate END,
            ResultNote = ISNULL(@ResultNote, ResultNote),
            AttachmentFile = ISNULL(@AttachmentFile, AttachmentFile),
            SortOrder = ISNULL(@SortOrder, SortOrder),
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName
        WHERE TrackingID = @TrackingID;

        SELECT @TrackingID;
        RETURN @TrackingID;
    END
END
GO

-- 4. Cập nhật Stored Procedure RM_DigitalSalesTracking_Delete
IF OBJECT_ID('dbo.RM_DigitalSalesTracking_Delete', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_DigitalSalesTracking_Delete;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesTracking_Delete
    @TrackingID INT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.RM_DigitalSalesTracking WHERE ParentID = @TrackingID;
    DELETE FROM dbo.RM_DigitalSalesTracking WHERE TrackingID = @TrackingID;

    SELECT @@ROWCOUNT;
    RETURN @@ROWCOUNT;
END
GO

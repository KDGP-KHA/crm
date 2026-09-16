$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

# Step 1: Add column if not exists
$cmd = $conn.CreateCommand()
$cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RM_DigitalSalesTracking' AND COLUMN_NAME = 'CompletedBy')
BEGIN
    ALTER TABLE dbo.RM_DigitalSalesTracking ADD CompletedBy VARCHAR(150) NULL;
END
"@
$cmd.ExecuteNonQuery()
Write-Host "Step 1: Column CompletedBy ensured."

# Step 2: Update existing rows
$cmd.CommandText = @"
UPDATE dbo.RM_DigitalSalesTracking 
SET CompletedBy = ISNULL(LastModifiedBy, CreatedBy)
WHERE Status = 3 AND CompletedBy IS NULL;
"@
$cmd.ExecuteNonQuery()
Write-Host "Step 2: Existing rows updated."

# Step 3: Update SP RM_DigitalSalesTracking_GetBySalesID
$cmd.CommandText = @"
ALTER PROCEDURE dbo.RM_DigitalSalesTracking_GetBySalesID
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
        t.CompletedBy,
        ISNULL(uCompleted.FullName, ISNULL(t.CompletedBy, ISNULL(uModified.FullName, t.LastModifiedBy))) AS CompletedByName,
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
        t.CreatedDate,
        t.CreatedBy,
        ISNULL(uCreated.FullName, t.CreatedBy) AS CreatedByName,
        t.LastModifiedDate,
        t.LastModifiedBy,
        ISNULL(uModified.FullName, t.LastModifiedBy) AS LastModifiedByName,
        t.TrackingCode,
        t.TimelineID,
        tl.ActionDate AS TimelineDate
    FROM dbo.RM_DigitalSalesTracking t
    LEFT JOIN dbo.RM_DigitalSalesProgress pg ON t.ProgressID = pg.ProgressID
    LEFT JOIN dbo.RM_DigitalSalesProcess p ON t.ProcessID = p.ProcessID
    LEFT JOIN dbo.RM_DigitalSalesStatus st ON p.StatusID = st.StatusID
    LEFT JOIN dbo.Sys_Users u ON t.AssignedUserID = u.UserId
    LEFT JOIN dbo.Sys_Users uCreated ON t.CreatedBy = uCreated.UserName
    LEFT JOIN dbo.Sys_Users uModified ON t.LastModifiedBy = uModified.UserName
    LEFT JOIN dbo.Sys_Users uCompleted ON t.CompletedBy = uCompleted.UserName
    LEFT JOIN dbo.RM_DigitalSalesTimeline tl ON t.TimelineID = tl.TimelineID
    WHERE t.DigitalSalesID = @DigitalSalesID
    ORDER BY ISNULL(t.TimelineID, t.TrackingID) ASC, ISNULL(p.SortOrder, 999) ASC, ISNULL(pg.SortOrder, 999) ASC, ISNULL(t.ParentID, 0) ASC, t.SortOrder ASC, t.TrackingID ASC;
END
"@
$cmd.ExecuteNonQuery()
Write-Host "Step 3: SP RM_DigitalSalesTracking_GetBySalesID updated."

# Step 4: Update SP RM_DigitalSalesTracking_UpdateStatus
$cmd.CommandText = @"
ALTER PROCEDURE dbo.RM_DigitalSalesTracking_UpdateStatus
    @TrackingID INT,
    @Status TINYINT,                     -- 1: Chưa làm, 2: Đang làm, 3: Hoàn thành
    @ResultNote NVARCHAR(MAX) = NULL,
    @AttachmentFile NVARCHAR(500) = NULL,
    @AssignedUserID INT = NULL,
    @Deadline DATETIME = NULL,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DigitalSalesID INT;
    DECLARE @TaskName NVARCHAR(500);
    DECLARE @OldStatus TINYINT;
    DECLARE @ProcessID INT;
    DECLARE @ParentID INT;

    SELECT 
        @DigitalSalesID = DigitalSalesID,
        @TaskName = TaskName,
        @OldStatus = Status,
        @ProcessID = ProcessID,
        @ParentID = ParentID
    FROM dbo.RM_DigitalSalesTracking
    WHERE TrackingID = @TrackingID;

    IF @DigitalSalesID IS NULL
    BEGIN
        SELECT 0;
        RETURN 0;
    END

    -- Cập nhật bản thân tiến trình / công việc
    UPDATE dbo.RM_DigitalSalesTracking
    SET
        Status = @Status,
        CompletedDate = CASE WHEN @Status = 3 THEN ISNULL(CompletedDate, GETDATE()) ELSE CompletedDate END,
        CompletedBy = CASE WHEN @Status = 3 THEN ISNULL(CompletedBy, @UserName) ELSE CompletedBy END,
        ResultNote = ISNULL(@ResultNote, ResultNote),
        AttachmentFile = ISNULL(@AttachmentFile, AttachmentFile),
        AssignedUserID = ISNULL(@AssignedUserID, AssignedUserID),
        Deadline = ISNULL(@Deadline, Deadline),
        LastModifiedDate = GETDATE(),
        LastModifiedBy = @UserName
    WHERE TrackingID = @TrackingID;

    DECLARE @ActionByName NVARCHAR(250);
    SELECT TOP 1 @ActionByName = FullName FROM dbo.Sys_Users WHERE UserName = @UserName;
    IF @ActionByName IS NULL SET @ActionByName = @UserName;

    -- XỬ LÝ KHI TIẾN TRÌNH CHUYỂN SANG HOÀN THÀNH (Status = 3)
    IF @Status = 3
    BEGIN
        -- 1. Nếu đây là Tiến trình cha: Tự động hoàn thành tất cả công việc con chưa hoàn thành
        DECLARE @IncompleteChildren TABLE (
            ChildTrackingID INT,
            ChildTaskName NVARCHAR(500),
            ChildOldStatus TINYINT
        );

        INSERT INTO @IncompleteChildren (ChildTrackingID, ChildTaskName, ChildOldStatus)
        SELECT TrackingID, TaskName, Status
        FROM dbo.RM_DigitalSalesTracking
        WHERE ParentID = @TrackingID AND Status <> 3;

        DECLARE @ChildCount INT = 0;
        SELECT @ChildCount = COUNT(*) FROM @IncompleteChildren;

        IF @ChildCount > 0
        BEGIN
            -- Chuyển trạng thái các công việc con sang Hoàn thành (3)
            UPDATE dbo.RM_DigitalSalesTracking
            SET
                Status = 3,
                CompletedDate = GETDATE(),
                CompletedBy = @UserName,
                LastModifiedDate = GETDATE(),
                LastModifiedBy = @UserName
            WHERE ParentID = @TrackingID AND Status <> 3;

            -- Ghi log Activity cho từng công việc con tự động hoàn thành
            DECLARE @ChildID INT;
            DECLARE @ChildName NVARCHAR(500);
            DECLARE @ChildOldStat TINYINT;

            DECLARE curChildren CURSOR LOCAL FAST_FORWARD FOR
            SELECT ChildTrackingID, ChildTaskName, ChildOldStatus FROM @IncompleteChildren;

            OPEN curChildren;
            FETCH NEXT FROM curChildren INTO @ChildID, @ChildName, @ChildOldStat;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                INSERT INTO dbo.RM_DigitalSalesActivity
                (
                    DigitalSalesID, ActivityType, Content, Attachments, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted
                )
                VALUES
                (
                    @DigitalSalesID, 3, 
                    N'Tự động hoàn thành công việc: <b>' + @ChildName + N'</b> (theo tiến trình cha: ' + @TaskName + N')',
                    NULL, @ChildID, GETDATE(), @UserName, @ActionByName, 0
                );

                FETCH NEXT FROM curChildren INTO @ChildID, @ChildName, @ChildOldStat;
            END;

            CLOSE curChildren;
            DEALLOCATE curChildren;
        END

        -- 2. Ghi Activity Log cho bản thân Tiến trình / Công việc này nếu chuyển từ trạng thái khác sang Hoàn thành
        IF @OldStatus <> 3
        BEGIN
            DECLARE @CompleteContent NVARCHAR(MAX);
            IF @ParentID IS NULL OR @ParentID <= 0
            BEGIN
                SET @CompleteContent = N'Đã hoàn thành tiến trình: ' + @TaskName;
                IF @ChildCount > 0
                BEGIN
                    SET @CompleteContent = @CompleteContent + N' (Tự động hoàn thành ' + CAST(@ChildCount AS NVARCHAR(10)) + N' công việc con đính kèm)';
                END
            END
            ELSE
            BEGIN
                SET @CompleteContent = N'Đã hoàn thành công việc checklist: ' + @TaskName;
            END

            IF @ResultNote IS NOT NULL AND LTRIM(RTRIM(@ResultNote)) <> ''
            BEGIN
                SET @CompleteContent = @CompleteContent + N' | Kết quả: ' + @ResultNote;
            END

            INSERT INTO dbo.RM_DigitalSalesActivity
            (
                DigitalSalesID, ActivityType, Content, Attachments, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted
            )
            VALUES
            (
                @DigitalSalesID, 3, @CompleteContent, @AttachmentFile, @TrackingID, GETDATE(), @UserName, @ActionByName, 0
            );

            -- Kiểm tra nếu toàn bộ task trong ProcessID này đã hoàn thành thì ghi log ActivityType = 4 (Hoàn thành quy trình)
            IF @ProcessID IS NOT NULL AND NOT EXISTS (
                SELECT 1 FROM dbo.RM_DigitalSalesTracking 
                WHERE DigitalSalesID = @DigitalSalesID AND ProcessID = @ProcessID AND Status <> 3
            )
            BEGIN
                DECLARE @ProcName NVARCHAR(250);
                SELECT @ProcName = ProcessName FROM dbo.RM_DigitalSalesProcess WHERE ProcessID = @ProcessID;
                IF @ProcName IS NOT NULL
                BEGIN
                    INSERT INTO dbo.RM_DigitalSalesActivity
                    (
                        DigitalSalesID, ActivityType, Content, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted
                    )
                    VALUES
                    (
                        @DigitalSalesID, 4, N'Đã hoàn thành 100% các công việc trong quy trình: ' + @ProcName, @ProcessID, GETDATE(), @UserName, @ActionByName, 0
                    );
                END
            END
        END
    END
    ELSE IF @ResultNote IS NOT NULL AND LTRIM(RTRIM(@ResultNote)) <> '' AND (@OldStatus = @Status OR @Status = 2)
    BEGIN
        -- ActivityType = 5: Cập nhật tiến độ / ghi chú trong checklist
        INSERT INTO dbo.RM_DigitalSalesActivity
        (
            DigitalSalesID, ActivityType, Content, Attachments, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted
        )
        VALUES
        (
            @DigitalSalesID, 5, N'Cập nhật tiến độ công việc [' + @TaskName + N']: ' + @ResultNote, @AttachmentFile, @TrackingID, GETDATE(), @UserName, @ActionByName, 0
        );
    END

    SELECT 1;
    RETURN 1;
END
"@
$cmd.ExecuteNonQuery()
Write-Host "Step 4: SP RM_DigitalSalesTracking_UpdateStatus updated."

$conn.Close()

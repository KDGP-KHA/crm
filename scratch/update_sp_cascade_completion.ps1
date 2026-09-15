$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

# 1. Update RM_DigitalSalesTracking_UpdateStatus
$sql1 = @"
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
        CompletedDate = CASE WHEN @Status = 3 THEN GETDATE() ELSE CompletedDate END,
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

$cmd1 = $conn.CreateCommand()
$cmd1.CommandText = $sql1
$cmd1.ExecuteNonQuery()
Write-Host "Updated RM_DigitalSalesTracking_UpdateStatus successfully."

# 2. Update RM_DigitalSalesTracking_Save
$sql2 = @"
ALTER PROCEDURE dbo.RM_DigitalSalesTracking_Save
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
    @DurationDays INT = NULL,
    @TimelineID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ActionByName NVARCHAR(250);
    SELECT TOP 1 @ActionByName = FullName FROM dbo.Sys_Users WHERE UserName = @UserName;
    IF @ActionByName IS NULL SET @ActionByName = @UserName;

    IF @TrackingID <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesTracking WHERE TrackingID = @TrackingID)
    BEGIN
        -- Sinh ma tu dong: CV (cong viec con) hoac PR (tien trinh)
        DECLARE @NewCode VARCHAR(20);
        IF @ParentID IS NOT NULL AND @ParentID > 0
        BEGIN
            -- Cong viec con: CV + YY + MM + 7 chu so (VD: CV26090000001)
            DECLARE @CVPrefix VARCHAR(6) = 'CV' + RIGHT(CONVERT(VARCHAR(4), YEAR(GETDATE())), 2) + RIGHT('0' + CONVERT(VARCHAR(2), MONTH(GETDATE())), 2);
            DECLARE @CVMaxSeq INT = 0;
            SELECT @CVMaxSeq = ISNULL(MAX(CAST(RIGHT(TrackingCode, 7) AS INT)), 0)
            FROM dbo.RM_DigitalSalesTracking
            WHERE TrackingCode LIKE @CVPrefix + '%' AND LEN(TrackingCode) = 13;
            SET @CVMaxSeq = @CVMaxSeq + 1;
            SET @NewCode = @CVPrefix + RIGHT('0000000' + CAST(@CVMaxSeq AS VARCHAR(7)), 7);
        END
        ELSE
        BEGIN
            -- Tien trinh: PR + YY + MM + 6 chu so (VD: PR2609000001)
            DECLARE @PRPrefix VARCHAR(6) = 'PR' + RIGHT(CONVERT(VARCHAR(4), YEAR(GETDATE())), 2) + RIGHT('0' + CONVERT(VARCHAR(2), MONTH(GETDATE())), 2);
            DECLARE @PRMaxSeq INT = 0;
            SELECT @PRMaxSeq = ISNULL(MAX(CAST(RIGHT(TrackingCode, 6) AS INT)), 0)
            FROM dbo.RM_DigitalSalesTracking
            WHERE TrackingCode LIKE @PRPrefix + '%' AND LEN(TrackingCode) = 12;
            SET @PRMaxSeq = @PRMaxSeq + 1;
            SET @NewCode = @PRPrefix + RIGHT('000000' + CAST(@PRMaxSeq AS VARCHAR(6)), 6);
        END

        INSERT INTO dbo.RM_DigitalSalesTracking
        (
            DigitalSalesID, ParentID, DurationDays, ProcessID, ProgressID, TaskName, AssignedUserID,
            StartDate, Deadline, Status, ResultNote, AttachmentFile, IsCustomTask, SortOrder,
            CreatedDate, CreatedBy, TrackingCode, TimelineID
        )
        VALUES
        (
            @DigitalSalesID, @ParentID, @DurationDays, @ProcessID, @ProgressID, @TaskName, @AssignedUserID,
            ISNULL(@StartDate, GETDATE()), @Deadline, ISNULL(@Status, 1), @ResultNote, @AttachmentFile, ISNULL(@IsCustomTask, 1), ISNULL(@SortOrder, 0),
            GETDATE(), @UserName, @NewCode, @TimelineID
        );
        DECLARE @NewTrackingID INT = SCOPE_IDENTITY();
        SELECT @NewTrackingID;
        RETURN @NewTrackingID;
    END
    ELSE
    BEGIN
        DECLARE @OldStatus TINYINT;
        SELECT @OldStatus = Status FROM dbo.RM_DigitalSalesTracking WHERE TrackingID = @TrackingID;

        UPDATE dbo.RM_DigitalSalesTracking
        SET
            ParentID = CASE WHEN @ParentID IS NOT NULL THEN @ParentID ELSE ParentID END,
            DurationDays = CASE WHEN @DurationDays IS NOT NULL THEN @DurationDays ELSE DurationDays END,
            ProcessID = CASE WHEN @ProcessID IS NOT NULL THEN @ProcessID ELSE ProcessID END,
            TimelineID = CASE WHEN @TimelineID IS NOT NULL THEN @TimelineID ELSE TimelineID END,
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

        -- Khi Tiến trình được cập nhật Hoàn thành (Status = 3) thì tự động chuyển các công việc con chưa hoàn thành sang Hoàn thành và ghi log
        IF @Status = 3
        BEGIN
            DECLARE @IncompleteChildTasks TABLE (
                ChildTrackingID INT,
                ChildTaskName NVARCHAR(500),
                ChildOldStatus TINYINT
            );

            INSERT INTO @IncompleteChildTasks (ChildTrackingID, ChildTaskName, ChildOldStatus)
            SELECT TrackingID, TaskName, Status
            FROM dbo.RM_DigitalSalesTracking
            WHERE ParentID = @TrackingID AND Status <> 3;

            DECLARE @NumChildren INT = 0;
            SELECT @NumChildren = COUNT(*) FROM @IncompleteChildTasks;

            IF @NumChildren > 0
            BEGIN
                UPDATE dbo.RM_DigitalSalesTracking
                SET
                    Status = 3,
                    CompletedDate = GETDATE(),
                    LastModifiedDate = GETDATE(),
                    LastModifiedBy = @UserName
                WHERE ParentID = @TrackingID AND Status <> 3;

                -- Ghi log cho từng công việc con
                DECLARE @cID INT;
                DECLARE @cName NVARCHAR(500);
                DECLARE @cOldStat TINYINT;

                DECLARE curChildSave CURSOR LOCAL FAST_FORWARD FOR
                SELECT ChildTrackingID, ChildTaskName, ChildOldStatus FROM @IncompleteChildTasks;

                OPEN curChildSave;
                FETCH NEXT FROM curChildSave INTO @cID, @cName, @cOldStat;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    INSERT INTO dbo.RM_DigitalSalesActivity
                    (
                        DigitalSalesID, ActivityType, Content, Attachments, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted
                    )
                    VALUES
                    (
                        @DigitalSalesID, 3, 
                        N'Tự động hoàn thành công việc: <b>' + @cName + N'</b> (theo tiến trình cha: ' + @TaskName + N')',
                        NULL, @cID, GETDATE(), @UserName, @ActionByName, 0
                    );

                    FETCH NEXT FROM curChildSave INTO @cID, @cName, @cOldStat;
                END;

                CLOSE curChildSave;
                DEALLOCATE curChildSave;
            END
        END

        SELECT @TrackingID;
        RETURN @TrackingID;
    END
END
"@

$cmd2 = $conn.CreateCommand()
$cmd2.CommandText = $sql2
$cmd2.ExecuteNonQuery()
Write-Host "Updated RM_DigitalSalesTracking_Save successfully."

$conn.Close()

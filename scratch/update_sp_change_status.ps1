$connStr = 'Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;'
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
$cmd = $conn.CreateCommand()

$cmd.CommandText = @"
ALTER PROCEDURE dbo.RM_DigitalSales_ChangeStatus
    @DigitalSalesID INT,
    @NewStatusID INT,
    @Note NVARCHAR(MAX) = NULL,
    @AttachmentPath NVARCHAR(MAX) = NULL,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStatusID INT;
    DECLARE @CurrentBusinessType TINYINT;
    DECLARE @NewBusinessType TINYINT;
    DECLARE @NewStatusName NVARCHAR(250);
    DECLARE @AssignedEmployeeID INT;

    SELECT 
        @CurrentStatusID = StatusID,
        @CurrentBusinessType = BusinessType,
        @AssignedEmployeeID = AssignedEmployeeID
    FROM dbo.RM_DigitalSales
    WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0;

    IF @CurrentStatusID IS NULL 
    BEGIN
        SELECT -1;
        RETURN -1;
    END

    SELECT 
        @NewBusinessType = BusinessType,
        @NewStatusName = StatusName
    FROM dbo.RM_DigitalSalesStatus
    WHERE StatusID = @NewStatusID AND IsActive = 1 AND IsDeleted = 0;

    IF @NewBusinessType IS NULL 
    BEGIN
        SELECT -2;
        RETURN -2;
    END

    -- Ràng buộc chuyển sang dự án
    IF @NewBusinessType = 2
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0)
        BEGIN
            SELECT -3;
            RETURN -3;
        END

        IF NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesMember WHERE DigitalSalesID = @DigitalSalesID AND IsActive = 1)
        BEGIN
            SELECT -4;
            RETURN -4;
        END
    END

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.RM_DigitalSales
        SET
            StatusID = @NewStatusID,
            BusinessType = @NewBusinessType,
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName,
            EndDate = CASE WHEN @NewStatusID = 8 THEN GETDATE() ELSE EndDate END
        WHERE DigitalSalesID = @DigitalSalesID;

        DECLARE @ActionDesc NVARCHAR(MAX);
        IF @CurrentBusinessType = 1 AND @NewBusinessType = 2
        BEGIN
            SET @ActionDesc = N'Chuyển đổi thành công từ CƠ HỘI sang DỰ ÁN. Trạng thái mới: ' + @NewStatusName;
        END
        ELSE
        BEGIN
            SET @ActionDesc = N'Chuyển trạng thái sang: ' + @NewStatusName;
        END

        IF @Note IS NOT NULL AND LTRIM(RTRIM(@Note)) <> ''
        BEGIN
            SET @ActionDesc = @ActionDesc + N' | Ghi chú: ' + @Note;
        END

        -- Ghi nhận Timeline cũ (AttachmentPath lưu tối đa 500 ký tự cho tương thích bảng Timeline)
        INSERT INTO dbo.RM_DigitalSalesTimeline
        (
            DigitalSalesID, FromStatusID, ToStatusID, FromBusinessType, ToBusinessType, ActionDate, ActionBy, Note, AttachmentPath
        )
        VALUES
        (
            @DigitalSalesID, @CurrentStatusID, @NewStatusID, @CurrentBusinessType, @NewBusinessType, GETDATE(), @UserName, @ActionDesc, LEFT(@AttachmentPath, 500)
        );

        DECLARE @NewTimelineID INT = SCOPE_IDENTITY();

        -- Tự động ghi nhận Activity Stream (ActivityType = 2: Chuyển trạng thái)
        DECLARE @ActionByName NVARCHAR(250);
        SELECT TOP 1 @ActionByName = FullName FROM dbo.Sys_Users WHERE UserName = @UserName;
        IF @ActionByName IS NULL SET @ActionByName = @UserName;

        INSERT INTO dbo.RM_DigitalSalesActivity
        (
            DigitalSalesID, ActivityType, Content, Attachments, ReferenceID, ActionDate, ActionBy, ActionByName, IsDeleted
        )
        VALUES
        (
            @DigitalSalesID, 2, @ActionDesc, @AttachmentPath, @NewTimelineID, GETDATE(), @UserName, @ActionByName, 0
        );

        -- Tự động sinh các tiến trình theo quy trình nếu chưa có
        INSERT INTO dbo.RM_DigitalSalesTracking (DigitalSalesID, ProcessID, ProgressID, TaskName, AssignedUserID, StartDate, Deadline, Status, IsCustomTask, SortOrder, CreatedDate, CreatedBy)
        SELECT 
            @DigitalSalesID,
            p.ProcessID,
            pg.ProgressID,
            pg.ProgressName,
            @AssignedEmployeeID,
            GETDATE(),
            DATEADD(day, ISNULL(pg.DefaultDurationDays, 3), GETDATE()),
            1, -- Chưa làm
            0,
            pg.SortOrder,
            GETDATE(),
            @UserName
        FROM dbo.RM_DigitalSalesProcess p
        INNER JOIN dbo.RM_DigitalSalesProgress pg ON p.ProcessID = pg.ProcessID
        WHERE p.StatusID = @NewStatusID 
          AND p.IsActive = 1 
          AND p.IsDeleted = 0 
          AND pg.IsActive = 1 
          AND pg.IsDeleted = 0
          AND NOT EXISTS (
              SELECT 1 FROM dbo.RM_DigitalSalesTracking t 
              WHERE t.DigitalSalesID = @DigitalSalesID AND t.ProgressID = pg.ProgressID
          );

        COMMIT TRANSACTION;
        SELECT 1;
        RETURN 1;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0;
        RETURN 0;
    END CATCH
END
"@

$cmd.ExecuteNonQuery()
Write-Host "Updated dbo.RM_DigitalSales_ChangeStatus successfully!"

# Đánh dấu IsDeleted = 1 cho Activity 154 (thảo luận trùng sinh ra từ đợt test cũ lúc 19:26:26)
$cmd.CommandText = "UPDATE dbo.RM_DigitalSalesActivity SET IsDeleted = 1 WHERE ActivityID = 154"
$affected = $cmd.ExecuteNonQuery()
Write-Host "Marked Activity 154 as deleted ($affected rows affected)"

$conn.Close()

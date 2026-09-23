-- ==========================================================================================
-- Stored Procedure: dbo.RM_DigitalSalesTracking_ChangeProcessOfStatus
-- Mục đích: Thực hiện US-08 - Cải tiến Đổi Quy Trình:
--          Chỉ cập nhật ProcessID mới cho các tiến trình thuộc trạng thái,
--          KHÔNG xóa bất kỳ tiến trình cũ nào, KHÔNG tự động chèn thêm tiến trình mẫu.
--          Bảo toàn 100% tiến trình, công việc con, kết quả báo cáo và file đính kèm.
-- ==========================================================================================
IF OBJECT_ID('dbo.RM_DigitalSalesTracking_ChangeProcessOfStatus', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_DigitalSalesTracking_ChangeProcessOfStatus;
GO

CREATE PROCEDURE dbo.RM_DigitalSalesTracking_ChangeProcessOfStatus
    @DigitalSalesID INT,
    @StatusID INT,
    @NewProcessID INT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Cập nhật ProcessID = @NewProcessID cho toàn bộ các task (cha và con) thuộc các quy trình của trạng thái @StatusID
    UPDATE t
    SET t.ProcessID = @NewProcessID,
        t.LastModifiedDate = GETDATE(),
        t.LastModifiedBy = @UserName
    FROM dbo.RM_DigitalSalesTracking t
    INNER JOIN dbo.RM_DigitalSalesProcess p ON t.ProcessID = p.ProcessID
    WHERE t.DigitalSalesID = @DigitalSalesID
      AND p.StatusID = @StatusID;

    DECLARE @UpdatedCount INT = @@ROWCOUNT;

    -- 2. Nếu hồ sơ chưa có bất kỳ task nào thuộc trạng thái này, tạo 1 placeholder task rỗng để hiển thị tiêu đề quy trình
    IF @UpdatedCount = 0 AND NOT EXISTS (
        SELECT 1 
        FROM dbo.RM_DigitalSalesTracking t 
        INNER JOIN dbo.RM_DigitalSalesProcess p ON t.ProcessID = p.ProcessID 
        WHERE t.DigitalSalesID = @DigitalSalesID AND p.StatusID = @StatusID
    )
    BEGIN
        INSERT INTO dbo.RM_DigitalSalesTracking
        (
            DigitalSalesID, ProcessID, ProgressID, TaskName, DurationDays, StartDate, Deadline,
            Status, IsCustomTask, SortOrder, CreatedDate, CreatedBy
        )
        VALUES
        (
            @DigitalSalesID, @NewProcessID, NULL, N'', 3, GETDATE(), DATEADD(day, 3, GETDATE()),
            1, 1, 1, GETDATE(), @UserName
        );
    END

    SELECT 1;
    RETURN 1;
END
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.RM_DigitalSalesReview_GetList', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_DigitalSalesReview_GetList;
GO
CREATE PROCEDURE dbo.RM_DigitalSalesReview_GetList
    @ReviewBatchID INT,
    @BusinessType TINYINT = 0,
    @DepartmentID INT = 0,
    @EmployeeID INT = 0,
    @StatusID INT = 0,
    @IsReviewed BIT = NULL,
    @Search NVARCHAR(250) = NULL,
    @Order VARCHAR(3) = '1',
    @OrderDir VARCHAR(10) = 'ASC',
    @PageIndex INT = 0,
    @PageSize INT = 10,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ReviewLevel INT;
    SELECT @ReviewLevel = ReviewLevel
    FROM dbo.Sys_Users
    WHERE UserName = @UserName AND ISNULL(IsDeleted, 0) = 0;

    DECLARE @UserTable TABLE (ID INT, UserName VARCHAR(150));
    INSERT INTO @UserTable (ID, UserName)
    EXEC dbo.Sys_User_GetByReviewDepartment @UserName;

    SET @Order = ISNULL(@Order, '1');
    SET @OrderDir = UPPER(ISNULL(@OrderDir, 'ASC'));
    SET @PageIndex = ISNULL(@PageIndex, 0);
    SET @PageSize = ISNULL(@PageSize, 10);
    SET @Search = NULLIF(LTRIM(RTRIM(@Search)), '');

    ;WITH DepartmentTree AS
    (
        SELECT BoPhan_ID
        FROM dbo.MN_BoPhan
        WHERE BoPhan_ID = @DepartmentID

        UNION ALL

        SELECT child.BoPhan_ID
        FROM dbo.MN_BoPhan child
        INNER JOIN DepartmentTree parent ON child.BoPhanCha_ID = parent.BoPhan_ID
        WHERE ISNULL(child.Da_Xoa, 0) = 0
    ),
    ReviewData AS
    (
        SELECT
            ds.DigitalSalesID,
            ds.Code,
            ds.Title,
            ds.BusinessType,
            CASE ds.BusinessType WHEN 1 THEN N'Cơ hội kinh doanh' WHEN 2 THEN N'Dự án' ELSE N'' END AS BusinessTypeName,
            st.StatusName,
            c.CustomerName,
            assigned.FullName AS AssignedEmployeeName,
            department.TenBoPhan AS DepartmentName,
            ds.ExpectedDate,
            ds.TotalExpectedRevenue,
            item.ReviewBatchItemID,
            ISNULL(item.HighestReviewedLevel, 0) AS HighestReviewedLevel,
            item.LastReviewedDate,
            CAST(CASE
                WHEN @ReviewLevel = 2 AND ISNULL(item.HighestReviewedLevel, 0) = 2 THEN 1
                WHEN @ReviewLevel = 3 AND ISNULL(item.HighestReviewedLevel, 0) IN (2, 3) THEN 1
                WHEN @ReviewLevel = 4 AND ISNULL(item.HighestReviewedLevel, 0) IN (2, 3, 4) THEN 1
                ELSE 0
            END AS BIT) AS IsReviewed,
            ds.CreatedDate
        FROM dbo.RM_DigitalSales ds
        LEFT JOIN dbo.RM_DigitalSalesStatus st ON st.StatusID = ds.StatusID AND ISNULL(st.IsDeleted, 0) = 0
        LEFT JOIN dbo.RM_Customer c ON c.CustomerID = ds.CustomerID
        LEFT JOIN dbo.Sys_Users assigned ON assigned.UserId = ds.AssignedEmployeeID
        LEFT JOIN dbo.MN_BoPhan department ON department.BoPhan_ID = ds.DepartmentID
        LEFT JOIN dbo.RM_ReviewBatchItem item
            ON item.ReviewBatchID = @ReviewBatchID
            AND item.ObjectType IS NULL
            AND item.ObjectID = ds.DigitalSalesID
            AND ISNULL(item.IsDeleted, 0) = 0
        WHERE ISNULL(ds.IsDeleted, 0) = 0
          AND @ReviewLevel IN (2, 3, 4)
          AND (
                @ReviewLevel = 2
                OR (@ReviewLevel = 3 AND ISNULL(item.HighestReviewedLevel, 0) <> 2)
                OR (@ReviewLevel = 4 AND ISNULL(item.HighestReviewedLevel, 0) NOT IN (2, 3))
              )
          AND (
                ds.CreatedBy = @UserName
                OR ds.AssignedEmployeeID IN (SELECT ID FROM @UserTable)
                OR EXISTS
                (
                    SELECT 1
                    FROM dbo.RM_DigitalSalesMember member
                    WHERE member.DigitalSalesID = ds.DigitalSalesID
                      AND ISNULL(member.IsActive, 0) = 1
                      AND member.UserID IN (SELECT ID FROM @UserTable)
                )
              )
          AND (@Search IS NULL OR ds.Code LIKE '%' + @Search + '%' OR ds.Title LIKE N'%' + @Search + '%' OR c.CustomerName LIKE N'%' + @Search + '%')
          AND (@BusinessType = 0 OR ds.BusinessType = @BusinessType)
          AND (@StatusID = 0 OR ds.StatusID = @StatusID)
          AND (@DepartmentID = 0 OR ds.DepartmentID IN (SELECT BoPhan_ID FROM DepartmentTree))
          AND (
                @EmployeeID = 0
                OR ds.AssignedEmployeeID = @EmployeeID
                OR EXISTS
                (
                    SELECT 1
                    FROM dbo.RM_DigitalSalesMember member
                    WHERE member.DigitalSalesID = ds.DigitalSalesID
                      AND ISNULL(member.IsActive, 0) = 1
                      AND member.UserID = @EmployeeID
                )
              )
    ),
    Filtered AS
    (
        SELECT *
        FROM ReviewData
        WHERE @IsReviewed IS NULL OR IsReviewed = @IsReviewed
    ),
    Numbered AS
    (
        SELECT
            ROW_NUMBER() OVER
            (
                ORDER BY
                    CASE WHEN @Order = '1' AND @OrderDir = 'ASC' THEN Title END ASC,
                    CASE WHEN @Order = '1' AND @OrderDir = 'DESC' THEN Title END DESC,
                    CASE WHEN @Order = '2' AND @OrderDir = 'ASC' THEN BusinessType END ASC,
                    CASE WHEN @Order = '2' AND @OrderDir = 'DESC' THEN BusinessType END DESC,
                    CASE WHEN @Order = '3' AND @OrderDir = 'ASC' THEN CustomerName END ASC,
                    CASE WHEN @Order = '3' AND @OrderDir = 'DESC' THEN CustomerName END DESC,
                    CASE WHEN @Order = '4' AND @OrderDir = 'ASC' THEN StatusName END ASC,
                    CASE WHEN @Order = '4' AND @OrderDir = 'DESC' THEN StatusName END DESC,
                    CASE WHEN @Order = '5' AND @OrderDir = 'ASC' THEN AssignedEmployeeName END ASC,
                    CASE WHEN @Order = '5' AND @OrderDir = 'DESC' THEN AssignedEmployeeName END DESC,
                    CreatedDate DESC,
                    DigitalSalesID DESC
            ) AS RowIndex,
            COUNT(1) OVER () AS TotalRow,
            *
        FROM Filtered
    )
    SELECT
        numbered.*,
        latestReview.LastReviewDate,
        latestReview.LastReviewerName,
        latestReview.LastReviewComment,
        ISNULL(latestReview.ReviewCount, 0) AS ReviewCount
    FROM Numbered numbered
    OUTER APPLY
    (
        SELECT TOP 1
            history.CreatedDate AS LastReviewDate,
            ISNULL(NULLIF(reviewer.FullName, ''), history.CreatedBy) AS LastReviewerName,
            dbo.fn_DecodeHtmlEntities(dbo.fn_StripHtml(ISNULL(history.ReviewComment, ''))) AS LastReviewComment,
            (SELECT COUNT(1) FROM dbo.RM_ReviewHistory historyCount WHERE historyCount.ReviewBatchItemID = numbered.ReviewBatchItemID AND ISNULL(historyCount.IsDeleted, 0) = 0) AS ReviewCount
        FROM dbo.RM_ReviewHistory history
        LEFT JOIN dbo.Sys_Users reviewer ON reviewer.UserName = history.CreatedBy
        WHERE history.ReviewBatchItemID = numbered.ReviewBatchItemID
          AND ISNULL(history.IsDeleted, 0) = 0
        ORDER BY history.CreatedDate DESC, history.ReviewHistoryID DESC
    ) latestReview
    WHERE (@PageSize <= 0 OR numbered.RowIndex BETWEEN @PageIndex + 1 AND @PageIndex + @PageSize)
    ORDER BY numbered.RowIndex
    OPTION (MAXRECURSION 100);
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesReview_Save', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_DigitalSalesReview_Save;
GO
CREATE PROCEDURE dbo.RM_DigitalSalesReview_Save
    @ReviewBatchID INT,
    @DigitalSalesID INT,
    @ReviewComment NVARCHAR(MAX),
    @IsConfirmed BIT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ReviewBatchItemID INT;
    DECLARE @ReviewLevel TINYINT;
    DECLARE @Result INT = -1;

    SELECT @ReviewLevel = ReviewLevel
    FROM dbo.Sys_Users
    WHERE UserName = @UserName AND ISNULL(IsDeleted, 0) = 0;

    IF @ReviewBatchID <= 0 OR @DigitalSalesID <= 0 OR @ReviewLevel NOT IN (2, 3, 4)
        RETURN -1;

    IF NOT EXISTS (SELECT 1 FROM dbo.RM_ReviewBatch WHERE ReviewBatchID = @ReviewBatchID AND ISNULL(IsDeleted, 0) = 0)
        RETURN -1;

    IF NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSales WHERE DigitalSalesID = @DigitalSalesID AND ISNULL(IsDeleted, 0) = 0)
        RETURN -1;

    BEGIN TRANSACTION;
    BEGIN TRY
        SELECT TOP 1 @ReviewBatchItemID = ReviewBatchItemID
        FROM dbo.RM_ReviewBatchItem WITH (UPDLOCK, HOLDLOCK)
        WHERE ReviewBatchID = @ReviewBatchID
          AND ObjectType IS NULL
          AND ObjectID = @DigitalSalesID
          AND ISNULL(IsDeleted, 0) = 0;

        IF @ReviewBatchItemID IS NULL
        BEGIN
            INSERT INTO dbo.RM_ReviewBatchItem
            (
                ReviewBatchID, ObjectType, ObjectID, CurrentReviewLevel,
                HighestReviewedLevel, LastReviewedDate, IsCompleted,
                IsDeleted, CreatedBy, CreatedDate
            )
            VALUES
            (
                @ReviewBatchID, NULL, @DigitalSalesID, @ReviewLevel,
                CASE WHEN @IsConfirmed = 1 THEN @ReviewLevel ELSE 0 END,
                GETDATE(), CASE WHEN @IsConfirmed = 1 THEN 1 ELSE 0 END,
                0, @UserName, GETDATE()
            );
            SET @ReviewBatchItemID = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE dbo.RM_ReviewBatchItem
            SET CurrentReviewLevel = @ReviewLevel,
                HighestReviewedLevel = CASE
                    WHEN @IsConfirmed = 1 AND ISNULL(HighestReviewedLevel, 0) = 0 THEN @ReviewLevel
                    WHEN @IsConfirmed = 1 AND @ReviewLevel < HighestReviewedLevel THEN @ReviewLevel
                    ELSE HighestReviewedLevel
                END,
                LastReviewedDate = GETDATE(),
                IsCompleted = CASE WHEN @IsConfirmed = 1 THEN 1 ELSE IsCompleted END,
                LastModifiedBy = @UserName,
                LastModifiedDate = GETDATE()
            WHERE ReviewBatchItemID = @ReviewBatchItemID;
        END

        INSERT INTO dbo.RM_ReviewHistory
        (
            ReviewBatchItemID, Reviewer, ReviewLevel, ReviewAction,
            ReviewComment, IsConfirmed, CreatedBy, CreatedDate
        )
        VALUES
        (
            @ReviewBatchItemID, @UserName, @ReviewLevel,
            CASE WHEN @IsConfirmed = 1 THEN 2 ELSE 1 END,
            @ReviewComment, @IsConfirmed, @UserName, GETDATE()
        );

        SET @Result = SCOPE_IDENTITY();
        COMMIT TRANSACTION;
        RETURN @Result;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        INSERT INTO dbo.Sys_ProcedureLogs (LogDate, ProcedureName, ErrorLine, ErrorMessage, AdditionalInfo)
        SELECT GETDATE(), ERROR_PROCEDURE(), ERROR_LINE(), ERROR_MESSAGE(), CONCAT('DigitalSalesID=', @DigitalSalesID);
        RETURN -1;
    END CATCH
END
GO

IF OBJECT_ID('dbo.RM_DigitalSalesReview_GetHistory', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_DigitalSalesReview_GetHistory;
GO
CREATE PROCEDURE dbo.RM_DigitalSalesReview_GetHistory
    @DigitalSalesID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        history.ReviewHistoryID,
        history.ReviewBatchItemID,
        batch.ReviewBatchID,
        batch.BatchCode,
        batch.BatchName,
        batch.FromDate,
        batch.ToDate,
        ISNULL(NULLIF(reviewer.FullName, ''), history.Reviewer) AS Reviewer,
        history.ReviewLevel,
        history.ReviewAction,
        history.ReviewComment,
        history.IsConfirmed,
        history.CreatedBy,
        history.CreatedDate
    FROM dbo.RM_ReviewHistory history
    INNER JOIN dbo.RM_ReviewBatchItem item
        ON item.ReviewBatchItemID = history.ReviewBatchItemID
       AND item.ObjectType IS NULL
       AND item.ObjectID = @DigitalSalesID
       AND ISNULL(item.IsDeleted, 0) = 0
    INNER JOIN dbo.RM_ReviewBatch batch
        ON batch.ReviewBatchID = item.ReviewBatchID
       AND ISNULL(batch.IsDeleted, 0) = 0
    LEFT JOIN dbo.Sys_Users reviewer ON reviewer.UserName = history.Reviewer
    WHERE ISNULL(history.IsDeleted, 0) = 0
    ORDER BY batch.FromDate DESC, history.CreatedDate DESC, history.ReviewHistoryID DESC;
END
GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RM_ReviewBatchItem_DigitalSales'
      AND object_id = OBJECT_ID('dbo.RM_ReviewBatchItem')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RM_ReviewBatchItem_DigitalSales
        ON dbo.RM_ReviewBatchItem (ReviewBatchID, ObjectID)
        INCLUDE (HighestReviewedLevel, LastReviewedDate, IsCompleted)
        WHERE ObjectType IS NULL AND IsDeleted = 0;
END
GO

DECLARE @Messages TABLE (LabelKey VARCHAR(500) NOT NULL PRIMARY KEY, Message NVARCHAR(MAX) NOT NULL);
INSERT INTO @Messages (LabelKey, Message)
VALUES
    ('ReviewDigitalSales_Title', N'Rà soát DigitalSales'),
    ('ReviewDigitalSales_List_Title', N'Danh sách DigitalSales cần rà soát'),
    ('ReviewDigitalSales_Keyword_Label', N'Từ khóa'),
    ('ReviewDigitalSales_Keyword_Placeholder', N'Tìm theo mã, tên hồ sơ hoặc khách hàng'),
    ('ReviewDigitalSales_BusinessType_Label', N'Loại hình'),
    ('ReviewDigitalSales_Status_Label', N'Trạng thái'),
    ('ReviewDigitalSales_Department_Label', N'Phòng ban'),
    ('ReviewDigitalSales_Employee_Label', N'Nhân sự phụ trách'),
    ('ReviewDigitalSales_IsReviewed_Label', N'Tình trạng rà soát'),
    ('ReviewDigitalSales_Batch_Option', N'-- Chọn đợt rà soát --'),
    ('ReviewDigitalSales_Column_Record', N'Hồ sơ DigitalSales'),
    ('ReviewDigitalSales_Column_ReviewInfo', N'Thông tin rà soát'),
    ('ReviewDigitalSales_NotReviewed', N'Chưa rà soát'),
    ('ReviewDigitalSales_Action_Review', N'Rà soát'),
    ('ReviewDigitalSales_Action_ViewDetail', N'Xem chi tiết'),
    ('ReviewDigitalSales_ReviewCount_Format', N'{0} lượt'),
    ('ReviewDigitalSales_InvalidData_Message', N'Dữ liệu rà soát DigitalSales không hợp lệ.'),
    ('ReviewDigitalSales_Collapse', N'Thu gọn form rà soát'),
    ('ReviewDigitalSales_Expand', N'Mở rộng form rà soát'),
    ('ReviewDigitalSales_Form_AriaLabel', N'Form rà soát DigitalSales'),
    ('ReviewDigitalSales_HistoryTab', N'5. Lịch sử rà soát'),
    ('ReviewDigitalSales_Level', N'Cấp'),
    ('ReviewDigitalSales_Confirmed', N'Đã xác nhận rà soát'),
    ('ReviewDigitalSales_Commented', N'Đã cho ý kiến'),
    ('ReviewDigitalSales_ShowMore', N'Xem thêm'),
    ('ReviewDigitalSales_ShowLess', N'Ẩn bớt'),
    ('ReviewDigitalSales_HistoryEmpty', N'Chưa có lịch sử rà soát'),
    ('Button_Reset', N'Đặt lại');

UPDATE target
SET target.Message = source.Message
FROM dbo.Sys_Messages target
INNER JOIN @Messages source ON source.LabelKey = target.LabelKey
WHERE target.LangCode = 'vi-VN';

INSERT INTO dbo.Sys_Messages (LangCode, LabelKey, Message)
SELECT 'vi-VN', source.LabelKey, source.Message
FROM @Messages source
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Sys_Messages target
    WHERE target.LangCode = 'vi-VN' AND target.LabelKey = source.LabelKey
);
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF COL_LENGTH('dbo.RM_ReviewHistory', 'ReviewConclusion') IS NULL
BEGIN
    ALTER TABLE dbo.RM_ReviewHistory ADD ReviewConclusion TINYINT NULL;
END
GO

IF OBJECT_ID('dbo.RM_ReviewHistory_Save', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_ReviewHistory_Save;
GO
CREATE PROCEDURE dbo.RM_ReviewHistory_Save
    @ReviewHistoryID INT,
    @ReviewBatchItemID INT,
    @ReviewComment NVARCHAR(MAX),
    @IsConfirmed BIT,
    @ReviewConclusion TINYINT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ReviewLevel TINYINT;
    SELECT @ReviewLevel = ReviewLevel
    FROM dbo.Sys_Users
    WHERE UserName = @UserName AND ISNULL(IsDeleted, 0) = 0;

    IF @ReviewHistoryID <= 0 OR @ReviewBatchItemID <= 0 OR @ReviewLevel NOT IN (2, 3, 4)
        RETURN -1;

    IF (@IsConfirmed = 1 AND ISNULL(@ReviewConclusion, 0) NOT IN (1, 2, 3))
       OR (@ReviewConclusion IS NOT NULL AND @ReviewConclusion NOT IN (1, 2, 3))
        RETURN -1;

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.RM_ReviewHistory
        SET ReviewBatchItemID = @ReviewBatchItemID,
            Reviewer = @UserName,
            ReviewLevel = @ReviewLevel,
            ReviewAction = CASE WHEN @IsConfirmed = 1 THEN 2 ELSE 1 END,
            ReviewComment = @ReviewComment,
            IsConfirmed = @IsConfirmed,
            ReviewConclusion = @ReviewConclusion,
            LastModifiedBy = @UserName,
            LastModifiedDate = GETDATE()
        WHERE ReviewHistoryID = @ReviewHistoryID
          AND ISNULL(IsDeleted, 0) = 0;

        IF @@ROWCOUNT = 0
        BEGIN
            ROLLBACK TRANSACTION;
            RETURN -1;
        END

        COMMIT TRANSACTION;
        RETURN @ReviewHistoryID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        INSERT INTO dbo.Sys_ProcedureLogs (LogDate, ProcedureName, ErrorLine, ErrorMessage, AdditionalInfo)
        SELECT GETDATE(), ERROR_PROCEDURE(), ERROR_LINE(), ERROR_MESSAGE(), CONCAT('ReviewHistoryID=', @ReviewHistoryID);
        RETURN -1;
    END CATCH
END
GO

IF OBJECT_ID('dbo.RM_Review_Report_Get', 'P') IS NOT NULL
    DROP PROCEDURE dbo.RM_Review_Report_Get;
GO
CREATE PROCEDURE dbo.RM_Review_Report_Get
    @Search NVARCHAR(250),
    @Order VARCHAR(3),
    @OrderDir VARCHAR(10),
    @PageIndex INT,
    @PageSize INT,
    @ReviewBatchID INT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Order = ISNULL(@Order, '0');
    SET @OrderDir = UPPER(ISNULL(@OrderDir, 'ASC'));
    SET @PageIndex = ISNULL(@PageIndex, 0);
    SET @PageSize = ISNULL(@PageSize, 10);
    SET @Search = NULLIF(LTRIM(RTRIM(@Search)), '');

    ;WITH SourceData AS
    (
        SELECT
            0 AS ObjectType,
            ds.DigitalSalesID AS ObjectID,
            ds.Code AS ObjectCode,
            ds.Title AS ObjectName,
            N'DigitalSales' AS ObjectTypeName,
            level4.Reviewer AS Level4Reviewer,
            level4.CreatedDate AS Level4Date,
            level4.ReviewComment AS Level4Comment,
            level4.IsConfirmed AS Level4Status,
            level3.Reviewer AS Level3Reviewer,
            level3.CreatedDate AS Level3Date,
            level3.ReviewComment AS Level3Comment,
            level3.IsConfirmed AS Level3Status,
            level2.Reviewer AS Level2Reviewer,
            level2.CreatedDate AS Level2Date,
            level2.ReviewComment AS Level2Comment,
            level2.IsConfirmed AS Level2Status,
            CAST(CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.RM_ReviewHistory reviewedHistory
                WHERE reviewedHistory.ReviewBatchItemID = item.ReviewBatchItemID
                  AND reviewedHistory.IsConfirmed = 1
                  AND ISNULL(reviewedHistory.IsDeleted, 0) = 0
            ) THEN 1 ELSE 0 END AS BIT) AS IsReviewed,
            finalReview.ReviewConclusion AS FinalReviewConclusion
        FROM dbo.RM_ReviewBatchItem item
        INNER JOIN dbo.RM_DigitalSales ds
            ON item.ObjectType IS NULL
           AND item.ObjectID = ds.DigitalSalesID
           AND ISNULL(ds.IsDeleted, 0) = 0
        LEFT JOIN dbo.RM_Customer customer ON customer.CustomerID = ds.CustomerID
        OUTER APPLY
        (
            SELECT TOP 1 ISNULL(NULLIF(users.FullName, ''), history.Reviewer) AS Reviewer,
                history.CreatedDate, history.ReviewComment, history.IsConfirmed
            FROM dbo.RM_ReviewHistory history
            LEFT JOIN dbo.Sys_Users users ON users.UserName = history.Reviewer
            WHERE history.ReviewBatchItemID = item.ReviewBatchItemID
              AND history.ReviewLevel = 4 AND ISNULL(history.IsDeleted, 0) = 0
            ORDER BY history.CreatedDate DESC, history.ReviewHistoryID DESC
        ) level4
        OUTER APPLY
        (
            SELECT TOP 1 ISNULL(NULLIF(users.FullName, ''), history.Reviewer) AS Reviewer,
                history.CreatedDate, history.ReviewComment, history.IsConfirmed
            FROM dbo.RM_ReviewHistory history
            LEFT JOIN dbo.Sys_Users users ON users.UserName = history.Reviewer
            WHERE history.ReviewBatchItemID = item.ReviewBatchItemID
              AND history.ReviewLevel = 3 AND ISNULL(history.IsDeleted, 0) = 0
            ORDER BY history.CreatedDate DESC, history.ReviewHistoryID DESC
        ) level3
        OUTER APPLY
        (
            SELECT TOP 1 ISNULL(NULLIF(users.FullName, ''), history.Reviewer) AS Reviewer,
                history.CreatedDate, history.ReviewComment, history.IsConfirmed
            FROM dbo.RM_ReviewHistory history
            LEFT JOIN dbo.Sys_Users users ON users.UserName = history.Reviewer
            WHERE history.ReviewBatchItemID = item.ReviewBatchItemID
              AND history.ReviewLevel = 2 AND ISNULL(history.IsDeleted, 0) = 0
            ORDER BY history.CreatedDate DESC, history.ReviewHistoryID DESC
        ) level2
        OUTER APPLY
        (
            SELECT TOP 1 history.ReviewConclusion
            FROM dbo.RM_ReviewHistory history
            WHERE history.ReviewBatchItemID = item.ReviewBatchItemID
              AND history.IsConfirmed = 1
              AND history.ReviewConclusion IN (1, 2, 3)
              AND ISNULL(history.IsDeleted, 0) = 0
            ORDER BY history.ReviewLevel ASC, history.CreatedDate DESC, history.ReviewHistoryID DESC
        ) finalReview
        WHERE item.ReviewBatchID = @ReviewBatchID
          AND ISNULL(item.IsDeleted, 0) = 0
          AND (@Search IS NULL OR ds.Code LIKE '%' + @Search + '%'
               OR ds.Title LIKE N'%' + @Search + '%'
               OR customer.CustomerName LIKE N'%' + @Search + '%')
    ),
    Numbered AS
    (
        SELECT ROW_NUMBER() OVER
        (
            ORDER BY
                CASE WHEN @Order = '0' AND @OrderDir = 'ASC' THEN ObjectName END ASC,
                CASE WHEN @Order = '0' AND @OrderDir = 'DESC' THEN ObjectName END DESC,
                CASE WHEN @Order = '1' AND @OrderDir = 'ASC' THEN ObjectCode END ASC,
                CASE WHEN @Order = '1' AND @OrderDir = 'DESC' THEN ObjectCode END DESC,
                ObjectID DESC
        ) AS RowIndex,
        COUNT(1) OVER () AS TotalRow,
        *
        FROM SourceData
    )
    SELECT *
    FROM Numbered
    WHERE @PageSize <= 0 OR RowIndex BETWEEN @PageIndex + 1 AND @PageIndex + @PageSize
    ORDER BY RowIndex;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_RM_ReviewHistory_ReviewConclusion'
      AND parent_object_id = OBJECT_ID('dbo.RM_ReviewHistory')
)
BEGIN
    ALTER TABLE dbo.RM_ReviewHistory WITH CHECK
    ADD CONSTRAINT CK_RM_ReviewHistory_ReviewConclusion
        CHECK (ReviewConclusion IS NULL OR ReviewConclusion IN (1, 2, 3));
END
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
    @UserName VARCHAR(150),
    @ProcessID INT = 0,
    @ProgressID INT = 0
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
            ds.StatusID,
            st.StatusName,
            ISNULL(ds.IsKeyProject, 0) AS IsKeyProject,
            CAST(CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.RM_DigitalSalesFollow salesFollow
                WHERE salesFollow.DigitalSalesID = ds.DigitalSalesID
                  AND salesFollow.UserName = @UserName
            ) THEN 1 ELSE 0 END AS BIT) AS IsFollowed,
            STUFF
            ((
                SELECT N', ' + productService.NameProduct
                FROM dbo.RM_DigitalSalesProduct salesProduct
                INNER JOIN dbo.RM_ProductService productService
                    ON productService.ProductServiceID = salesProduct.ProductServiceID
                WHERE salesProduct.DigitalSalesID = ds.DigitalSalesID
                  AND ISNULL(salesProduct.IsDeleted, 0) = 0
                FOR XML PATH(''), TYPE
            ).value('.', 'NVARCHAR(MAX)'), 1, 2, N'') AS ProductServiceNames,
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
          AND (
                @ProcessID = 0
                OR EXISTS
                (
                    SELECT 1
                    FROM dbo.RM_DigitalSalesTracking t
                    WHERE t.DigitalSalesID = ds.DigitalSalesID
                      AND (
                            t.ProcessID = @ProcessID
                            OR EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesProgress pg WHERE pg.ProgressID = t.ProgressID AND pg.ProcessID = @ProcessID)
                          )
                )
                OR (
                    NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesTracking t WHERE t.DigitalSalesID = ds.DigitalSalesID AND (t.ProcessID IS NOT NULL OR t.ProgressID IS NOT NULL))
                    AND EXISTS (SELECT 1 FROM dbo.RM_DigitalSalesProcess pr WHERE pr.ProcessID = @ProcessID AND pr.StatusID = ds.StatusID)
                )
              )
          AND (
                @ProgressID = 0
                OR EXISTS
                (
                    SELECT 1
                    FROM dbo.RM_DigitalSalesTracking t
                    WHERE t.DigitalSalesID = ds.DigitalSalesID
                      AND t.ProgressID = @ProgressID
                )
              )
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
                    CASE WHEN @Order = '2' AND @OrderDir = 'ASC' THEN CustomerName END ASC,
                    CASE WHEN @Order = '2' AND @OrderDir = 'DESC' THEN CustomerName END DESC,
                    CASE WHEN @Order = '3' AND @OrderDir = 'ASC' THEN AssignedEmployeeName END ASC,
                    CASE WHEN @Order = '3' AND @OrderDir = 'DESC' THEN AssignedEmployeeName END DESC,
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
        finalReview.ReviewConclusion AS FinalReviewConclusion,
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
    OUTER APPLY
    (
        SELECT TOP 1 history.ReviewConclusion
        FROM dbo.RM_ReviewHistory history
        WHERE history.ReviewBatchItemID = numbered.ReviewBatchItemID
          AND ISNULL(history.IsDeleted, 0) = 0
          AND history.IsConfirmed = 1
          AND history.ReviewConclusion IN (1, 2, 3)
        ORDER BY history.ReviewLevel ASC, history.CreatedDate DESC, history.ReviewHistoryID DESC
    ) finalReview
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
    @ReviewConclusion TINYINT,
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

    IF (@IsConfirmed = 1 AND ISNULL(@ReviewConclusion, 0) NOT IN (1, 2, 3))
       OR (@ReviewConclusion IS NOT NULL AND @ReviewConclusion NOT IN (1, 2, 3))
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
            ReviewComment, IsConfirmed, ReviewConclusion, CreatedBy, CreatedDate
        )
        VALUES
        (
            @ReviewBatchItemID, @UserName, @ReviewLevel,
            CASE WHEN @IsConfirmed = 1 THEN 2 ELSE 1 END,
            @ReviewComment, @IsConfirmed, @ReviewConclusion, @UserName, GETDATE()
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
        history.ReviewConclusion,
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
    ('ReviewDigitalSales_Column_Record', N'Hồ sơ KD sản phẩm DVS'),
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
    ('ReviewConclusion_Label', N'Kết luận rà soát'),
    ('ReviewConclusion_Final_Label', N'Kết luận cuối'),
    ('ReviewConclusion_Placeholder', N'-- Chọn kết luận rà soát --'),
    ('ReviewConclusion_Accepted', N'Chấp nhận'),
    ('ReviewConclusion_Interested', N'Quan tâm'),
    ('ReviewConclusion_Rejected', N'Không chấp nhận'),
    ('ReviewConclusion_Required', N'Vui lòng chọn kết luận khi xác nhận rà soát.'),
    ('ReviewConclusion_Invalid', N'Kết luận rà soát không hợp lệ.'),
    ('ReviewReport_DigitalSales', N'Hồ sơ KD sản phẩm DVS'),
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

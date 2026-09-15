SET ANSI_NULLS ON;
GO
/*
Unicode execution requirement:
- Recommended: .\scripts\apply_database_script_utf8.ps1 -ConnectionString "<connection-string>"
- If sqlcmd is used directly, always specify UTF-8: sqlcmd ... -f 65001 -i Database\DigitalSalesProductDetail.sql

Do not run this file with a client/code page that does not decode UTF-8 explicitly;
otherwise Vietnamese values inserted into dbo.Sys_Messages will be stored as mojibake.
*/
SET QUOTED_IDENTIFIER ON;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.RM_DigitalSalesProductCost', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RM_DigitalSalesProductCost
    (
        SalesProductCostID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RM_DigitalSalesProductCost PRIMARY KEY,
        SalesProductID INT NOT NULL,
        CostTypeID INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaymentDate DATETIME NULL,
        Note NVARCHAR(MAX) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_RM_DigitalSalesProductCost_IsDeleted DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_RM_DigitalSalesProductCost_CreatedDate DEFAULT (GETDATE()),
        CreatedBy VARCHAR(150) NULL,
        LastModifiedDate DATETIME NULL,
        LastModifiedBy VARCHAR(150) NULL,
        CONSTRAINT FK_RM_DigitalSalesProductCost_Product FOREIGN KEY (SalesProductID) REFERENCES dbo.RM_DigitalSalesProduct(SalesProductID)
    );
    CREATE INDEX IX_RM_DigitalSalesProductCost_Product ON dbo.RM_DigitalSalesProductCost(SalesProductID, IsDeleted);
END;

IF OBJECT_ID(N'dbo.RM_DigitalSalesProductRevenue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RM_DigitalSalesProductRevenue
    (
        SalesProductRevenueID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RM_DigitalSalesProductRevenue PRIMARY KEY,
        SalesProductID INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        ReceivedDate DATETIME NOT NULL,
        ReceivedTime DATETIME NULL,
        Note NVARCHAR(MAX) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_RM_DigitalSalesProductRevenue_IsDeleted DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_RM_DigitalSalesProductRevenue_CreatedDate DEFAULT (GETDATE()),
        CreatedBy VARCHAR(150) NULL,
        LastModifiedDate DATETIME NULL,
        LastModifiedBy VARCHAR(150) NULL,
        CONSTRAINT FK_RM_DigitalSalesProductRevenue_Product FOREIGN KEY (SalesProductID) REFERENCES dbo.RM_DigitalSalesProduct(SalesProductID)
    );
    CREATE INDEX IX_RM_DigitalSalesProductRevenue_Product ON dbo.RM_DigitalSalesProductRevenue(SalesProductID, IsDeleted);
END;

IF OBJECT_ID(N'dbo.RM_DigitalSalesProductMember', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RM_DigitalSalesProductMember
    (
        SalesProductMemberID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RM_DigitalSalesProductMember PRIMARY KEY,
        SalesProductID INT NOT NULL,
        EmployeeID INT NOT NULL,
        RoleIDs VARCHAR(200) NOT NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_RM_DigitalSalesProductMember_IsDeleted DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_RM_DigitalSalesProductMember_CreatedDate DEFAULT (GETDATE()),
        CreatedBy VARCHAR(150) NULL,
        LastModifiedDate DATETIME NULL,
        LastModifiedBy VARCHAR(150) NULL,
        CONSTRAINT FK_RM_DigitalSalesProductMember_Product FOREIGN KEY (SalesProductID) REFERENCES dbo.RM_DigitalSalesProduct(SalesProductID)
    );
    CREATE INDEX IX_RM_DigitalSalesProductMember_Product ON dbo.RM_DigitalSalesProductMember(SalesProductID, IsDeleted);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RM_DigitalSalesProductMember') AND name = N'UX_RM_DigitalSalesProductMember_Active')
    CREATE UNIQUE INDEX UX_RM_DigitalSalesProductMember_Active
        ON dbo.RM_DigitalSalesProductMember(SalesProductID, EmployeeID)
        WHERE IsDeleted = 0;

-- Bảo toàn doanh thu thực tế hiện hữu khi chuyển sang quản lý theo nhiều lần ghi nhận.
INSERT INTO dbo.RM_DigitalSalesProductRevenue
(
    SalesProductID, Amount, ReceivedDate, ReceivedTime, Note,
    IsDeleted, CreatedDate, CreatedBy
)
SELECT p.SalesProductID,
       p.ActualRevenue / 1000000.0,
       ISNULL(p.LastModifiedDate, p.CreatedDate),
       ISNULL(p.LastModifiedDate, p.CreatedDate),
       NULL,
       0,
       GETDATE(),
       'migration'
FROM dbo.RM_DigitalSalesProduct p
WHERE p.IsDeleted = 0
  AND ISNULL(p.ActualRevenue, 0) > 0
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.RM_DigitalSalesProductRevenue r
      WHERE r.SalesProductID = p.SalesProductID
        AND r.IsDeleted = 0
  );

IF OBJECT_ID(N'dbo.RM_DigitalSalesProductCost_GetByProductID', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.RM_DigitalSalesProductCost_GetByProductID AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE dbo.RM_DigitalSalesProductCost_GetByProductID
    @SalesProductID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.SalesProductCostID,
           c.SalesProductID,
           c.CostTypeID,
           ct.CostTypeName,
           c.Amount,
           c.PaymentDate,
           c.Note
    FROM dbo.RM_DigitalSalesProductCost c
    INNER JOIN dbo.RM_CostType ct ON ct.CostTypeID = c.CostTypeID
    WHERE c.SalesProductID = @SalesProductID
      AND c.IsDeleted = 0
    ORDER BY c.PaymentDate, c.SalesProductCostID;
END;
GO

IF OBJECT_ID(N'dbo.RM_DigitalSalesProductRevenue_GetByProductID', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.RM_DigitalSalesProductRevenue_GetByProductID AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE dbo.RM_DigitalSalesProductRevenue_GetByProductID
    @SalesProductID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SalesProductRevenueID,
           SalesProductID,
           Amount,
           ReceivedDate,
           ReceivedTime,
           Note
    FROM dbo.RM_DigitalSalesProductRevenue
    WHERE SalesProductID = @SalesProductID
      AND IsDeleted = 0
    ORDER BY ReceivedDate, SalesProductRevenueID;
END;
GO

IF OBJECT_ID(N'dbo.RM_DigitalSalesProductMember_GetByProductID', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.RM_DigitalSalesProductMember_GetByProductID AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE dbo.RM_DigitalSalesProductMember_GetByProductID
    @SalesProductID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.SalesProductMemberID,
           m.SalesProductID,
           m.EmployeeID,
           u.FullName AS EmployeeName,
           bp.TenBoPhan AS DepartmentName,
           m.RoleIDs AS RoleIDsText,
           STUFF
           (
               (
                   SELECT N', ' + r.RoleName
                   FROM dbo.RM_Roles r
                   WHERE r.IsDeleted = 0
                     AND ';' + m.RoleIDs + ';' LIKE '%;' + CONVERT(VARCHAR(20), r.RoleID) + ';%'
                   ORDER BY r.RoleID
                   FOR XML PATH(''), TYPE
               ).value('.', 'NVARCHAR(MAX)'), 1, 2, N''
           ) AS RoleNames
    FROM dbo.RM_DigitalSalesProductMember m
    INNER JOIN dbo.Sys_Users u ON u.UserId = m.EmployeeID
    LEFT JOIN dbo.MN_BoPhan bp ON bp.MaBoPhan = NULLIF(u.MaBoPhan, '')
    WHERE m.SalesProductID = @SalesProductID
      AND m.IsDeleted = 0
    ORDER BY u.FullName, m.SalesProductMemberID;
END;
GO

IF OBJECT_ID(N'dbo.RM_DigitalSalesProduct_SaveDetail', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.RM_DigitalSalesProduct_SaveDetail AS BEGIN SET NOCOUNT ON; END');
GO
ALTER PROCEDURE dbo.RM_DigitalSalesProduct_SaveDetail
    @SalesProductID INT,
    @DigitalSalesID INT,
    @ProductServiceID INT,
    @ExpectedRevenue DECIMAL(18,2) = NULL,
    @PackageName NVARCHAR(255) = NULL,
    @Quantity INT = 1,
    @StartDate DATETIME = NULL,
    @EndDate DATETIME = NULL,
    @Note NVARCHAR(MAX) = NULL,
    @CostsXml XML,
    @RevenuesXml XML,
    @MembersXml XML,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Costs TABLE
    (
        ID INT,
        CostTypeID INT,
        Amount DECIMAL(18,2),
        PaymentDate DATETIME,
        Note NVARCHAR(MAX)
    );
    DECLARE @Revenues TABLE
    (
        ID INT,
        Amount DECIMAL(18,2),
        ReceivedDate DATETIME,
        ReceivedTime DATETIME,
        Note NVARCHAR(MAX)
    );
    INSERT INTO @Costs(ID, CostTypeID, Amount, PaymentDate, Note)
    SELECT TRY_CONVERT(INT, node.value('@ID', 'NVARCHAR(20)')),
           TRY_CONVERT(INT, node.value('@CostTypeID', 'NVARCHAR(20)')),
           TRY_CONVERT(DECIMAL(18,2), node.value('@Amount', 'NVARCHAR(50)')),
           TRY_CONVERT(DATETIME, NULLIF(node.value('@PaymentDate', 'NVARCHAR(30)'), '')),
           NULLIF(node.value('(Note/text())[1]', 'NVARCHAR(MAX)'), '')
    FROM @CostsXml.nodes('/Items/Item') data(node);

    INSERT INTO @Revenues(ID, Amount, ReceivedDate, ReceivedTime, Note)
    SELECT TRY_CONVERT(INT, node.value('@ID', 'NVARCHAR(20)')),
           TRY_CONVERT(DECIMAL(18,2), node.value('@Amount', 'NVARCHAR(50)')),
           TRY_CONVERT(DATETIME, NULLIF(node.value('@ReceivedDate', 'NVARCHAR(30)'), '')),
           TRY_CONVERT(DATETIME, NULLIF(node.value('@ReceivedTime', 'NVARCHAR(30)'), '')),
           NULLIF(node.value('(Note/text())[1]', 'NVARCHAR(MAX)'), '')
    FROM @RevenuesXml.nodes('/Items/Item') data(node);

    IF @DigitalSalesID <= 0
       OR @ProductServiceID <= 0
       OR NOT EXISTS (SELECT 1 FROM dbo.RM_DigitalSales WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0)
       OR EXISTS (SELECT 1 FROM @Costs WHERE CostTypeID <= 0 OR Amount <= 0)
       OR EXISTS (SELECT 1 FROM @Revenues WHERE Amount <= 0 OR ReceivedDate IS NULL)
    BEGIN
        SELECT 0;
        RETURN 0;
    END;

    BEGIN TRANSACTION;
    BEGIN TRY
        IF @SalesProductID <= 0
           OR NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.RM_DigitalSalesProduct
                  WHERE SalesProductID = @SalesProductID
                    AND DigitalSalesID = @DigitalSalesID
                    AND IsDeleted = 0
              )
        BEGIN
            INSERT INTO dbo.RM_DigitalSalesProduct
            (
                DigitalSalesID, ProductServiceID, ExpectedRevenue, ActualRevenue,
                PackageName, Quantity, StartDate, EndDate, Note,
                IsDeleted, CreatedDate, CreatedBy
            )
            VALUES
            (
                @DigitalSalesID, @ProductServiceID, @ExpectedRevenue, 0,
                @PackageName, CASE WHEN ISNULL(@Quantity, 0) <= 0 THEN 1 ELSE @Quantity END,
                @StartDate, @EndDate, @Note, 0, GETDATE(), @UserName
            );
            SET @SalesProductID = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE dbo.RM_DigitalSalesProduct
            SET ProductServiceID = @ProductServiceID,
                ExpectedRevenue = @ExpectedRevenue,
                PackageName = @PackageName,
                Quantity = CASE WHEN ISNULL(@Quantity, 0) <= 0 THEN 1 ELSE @Quantity END,
                StartDate = @StartDate,
                EndDate = @EndDate,
                Note = @Note,
                LastModifiedDate = GETDATE(),
                LastModifiedBy = @UserName
            WHERE SalesProductID = @SalesProductID;
        END;

        UPDATE target
        SET target.CostTypeID = source.CostTypeID,
            target.Amount = source.Amount,
            target.PaymentDate = source.PaymentDate,
            target.Note = source.Note,
            target.IsDeleted = 0,
            target.LastModifiedDate = GETDATE(),
            target.LastModifiedBy = @UserName
        FROM dbo.RM_DigitalSalesProductCost target
        INNER JOIN @Costs source ON source.ID = target.SalesProductCostID
        WHERE target.SalesProductID = @SalesProductID;

        UPDATE dbo.RM_DigitalSalesProductCost
        SET IsDeleted = 1,
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName
        WHERE SalesProductID = @SalesProductID
          AND IsDeleted = 0
          AND SalesProductCostID NOT IN (SELECT ID FROM @Costs WHERE ID > 0);

        INSERT INTO dbo.RM_DigitalSalesProductCost
        (SalesProductID, CostTypeID, Amount, PaymentDate, Note, IsDeleted, CreatedDate, CreatedBy)
        SELECT @SalesProductID, CostTypeID, Amount, PaymentDate, Note, 0, GETDATE(), @UserName
        FROM @Costs source
        WHERE ISNULL(source.ID, 0) <= 0
           OR NOT EXISTS
              (
                  SELECT 1 FROM dbo.RM_DigitalSalesProductCost target
                  WHERE target.SalesProductCostID = source.ID
                    AND target.SalesProductID = @SalesProductID
              );

        UPDATE target
        SET target.Amount = source.Amount,
            target.ReceivedDate = source.ReceivedDate,
            target.ReceivedTime = source.ReceivedTime,
            target.Note = source.Note,
            target.IsDeleted = 0,
            target.LastModifiedDate = GETDATE(),
            target.LastModifiedBy = @UserName
        FROM dbo.RM_DigitalSalesProductRevenue target
        INNER JOIN @Revenues source ON source.ID = target.SalesProductRevenueID
        WHERE target.SalesProductID = @SalesProductID;

        UPDATE dbo.RM_DigitalSalesProductRevenue
        SET IsDeleted = 1,
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName
        WHERE SalesProductID = @SalesProductID
          AND IsDeleted = 0
          AND SalesProductRevenueID NOT IN (SELECT ID FROM @Revenues WHERE ID > 0);

        INSERT INTO dbo.RM_DigitalSalesProductRevenue
        (SalesProductID, Amount, ReceivedDate, ReceivedTime, Note, IsDeleted, CreatedDate, CreatedBy)
        SELECT @SalesProductID, Amount, ReceivedDate, ReceivedTime, Note, 0, GETDATE(), @UserName
        FROM @Revenues source
        WHERE ISNULL(source.ID, 0) <= 0
           OR NOT EXISTS
              (
                  SELECT 1 FROM dbo.RM_DigitalSalesProductRevenue target
                  WHERE target.SalesProductRevenueID = source.ID
                    AND target.SalesProductID = @SalesProductID
              );

        UPDATE dbo.RM_DigitalSalesProduct
        SET ActualRevenue =
            (
                SELECT ISNULL(SUM(Amount), 0) * 1000000.0
                FROM dbo.RM_DigitalSalesProductRevenue
                WHERE SalesProductID = @SalesProductID
                  AND IsDeleted = 0
            ),
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName
        WHERE SalesProductID = @SalesProductID;

        UPDATE dbo.RM_DigitalSales
        SET TotalExpectedRevenue =
            (
                SELECT ISNULL(SUM(ExpectedRevenue), 0)
                FROM dbo.RM_DigitalSalesProduct
                WHERE DigitalSalesID = @DigitalSalesID
                  AND IsDeleted = 0
            ),
            TotalActualRevenue =
            (
                SELECT ISNULL(SUM(ActualRevenue), 0)
                FROM dbo.RM_DigitalSalesProduct
                WHERE DigitalSalesID = @DigitalSalesID
                  AND IsDeleted = 0
            ),
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName
        WHERE DigitalSalesID = @DigitalSalesID;

        COMMIT TRANSACTION;
        SELECT @SalesProductID;
        RETURN @SalesProductID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        INSERT INTO dbo.Sys_ProcedureLogs(LogDate, ProcedureName, ErrorLine, ErrorMessage, AdditionalInfo)
        VALUES(GETDATE(), ERROR_PROCEDURE(), ERROR_LINE(), ERROR_MESSAGE(), CONCAT('DigitalSalesID=', @DigitalSalesID, '; SalesProductID=', @SalesProductID));
        SELECT 0;
        RETURN 0;
    END CATCH;
END;
GO

ALTER PROCEDURE dbo.RM_DigitalSalesProduct_GetBySalesID
    @DigitalSalesID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.SalesProductID,
           p.DigitalSalesID,
           p.ProductServiceID,
           ps.NameProduct AS ProductServiceName,
           ps.CodeProduct AS ProductServiceCode,
           p.ExpectedRevenue,
           p.ActualRevenue,
           p.PackageName,
           p.Quantity,
           p.StartDate,
           p.EndDate,
           p.Note,
           p.CreatedDate,
           p.CreatedBy,
           ISNULL(cost.TotalCostMillion, 0) AS TotalCostMillion,
           ISNULL(memberData.MemberCount, 0) AS MemberCount,
           ISNULL(revenueData.RevenueCount, 0) AS RevenueCount
    FROM dbo.RM_DigitalSalesProduct p
    INNER JOIN dbo.RM_ProductService ps ON ps.ProductServiceID = p.ProductServiceID
    OUTER APPLY
    (
        SELECT SUM(c.Amount) AS TotalCostMillion
        FROM dbo.RM_DigitalSalesProductCost c
        WHERE c.SalesProductID = p.SalesProductID
          AND c.IsDeleted = 0
    ) cost
    OUTER APPLY
    (
        SELECT COUNT(1) AS MemberCount
        FROM dbo.RM_DigitalSalesProductMember m
        WHERE m.SalesProductID = p.SalesProductID
          AND m.IsDeleted = 0
    ) memberData
    OUTER APPLY
    (
        SELECT COUNT(1) AS RevenueCount
        FROM dbo.RM_DigitalSalesProductRevenue r
        WHERE r.SalesProductID = p.SalesProductID
          AND r.IsDeleted = 0
    ) revenueData
    WHERE p.DigitalSalesID = @DigitalSalesID
      AND p.IsDeleted = 0
    ORDER BY p.SalesProductID;
END;
GO

ALTER PROCEDURE dbo.RM_DigitalSalesProduct_Delete
    @SalesProductID INT,
    @UserName VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @DigitalSalesID INT;
    SELECT @DigitalSalesID = DigitalSalesID
    FROM dbo.RM_DigitalSalesProduct
    WHERE SalesProductID = @SalesProductID
      AND IsDeleted = 0;

    IF @DigitalSalesID IS NULL
    BEGIN
        SELECT 0;
        RETURN 0;
    END;

    BEGIN TRANSACTION;
    BEGIN TRY
        UPDATE dbo.RM_DigitalSalesProduct
        SET IsDeleted = 1, LastModifiedDate = GETDATE(), LastModifiedBy = @UserName
        WHERE SalesProductID = @SalesProductID;

        UPDATE dbo.RM_DigitalSalesProductCost
        SET IsDeleted = 1, LastModifiedDate = GETDATE(), LastModifiedBy = @UserName
        WHERE SalesProductID = @SalesProductID AND IsDeleted = 0;

        UPDATE dbo.RM_DigitalSalesProductRevenue
        SET IsDeleted = 1, LastModifiedDate = GETDATE(), LastModifiedBy = @UserName
        WHERE SalesProductID = @SalesProductID AND IsDeleted = 0;

        UPDATE dbo.RM_DigitalSalesProductMember
        SET IsDeleted = 1, LastModifiedDate = GETDATE(), LastModifiedBy = @UserName
        WHERE SalesProductID = @SalesProductID AND IsDeleted = 0;

        UPDATE dbo.RM_DigitalSales
        SET TotalExpectedRevenue =
            (SELECT ISNULL(SUM(ExpectedRevenue), 0) FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0),
            TotalActualRevenue =
            (SELECT ISNULL(SUM(ActualRevenue), 0) FROM dbo.RM_DigitalSalesProduct WHERE DigitalSalesID = @DigitalSalesID AND IsDeleted = 0),
            LastModifiedDate = GETDATE(),
            LastModifiedBy = @UserName
        WHERE DigitalSalesID = @DigitalSalesID;

        COMMIT TRANSACTION;
        SELECT 1;
        RETURN 1;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SELECT 0;
        RETURN 0;
    END CATCH;
END;
GO

DECLARE @Messages TABLE(LabelKey VARCHAR(500), Message NVARCHAR(MAX));
INSERT INTO @Messages(LabelKey, Message)
VALUES
('DigitalSalesProduct_ExpectedRevenueMillion_Label', N'Doanh thu dự kiến (triệu VNĐ)'),
('DigitalSalesProduct_ActualRevenueMillion_Label', N'Doanh thu thực thu (triệu VNĐ)'),
('DigitalSalesProduct_List_Title', N'Danh sách Sản phẩm / Dịch vụ số'),
('DigitalSalesProduct_Add_Button', N'Thêm sản phẩm / dịch vụ'),
('DigitalSalesProduct_GeneralSection_Title', N'Thông tin dịch vụ'),
('DigitalSalesProduct_MemberSection_Title', N'Thành viên thực hiện'),
('DigitalSalesProduct_CostSection_Title', N'Chi phí dịch vụ'),
('DigitalSalesProduct_RevenueSection_Title', N'Doanh thu thực thu'),
('DigitalSalesProduct_SummarySection_Title', N'Tổng hợp tài chính'),
('DigitalSalesProductMember_Employee_Label', N'Thành viên'),
('DigitalSalesProductMember_Roles_Label', N'Vai trò'),
('DigitalSalesProduct_AddMember_Button', N'Thêm thành viên'),
('DigitalSalesProduct_AddCost_Button', N'Thêm chi phí'),
('DigitalSalesProduct_AddRevenue_Button', N'Thêm doanh thu'),
('DigitalSalesProduct_RemoveRow_Title', N'Xóa dòng'),
('DigitalSalesProduct_ExpectedRevenue_Summary', N'Doanh thu dự kiến'),
('DigitalSalesProduct_ActualRevenue_Summary', N'Doanh thu thực thu'),
('DigitalSalesProduct_TotalCost_Summary', N'Tổng chi phí'),
('DigitalSalesProduct_Profit_Summary', N'Lợi nhuận'),
('DigitalSalesProduct_Empty_Title', N'Chưa có sản phẩm / dịch vụ số'),
('DigitalSalesProduct_Empty_Description', N'Thêm dịch vụ để theo dõi doanh thu và chi phí.'),
('DigitalSalesProduct_DateRange_Label', N'Thời hạn'),
('DigitalSalesProduct_MemberCount_Label', N'Thành viên'),
('DigitalSalesProduct_RevenueCount_Label', N'Lần ghi nhận doanh thu'),
('DigitalSalesProduct_CostType_Option', N'-- Chọn loại chi phí --'),
('DigitalSalesProduct_Employee_Option', N'-- Chọn thành viên --'),
('DigitalSalesProduct_Role_Option', N'-- Chọn vai trò --'),
('DigitalSalesProduct_NoMember_Message', N'Chưa có thành viên riêng cho dịch vụ.'),
('DigitalSalesProduct_NoCost_Message', N'Chưa có chi phí dịch vụ.'),
('DigitalSalesProduct_NoRevenue_Message', N'Chưa có doanh thu thực thu.'),
('DigitalSalesProduct_Msg_CostInvalid', N'Vui lòng nhập đầy đủ loại chi phí và số tiền lớn hơn 0.'),
('DigitalSalesProduct_Msg_RevenueInvalid', N'Vui lòng nhập số tiền lớn hơn 0 và ngày nhận doanh thu.'),
('DigitalSalesProduct_Msg_MemberInvalid', N'Vui lòng chọn thành viên và ít nhất một vai trò.'),
('DigitalSalesProduct_Msg_MemberDuplicate', N'Mỗi thành viên chỉ được thêm một lần trong một dịch vụ.'),
('DigitalSalesProduct_Msg_DateRangeInvalid', N'Thời hạn kết thúc không được nhỏ hơn thời hạn bắt đầu.'),
('DigitalSalesProduct_Msg_ExpectedRevenueInvalid', N'Doanh thu dự kiến không được nhỏ hơn 0.'),
('DigitalSalesProduct_Msg_AmountTooLarge', N'Số tiền vượt quá giới hạn lưu trữ của hệ thống.'),
('DigitalSalesProduct_Msg_QuantityInvalid', N'Số lượng phải lớn hơn 0.'),
('DigitalSalesProduct_Msg_PackageTooLong', N'Gói cước / Quy mô không được vượt quá 255 ký tự.'),
('DigitalSalesProduct_Msg_ReferenceInvalid', N'Dữ liệu danh mục được chọn không hợp lệ hoặc không còn tồn tại.'),
('DigitalSalesProduct_Msg_LoadFormFail', N'Không thể tải biểu mẫu sản phẩm / dịch vụ.'),
('DigitalSalesProduct_Msg_ConnectionError', N'Không thể kết nối máy chủ. Vui lòng thử lại.'),
('DigitalSalesProduct_Column_Product', N'Sản phẩm / Dịch vụ'),
('DigitalSalesProduct_Column_Package', N'Gói cước / Quy mô'),
('DigitalSalesProduct_Column_Quantity', N'Số lượng'),
('DigitalSalesProduct_Column_Note', N'Ghi chú'),
('DigitalSalesProduct_Column_Action', N'Thao tác'),
('DigitalSalesProduct_Unit_Million', N'triệu VNĐ');

UPDATE target
SET target.Message = source.Message
FROM dbo.Sys_Messages target
INNER JOIN @Messages source ON source.LabelKey = target.LabelKey
WHERE target.LangCode = 'vi-VN';

INSERT INTO dbo.Sys_Messages(LangCode, LabelKey, Message)
SELECT 'vi-VN', source.LabelKey, source.Message
FROM @Messages source
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Sys_Messages target
    WHERE target.LangCode = 'vi-VN'
      AND target.LabelKey = source.LabelKey
);
GO

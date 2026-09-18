-- ==============================================================================
-- MASTER MIGRATION SCRIPT: QUẢN LÝ TÀI LIỆU CHUNG (SHARED DOCUMENT MANAGEMENT)
-- Solution: CenIT TOC CRM
-- Database: quanlydoanhthucenit
-- Date: 2026-09-17
-- Description:
--   1. Schema: Sys_DocumentCategory, Sys_SharedDocument
--   2. Seed Data: 4 Initial Document Categories
--   3. Menu Registration: Sys_Menus (ParentId = 1 / QTHT, Link = '/Sys/SharedDocument')
--   4. Stored Procedures: 10 Standard CenIT TOC CRM SPs
--   5. Localization: Sys_Messages dictionary (LangCode = 'vi-VN') via MERGE
-- ==============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- ------------------------------------------------------------------------------
-- 1. TABLE: Sys_DocumentCategory
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Sys_DocumentCategory')
BEGIN
    CREATE TABLE dbo.Sys_DocumentCategory
    (
        CategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sys_DocumentCategory PRIMARY KEY,
        CategoryName NVARCHAR(250) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        DisplayOrder INT NOT NULL CONSTRAINT DF_Sys_DocumentCategory_DisplayOrder DEFAULT (1),
        IsActive BIT NOT NULL CONSTRAINT DF_Sys_DocumentCategory_IsActive DEFAULT (1),
        IsDeleted BIT NOT NULL CONSTRAINT DF_Sys_DocumentCategory_IsDeleted DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_Sys_DocumentCategory_CreatedDate DEFAULT (GETDATE()),
        CreatedBy NVARCHAR(150) NULL,
        UpdatedDate DATETIME NULL,
        UpdatedBy NVARCHAR(150) NULL
    );

    CREATE INDEX IX_Sys_DocumentCategory_IsActive ON dbo.Sys_DocumentCategory(IsActive, IsDeleted);
    PRINT N'Created table dbo.Sys_DocumentCategory successfully.';
END
ELSE
BEGIN
    PRINT N'Table dbo.Sys_DocumentCategory already exists.';
END
GO

-- ------------------------------------------------------------------------------
-- 2. TABLE: Sys_SharedDocument
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Sys_SharedDocument')
BEGIN
    CREATE TABLE dbo.Sys_SharedDocument
    (
        DocumentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Sys_SharedDocument PRIMARY KEY,
        CategoryId INT NOT NULL CONSTRAINT FK_Sys_SharedDocument_Category FOREIGN KEY REFERENCES dbo.Sys_DocumentCategory(CategoryId),
        DocumentName NVARCHAR(250) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        FileName NVARCHAR(255) NOT NULL,
        OriginalFileName NVARCHAR(255) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        FileSize BIGINT NOT NULL CONSTRAINT DF_Sys_SharedDocument_FileSize DEFAULT (0),
        FileExtension VARCHAR(20) NOT NULL,
        DownloadCount INT NOT NULL CONSTRAINT DF_Sys_SharedDocument_DownloadCount DEFAULT (0),
        LastDownloadDate DATETIME NULL,
        LastDownloadBy NVARCHAR(150) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_Sys_SharedDocument_IsDeleted DEFAULT (0),
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_Sys_SharedDocument_CreatedDate DEFAULT (GETDATE()),
        CreatedBy NVARCHAR(150) NOT NULL,
        UpdatedDate DATETIME NULL,
        UpdatedBy NVARCHAR(150) NULL
    );

    CREATE INDEX IX_Sys_SharedDocument_CategoryId ON dbo.Sys_SharedDocument(CategoryId, IsDeleted);
    CREATE INDEX IX_Sys_SharedDocument_CreatedBy ON dbo.Sys_SharedDocument(CreatedBy, IsDeleted);
    CREATE INDEX IX_Sys_SharedDocument_CreatedDate ON dbo.Sys_SharedDocument(CreatedDate DESC);
    CREATE INDEX IX_Sys_SharedDocument_IsDeleted ON dbo.Sys_SharedDocument(IsDeleted);
    PRINT N'Created table dbo.Sys_SharedDocument successfully.';
END
ELSE
BEGIN
    PRINT N'Table dbo.Sys_SharedDocument already exists.';
END
GO

-- ------------------------------------------------------------------------------
-- 3. SEED DATA: Sys_DocumentCategory (4 Initial Categories)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Sys_DocumentCategory WHERE CategoryId = 1)
BEGIN
    INSERT INTO dbo.Sys_DocumentCategory (CategoryName, Description, DisplayOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES (N'Biểu mẫu Hợp đồng', N'Các biểu mẫu hợp đồng kinh tế, phụ lục hợp đồng, biên bản thỏa thuận', 1, 1, 0, GETDATE(), 'admin');
END
ELSE
BEGIN
    UPDATE dbo.Sys_DocumentCategory 
    SET CategoryName = N'Biểu mẫu Hợp đồng', 
        Description = N'Các biểu mẫu hợp đồng kinh tế, phụ lục hợp đồng, biên bản thỏa thuận',
        DisplayOrder = 1,
        IsActive = 1,
        IsDeleted = 0
    WHERE CategoryId = 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_DocumentCategory WHERE CategoryId = 2)
BEGIN
    INSERT INTO dbo.Sys_DocumentCategory (CategoryName, Description, DisplayOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES (N'Quy trình ISO', N'Quy trình quản lý chất lượng ISO, tài liệu vận hành và kiểm soát nội bộ', 2, 1, 0, GETDATE(), 'admin');
END
ELSE
BEGIN
    UPDATE dbo.Sys_DocumentCategory 
    SET CategoryName = N'Quy trình ISO', 
        Description = N'Quy trình quản lý chất lượng ISO, tài liệu vận hành và kiểm soát nội bộ',
        DisplayOrder = 2,
        IsActive = 1,
        IsDeleted = 0
    WHERE CategoryId = 2;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_DocumentCategory WHERE CategoryId = 3)
BEGIN
    INSERT INTO dbo.Sys_DocumentCategory (CategoryName, Description, DisplayOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES (N'Tài liệu kỹ thuật', N'Tài liệu giải pháp kỹ thuật, tài liệu kiến trúc hệ thống và hướng dẫn tích hợp', 3, 1, 0, GETDATE(), 'admin');
END
ELSE
BEGIN
    UPDATE dbo.Sys_DocumentCategory 
    SET CategoryName = N'Tài liệu kỹ thuật', 
        Description = N'Tài liệu giải pháp kỹ thuật, tài liệu kiến trúc hệ thống và hướng dẫn tích hợp',
        DisplayOrder = 3,
        IsActive = 1,
        IsDeleted = 0
    WHERE CategoryId = 3;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_DocumentCategory WHERE CategoryId = 4)
BEGIN
    INSERT INTO dbo.Sys_DocumentCategory (CategoryName, Description, DisplayOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
    VALUES (N'Mẫu biểu hành chính', N'Mẫu phiếu đề xuất, giấy thanh toán, biểu mẫu hành chính nhân sự', 4, 1, 0, GETDATE(), 'admin');
END
ELSE
BEGIN
    UPDATE dbo.Sys_DocumentCategory 
    SET CategoryName = N'Mẫu biểu hành chính', 
        Description = N'Mẫu phiếu đề xuất, giấy thanh toán, biểu mẫu hành chính nhân sự',
        DisplayOrder = 4,
        IsActive = 1,
        IsDeleted = 0
    WHERE CategoryId = 4;
END
PRINT N'Seeded/Updated 4 initial document categories with clean Unicode.';
GO

-- ------------------------------------------------------------------------------
-- 4. MENU REGISTRATION: Sys_Menus (ParentId = 1, QTHT)
-- ------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Sys_Menus WHERE Link = '/Sys/SharedDocument' AND IsDelete = 0)
BEGIN
    DECLARE @NewMenuId INT;
    INSERT INTO dbo.Sys_Menus 
    (
        Name, 
        Position, 
        LevelMenu, 
        Depth, 
        ParentId, 
        Link, 
        Icon, 
        FunctionActionId, 
        IsShow, 
        UseModal, 
        ModalId, 
        IsDelete
    )
    VALUES 
    (
        N'Tài liệu chung', 
        10, 
        2, 
        '1', 
        1, 
        '/Sys/SharedDocument', 
        'fas fa-folder-open', 
        NULL, 
        1, 
        0, 
        NULL, 
        0
    );

    SET @NewMenuId = SCOPE_IDENTITY();
    UPDATE dbo.Sys_Menus 
    SET Depth = '1,' + CAST(@NewMenuId AS VARCHAR(50)) 
    WHERE MenuId = @NewMenuId;

    PRINT N'Registered Menu "Tài liệu chung" in Sys_Menus under ParentId = 1 with MenuId = ' + CAST(@NewMenuId AS VARCHAR(50));
END
ELSE
BEGIN
    UPDATE dbo.Sys_Menus
    SET Name = N'Tài liệu chung',
        Position = 10,
        LevelMenu = 2,
        ParentId = 1,
        Icon = 'fas fa-folder-open',
        IsShow = 1,
        IsDelete = 0
    WHERE Link = '/Sys/SharedDocument';
    PRINT N'Updated existing Menu "Tài liệu chung" in Sys_Menus.';
END
GO

-- ------------------------------------------------------------------------------
-- 5. STORED PROCEDURES (10 PROCEDURES)
-- ------------------------------------------------------------------------------

-- SP 1: Sys_DocumentCategory_GetList
IF OBJECT_ID('dbo.Sys_DocumentCategory_GetList', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.Sys_DocumentCategory_GetList;
GO
CREATE PROCEDURE dbo.Sys_DocumentCategory_GetList
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        CategoryId,
        CategoryName,
        Description,
        DisplayOrder,
        IsActive,
        CreatedDate,
        CreatedBy
    FROM dbo.Sys_DocumentCategory
    WHERE IsDeleted = 0
      AND (@IsActive IS NULL OR IsActive = @IsActive)
    ORDER BY DisplayOrder ASC, CategoryName ASC;
END
GO
PRINT N'Created Stored Procedure dbo.Sys_DocumentCategory_GetList.';
GO

-- SP 2: Sys_DocumentCategory_GetById
IF OBJECT_ID('dbo.Sys_DocumentCategory_GetById', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.Sys_DocumentCategory_GetById;
GO
CREATE PROCEDURE dbo.Sys_DocumentCategory_GetById
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        CategoryId,
        CategoryName,
        Description,
        DisplayOrder,
        IsActive,
        CreatedDate,
        CreatedBy,
        UpdatedDate,
        UpdatedBy
    FROM dbo.Sys_DocumentCategory
    WHERE CategoryId = @CategoryId AND IsDeleted = 0;
END
GO
PRINT N'Created Stored Procedure dbo.Sys_DocumentCategory_GetById.';
GO

-- SP 3: Sys_DocumentCategory_InsertUpdate
IF OBJECT_ID('dbo.Sys_DocumentCategory_InsertUpdate', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.Sys_DocumentCategory_InsertUpdate;
GO
CREATE PROCEDURE dbo.Sys_DocumentCategory_InsertUpdate
    @CategoryId INT = 0,
    @CategoryName NVARCHAR(250),
    @Description NVARCHAR(MAX) = NULL,
    @DisplayOrder INT = 1,
    @IsActive BIT = 1,
    @UserName NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check duplicate CategoryName
    IF EXISTS (
        SELECT 1 FROM dbo.Sys_DocumentCategory 
        WHERE CategoryName = @CategoryName 
          AND CategoryId <> ISNULL(@CategoryId, 0)
          AND IsDeleted = 0
    )
    BEGIN
        SELECT -9 AS Result; -- Category name already exists
        RETURN;
    END

    IF ISNULL(@CategoryId, 0) = 0
    BEGIN
        INSERT INTO dbo.Sys_DocumentCategory (CategoryName, Description, DisplayOrder, IsActive, IsDeleted, CreatedDate, CreatedBy)
        VALUES (@CategoryName, @Description, ISNULL(@DisplayOrder, 1), ISNULL(@IsActive, 1), 0, GETDATE(), @UserName);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS Result;
    END
    ELSE
    BEGIN
        UPDATE dbo.Sys_DocumentCategory
        SET CategoryName = @CategoryName,
            Description = @Description,
            DisplayOrder = ISNULL(@DisplayOrder, 1),
            IsActive = ISNULL(@IsActive, 1),
            UpdatedDate = GETDATE(),
            UpdatedBy = @UserName
        WHERE CategoryId = @CategoryId AND IsDeleted = 0;

        IF @@ROWCOUNT = 0
            SELECT 0 AS Result;
        ELSE
            SELECT @CategoryId AS Result;
    END
END
GO
PRINT N'Created Stored Procedure dbo.Sys_DocumentCategory_InsertUpdate.';
GO

-- SP 4: Sys_DocumentCategory_Delete
IF OBJECT_ID('dbo.Sys_DocumentCategory_Delete', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.Sys_DocumentCategory_Delete;
GO
CREATE PROCEDURE dbo.Sys_DocumentCategory_Delete
    @CategoryId INT,
    @DeletedBy NVARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if any non-deleted documents reference this category
    IF EXISTS (
        SELECT 1 FROM dbo.Sys_SharedDocument 
        WHERE CategoryId = @CategoryId AND IsDeleted = 0
    )
    BEGIN
        SELECT -1 AS Result; -- Cannot delete category that still has documents
        RETURN;
    END

    UPDATE dbo.Sys_DocumentCategory
    SET IsDeleted = 1,
        UpdatedDate = GETDATE(),
        UpdatedBy = @DeletedBy
    WHERE CategoryId = @CategoryId;

    SELECT 1 AS Result;
END
GO
PRINT N'Created Stored Procedure dbo.Sys_DocumentCategory_Delete.';
GO

-- SP 5: Sys_SharedDocument_GetList
IF OBJECT_ID('dbo.Sys_SharedDocument_GetList', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.Sys_SharedDocument_GetList;
GO
CREATE PROCEDURE dbo.Sys_SharedDocument_GetList
    @Keyword NVARCHAR(250) = NULL,
    @CategoryId INT = NULL,
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @TotalRows INT = 0 OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Keyword = NULLIF(LTRIM(RTRIM(@Keyword)), '');
    IF @CategoryId = 0 SET @CategoryId = NULL;
    IF @PageNumber IS NULL OR @PageNumber < 1 SET @PageNumber = 1;
    IF @PageSize IS NULL SET @PageSize = 20;

    -- Adjust ToDate to end of day if time is not specified
    DECLARE @ToDateEnd DATETIME = NULL;
    IF @ToDate IS NOT NULL
    BEGIN
        SET @ToDateEnd = DATEADD(MILLISECOND, -3, DATEADD(DAY, 1, CAST(CAST(@ToDate AS DATE) AS DATETIME)));
    END

    -- Compute TotalRows matching conditions
    SELECT @TotalRows = COUNT(1)
    FROM dbo.Sys_SharedDocument d
    WHERE d.IsDeleted = 0
      AND (@CategoryId IS NULL OR d.CategoryId = @CategoryId)
      AND (@Keyword IS NULL OR d.DocumentName LIKE '%' + @Keyword + '%' 
                           OR d.Description LIKE '%' + @Keyword + '%'
                           OR d.OriginalFileName LIKE '%' + @Keyword + '%')
      AND (@FromDate IS NULL OR d.CreatedDate >= @FromDate)
      AND (@ToDateEnd IS NULL OR d.CreatedDate <= @ToDateEnd);

    ;WITH FilteredData AS
    (
        SELECT 
            d.DocumentId,
            d.CategoryId,
            c.CategoryName,
            d.DocumentName,
            d.Description,
            d.FileName,
            d.OriginalFileName,
            d.FilePath,
            d.FileSize,
            d.FileExtension,
            d.DownloadCount,
            d.LastDownloadDate,
            d.LastDownloadBy,
            d.CreatedDate,
            d.CreatedBy,
            d.UpdatedDate,
            d.UpdatedBy,
            ROW_NUMBER() OVER (ORDER BY d.CreatedDate DESC, d.DocumentId DESC) AS RowIndex,
            @TotalRows AS TotalRows
        FROM dbo.Sys_SharedDocument d
        LEFT JOIN dbo.Sys_DocumentCategory c ON c.CategoryId = d.CategoryId
        WHERE d.IsDeleted = 0
          AND (@CategoryId IS NULL OR d.CategoryId = @CategoryId)
          AND (@Keyword IS NULL OR d.DocumentName LIKE '%' + @Keyword + '%' 
                               OR d.Description LIKE '%' + @Keyword + '%'
                               OR d.OriginalFileName LIKE '%' + @Keyword + '%')
          AND (@FromDate IS NULL OR d.CreatedDate >= @FromDate)
          AND (@ToDateEnd IS NULL OR d.CreatedDate <= @ToDateEnd)
    )
    SELECT *
    FROM FilteredData
    WHERE (@PageSize <= 0)
       OR (RowIndex BETWEEN (@PageNumber - 1) * @PageSize + 1 AND @PageNumber * @PageSize)
    ORDER BY RowIndex;
END
GO
PRINT N'Created Stored Procedure dbo.Sys_SharedDocument_GetList.';
GO

-- SP 6: Sys_SharedDocument_GetById
IF OBJECT_ID('dbo.Sys_SharedDocument_GetById', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.Sys_SharedDocument_GetById;
GO
CREATE PROCEDURE dbo.Sys_SharedDocument_GetById
    @DocumentId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        d.DocumentId,
        d.CategoryId,
        c.CategoryName,
        d.DocumentName,
        d.Description,
        d.FileName,
        d.OriginalFileName,
        d.FilePath,
        d.FileSize,
        d.FileExtension,
        d.DownloadCount,
        d.LastDownloadDate,
        d.LastDownloadBy,
        d.IsDeleted,
        d.CreatedDate,
        d.CreatedBy,
        d.UpdatedDate,
        d.UpdatedBy
    FROM dbo.Sys_SharedDocument d
    LEFT JOIN dbo.Sys_DocumentCategory c ON c.CategoryId = d.CategoryId
    WHERE d.DocumentId = @DocumentId AND d.IsDeleted = 0;
END
GO
PRINT N'Created Stored Procedure dbo.Sys_SharedDocument_GetById.';
GO

-- SP 7: Sys_SharedDocument_Insert
IF OBJECT_ID('dbo.Sys_SharedDocument_Insert', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.Sys_SharedDocument_Insert;
GO
CREATE PROCEDURE dbo.Sys_SharedDocument_Insert
    @CategoryId INT,
    @DocumentName NVARCHAR(250),
    @Description NVARCHAR(MAX) = NULL,
    @FileName NVARCHAR(255),
    @OriginalFileName NVARCHAR(255),
    @FilePath NVARCHAR(500),
    @FileSize BIGINT = 0,
    @FileExtension VARCHAR(20),
    @CreatedBy NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Sys_SharedDocument
    (
        CategoryId,
        DocumentName,
        Description,
        FileName,
        OriginalFileName,
        FilePath,
        FileSize,
        FileExtension,
        DownloadCount,
        IsDeleted,
        CreatedDate,
        CreatedBy
    )
    VALUES
    (
        @CategoryId,
        @DocumentName,
        @Description,
        @FileName,
        @OriginalFileName,
        @FilePath,
        ISNULL(@FileSize, 0),
        @FileExtension,
        0,
        0,
        GETDATE(),
        @CreatedBy
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Result;
END
GO
PRINT N'Created Stored Procedure dbo.Sys_SharedDocument_Insert.';
GO

-- SP 8: Sys_SharedDocument_Update
IF OBJECT_ID('dbo.Sys_SharedDocument_Update', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.Sys_SharedDocument_Update;
GO
CREATE PROCEDURE dbo.Sys_SharedDocument_Update
    @DocumentId INT,
    @CategoryId INT,
    @DocumentName NVARCHAR(250),
    @Description NVARCHAR(MAX) = NULL,
    @FileName NVARCHAR(255) = NULL,
    @OriginalFileName NVARCHAR(255) = NULL,
    @FilePath NVARCHAR(500) = NULL,
    @FileSize BIGINT = NULL,
    @FileExtension VARCHAR(20) = NULL,
    @UpdatedBy NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Sys_SharedDocument
    SET CategoryId = @CategoryId,
        DocumentName = @DocumentName,
        Description = @Description,
        FileName = CASE WHEN @FileName IS NOT NULL AND LTRIM(RTRIM(@FileName)) <> '' THEN @FileName ELSE FileName END,
        OriginalFileName = CASE WHEN @OriginalFileName IS NOT NULL AND LTRIM(RTRIM(@OriginalFileName)) <> '' THEN @OriginalFileName ELSE OriginalFileName END,
        FilePath = CASE WHEN @FilePath IS NOT NULL AND LTRIM(RTRIM(@FilePath)) <> '' THEN @FilePath ELSE FilePath END,
        FileSize = CASE WHEN @FileSize IS NOT NULL AND @FileSize > 0 THEN @FileSize ELSE FileSize END,
        FileExtension = CASE WHEN @FileExtension IS NOT NULL AND LTRIM(RTRIM(@FileExtension)) <> '' THEN @FileExtension ELSE FileExtension END,
        UpdatedDate = GETDATE(),
        UpdatedBy = @UpdatedBy
    WHERE DocumentId = @DocumentId AND IsDeleted = 0;

    IF @@ROWCOUNT = 0
        SELECT 0 AS Result;
    ELSE
        SELECT @DocumentId AS Result;
END
GO
PRINT N'Created Stored Procedure dbo.Sys_SharedDocument_Update.';
GO

-- SP 9: Sys_SharedDocument_Delete
IF OBJECT_ID('dbo.Sys_SharedDocument_Delete', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.Sys_SharedDocument_Delete;
GO
CREATE PROCEDURE dbo.Sys_SharedDocument_Delete
    @DocumentId INT,
    @DeletedBy NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Sys_SharedDocument
    SET IsDeleted = 1,
        UpdatedDate = GETDATE(),
        UpdatedBy = @DeletedBy
    WHERE DocumentId = @DocumentId AND IsDeleted = 0;

    IF @@ROWCOUNT = 0
        SELECT 0 AS Result;
    ELSE
        SELECT 1 AS Result;
END
GO
PRINT N'Created Stored Procedure dbo.Sys_SharedDocument_Delete.';
GO

-- SP 10: Sys_SharedDocument_TrackDownload
IF OBJECT_ID('dbo.Sys_SharedDocument_TrackDownload', 'P') IS NOT NULL 
    DROP PROCEDURE dbo.Sys_SharedDocument_TrackDownload;
GO
CREATE PROCEDURE dbo.Sys_SharedDocument_TrackDownload
    @DocumentId INT,
    @DownloadedBy NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Sys_SharedDocument
    SET DownloadCount = DownloadCount + 1,
        LastDownloadDate = GETDATE(),
        LastDownloadBy = @DownloadedBy
    WHERE DocumentId = @DocumentId AND IsDeleted = 0;

    SELECT DownloadCount
    FROM dbo.Sys_SharedDocument
    WHERE DocumentId = @DocumentId;
END
GO
PRINT N'Created Stored Procedure dbo.Sys_SharedDocument_TrackDownload.';
GO

-- ------------------------------------------------------------------------------
-- 6. SYS_MESSAGES DICTIONARY (MERGE STATEMENT, LangCode = 'vi-VN')
-- ------------------------------------------------------------------------------
MERGE dbo.Sys_Messages AS Target
USING (VALUES
    -- Tiêu đề phân hệ & Menu (SharedDoc_*)
    ('vi-VN', 'SharedDoc_Title', N'Quản lý tài liệu chung'),
    ('vi-VN', 'SharedDoc_Badge_Title', N'Kho tài liệu biểu mẫu chung'),
    ('vi-VN', 'SharedDoc_Menu_Title', N'Tài liệu chung'),
    ('vi-VN', 'SharedDoc_List_Title', N'Danh sách tài liệu biểu mẫu dùng chung'),
    ('vi-VN', 'SharedDoc_Upload_Button', N'Tải lên tài liệu'),
    ('vi-VN', 'SharedDoc_Button_Upload', N'Tải lên tài liệu'),
    ('vi-VN', 'SharedDoc_Button_Download', N'Tải về'),
    ('vi-VN', 'SharedDoc_Button_Detail', N'Xem chi tiết'),
    ('vi-VN', 'SharedDoc_Button_Edit', N'Chỉnh sửa'),
    ('vi-VN', 'SharedDoc_Button_Delete', N'Xóa tài liệu'),

    -- Modal Titles (SharedDoc_*)
    ('vi-VN', 'SharedDoc_Modal_Title_Add', N'Đăng tải tài liệu biểu mẫu chung'),
    ('vi-VN', 'SharedDoc_Modal_Title_Edit', N'Cập nhật thông tin tài liệu chung'),
    ('vi-VN', 'SharedDoc_Modal_Title_Detail', N'Thông tin chi tiết tài liệu [{0}]'),
    ('vi-VN', 'SharedDoc_Modal_Title_Delete', N'Xác nhận xóa tài liệu chung'),

    -- Labels (SharedDoc_*)
    ('vi-VN', 'SharedDoc_Name_Label', N'Tên tài liệu biểu mẫu'),
    ('vi-VN', 'SharedDoc_Category_Label', N'Chuyên mục tài liệu'),
    ('vi-VN', 'SharedDoc_Description_Label', N'Mô tả / Ghi chú sử dụng'),
    ('vi-VN', 'SharedDoc_File_Label', N'Tệp đính kèm'),
    ('vi-VN', 'SharedDoc_FileSize_Label', N'Dung lượng tệp'),
    ('vi-VN', 'SharedDoc_FileExtension_Label', N'Định dạng'),
    ('vi-VN', 'SharedDoc_DownloadCount_Label', N'Lượt tải'),
    ('vi-VN', 'SharedDoc_LastDownloadDate_Label', N'Tải lần cuối'),
    ('vi-VN', 'SharedDoc_LastDownloadBy_Label', N'Người tải cuối'),
    ('vi-VN', 'SharedDoc_CreatedBy_Label', N'Người đăng'),
    ('vi-VN', 'SharedDoc_CreatedDate_Label', N'Ngày đăng'),

    -- Search & Placeholders (SharedDoc_*)
    ('vi-VN', 'SharedDoc_Search_Keyword', N'Nhập tên tài liệu, mô tả, tên file...'),
    ('vi-VN', 'SharedDoc_Search_Category', N'-- Tất cả chuyên mục --'),
    ('vi-VN', 'SharedDoc_Search_FromDate', N'Từ ngày (dd/mm/yyyy)'),
    ('vi-VN', 'SharedDoc_Search_ToDate', N'Đến ngày (dd/mm/yyyy)'),
    ('vi-VN', 'SharedDoc_Placeholder_Name', N'Nhập tên tài liệu biểu mẫu...'),
    ('vi-VN', 'SharedDoc_Placeholder_Category', N'-- Chọn chuyên mục tài liệu --'),
    ('vi-VN', 'SharedDoc_Placeholder_Description', N'Nhập mục đích sử dụng, phạm vi áp dụng, lưu ý...'),
    ('vi-VN', 'SharedDoc_Placeholder_ChooseFile', N'Chọn tệp văn bản đính kèm (.doc, .docx, .xls, .xlsx, .pdf, .ppt, .pptx)'),

    -- Nhãn thời gian & thông báo chung dùng cho View Search & Form AJAX
    ('vi-VN', 'Label_TuNgay', N'Từ ngày'),
    ('vi-VN', 'Label_DenNgay', N'Đến ngày'),
    ('vi-VN', 'Common_Message_Error', N'Đã có lỗi xảy ra, vui lòng thử lại sau!'),

    -- Table Column Headers (SharedDoc_*)
    ('vi-VN', 'SharedDoc_Col_No', N'STT'),
    ('vi-VN', 'SharedDoc_Col_Order', N'STT'),
    ('vi-VN', 'SharedDoc_Col_Name', N'Tên tài liệu / Tệp tin'),
    ('vi-VN', 'SharedDoc_Col_Category', N'Chuyên mục'),
    ('vi-VN', 'SharedDoc_Col_FileInfo', N'Tệp đính kèm / Kích thước'),
    ('vi-VN', 'SharedDoc_Col_FileSize', N'Dung lượng'),
    ('vi-VN', 'SharedDoc_Col_Downloads', N'Lượt tải'),
    ('vi-VN', 'SharedDoc_Col_DownloadCount', N'Lượt tải'),
    ('vi-VN', 'SharedDoc_Col_CreatedBy', N'Người đăng / Ngày đăng'),
    ('vi-VN', 'SharedDoc_Col_Uploader', N'Người đăng'),
    ('vi-VN', 'SharedDoc_Col_CreatedDate', N'Ngày đăng'),
    ('vi-VN', 'SharedDoc_Col_Action', N'Thao tác'),
    ('vi-VN', 'SharedDoc_Col_Actions', N'Thao tác'),

    -- Messages & Validations (SharedDoc_*)
    ('vi-VN', 'SharedDoc_Msg_ConfirmDelete', N'Bạn có chắc chắn muốn xóa tài liệu [{0}] không?'),
    ('vi-VN', 'SharedDoc_Msg_NameRequired', N'Vui lòng nhập tên tài liệu biểu mẫu!'),
    ('vi-VN', 'SharedDoc_Msg_CategoryRequired', N'Vui lòng chọn chuyên mục tài liệu!'),
    ('vi-VN', 'SharedDoc_Msg_FileRequired', N'Vui lòng đính kèm tệp tài liệu!'),
    ('vi-VN', 'SharedDoc_Msg_FileExtensionInvalid', N'Định dạng tệp không được hỗ trợ! Chỉ chấp nhận .doc, .docx, .xls, .xlsx, .pdf, .ppt, .pptx.'),
    ('vi-VN', 'SharedDoc_Msg_FileDangerousBlocked', N'Tệp thực thi hoặc có nguy cơ độc hại đã bị hệ thống chặn!'),
    ('vi-VN', 'SharedDoc_Msg_FileSizeExceeded', N'Dung lượng tệp vượt quá giới hạn tối đa cho phép (50MB)!'),
    ('vi-VN', 'SharedDoc_Msg_FileNotFound', N'Tệp tài liệu không tồn tại trên máy chủ hoặc đã bị xóa!'),
    ('vi-VN', 'SharedDoc_Msg_PermissionDenied', N'Bạn không có quyền thực hiện thao tác trên tài liệu của người khác!'),
    ('vi-VN', 'SharedDoc_Msg_NameExisted', N'Tên tài liệu này đã tồn tại trong chuyên mục, vui lòng kiểm tra lại!'),
    ('vi-VN', 'SharedDoc_Msg_AddSuccess', N'Đăng tải tài liệu mới thành công!'),
    ('vi-VN', 'SharedDoc_Msg_AddFail', N'Đăng tải tài liệu thất bại, vui lòng thử lại!'),
    ('vi-VN', 'SharedDoc_Msg_EditSuccess', N'Cập nhật hồ sơ tài liệu thành công!'),
    ('vi-VN', 'SharedDoc_Msg_EditFail', N'Cập nhật hồ sơ tài liệu thất bại!'),
    ('vi-VN', 'SharedDoc_Msg_DeleteSuccess', N'Xóa tài liệu thành công!'),
    ('vi-VN', 'SharedDoc_Msg_DeleteFail', N'Xóa tài liệu thất bại!'),

    -- Alias Keys (Document_*)
    ('vi-VN', 'Document_Title', N'Quản lý tài liệu chung'),
    ('vi-VN', 'Document_Badge_Title', N'Kho tài liệu biểu mẫu chung'),
    ('vi-VN', 'Document_Menu_Title', N'Tài liệu chung'),
    ('vi-VN', 'Document_List_Title', N'Danh sách tài liệu biểu mẫu dùng chung'),
    ('vi-VN', 'Document_Modal_Title_Add', N'Đăng tải tài liệu mới'),
    ('vi-VN', 'Document_Modal_Title_Edit', N'Cập nhật tài liệu [{0}]'),
    ('vi-VN', 'Document_Modal_Title_Detail', N'Thông tin chi tiết tài liệu [{0}]'),
    ('vi-VN', 'Document_Modal_Title_Delete', N'Xác nhận xóa tài liệu'),
    ('vi-VN', 'Document_Label_Name', N'Tên tài liệu'),
    ('vi-VN', 'Document_Label_Category', N'Chuyên mục tài liệu'),
    ('vi-VN', 'Document_Label_Description', N'Mô tả / Ghi chú'),
    ('vi-VN', 'Document_Label_File', N'Tệp tài liệu đính kèm'),
    ('vi-VN', 'Document_Label_FileSize', N'Dung lượng tệp'),
    ('vi-VN', 'Document_Label_FileExtension', N'Định dạng'),
    ('vi-VN', 'Document_Label_DownloadCount', N'Lượt tải'),
    ('vi-VN', 'Document_Label_LastDownloadDate', N'Tải lần cuối'),
    ('vi-VN', 'Document_Label_LastDownloadBy', N'Người tải cuối'),
    ('vi-VN', 'Document_Label_CreatedBy', N'Người đăng'),
    ('vi-VN', 'Document_Label_CreatedDate', N'Ngày đăng'),
    ('vi-VN', 'Document_Placeholder_Name', N'Nhập tên tài liệu biểu mẫu...'),
    ('vi-VN', 'Document_Placeholder_Category', N'-- Chọn chuyên mục tài liệu --'),
    ('vi-VN', 'Document_Placeholder_Description', N'Nhập mục đích sử dụng, phạm vi áp dụng, lưu ý...'),
    ('vi-VN', 'Document_Placeholder_ChooseFile', N'Chọn tệp văn bản đính kèm (.doc, .docx, .xls, .xlsx, .pdf, .ppt, .pptx)'),
    ('vi-VN', 'Document_Placeholder_Keyword', N'Nhập từ khóa tên tài liệu, mô tả, tên file...'),
    ('vi-VN', 'Document_Placeholder_FromDate', N'Từ ngày (dd/mm/yyyy)'),
    ('vi-VN', 'Document_Placeholder_ToDate', N'Đến ngày (dd/mm/yyyy)'),
    ('vi-VN', 'Document_Col_Order', N'STT'),
    ('vi-VN', 'Document_Col_Name', N'Tên tài liệu / Tệp tin'),
    ('vi-VN', 'Document_Col_Category', N'Chuyên mục'),
    ('vi-VN', 'Document_Col_FileSize', N'Dung lượng'),
    ('vi-VN', 'Document_Col_Uploader', N'Người đăng'),
    ('vi-VN', 'Document_Col_CreatedDate', N'Ngày đăng'),
    ('vi-VN', 'Document_Col_DownloadCount', N'Lượt tải'),
    ('vi-VN', 'Document_Col_Actions', N'Thao tác'),
    ('vi-VN', 'Button_Add_Document', N'Đăng tải tài liệu'),
    ('vi-VN', 'Button_Download_Document', N'Tải về tệp gốc'),
    ('vi-VN', 'Button_View_Detail', N'Xem chi tiết'),
    ('vi-VN', 'Button_Edit_Document', N'Chỉnh sửa'),
    ('vi-VN', 'Button_Delete_Document', N'Xóa tài liệu'),
    ('vi-VN', 'Document_Msg_NameRequired', N'Vui lòng nhập tên tài liệu!'),
    ('vi-VN', 'Document_Msg_CategoryRequired', N'Vui lòng chọn chuyên mục tài liệu!'),
    ('vi-VN', 'Document_Msg_FileRequired', N'Vui lòng chọn tệp tài liệu đính kèm!'),
    ('vi-VN', 'Document_Msg_FileTooLarge', N'Dung lượng tệp vượt quá giới hạn tối đa cho phép (50MB)!'),
    ('vi-VN', 'Document_Msg_InvalidExtension', N'Định dạng tệp không được hỗ trợ! Hệ thống chỉ chấp nhận tệp .doc, .docx, .xls, .xlsx, .pdf, .ppt, .pptx.'),
    ('vi-VN', 'Document_Msg_BlacklistExtension', N'Phát hiện định dạng tệp nguy hiểm bị cấm tải lên hệ thống!'),
    ('vi-VN', 'Document_Msg_AddSuccess', N'Đăng tải tài liệu mới thành công!'),
    ('vi-VN', 'Document_Msg_AddFail', N'Đăng tải tài liệu thất bại, vui lòng thử lại!'),
    ('vi-VN', 'Document_Msg_EditSuccess', N'Cập nhật hồ sơ tài liệu thành công!'),
    ('vi-VN', 'Document_Msg_EditFail', N'Cập nhật hồ sơ tài liệu thất bại!'),
    ('vi-VN', 'Document_Msg_DeleteSuccess', N'Xóa tài liệu thành công!'),
    ('vi-VN', 'Document_Msg_DeleteFail', N'Xóa tài liệu thất bại!'),
    ('vi-VN', 'Document_Msg_DeleteConfirm', N'Bạn có chắc chắn muốn xóa tài liệu [{0}] không?'),
    ('vi-VN', 'Document_Msg_FileNotFound', N'Tệp tin đính kèm không tồn tại trên máy chủ lưu trữ!'),
    ('vi-VN', 'Document_Msg_NoPermission', N'Bạn không có quyền chỉnh sửa hoặc xóa tài liệu do người khác tải lên!'),
    ('vi-VN', 'Document_Msg_NameExisted', N'Tên tài liệu này đã tồn tại trong chuyên mục, vui lòng kiểm tra lại!')
) AS Source (LangCode, LabelKey, Message)
ON Target.LangCode = Source.LangCode AND Target.LabelKey = Source.LabelKey
WHEN MATCHED THEN
    UPDATE SET Target.Message = Source.Message
WHEN NOT MATCHED THEN
    INSERT (LangCode, LabelKey, Message)
    VALUES (Source.LangCode, Source.LabelKey, Source.Message);

PRINT N'Merged all Sys_Messages localization keys successfully.';
GO

PRINT N'==============================================================================';
PRINT N'MIGRATION COMPLETED SUCCESSFULLY FOR QUANLYDOANHTHUCENIT!';
PRINT N'==============================================================================';
GO

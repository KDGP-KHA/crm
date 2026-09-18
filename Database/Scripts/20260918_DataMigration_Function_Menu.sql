-- Script: 20260918_DataMigration_Function_Menu.sql
-- Mục đích: Đăng ký chức năng, phân quyền QTHT và thêm menu Chuyển đổi dữ liệu vào nhóm Hệ thống

SET NOCOUNT ON;

DECLARE @ModuleId INT = 1; -- Module Sys
DECLARE @FunctionId INT;
DECLARE @ViewActionId INT;
DECLARE @AddActionId INT;
DECLARE @RoleId_QTHT INT = 1; -- Role QTHT

-- 1. Đăng ký Function DataMigration
SELECT @FunctionId = FunctionId 
FROM Sys_Functions 
WHERE Area = 'Sys' AND Name = 'DataMigration';

IF @FunctionId IS NULL
BEGIN
    INSERT INTO Sys_Functions (ModuleId, Area, Name, Description, IsDeleted)
    VALUES (@ModuleId, 'Sys', 'DataMigration', N'Chuyển đổi dữ liệu Cơ hội / Dự án sang SPDV Số', 0);

    SET @FunctionId = SCOPE_IDENTITY();
    PRINT N'-> Đã tạo Function DataMigration với ID = ' + CAST(@FunctionId AS NVARCHAR(10));
END
ELSE
BEGIN
    UPDATE Sys_Functions 
    SET IsDeleted = 0, Description = N'Chuyển đổi dữ liệu Cơ hội / Dự án sang SPDV Số'
    WHERE FunctionId = @FunctionId;
    PRINT N'-> Function DataMigration đã tồn tại với ID = ' + CAST(@FunctionId AS NVARCHAR(10));
END

-- 2. Đăng ký FunctionActions: View & Add
SELECT @ViewActionId = FunctionActionId FROM Sys_FunctionActions WHERE FunctionId = @FunctionId AND Action = 'View';
IF @ViewActionId IS NULL
BEGIN
    INSERT INTO Sys_FunctionActions (FunctionId, Action, IsDeleted)
    VALUES (@FunctionId, 'View', 0);
    SET @ViewActionId = SCOPE_IDENTITY();
    PRINT N'-> Đã tạo Action View với ID = ' + CAST(@ViewActionId AS NVARCHAR(10));
END
ELSE
BEGIN
    UPDATE Sys_FunctionActions SET IsDeleted = 0 WHERE FunctionActionId = @ViewActionId;
END

SELECT @AddActionId = FunctionActionId FROM Sys_FunctionActions WHERE FunctionId = @FunctionId AND Action = 'Add';
IF @AddActionId IS NULL
BEGIN
    INSERT INTO Sys_FunctionActions (FunctionId, Action, IsDeleted)
    VALUES (@FunctionId, 'Add', 0);
    SET @AddActionId = SCOPE_IDENTITY();
    PRINT N'-> Đã tạo Action Add với ID = ' + CAST(@AddActionId AS NVARCHAR(10));
END
ELSE
BEGIN
    UPDATE Sys_FunctionActions SET IsDeleted = 0 WHERE FunctionActionId = @AddActionId;
END

-- 3. Phân quyền cho Role QTHT (RoleId = 1)
IF NOT EXISTS (SELECT 1 FROM Sys_Permissions WHERE RoleId = @RoleId_QTHT AND FunctionId = @FunctionId AND Action = 'View')
BEGIN
    INSERT INTO Sys_Permissions (RoleId, FunctionId, Action)
    VALUES (@RoleId_QTHT, @FunctionId, 'View');
    PRINT N'-> Đã cấp quyền View cho QTHT';
END

IF NOT EXISTS (SELECT 1 FROM Sys_Permissions WHERE RoleId = @RoleId_QTHT AND FunctionId = @FunctionId AND Action = 'Add')
BEGIN
    INSERT INTO Sys_Permissions (RoleId, FunctionId, Action)
    VALUES (@RoleId_QTHT, @FunctionId, 'Add');
    PRINT N'-> Đã cấp quyền Add cho QTHT';
END

-- 4. Đăng ký Menu vào nhóm Hệ thống (ParentId = 1)
DECLARE @MenuId INT;
SELECT @MenuId = MenuId FROM Sys_Menus WHERE ParentId = 1 AND (Link = '/Sys/DataMigration' OR Link = '/sys/datamigration');

IF @MenuId IS NULL
BEGIN
    DECLARE @MaxPos INT;
    SELECT @MaxPos = ISNULL(MAX(Position), 0) + 1 FROM Sys_Menus WHERE ParentId = 1;

    INSERT INTO Sys_Menus (Name, Position, LevelMenu, Depth, ParentId, Link, Icon, FunctionActionId, IsShow, UseModal, ModalId, IsDelete)
    VALUES (N'Chuyển đổi dữ liệu', @MaxPos, 2, '1', 1, '/Sys/DataMigration', 'fas fa-exchange-alt', @ViewActionId, 1, 0, NULL, 0);

    SET @MenuId = SCOPE_IDENTITY();
    UPDATE Sys_Menus SET Depth = '1,' + CAST(@MenuId AS VARCHAR(10)) WHERE MenuId = @MenuId;

    PRINT N'-> Đã tạo Menu Chuyển đổi dữ liệu (ID = ' + CAST(@MenuId AS NVARCHAR(10)) + N', ParentId = 1, Depth = 1,' + CAST(@MenuId AS NVARCHAR(10)) + N')';
END
ELSE
BEGIN
    UPDATE Sys_Menus 
    SET Name = N'Chuyển đổi dữ liệu',
        Link = '/Sys/DataMigration',
        Icon = 'fas fa-exchange-alt',
        FunctionActionId = @ViewActionId,
        IsShow = 1,
        IsDelete = 0
    WHERE MenuId = @MenuId;

    PRINT N'-> Đã cập nhật Menu Chuyển đổi dữ liệu (ID = ' + CAST(@MenuId AS NVARCHAR(10)) + N')';
END

PRINT N'=== HOÀN TẤT ĐĂNG KÝ FUNCTION, PHÂN QUYỀN VÀ MENU ===';

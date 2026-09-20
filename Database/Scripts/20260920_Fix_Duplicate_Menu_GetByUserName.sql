-- ============================================================================
-- Script: 20260920_Fix_Duplicate_Menu_GetByUserName.sql
-- Description: Khắc phục lỗi menu Nghiệp vụ (và các menu cha khác) xuất hiện 2 lần
--              do af.Area trong CTE đệ quy làm mất tính duy nhất khi DISTINCT.
-- Author: Antigravity AI
-- Date: 2026-09-20
-- ============================================================================

IF OBJECT_ID('[dbo].[p_Sys_Menu_GetByUserName]', 'P') IS NOT NULL
BEGIN
    PRINT N'Altering procedure [dbo].[p_Sys_Menu_GetByUserName]...';
END
GO

ALTER PROCEDURE [dbo].[p_Sys_Menu_GetByUserName]
	@UserName VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    
    ;WITH M AS (
        SELECT am.MenuId
              ,am.Name
              ,am.Position
              ,am.LevelMenu
              ,am.Depth
              ,am.ParentId
              ,am.Link
              ,am.Icon
              ,am.FunctionActionId
              ,am.IsShow
              ,am.UseModal
              ,am.ModalId
              ,am.IsDelete
              ,am.TitleView
        FROM   vMenus                     AS am
               LEFT OUTER JOIN Sys_FunctionActions AS afa
                    ON  am.FunctionActionId = afa.FunctionActionId
               JOIN Sys_Permissions  AS ap
                    ON  ap.FunctionId = afa.FunctionId
                        AND ap.[Action] = afa.[Action]
               JOIN Sys_Roles       AS ag
                    ON  ap.RoleId = ag.RoleId
               JOIN Sys_UserRoles    AS aug
                    ON  aug.RoleId = ap.RoleId
               JOIN Sys_Users        AS au
                    ON  aug.UserId = au.UserId
                        AND au.UserName = @UserName
               LEFT OUTER JOIN Sys_Functions AS af
                    ON  af.FunctionId = afa.FunctionId
               JOIN Sys_PermissionModule AS apm
                    ON  apm.UserId = au.UserId
                        AND apm.ModuleId = af.ModuleId
        WHERE  ((am.ParentId IS NOT NULL AND am.ParentId>0) OR (am.ParentId IS NULL AND am.FunctionActionId IS NOT NULL))
               AND am.IsDelete = 0
               AND am.IsShow = 1
        
        UNION ALL
        
        SELECT amc.MenuId
              ,amc.Name
              ,amc.Position
              ,amc.LevelMenu
              ,amc.Depth
              ,amc.ParentId
              ,amc.Link
              ,amc.Icon
              ,amc.FunctionActionId
              ,amc.IsShow
              ,amc.UseModal
              ,amc.ModalId
              ,amc.IsDelete
              ,amc.TitleView
        FROM   vMenus        AS amc
               INNER JOIN M  AS m
                    ON  amc.MenuId = m.ParentId
        WHERE  amc.IsDelete = 0
               AND amc.IsShow = 1
    )
    
    SELECT DISTINCT am.*
    INTO   #Menus
    FROM   M AS am
    
    SELECT *
    FROM   #Menus AS am
    WHERE  am.FunctionActionId IS NOT NULL
           OR (
                  am.FunctionActionId IS NULL
                  AND EXISTS(
                          SELECT 1
                          FROM   #Menus AS m
                          WHERE  m.ParentId = am.MenuId
                      )
              )
    ORDER BY
           am.Position
END
GO

PRINT N'Successfully updated [dbo].[p_Sys_Menu_GetByUserName].';
GO

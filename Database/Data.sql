SELECT
    P.ProjectID AS ID,
    CAST(NULL AS NVARCHAR(100)) AS Code,
    P.ProjectName AS Name,
    P.Status AS StatusID,
    S.StatusCode,
    S.StatusName,
    N'Dự án' AS ObjectType,
    -- Số lượng thành viên (không trùng lặp): RM_ProjectMember join Sys_Users
    (
        SELECT COUNT(DISTINCT PM.Employee_ID)
        FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_ProjectMember] PM
        INNER JOIN [crm.vnptkhanhhoa.vn].[dbo].[RM_ProductProject] PP_M
            ON PP_M.ProductProjectID = PM.ProductProjectID
        INNER JOIN [crm.vnptkhanhhoa.vn].[dbo].[Sys_Users] U
            ON U.UserID = PM.Employee_ID
        WHERE PP_M.ProjectID = P.ProjectID
          AND ISNULL(PM.IsDeleted, 0) = 0
          AND ISNULL(PP_M.IsDeleted, 0) = 0
    ) AS Members,
    -- Số lượng dịch vụ: RM_ProductProject
    (
        SELECT COUNT(DISTINCT PP_S.ProductProjectID)
        FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_ProductProject] PP_S
        INNER JOIN [crm.vnptkhanhhoa.vn].[dbo].[RM_ProductService] PS
            ON PS.ProductServiceID = PP_S.ProductServiceID
        WHERE PP_S.ProjectID = P.ProjectID
          AND ISNULL(PP_S.IsDeleted, 0) = 0
          AND ISNULL(PS.IsDeleted, 0) = 0
    ) AS Services,
    -- Doanh thu: RM_ProductProject cột ExpectedRevenue (tổng doanh thu dự kiến)
    (
        SELECT SUM(PP_R.ExpectedRevenue)
        FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_ProductProject] PP_R
        WHERE PP_R.ProjectID = P.ProjectID
          AND ISNULL(PP_R.IsDeleted, 0) = 0
    ) AS TotalRevenue,
    -- Số lượng trao đổi/công việc: RM_ProjectTask join RM_TaskManagement
    (
        SELECT COUNT(*)
        FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_TaskManagement] TM
        FULL OUTER JOIN [crm.vnptkhanhhoa.vn].[dbo].[RM_ProjectTask] PT
            ON (PT.TaskID = TM.TaskID)
        LEFT JOIN [crm.vnptkhanhhoa.vn].[dbo].[RM_ProductProject] PP_TM
            ON PP_TM.ProductProjectID = TM.ProductProjectID
        LEFT JOIN [crm.vnptkhanhhoa.vn].[dbo].[RM_ProductProject] PP_PT
            ON PP_PT.ProductProjectID = PT.ProductProjectID
        WHERE (
                TM.ProjectID = P.ProjectID 
                OR PP_TM.ProjectID = P.ProjectID 
                OR PP_PT.ProjectID = P.ProjectID
              )
          AND ISNULL(TM.IsDeleted, 0) = 0
          AND ISNULL(PT.IsDeleted, 0) = 0
    ) AS ExchangeInfo
FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_Project] P
LEFT JOIN [crm.vnptkhanhhoa.vn].[dbo].[RM_Status] S
    ON S.ID = P.Status
WHERE ISNULL(P.IsDeleted, 0) = 0

UNION ALL

SELECT
    BO.BusinessOpportunityID AS ID,
    BO.CodeOpportunity AS Code,
    BO.OpportunityName AS Name,
    BO.StatusID,
    S.StatusCode,
    S.StatusName,
    N'Cơ hội' AS ObjectType,
    -- Số lượng thành viên (không trùng lặp): RM_SalesTeamMembers join Sys_Users
    (
        SELECT COUNT(DISTINCT STM.EmployeeID)
        FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_SalesTeamMembers] STM
        INNER JOIN [crm.vnptkhanhhoa.vn].[dbo].[Sys_Users] U
            ON U.UserID = STM.EmployeeID
        WHERE STM.BusinessOpportunityID = BO.BusinessOpportunityID
          AND ISNULL(STM.IsDeleted, 0) = 0
    ) AS Members,
    -- Số lượng dịch vụ: RM_ProductService
    (
        SELECT COUNT(DISTINCT PS.ProductServiceID)
        FROM [crm.vnptkhanhhoa.vn].[dbo].SplitString(REPLACE(BO.ProductServiceIDs, ',', ';'), ';') ss
        INNER JOIN [crm.vnptkhanhhoa.vn].[dbo].[RM_ProductService] PS 
            ON PS.ProductServiceID = CASE WHEN ISNUMERIC(ss.Value) = 1 THEN CAST(ss.Value AS INT) ELSE 0 END
        WHERE ISNULL(PS.IsDeleted, 0) = 0
    ) AS Services,
    -- Doanh thu dự kiến: RM_BusinessOpportunity cột ExpectedValue
    BO.ExpectedValue AS TotalRevenue,
    -- Số lượng trao đổi: RM_ExchangeHistory
    (
        SELECT COUNT(*)
        FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_ExchangeHistory] EH
        WHERE EH.BusinessOpportunityID = BO.BusinessOpportunityID
          AND ISNULL(EH.IsDeleted, 0) = 0
    ) AS ExchangeInfo
FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_BusinessOpportunity] BO
LEFT JOIN [crm.vnptkhanhhoa.vn].[dbo].[RM_Status] S
    ON S.ID = BO.StatusID
WHERE ISNULL(BO.IsDeleted, 0) = 0
  AND (BO.StatusID IS NULL OR BO.StatusID <> 36)

ORDER BY ObjectType, Name;
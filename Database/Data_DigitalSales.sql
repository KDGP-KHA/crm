-- ==============================================================================
-- BẢNG KÊ THÔNG TIN HỒ SƠ KINH DOANH SẢN PHẨM DỊCH VỤ SỐ (RM_DigitalSales)
-- Cấu trúc tương tự và chuẩn hóa từ Database/Data.sql (Chỉ đếm số lượng)
-- Áp dụng cho: Cơ sở dữ liệu CRM VNPT Khánh Hòa ([crm.vnptkhanhhoa.vn])
-- ==============================================================================
-- Các cột thông tin tương tự Database/Data.sql:
-- 1. ID           : ID hồ sơ kinh doanh (DigitalSalesID)
-- 2. Code         : Mã hồ sơ SPDV Số (Code, ví dụ: SPDV-2026-0001)
-- 3. Name         : Tên hồ sơ Cơ hội / Dự án SPDV Số (Title)
-- 4. StatusID     : ID trạng thái quy trình (StatusID)
-- 5. StatusCode   : Mã code trạng thái (UNCAPTURED, APPROACHING, FORMATION, IMPLEMENTING,...)
-- 6. StatusName   : Tên trạng thái hiển thị (Chưa tiếp cận, Đang tiếp cận, Hình thành dự án,...)
-- 7. ObjectType   : Phân loại hồ sơ (N'Cơ hội' khi BusinessType = 1, N'Dự án' khi BusinessType = 2)
-- 8. Members      : Số lượng thành viên tham gia (RM_DigitalSalesMember join Sys_Users, không trùng lặp)
-- 9. Services     : Số lượng sản phẩm dịch vụ số (RM_DigitalSalesProduct)
-- 10. TotalRevenue: Doanh thu dự kiến (TotalExpectedRevenue)
-- 11. ExchangeInfo: Số lượng trao đổi / hoạt động (RM_DigitalSalesActivity)
-- ==============================================================================

SELECT
    DS.DigitalSalesID AS ID,
    DS.Code,
    DS.Title AS Name,
    DS.StatusID,
    S.StatusCode,
    S.StatusName,
    CASE 
        WHEN DS.BusinessType = 1 THEN N'Cơ hội' 
        WHEN DS.BusinessType = 2 THEN N'Dự án' 
        ELSE N'SPDV Số' 
    END AS ObjectType,
    -- Số lượng thành viên (không trùng lặp): RM_DigitalSalesMember join Sys_Users
    (
        SELECT COUNT(DISTINCT M.UserID)
        FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_DigitalSalesMember] M
        INNER JOIN [crm.vnptkhanhhoa.vn].[dbo].[Sys_Users] U
            ON U.UserID = M.UserID
        WHERE M.DigitalSalesID = DS.DigitalSalesID
          AND ISNULL(M.IsActive, 1) = 1
    ) AS Members,
    -- Số lượng dịch vụ: RM_DigitalSalesProduct
    (
        SELECT COUNT(*)
        FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_DigitalSalesProduct] P
        WHERE P.DigitalSalesID = DS.DigitalSalesID
          AND ISNULL(P.IsDeleted, 0) = 0
    ) AS Services,
    -- Doanh thu dự kiến của hồ sơ
    DS.TotalExpectedRevenue AS TotalRevenue,
    -- Số lượng trao đổi / hoạt động: RM_DigitalSalesActivity
    (
        SELECT COUNT(*)
        FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_DigitalSalesActivity] A
        WHERE A.DigitalSalesID = DS.DigitalSalesID
          AND ISNULL(A.IsDeleted, 0) = 0
    ) AS ExchangeInfo
FROM [crm.vnptkhanhhoa.vn].[dbo].[RM_DigitalSales] DS
LEFT JOIN [crm.vnptkhanhhoa.vn].[dbo].[RM_DigitalSalesStatus] S
    ON S.StatusID = DS.StatusID
WHERE ISNULL(DS.IsDeleted, 0) = 0
ORDER BY ObjectType, Name;

-- ============================================================
-- Migration: Bổ sung Mã tiến trình tự động (TrackingCode)
-- Format: PR + YY + MM + 6 chữ số (ví dụ: PR2609000001)
-- Date: 2026-09-14
-- ============================================================

-- 1. Thêm cột TrackingCode
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RM_DigitalSalesTracking' AND COLUMN_NAME = 'TrackingCode')
BEGIN
    ALTER TABLE dbo.RM_DigitalSalesTracking ADD TrackingCode VARCHAR(20) NULL;
END
GO

-- 2. Tạo UNIQUE INDEX (chỉ cho NOT NULL, cho phép nhiều NULL)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_RM_DigitalSalesTracking_TrackingCode' AND object_id = OBJECT_ID('RM_DigitalSalesTracking'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_RM_DigitalSalesTracking_TrackingCode 
    ON dbo.RM_DigitalSalesTracking(TrackingCode) 
    WHERE TrackingCode IS NOT NULL;
END
GO

-- 3. Backfill TrackingCode cho các bản ghi cũ (nếu cần)
DECLARE @Prefix VARCHAR(6) = 'PR' + RIGHT(CONVERT(VARCHAR(4), YEAR(GETDATE())), 2) + RIGHT('0' + CONVERT(VARCHAR(2), MONTH(GETDATE())), 2);
DECLARE @BaseSeq INT = 0;
SELECT @BaseSeq = ISNULL(MAX(CAST(RIGHT(TrackingCode, 6) AS INT)), 0)
FROM dbo.RM_DigitalSalesTracking
WHERE TrackingCode LIKE @Prefix + '%' AND LEN(TrackingCode) = 12;

;WITH CTE AS (
    SELECT TrackingID, TrackingCode,
           ROW_NUMBER() OVER (ORDER BY TrackingID) AS RowNum
    FROM dbo.RM_DigitalSalesTracking
    WHERE TrackingCode IS NULL
)
UPDATE CTE
SET TrackingCode = @Prefix + RIGHT('000000' + CAST(@BaseSeq + RowNum AS VARCHAR(6)), 6);
GO

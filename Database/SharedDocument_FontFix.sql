SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- Sửa các message đã bị ghi sai mã hóa bởi lần chạy script SharedDocument trước đó.
UPDATE dbo.Sys_Messages
SET Message = N'Từ ngày'
WHERE LangCode = 'vi-VN' AND LabelKey = 'Label_TuNgay';

UPDATE dbo.Sys_Messages
SET Message = N'Đến ngày'
WHERE LangCode = 'vi-VN' AND LabelKey = 'Label_DenNgay';
GO

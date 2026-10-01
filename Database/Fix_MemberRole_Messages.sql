-- Cập nhật chuỗi hiển thị Vai trò thành viên trong Sys_Messages
UPDATE dbo.Sys_Messages
SET Message = N'Vai trò thành viên'
WHERE LabelKey = 'DigitalSalesSearch_MemberRole_Label' AND LangCode IN ('vi', 'vi-VN');
GO

UPDATE dbo.Sys_Messages
SET Message = N'-- Chọn vai trò --'
WHERE LabelKey = 'DigitalSalesSearch_MemberRole_Option' AND LangCode IN ('vi', 'vi-VN');
GO

UPDATE dbo.Sys_Messages
SET Message = N'-- Chọn nhân viên trước --'
WHERE LabelKey = 'DigitalSalesSearch_MemberRole_Empty' AND LangCode IN ('vi', 'vi-VN');
GO

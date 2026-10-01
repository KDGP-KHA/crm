-- Cập nhật Sys_Messages: Thay thế 'Cơ hội' thành 'Kế hoạch', 'cơ hội' thành 'kế hoạch'
UPDATE Sys_Messages
SET Message = REPLACE(REPLACE(REPLACE(CAST(Message AS NVARCHAR(MAX)), N'CƠ HỘI', N'KẾ HOẠCH'), N'Cơ hội', N'Kế hoạch'), N'cơ hội', N'kế hoạch')
WHERE CAST(Message AS NVARCHAR(MAX)) LIKE N'%cơ hội%' 
   OR CAST(Message AS NVARCHAR(MAX)) LIKE N'%Cơ hội%'
   OR CAST(Message AS NVARCHAR(MAX)) LIKE N'%CƠ HỘI%';

-- Cập nhật Sys_Menus: Thay thế tên menu
UPDATE Sys_Menus
SET Name = REPLACE(REPLACE(REPLACE(Name, N'CƠ HỘI', N'KẾ HOẠCH'), N'Cơ hội', N'Kế hoạch'), N'cơ hội', N'kế hoạch')
WHERE Name LIKE N'%cơ hội%' 
   OR Name LIKE N'%Cơ hội%'
   OR Name LIKE N'%CƠ HỘI%';

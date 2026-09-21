/* Cấu hình mẫu mail cho DigitalSales. Chạy sau khi deploy file _TemplateDigitalSalesNotification.cshtml. */
DECLARE @UpdatedBy nvarchar(100) = N'SYSTEM';
DECLARE @Templates TABLE (TemplateCode nvarchar(100), TemplateName nvarchar(250), SubjectTemplate nvarchar(500));
INSERT INTO @Templates VALUES
(N'DIGITALSALES_KHOITAO', N'Khởi tạo Hồ sơ KD SPDV số', N'Khởi tạo hồ sơ: {{DigitalSalesName}}'),
(N'DIGITALSALES_THEMTHANHVIEN', N'Thêm thành viên Hồ sơ KD SPDV số', N'Bạn được thêm vào hồ sơ: {{DigitalSalesName}}'),
(N'DIGITALSALES_XOATHANHVIEN', N'Xóa thành viên Hồ sơ KD SPDV số', N'Bạn bị xóa khỏi hồ sơ: {{DigitalSalesName}}'),
(N'DIGITALSALES_CAPNHATTRANGTHAI', N'Cập nhật trạng thái Hồ sơ KD SPDV số', N'Cập nhật trạng thái hồ sơ: {{DigitalSalesName}}'),
(N'DIGITALSALES_CAPNHATTIENTRINH', N'Cập nhật tiến trình/công việc Hồ sơ KD SPDV số', N'Cập nhật tiến trình: {{DigitalSalesName}}'),
(N'DIGITALSALES_RASOAT', N'Kết quả rà soát Hồ sơ KD SPDV số', N'Kết quả rà soát hồ sơ: {{DigitalSalesName}}');

DECLARE @Code nvarchar(100), @Name nvarchar(250), @Subject nvarchar(500), @MailTemplateId int, @FilePath nvarchar(500);
DECLARE template_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT TemplateCode, TemplateName, SubjectTemplate FROM @Templates;
OPEN template_cursor; FETCH NEXT FROM template_cursor INTO @Code, @Name, @Subject;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @MailTemplateId = MailTemplateId FROM dbo.Sys_MailTemplate WHERE TemplateCode = @Code;
    SET @FilePath = CASE WHEN @Code = N'DIGITALSALES_CAPNHATTIENTRINH'
                         THEN N'Contents/Modules/Cate/EmailTemplates/_TemplateDigitalSalesTrackingUpdated.cshtml'
                         WHEN @Code = N'DIGITALSALES_RASOAT'
                         THEN N'Contents/Modules/Cate/EmailTemplates/_TemplateDigitalSalesReviewCompleted.cshtml'
                         ELSE N'Contents/Modules/Cate/EmailTemplates/_TemplateDigitalSalesNotification.cshtml' END;
    EXEC dbo.Sys_MailTemplate_Save @MailTemplateId, @Code, @Name, @Subject,
        @FilePath, 1,
        N'Thông báo nghiệp vụ DigitalSales', @UpdatedBy;
    FETCH NEXT FROM template_cursor INTO @Code, @Name, @Subject;
END
CLOSE template_cursor; DEALLOCATE template_cursor;

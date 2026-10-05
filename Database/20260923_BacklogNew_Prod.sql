/*
  Backlog DigitalSales - script deploy Production
  Chạy bằng SQLCMD hoặc bật Query > SQLCMD Mode trong SSMS.

  Phạm vi: cây tiến trình/công việc (US-01..US-04), import theo mã tiến trình
  (US-03) và đổi quy trình không sinh checklist mẫu (US-08).
  Không bao gồm scripts/create_digital_sales_db.sql vì đó là script khởi tạo
  toàn bộ phân hệ, không an toàn để chạy trên production đang vận hành.
*/
:on error exit

:r .\DigitalSales_TrackingCode_Migration.sql
:r .\DigitalSales_TrackingTree_Update.sql
:r .\RM_DigitalSalesTracking_ChangeProcessOfStatus.sql

PRINT N'Hoàn tất cập nhật DB cho backlog DigitalSales.';

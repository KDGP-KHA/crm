---
name: deploy-ftp-demo
description: Triển khai tăng dần publish_source lên FTP Demo từ máy Windows trong mạng VNPT, chỉ upload file mới hoặc thay đổi theo manifest SHA-256 và không dùng GitHub Actions. Luôn dùng khi người dùng yêu cầu "deploy-ftp-demo", "deploy ftp demo", "deploy demo", "đẩy code lên ftp demo" hoặc "upload publish_source lên ftp demo".
---

# Deploy FTP Demo cục bộ

Thực hiện deploy trực tiếp bằng `scripts/deploy_ftp_demo.ps1`. Không checkout, merge, commit, push hoặc kích hoạt GitHub Actions trong quy trình này.

## Điều kiện

- Chỉ triển khai nội dung trong `publish_source/`; không upload `bin/obj` trung gian của project nguồn.
- Tuyệt đối không upload hoặc ghi đè các file cấu hình theo môi trường: `Web.config` ở thư mục gốc, `Configs/AppSettings.config` và `Configs/ConnectionStrings.config`. Giữ nguyên bản đang có trên máy chủ Demo.
- Chỉ loại trừ đúng các đường dẫn trên; các file trùng tên ở thư mục khác như `Views/Web.config` vẫn được upload.
- FTP Demo mặc định là `10.57.30.10:21`, tài khoản `quanlydoanhthucenit`.
- Mật khẩu phải đến từ `FTP_PASSWORD_DEMO` hoặc credential DPAPI cục bộ `.secrets/ftp-demo.credential.xml`; không ghi mật khẩu vào skill, script, log hoặc commit Git.
- File credential DPAPI chỉ sử dụng được bởi đúng tài khoản Windows trên máy đã tạo file.
- Manifest `.deploy-manifest.sha256.json` trên FTP lưu SHA-256 của lần deploy thành công gần nhất. Không xóa file thừa trên FTP khi file không còn trong `publish_source`.
- Nếu manifest chưa tồn tại, không tự suy đoán FTP đã đồng bộ: chỉ dùng `-InitializeManifest` sau khi đã xác nhận FTP khớp với `publish_source`, hoặc dùng `-Force` để upload lại toàn bộ.

## Quy trình bắt buộc

1. Xác nhận `publish_source/bin/CenIT.Solution.TOC.WebApp.dll` tồn tại. Nếu thiếu, chạy `scripts/build_publish.ps1` và chỉ tiếp tục khi build thành công.
2. Xác nhận TCP tới FTP bằng `Test-NetConnection 10.57.30.10 -Port 21`. Dừng và báo lỗi nếu không kết nối được.
3. Xác nhận credential cục bộ hoặc biến môi trường tồn tại. Nếu chưa có, yêu cầu người dùng khởi tạo credential một lần; không tự đưa mật khẩu vào command hoặc log.
4. Chạy:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\deploy_ftp_demo.ps1
   ```

5. Script đọc manifest SHA-256 trên FTP và chỉ upload file mới hoặc có nội dung thay đổi. Không dùng kích thước hoặc timestamp làm tiêu chí duy nhất.
6. Xác nhận báo cáo có hai file cấu hình được bỏ qua, không được tính là lỗi upload.
7. Chỉ báo thành công khi script trả exit code `0`, số file upload thất bại bằng `0` và manifest đã cập nhật. Báo số file uploaded, unchanged, skipped, failed và không tự động retry quá một lần.

## Chế độ đặc biệt

- Khởi tạo manifest sau khi vừa upload toàn bộ thành công và đã xác nhận site đúng bản build:

  ```powershell
  powershell -ExecutionPolicy Bypass -File .\scripts\deploy_ftp_demo.ps1 -InitializeManifest
  ```

- Ép upload lại toàn bộ file ứng dụng khi cần khôi phục (vẫn bỏ qua hai file cấu hình môi trường):

  ```powershell
  powershell -ExecutionPolicy Bypass -File .\scripts\deploy_ftp_demo.ps1 -Force
  ```

- Không dùng đồng thời `-InitializeManifest` và `-Force`.

## Khởi tạo credential một lần

Chạy tương tác để mật khẩu không xuất hiện trong lịch sử lệnh:

```powershell
$credential = Get-Credential -UserName "quanlydoanhthucenit" -Message "FTP Demo"
New-Item -ItemType Directory -Force .secrets | Out-Null
$credential | Export-Clixml .secrets/ftp-demo.credential.xml
```

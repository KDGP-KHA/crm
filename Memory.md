---

# 2026-09-09 Vấn đề: GitHub Actions deploy FTP Demo thất bại

## 1. Mô tả vấn đề
Hai lần chạy workflow `Deploy Demo via FTP` trên nhánh `upcode-demo` đều báo lỗi sau khoảng 3–4 giây.

## 2. Phân tích ban đầu
- Bối cảnh: Workflow `.github/workflows/deploy-demo.yml` dùng `SamKirkland/FTP-Deploy-Action@v4.3.5` để tải `publish_source/` lên FTP Demo.
- Mục tiêu: Xác định nguyên nhân job lỗi và triển khai thành công bản demo.
- Phạm vi: Cấu hình GitHub Actions, ba GitHub Secrets FTP, khả năng kết nối FTP từ runner và phương án upload cục bộ.
- Ràng buộc: Repository riêng tư nên không thể đọc log Actions khi chưa xác thực; FTP có khả năng là địa chỉ mạng nội bộ.
- Rủi ro / Giả định: Job thất bại rất sớm nên có thể secret bị thiếu/rỗng; nếu FTP dùng IP `10.x` thì public GitHub runner có thể không có tuyến mạng. Script fallback hiện có thông tin đăng nhập mặc định trong mã nguồn, tạo rủi ro lộ bí mật và cần được khắc phục.
- Phương án sơ bộ: (1) đọc dòng lỗi chi tiết trong run; (2) bổ sung/sửa Secrets nếu thiếu; (3) nếu runner không vào được mạng nội bộ thì dùng máy nội bộ hoặc self-hosted runner; (4) xoay vòng thông tin FTP và loại bỏ bí mật khỏi source.

## 3. Câu hỏi làm rõ
1. Trong run lỗi, bước nào có dấu X đỏ và dòng lỗi cuối cùng ghi chính xác nội dung gì?
2. Trong `Settings > Secrets and variables > Actions`, cả ba secret `FTP_SERVER_DEMO`, `FTP_USERNAME_DEMO`, `FTP_PASSWORD_DEMO` đã tồn tại chưa?
3. `FTP_SERVER_DEMO` có phải là IP nội bộ dạng `10.x.x.x` và máy hiện tại có đang kết nối mạng/VPN VNPT không?

## 4. Câu trả lời & Quyết định
1. Log GitHub Actions báo: job không được khởi chạy do thanh toán tài khoản gần đây thất bại hoặc spending limit cần được tăng.
2. Quyết định: Không sửa workflow hoặc cấu hình FTP vì lỗi xảy ra trước khi runner bắt đầu chạy.
3. Hướng xử lý: Khắc phục Billing & plans rồi chạy lại workflow; nếu cần triển khai ngay thì dùng script fallback từ máy trong mạng/VPN VNPT.

## 5. Checklist
### Cập nhật chẩn đoán kết nối
- [x] Xác nhận workflow đã nhận được `FTP_SERVER_DEMO` và bắt đầu kết nối.
- [x] Xác định GitHub-hosted runner lỗi `Timeout (control socket)` khi mở kết nối FTP.
- [x] Kiểm tra máy nội bộ `10.57.33.71` kết nối thành công tới FTP `10.57.30.10:21`.
- [ ] Triển khai trực tiếp từ máy nội bộ hoặc cấu hình self-hosted runner trong mạng VNPT.

### Chuẩn bị
- [x] Xác nhận thông báo lỗi chính xác từ GitHub Actions.
- [ ] Kiểm tra phương thức thanh toán trong GitHub `Settings > Billing & plans`.
- [ ] Kiểm tra và tăng Actions spending limit nếu giới hạn đang bằng 0 hoặc đã dùng hết.

### Thực hiện
- [ ] Cập nhật phương thức thanh toán hoặc spending limit của tài khoản/tổ chức sở hữu repository.
- [ ] Chạy lại workflow `Deploy Demo via FTP` sau khi Billing hoạt động.
- [ ] Chạy `scripts/deploy_ftp_demo.ps1` từ máy trong mạng/VPN VNPT nếu cần deploy ngay mà không chờ GitHub Actions.

### Kiểm tra / Nghiệm thu
- [ ] Xác nhận GitHub runner bắt đầu job thay vì dừng ở bước khởi tạo.
- [ ] Xác nhận bước upload FTP hoàn tất thành công.
- [ ] Kiểm tra website Demo và chức năng vừa cập nhật.

### Ghi chú
- Workflow và source build hiện không phải hiện lỗi trong lần chạy này vì runner chưa được khởi tạo.
- Cần loại bỏ credential FTP mặc định khỏi source và đổi mật khẩu FTP đã lộ trong lịch sử repository.

---

# 2026-09-09 Vấn đề: Chuyển deploy FTP Demo sang chạy cục bộ

## 1. Mô tả vấn đề
Sửa skill `deploy-ftp-demo` để tự động upload `publish_source/` trực tiếp từ máy trong mạng VNPT, không thông qua GitHub Actions.

## 2. Phân tích ban đầu
- Bối cảnh: GitHub-hosted runner không truy cập được FTP nội bộ `10.57.30.10:21`, trong khi máy làm việc kết nối được.
- Mục tiêu: Lệnh deploy chạy trực tiếp, ổn định và báo lỗi chính xác khi upload không hoàn tất.
- Phạm vi: Skill deploy, script PowerShell upload FTP và cách lưu credential cục bộ.
- Ràng buộc: Không commit mật khẩu FTP; chỉ báo thành công khi mọi file được upload.
- Quyết định: Dùng credential DPAPI cục bộ, ưu tiên biến môi trường nếu được cung cấp; bỏ hoàn toàn GitHub khỏi quy trình skill.

## 3. Câu hỏi làm rõ
1. Có triển khai trực tiếp từ máy trong mạng VNPT không? → Người dùng đã xác nhận.
2. Có bỏ GitHub Actions khỏi quy trình skill không? → Người dùng đã xác nhận.

## 4. Câu trả lời & Quyết định
1. Triển khai FTP cục bộ tới server nội bộ bằng script PowerShell.
2. Lưu credential trong `.secrets/ftp-demo.credential.xml` được mã hóa theo tài khoản Windows và bị Git ignore.
3. Không ghi mật khẩu trong skill, script hoặc log.

## 5. Checklist
### Chuẩn bị
- [x] Kiểm tra kết nối TCP tới FTP nội bộ.
- [x] Tạo credential DPAPI cục bộ và thêm `.secrets/` vào `.gitignore`.

### Thực hiện
- [x] Cập nhật skill để bỏ checkout, push và GitHub Actions.
- [x] Xóa mật khẩu hard-code khỏi script deploy.
- [x] Bổ sung kiểm tra DLL publish và thống kê file upload thất bại.

### Kiểm tra / Nghiệm thu
- [x] Kiểm tra cú pháp PowerShell hợp lệ.
- [x] Kiểm tra credential cục bộ đọc được bởi tài khoản Windows hiện tại.
- [x] Kiểm tra `10.57.30.10:21` đang kết nối được.
- [x] Chạy deploy thực tế: upload thành công 3.721/3.721 file lên FTP Demo `10.57.30.10:21`.

### Ghi chú
- Mật khẩu từng tồn tại trong lịch sử Git; cần đổi mật khẩu FTP sau khi hoàn tất chuyển đổi.

---

# 2026-09-09 Vấn đề: Website Demo lỗi ngay sau deploy FTP cục bộ

## 1. Mô tả vấn đề
Sau khi upload thành công 3.721 file từ `publish_source/` lên FTP Demo, website phát sinh lỗi ngay.

## 2. Phân tích ban đầu
- Bối cảnh: Bản ASP.NET MVC 5 được upload từng file trực tiếp vào website đang chạy.
- Mục tiêu: Khôi phục website Demo và xác định nguyên nhân trước khi deploy lại.
- Phạm vi: HTTP error, IIS/Application log, `Web.config`, cấu hình môi trường, DLL và tính nhất quán của bản upload.
- Ràng buộc: Chưa có nội dung lỗi/HTTP status và chưa xác định toàn site hay một chức năng bị ảnh hưởng.
- Rủi ro / Giả định: Upload thành công về vận chuyển không chứng minh ứng dụng khởi động thành công; upload tuần tự có thể làm IIS nạp một bộ file chưa đồng nhất trong quá trình triển khai.
- Phương án sơ bộ: (1) thu thập lỗi HTTP/IIS; (2) đối chiếu cấu hình Demo; (3) rollback bản ổn định nếu cần khôi phục khẩn cấp; (4) cải tiến deploy theo gói/staging để tránh trạng thái nửa chừng.

## 3. Câu hỏi làm rõ
1. Website hiển thị chính xác mã và nội dung lỗi gì? Cần ảnh đầy đủ hoặc text lỗi, gồm HTTP 500/500.19/502/404 nếu có.
2. Toàn bộ website lỗi hay chỉ chức năng vừa cập nhật? URL nào đang lỗi?
3. Website hoạt động bình thường ngay trước lần deploy này không?
4. Có quyền xem IIS Event Viewer/log ứng dụng hoặc quyền phục hồi bản Demo cũ không?

## 4. Câu trả lời & Quyết định
1. URL lỗi: `http://crm.cenit.vn/`.
2. Lỗi toàn ứng dụng: `System.ArgumentException: Format of the initialization string does not conform to specification starting at index 0` trong `SqlConnection` khi `Application_Start` tải cache.
3. Kiểm tra cục bộ xác nhận cả 4 connection string trong `publish_source/Web.config` đều là token MSDeploy dạng `$(ReplacableToken_...)`, không phải connection string SQL; các chuỗi trong WebApp nguồn và `Source_Prod` đều hợp lệ.
4. Nguyên nhân gốc: target `Package` của MSBuild tự động parameterize connection string, sau đó `build_publish.ps1` copy trực tiếp `PackageTmp` sang `publish_source` mà không chạy bước MSDeploy thay token.
5. Quyết định đề xuất: tắt `AutoParameterizationWebConfigConnectionStrings` khi tạo package, rebuild, kiểm tra mọi connection string bằng `SqlConnectionStringBuilder`, rồi mới deploy lại.

## 5. Checklist
### Chuẩn bị
- [x] Xác định lỗi HTTP và stack trace khởi động ứng dụng.
- [x] Xác nhận connection string trong `publish_source/Web.config` không hợp lệ.
- [x] Xác định token MSDeploy là nguyên nhân trực tiếp.

### Thực hiện
- [ ] Cập nhật `build_publish.ps1` để tắt auto-parameterization connection string.
- [ ] Build lại `publish_source` ở cấu hình Release.
- [ ] Chặn deploy nếu `Web.config` còn token `$(ReplacableToken_...)` hoặc connection string không parse được.
- [ ] Deploy lại bản đã kiểm tra lên FTP Demo.

### Kiểm tra / Nghiệm thu
- [ ] Xác nhận `http://crm.cenit.vn/` khởi động không còn lỗi connection string.
- [ ] Xác nhận đăng nhập và truy vấn database Demo hoạt động.
- [ ] Xác nhận 4 provider kết nối đều dùng cấu hình hợp lệ.

### Ghi chú
- Không dùng trực tiếp `PackageTmp` khi connection string còn được MSDeploy parameterize.

---

# 2026-09-09 Vấn đề: Giữ cấu hình riêng khi deploy FTP

## 1. Mô tả vấn đề
Khi deploy FTP Demo, không upload `Web.config` và `Configs/AppSettings.config` vì Demo và Production sử dụng cấu hình môi trường khác nhau.

## 2. Phân tích ban đầu
- Bối cảnh: Quy trình hiện tại upload toàn bộ `publish_source`, từng ghi đè cấu hình server và làm ứng dụng lỗi.
- Mục tiêu: Cập nhật code ứng dụng nhưng giữ nguyên cấu hình đang hoạt động trên server.
- Phạm vi: Chỉ loại trừ `Web.config` gốc và `Configs/AppSettings.config`; các Web.config con vẫn triển khai.
- Ràng buộc: So sánh đường dẫn không phân biệt hoa thường trên Windows/FTP.
- Rủi ro / Giả định: Hai file cấu hình đã tồn tại và hợp lệ trên server trước khi deploy.
- Phương án: Lọc hai đường dẫn trong script trước khi upload và báo cáo chúng dưới trạng thái `SKIP`.

## 3. Câu hỏi làm rõ
1. Có giữ nguyên cả hai file trên server ở mọi lần deploy không? → Có, theo yêu cầu người dùng.
2. Có tiếp tục upload các `Web.config` nằm trong thư mục con không? → Có, chỉ loại trừ đúng hai đường dẫn được nêu.

## 4. Câu trả lời & Quyết định
1. Xem hai file cấu hình môi trường là server-owned và không ghi đè qua FTP.
2. Báo rõ số file được bỏ qua để kết quả deploy có thể kiểm chứng.

## 5. Checklist
### Chuẩn bị
- [x] Xác định chính xác hai đường dẫn cần bảo vệ.

### Thực hiện
- [x] Cập nhật skill với quy tắc không ghi đè cấu hình môi trường.
- [x] Cập nhật script để lọc đường dẫn không phân biệt hoa thường.
- [x] Bổ sung báo cáo file `SKIP` và tổng số file bỏ qua.

### Kiểm tra / Nghiệm thu
- [x] Xác nhận bằng kiểm thử khô rằng đúng hai file bị loại trừ.
- [x] Xác nhận `Views/Web.config` vẫn nằm trong danh sách upload.
- [x] Chạy deploy thực tế ngày 2026-09-10: upload thành công 3.720/3.720 file, bỏ qua `Web.config` và `Configs/AppSettings.config`; URL Demo phản hồi HTTP 200 và chuyển tới trang đăng nhập SSO.

### Ghi chú
- Server mới hoặc thư mục đã bị xóa sạch phải được khôi phục hai file cấu hình hợp lệ trước khi chạy deploy.

### Cập nhật chẩn đoán quyền ghi JobLogs
- [x] Xác nhận ứng dụng đã qua bước đọc connection string và tới `Application_Start` đăng ký jobs.
- [x] Xác định tài khoản IIS Application Pool không có quyền ghi vào `Contents/JobLogs`.
- [x] Xác định `JobLogWriter.FlushLogToFile` gọi `File.AppendAllText` không có cơ chế fallback, khiến lỗi ghi log làm sập quá trình khởi động và che lỗi gốc của `Jobs.ClearData.dll`.
- [ ] Xác định tên Application Pool/identity đang chạy website Demo.
- [ ] Cấp quyền `Modify` có kế thừa cho identity đó trên `Contents/JobLogs`.
- [ ] Khởi động lại Application Pool và kiểm tra website.
- [ ] Gia cố `JobLogWriter` để lỗi ghi log không làm sập `Application_Start`.
- [x] Xác định chính xác lệnh yêu cầu quyền ghi: `File.AppendAllText` tại `TSFramework.Libs/Models/Log/JobLogWriter.cs:144`, đường dẫn cố định `Contents/JobLogs`.
- [ ] Tạm đặt `App_Register_Job=0` trên Demo để cô lập khối khởi tạo job, sau đó phục hồi về `1` khi ACL đã sửa.

---

# 2026-09-11 Vấn đề: Phân luồng đăng nhập SSO theo host triển khai

## 1. Mô tả vấn đề
Cập nhật `CenIT.Solution.TOC.WebApp/Controllers/AccountController.cs`: site Demo và site chính đăng nhập qua SSO; khi chạy local hoặc truy cập bằng host khác thì sử dụng lại màn hình và luồng đăng nhập cũ.

## 2. Phân tích ban đầu
- Bối cảnh: `GET Account/Login` hiện luôn chuyển tới cổng SSO; hai action GET/POST đăng nhập cũ vẫn còn đầy đủ nhưng đang bị comment. `Logout` cũng luôn chuyển sang SSO sau khi xóa session CRM.
- Cấu hình hiện có: Demo đặt `App_HostUrl=http://crm.cenit.vn/`, `appCode=CRM_DEMO`; Production đặt `App_HostUrl=http://crm.vnptkhanhhoa.vn/`, `appCode=CRM_LIVE`. Vì cùng một code nhưng cấu hình riêng theo server, có thể nhận diện site chính thức bằng cách so sánh `Request.Url.Host` với host trong `App_HostUrl` thay vì hard-code tên miền.
- Mục tiêu: Bắt buộc SSO trên đúng host chính thức của từng môi trường, đồng thời cho phép lập trình viên hoặc host thử nghiệm dùng form đăng nhập cũ để phát triển/kiểm thử.
- Phạm vi: Phân nhánh GET Login, khôi phục POST Login cũ, phân nhánh Logout, chống open redirect và ngăn POST đăng nhập cũ trở thành đường vòng bỏ qua SSO trên host chính thức.
- Ngoài phạm vi: Không thay đổi cách xác thực SSO, mapping tài khoản, cấp quyền mặc định, giao diện form login hoặc cấu hình tài khoản.
- Các bên liên quan: Người dùng Demo/Production, lập trình viên chạy local, tài khoản nội bộ/VNPT, cổng SSO.
- Ràng buộc: `Web.config` dùng `configSource` cho AppSettings; file cấu hình Demo/Production khác nhau và không được FTP ghi đè. So sánh host phải không phân biệt hoa thường, không phụ thuộc port và không tin trực tiếp header proxy không được kiểm soát.
- Rủi ro / Giả định: Nếu chỉ phân nhánh GET mà mở lại POST cũ, người dùng có thể gọi POST trực tiếp để né SSO. Nếu SSO lỗi rồi tự fallback sang form cũ trên host chính thức, chính sách SSO có thể bị vô hiệu hóa. Khóa cấu hình cổng SSO hiện có dấu hiệu không thống nhất: code đọc `ssoPortaUrl`, còn config dùng `ssoPortalBaseUrl`.
- Phương án sơ bộ: (A, khuyến nghị) coi host là chính thức khi trùng host của `App_HostUrl`; GET/Logout dùng SSO, POST cũ bị chặn trên host này; mọi host khác dùng login cũ. (B) hard-code whitelist `crm.cenit.vn` và `crm.vnptkhanhhoa.vn`; dễ hiểu nhưng phải sửa code khi đổi tên miền. (C) thêm danh sách host SSO mới trong AppSettings; linh hoạt nhưng cần đồng bộ cấu hình riêng trên mọi server.

## 3. Câu hỏi làm rõ
1. Có chốt phương án A: so sánh host truy cập với host cấu hình trong `App_HostUrl`; trùng thì dùng SSO, khác (kể cả `localhost`, IP hoặc domain test) thì dùng login cũ không?
2. Trên host chính thức, có chặn luôn `POST /Account/Login` cũ để không thể dùng form/login request trực tiếp nhằm bỏ qua SSO không? (Khuyến nghị: có.)
3. Khi SSO lỗi trên Demo/Production, hệ thống tiếp tục hiển thị lỗi SSO và không fallback sang form cũ, đúng không? (Khuyến nghị: không fallback để giữ chính sách bảo mật.)
4. “Login như cũ” có nghĩa khôi phục nguyên luồng cũ, gồm cả tài khoản nội bộ và tài khoản email `@vnpt.vn` qua `VNPTEmailMembershipProvider`, đúng không?
5. Khi logout ở local/host khác, có xác nhận chỉ xóa Forms Authentication/session rồi quay về form Login; chỉ host chính thức mới gọi logout SSO và chuyển tới cổng SSO không?

## 4. Câu trả lời & Quyết định
- Dùng `App_HostUrl` để phân biệt host được phép đăng nhập SSO; giá trị này tự cấu hình khác nhau giữa site Demo và site chính.
- Giữ code giống nhau trên cả hai site; không hard-code tên miền Demo/Production trong `AccountController`.
- Tiếp tục không upload `Configs/AppSettings.config` khi deploy, nên cấu hình `App_HostUrl` riêng của từng server được giữ nguyên.
- Trên host trùng `App_HostUrl`, chặn POST login cũ; lỗi SSO không fallback sang form đăng nhập cũ.
- Trên host khác, khôi phục nguyên luồng login cũ gồm tài khoản nội bộ và tài khoản `@vnpt.vn`.
- Logout qua SSO chỉ áp dụng cho host trùng `App_HostUrl`; host khác chỉ xóa Forms Authentication/session và quay về form login cũ.

## 5. Checklist
### Chuẩn bị
- [x] [Bắt buộc] Tách điều kiện so sánh host thành hàm thuần, không phân biệt hoa thường và không phụ thuộc port.
- [x] [Bắt buộc] Kiểm tra đầy đủ code login cũ còn tương thích với model, view và các helper hiện tại.

### Thực hiện
- [x] [Bắt buộc] Cập nhật GET Login để chọn SSO hoặc form cũ theo `App_HostUrl`.
- [x] [Bắt buộc] Khôi phục POST Login cũ cho host không chính thức và chặn POST này trên host SSO.
- [x] [Bắt buộc] Cập nhật Logout để chỉ gọi dịch vụ/cổng SSO trên host chính thức.
- [x] [Bắt buộc] Sửa khóa cấu hình URL cổng SSO sang `ssoPortalBaseUrl`, giữ tương thích khóa cũ nếu có.
- [x] [Nên có] Giữ kiểm tra local URL cho mọi `returnUrl` trước khi redirect.

### Kiểm tra / Nghiệm thu
- [x] [Bắt buộc] Build WebApp Debug thành công, không phát sinh lỗi biên dịch.
- [x] [Bắt buộc] Test host trùng khác hoa thường, khác port và có dấu chấm cuối hostname vẫn dùng SSO.
- [x] [Bắt buộc] Test localhost, IP, host khác, cấu hình rỗng/sai đều dùng form login cũ.
- [x] [Bắt buộc] Kiểm tra code POST trên host SSO chuyển về GET Login SSO trước khi chạy xác thực cũ.
- [x] [Bắt buộc] Kiểm tra code logout local không gọi SSO và logout site chính thức vẫn hủy ticket/chuyển cổng SSO.

### Ghi chú
- `App_HostUrl` là cấu hình do từng server sở hữu và tiếp tục bị loại khỏi danh sách file upload FTP.
- Bộ test tự động `tests/account-login/Run-HostPolicyTests.ps1` build WebApp và gọi trực tiếp hàm policy đã biên dịch; kết quả 10/10 PASS.
- Không thay đổi `Configs/AppSettings.config`; Demo và Production tiếp tục dùng cấu hình server riêng.

---

# 2026-09-14 Vấn đề: Clone Rà soát định kỳ cho DigitalSales

## 1. Mô tả vấn đề
Clone chức năng Rà soát định kỳ hiện tại thành chức năng rà soát `DigitalSales`. Danh sách mới chỉ có một tab DigitalSales; khi chọn rà soát sẽ chuyển tới `DigitalSales/Detail`. Tại trang chi tiết hiển thị panel rà soát bên phải, có thể thu hẹp và hỗ trợ hai lựa chọn “Lưu” hoặc “Lưu và tiếp tục”, tương tự luồng rà soát Cơ hội/Dự án hiện có.

## 2. Phân tích ban đầu
- Bối cảnh: Luồng cũ nằm tại `ReviewBatchItemController`, `Views/ReviewBatchItem`, sử dụng hai danh sách Dự án/Cơ hội và mở `ProjectOverview` hoặc `BusinessOpportunityOverview` kèm `reviewBatchID`.
- Cơ chế hiện tại: `RM_ReviewBatchItem` và `RM_ReviewHistory` lưu theo cặp `ObjectType`/`ObjectID`; hiện `ObjectType=1` là Cơ hội và `ObjectType=2` là Dự án. Stored procedure lưu/lấy lịch sử có thể nhận loại mới, nhưng cần quy ước `ObjectType=3` cho DigitalSales và bổ sung stored procedure danh sách.
- Giao diện chi tiết cũ: `_ReviewBatch.cshtml` tạo panel neo bên phải, hỗ trợ thu gọn, không chặn nội dung nền, có “Lưu” và “Lưu và tiếp tục”. Khi tiếp tục, controller tìm đối tượng chưa rà soát kế tiếp rồi chuyển URL.
- Hiện trạng DigitalSales: `DigitalSalesController.Detail(int id)` và `Views/DigitalSales/Detail.cshtml` chưa nhận `reviewBatchID`, chưa có `reviewSplitView`, `reviewFormPane`, nút tự mở form hoặc vùng lịch sử rà soát.
- Mục tiêu đề xuất: Tạo màn hình rà soát DigitalSales độc lập nhưng tái sử dụng đợt rà soát, bảng lịch sử, form/panel và nghiệp vụ phân cấp hiện có; bổ sung URL `DigitalSales/Detail/{id}?reviewBatchID=...`.
- Phạm vi dự kiến: Model tìm kiếm/kết quả, Cache/Biz, controller và view danh sách một tab, stored procedure danh sách, tích hợp panel vào Detail, lưu với `ObjectType=3`, tìm bản ghi kế tiếp, App_Message/menu/quyền nếu cần.
- Ngoài phạm vi dự kiến: Không sửa luồng Cơ hội/Dự án cũ; không đổi cấu trúc bảng nếu `ObjectType=3` dùng được với schema hiện tại; không thay cơ chế `ReviewLevel`.
- Ràng buộc: Tuân thủ MVC_RULES, App_Message, anti-forgery, UTF-8 BOM; giữ panel thu gọn và hai chế độ lưu; danh sách cần paging/filter/quyền như luồng cũ.
- Rủi ro / giả định: “Clone” chưa xác định là tạo mới song song hay thay thế màn hình cũ; chưa rõ dùng chung đợt rà soát, tiêu chí chọn DigitalSales và vị trí lịch sử.
- Phương án khuyến nghị: Tạo chức năng mới song song `DigitalSalesReview`, dùng chung `RM_ReviewBatch` và lịch sử, quy ước `ObjectType=3`, tái sử dụng partial form/panel cũ.

## 3. Câu hỏi làm rõ
1. Chức năng mới sẽ chạy song song và giữ nguyên Rà soát định kỳ Cơ hội/Dự án cũ, đúng không? Khuyến nghị tạo route/controller riêng `Cate/DigitalSalesReview`.
2. DigitalSales có dùng chung danh mục “Đợt rà soát” (`RM_ReviewBatch`) hiện tại hay cần danh mục đợt riêng? Khuyến nghị dùng chung.
3. Có thống nhất dùng `ObjectType = 3` trong `RM_ReviewBatchItem`/`RM_ReviewHistory` cho DigitalSales và giữ nguyên form, file đính kèm, xác nhận, lịch sử không? Khuyến nghị có.
4. Danh sách một tab cần các bộ lọc nào? Khuyến nghị: từ khóa, đợt rà soát, loại hình DigitalSales, trạng thái, phòng ban, nhân sự phụ trách và Đã/Chưa rà soát.
5. Phạm vi dữ liệu có áp dụng quyền phòng ban và `ReviewLevel` như chức năng cũ, đồng thời chỉ hiển thị DigitalSales người dùng được quyền xem không? Khuyến nghị có cả hai lớp quyền.
6. Trên `DigitalSales/Detail`, ngoài panel tự mở khi có `reviewBatchID`, có cần hiển thị lịch sử rà soát trong tab “Trao đổi chung & Hoạt động” không? Khuyến nghị có.
7. Sau khi hoàn thiện có cần cập nhật stored procedure và App_Message trực tiếp trên DB Demo, đồng thời build kiểm tra nhưng chưa publish/deploy không? Khuyến nghị có.

## 4. Câu trả lời & cập nhật phạm vi
1. Không tạo chức năng rà soát DigitalSales chạy song song. Thay thế hoàn toàn màn hình rà soát Cơ hội/Dự án cũ bằng màn hình rà soát DigitalSales một tab.
2. Bổ sung một tab riêng “Lịch sử rà soát” tại `DigitalSales/Detail`, không ghép lịch sử vào tab “Trao đổi chung & Hoạt động”.
3. Các nội dung còn cần xác nhận: cách xử lý dữ liệu lịch sử cũ, danh mục đợt rà soát, bộ lọc, quyền và phạm vi cập nhật DB/build.

## 5. Câu hỏi làm rõ bổ sung
1. Dữ liệu rà soát Cơ hội/Dự án cũ có giữ nguyên trong DB để tra cứu/báo cáo về sau, chỉ loại khỏi giao diện mới không? Khuyến nghị giữ dữ liệu cũ và dùng `ObjectType=3` cho DigitalSales.
2. Có tiếp tục dùng chung danh mục “Đợt rà soát” và cơ chế cấp rà soát `ReviewLevel` hiện tại không? Khuyến nghị có.
3. Danh sách DigitalSales dùng các bộ lọc: từ khóa, đợt rà soát, loại hình, trạng thái, phòng ban, nhân sự phụ trách và Đã/Chưa rà soát, đúng không? Khuyến nghị có.
4. Có áp dụng quyền phòng ban/cấp rà soát như cũ và cập nhật stored procedure/App_Message trực tiếp trên DB Demo, sau đó build kiểm tra nhưng chưa publish/deploy không? Khuyến nghị có.

## 6. Câu trả lời & Quyết định cuối
1. Chức năng rà soát mới mặc định chỉ làm việc với DigitalSales, không sử dụng `ObjectType` trong model, bộ lọc hoặc luồng nghiệp vụ.
2. Tiếp tục dùng chung danh mục “Đợt rà soát” và cơ chế `ReviewLevel` hiện tại.
3. Danh sách sử dụng các bộ lọc: từ khóa, đợt rà soát, loại hình, trạng thái, phòng ban, nhân sự phụ trách và Đã/Chưa rà soát.
4. Giữ cơ chế phân quyền phòng ban/cấp rà soát; cập nhật stored procedure và App_Message trên DB Demo; build kiểm tra nhưng không publish/deploy.
5. Thay thế hoàn toàn giao diện rà soát Cơ hội/Dự án cũ bằng một danh sách DigitalSales; trang `DigitalSales/Detail` có tab “Lịch sử rà soát” riêng.

## 7. Checklist: Thay thế Rà soát định kỳ bằng Rà soát DigitalSales

### Chuẩn bị
- [x] Kiểm tra cấu trúc Controller, Model, Biz/Cache, View, JavaScript và stored procedure của chức năng rà soát hiện tại.
- [x] Đối chiếu `MVC_RULES.md` và các quy tắc form/AJAX liên quan.

### Thực hiện
- [x] Cập nhật model tìm kiếm/kết quả rà soát để chỉ biểu diễn DigitalSales và không lộ `ObjectType`.
- [x] Cập nhật Biz/Cache và stored procedure lấy danh sách DigitalSales theo đầy đủ bộ lọc, quyền phòng ban và `ReviewLevel`.
- [x] Thay màn hình hai tab Cơ hội/Dự án bằng một danh sách DigitalSales tại chức năng rà soát hiện tại.
- [x] Cập nhật hành động “Rà soát” chuyển tới `DigitalSales/Detail` kèm đợt rà soát.
- [x] Tích hợp panel rà soát bên phải tại trang chi tiết, hỗ trợ thu gọn, “Lưu” và “Lưu và tiếp tục”.
- [x] Bổ sung tab “Lịch sử rà soát” riêng trong `DigitalSales/Detail` và tải lịch sử của DigitalSales hiện tại.
- [x] Bổ sung/cập nhật App_Message và stored procedure trên DB Demo.
- [x] Đồng bộ View/JavaScript theo quy tắc Triple Mirroring và chuẩn hóa UTF-8 with BOM.

### Kiểm tra / Nghiệm thu
- [x] Kiểm tra danh sách, bộ lọc, phân quyền và trạng thái Đã/Chưa rà soát.
- [x] Kiểm tra panel mở/thu gọn và hai luồng lưu trên DigitalSales hiện tại/tiếp theo.
- [x] Kiểm tra tab lịch sử chỉ hiển thị dữ liệu của DigitalSales đang xem.
- [x] Chạy kiểm tra stored procedure/service liên quan trên DB Demo.
- [x] Build các project liên quan thành công, không publish/deploy.

### Ghi chú
- Phạm vi không gồm publish source, upload FTP hoặc deploy Demo.
- DB Demo đã có 3 stored procedure `RM_DigitalSalesReview_*` và 27 App_Message; kiểm tra lưu/lịch sử được chạy trong transaction rồi rollback.
- Kết quả kiểm thử: DigitalSales Management 44/44 PASS; DigitalSales Review 32/32 PASS; build chuẩn WebApp không có lỗi biên dịch.
- Kiểm tra biên dịch toàn bộ Razor bằng `MvcBuildViews` còn dừng tại các view `Sys/User` ngoài phạm vi do `SysUserSearchModel` thiếu các thuộc tính đang được view sử dụng.
- Cấu trúc DB cũ chỉ được giữ ở mức cần thiết để tương thích; nghiệp vụ mới không yêu cầu người dùng chọn loại đối tượng rà soát.

---

# 2026-09-14 Vấn đề: Sửa UI và hành vi mở rà soát DigitalSales

## 1. Mô tả vấn đề
- Giao diện panel rà soát đang lỗi và báo JavaScript `CKFinder is not defined`.
- Cần bỏ nút tắt modal rà soát.
- Khi bấm tên DigitalSales trong danh sách rà soát cũng phải chuyển vào luồng rà soát, thay vì chỉ nút “Rà soát” thực hiện hành vi này.

## 2. Phân tích ban đầu
- Bối cảnh: lỗi nằm trong luồng từ danh sách `ReviewBatchItem` sang `DigitalSales/Detail` và panel rà soát bên phải.
- Nguyên nhân JavaScript: trang chi tiết đang nạp `ckeditor.js`, nhưng cấu hình CKEditor gọi `CKFinder.setupCKEditor(...)` khi global `CKFinder` chưa được nạp; lỗi làm trình soạn thảo nội dung không khởi tạo hoàn chỉnh.
- Nguyên nhân điều hướng: renderer cột tên chỉ tạo URL `/Cate/DigitalSales/Detail/{id}`; renderer nút hành động mới bổ sung `reviewBatchID` cho bản ghi chưa rà soát.
- Nút đóng: layout `_Form.cshtml` dùng chung tự sinh nút đóng ở header và nút Hủy ở footer; cần ẩn theo phạm vi `ReviewBatch` thay vì sửa layout dùng chung.
- Rủi ro: nếu bỏ mọi đường đóng panel, người dùng có thể bị giữ ở màn hình khi không muốn lưu; nếu tên luôn truyền đợt rà soát cho cả bản ghi đã rà soát, hành vi có thể trở thành rà soát lại thay vì chỉ xem chi tiết.
- Phương án sơ bộ: nạp CKFinder trước CKEditor hoặc chặn cấu hình CKFinder khi thư viện không tồn tại; ẩn nút đóng bằng selector riêng của modal rà soát; dùng chung một hàm sinh URL cho cột tên và nút hành động.

## 3. Câu hỏi làm rõ
1. “Bỏ nút tắt modal rà soát” là chỉ bỏ dấu `X` trên tiêu đề, hay bỏ cả dấu `X` và nút “Hủy” ở footer?
2. Khi bấm tên một bản ghi chưa rà soát và đã chọn đợt, có đúng là mở `DigitalSales/Detail` kèm panel rà soát như nút “Rà soát” không?
3. Với bản ghi đã rà soát, bấm tên chỉ mở chi tiết/lịch sử hay vẫn mở panel để rà soát lại trong cùng đợt?
4. Với CKFinder, có dùng đầy đủ chức năng duyệt/chèn ảnh trong nội dung rà soát không? Phương án đề xuất là nạp đúng `ckfinder.js` trước CKEditor để giữ nguyên trình soạn thảo đầy đủ.

## 4. Câu trả lời & Quyết định
- Bỏ cả dấu `X` trên header và nút “Hủy” ở footer của riêng panel rà soát; không thay đổi layout modal dùng chung.
- Bấm tên bản ghi chưa rà soát sẽ mở `DigitalSales/Detail` kèm đợt rà soát, giống nút “Rà soát”.
- Bấm tên bản ghi đã rà soát chỉ mở chi tiết để xem lịch sử, không tự mở panel rà soát lại.
- Giữ đầy đủ CKFinder và nạp thư viện trước CKEditor.

## 5. Checklist

### Chuẩn bị
- [x] Kiểm tra đường dẫn và thứ tự nạp CKFinder/CKEditor hiện có.
- [x] Kiểm tra renderer liên kết tên và nút hành động trong danh sách rà soát.

### Thực hiện
- [x] Nạp CKFinder trước CKEditor tại trang chi tiết DigitalSales.
- [x] Loại bỏ dấu `X` và nút “Hủy” trong riêng modal `ReviewBatch`.
- [x] Dùng chung quy tắc tạo URL cho tên bản ghi và nút hành động.
- [x] Giữ bản ghi đã rà soát ở chế độ xem chi tiết/lịch sử, không mở panel tự động.
- [x] Đồng bộ View/JavaScript theo Triple Mirroring và UTF-8 BOM.

### Kiểm tra / Nghiệm thu
- [x] Kiểm tra cú pháp JavaScript và thứ tự nạp thư viện.
- [x] Kiểm tra URL của tên bản ghi cho cả trạng thái đã/chưa rà soát.
- [x] Chạy bộ test DigitalSales Review.
- [x] Build các project liên quan, không publish/deploy.

### Ghi chú
- Không sửa `_Form.cshtml` dùng chung để tránh ảnh hưởng các modal khác.
- Kết quả: DigitalSales Review 35/35 PASS; WebApp build thành công.
- Không publish source và không deploy.

---

# 2026-09-15 Vấn đề: Lỗi font chữ tại danh sách sản phẩm / dịch vụ DigitalSales

## 1. Mô tả vấn đề
- Các nhãn tiếng Việt trong `DigitalSales/Detail#tab-products` hiển thị sai dạng mojibake, ví dụ `Danh sÃ¡ch`, `ThÃªm sáº£n pháº©m`, trong khi tên sản phẩm / dịch vụ vẫn hiển thị đúng.

## 2. Phân tích ban đầu
- Bối cảnh: giao diện lấy nhãn qua `AppProcessor.Messagor.GetMessage(...)` từ bảng `Sys_Messages`; dữ liệu tên dịch vụ lấy từ bảng nghiệp vụ riêng.
- Mục tiêu: khôi phục đúng toàn bộ nhãn tiếng Việt của phần chi tiết sản phẩm / dịch vụ DigitalSales và ngăn lỗi tái diễn khi chạy script DB.
- Phạm vi dự kiến: các message mới có khóa `DigitalSalesProduct_*`, script `Database/DigitalSalesProductDetail.sql` và kiểm thử hồi quy encoding; không thay đổi font CSS hay dữ liệu tên dịch vụ.
- Ràng buộc: giữ yêu cầu trước đó là không đồng bộ sang WebApp và `publish_source`, không publish/deploy.
- Kết quả kiểm tra: DB Demo hiện có 60 message `DigitalSalesProduct_*`, trong đó 42 message chứa dấu hiệu mojibake. Các message được thêm riêng bằng truy vấn Unicode an toàn vẫn hiển thị đúng.
- Kết quả kiểm tra nguồn: `Database/DigitalSalesProductDetail.sql` có UTF-8 BOM (`EF BB BF`) và chuỗi `N'...'` đúng tiếng Việt; lỗi phát sinh ở bước công cụ thực thi/giải mã script trước khi SQL Server lưu dữ liệu.
- Rủi ro: chỉ sửa view hoặc CSS sẽ không xử lý nguyên nhân; chạy lại script theo cách cũ có thể tiếp tục ghi đè message đúng thành mojibake; cache message của ứng dụng có thể cần nạp lại sau khi cập nhật DB.
- Phương án sơ bộ: cập nhật lại toàn bộ message từ nguồn Unicode bằng truy vấn tham số hoặc ép `sqlcmd` dùng code page UTF-8; bổ sung test so sánh chính xác nội dung tiếng Việt và kiểm tra mojibake.

## 3. Câu hỏi làm rõ
1. Có sửa toàn bộ 42 message `DigitalSalesProduct_*` đang lỗi trong DB Demo, thay vì chỉ các nhãn đang thấy trong ảnh không?
2. Có cập nhật quy trình/script chạy migration theo hướng Unicode an toàn để lần chạy sau không tái diễn không?
3. Có bổ sung kiểm thử đối chiếu chính xác nội dung tiếng Việt trong `Sys_Messages`, không chỉ kiểm tra message tồn tại không?
4. Sau khi sửa DB, có cho phép làm mới cache message của site local nếu refresh trình duyệt chưa nhận dữ liệu mới không?
5. Tiếp tục giữ phạm vi không đồng bộ WebApp/`publish_source` và không publish/deploy, đúng không?

## 4. Câu trả lời & Quyết định
- Sửa toàn bộ 42 message `DigitalSalesProduct_*` đang bị mojibake trong DB Demo.
- Cập nhật cách chạy migration theo hướng Unicode an toàn.
- Không bổ sung kiểm thử hồi quy theo yêu cầu.
- Không chủ động làm mới cache message của ứng dụng; người dùng sẽ tải lại trang sau khi DB được sửa.
- Không đồng bộ WebApp/`publish_source`, không publish/deploy.

## 5. Checklist

### Chuẩn bị
- [x] Đối chiếu nhãn lỗi trên ảnh với các khóa message được view sử dụng.
- [x] Kiểm tra trực tiếp dữ liệu `Sys_Messages` trên DB Demo.
- [x] Kiểm tra encoding và nội dung Unicode của script nguồn.

### Thực hiện
- [x] Cập nhật toàn bộ message `DigitalSalesProduct_*` từ nguồn bằng phương thức truyền Unicode an toàn.
- [x] Thêm `scripts/apply_database_script_utf8.ps1` để đọc UTF-8 nghiêm ngặt và thực thi từng batch SQL.
- [x] Ghi rõ yêu cầu dùng runner Unicode hoặc `sqlcmd -f 65001` trong script migration.
- [x] Không bổ sung kiểm thử hồi quy theo quyết định của người dùng.

### Kiểm tra / Nghiệm thu
- [x] Xác nhận 62 message liên quan `DigitalSalesProduct...` không còn mojibake.
- [x] Xác nhận 48 message do migration quản lý khớp chính xác với nội dung tiếng Việt trong file SQL.
- [x] Xác nhận các nhãn chính trả về đúng: `Danh sách Sản phẩm / Dịch vụ số`, `Thêm sản phẩm / dịch vụ`, `triệu VNĐ`.
- [x] Kiểm tra cú pháp runner PowerShell và thực thi thành công 13 batch trên DB Demo.
- [x] Không build vì không thay đổi mã nguồn ứng dụng; không publish/deploy.

### Ghi chú
- Không sửa CSS/font vì bằng chứng hiện tại xác định lỗi nằm ở dữ liệu message trong DB.
- Không chủ động xóa/làm mới cache ứng dụng theo yêu cầu; cần tải lại trang để kiểm tra giao diện.

---

# 2026-09-14 Yêu cầu: Hoàn thiện thông tin dịch vụ trong chi tiết DigitalSales

## 1. Mô tả vấn đề
- Tham chiếu phần dịch vụ của `Cate/ProjectOverview/Index` và màn chi tiết `ProductProjectOverview` để cập nhật phần Sản phẩm / Dịch vụ tại `DigitalSales/Detail/{id}#tab-products`.
- Giao diện mới cần tối giản hơn `ProductProjectOverview`: thông tin của một dịch vụ được hiển thị và cập nhật trong một form, không chia thành nhiều tab.
- Không đồng bộ thay đổi sang `CenIT.Solution.TOC.WebApp` và `publish_source` sau khi thực hiện.

## 2. Phân tích ban đầu
- Tab sản phẩm của DigitalSales hiện hiển thị dạng bảng và mở modal thêm/sửa riêng cho từng dịch vụ.
- `RM_DigitalSalesProductModel` hiện chỉ lưu: sản phẩm/dịch vụ, gói cước/quy mô, số lượng, doanh thu dự kiến, doanh thu thực tế, ngày bắt đầu/kết thúc và ghi chú.
- `ProductProjectOverview` ngoài thông tin chung còn có các tập dữ liệu con độc lập: thành viên theo dịch vụ, chi phí, doanh thu thực thu, hợp đồng và công việc. Mỗi tập dữ liệu hiện dùng model/cache/stored procedure riêng, liên kết bằng `ProductProjectID`.
- DigitalSales đã có thành viên và tiến trình/checklist ở cấp hồ sơ, nhưng chưa có cấu trúc tương ứng ở cấp từng `SalesProductID`.
- Nếu yêu cầu đưa cả chi phí, các lần thu doanh thu, hợp đồng hoặc thành viên vào DigitalSales thì cần bổ sung model/bảng/stored procedure riêng hoặc xác định rõ cơ chế tái sử dụng dữ liệu cũ. Đây không chỉ là thay đổi giao diện.
- Phương án sơ bộ an toàn là giữ thao tác theo từng dịch vụ, dùng một form tổng hợp duy nhất; các nhóm dữ liệu được bố trí thành từng section/khối trên cùng form thay vì tab.
- Phạm vi source dự kiến chỉ gồm `Core.Cate`, `Modules.Cate`, script DB và kiểm thử liên quan; tuyệt đối không mirror sang WebApp/publish_source theo yêu cầu.

## 3. Câu hỏi làm rõ
1. Một form dịch vụ cần quản lý những nhóm nào: chỉ thông tin chung hiện có, hay thêm cả chi phí, doanh thu thực thu, hợp đồng và thành viên giống `ProductProjectOverview`?
2. Nếu có chi phí/doanh thu/hợp đồng, mỗi nhóm có được nhập nhiều dòng ngay trong form (thêm/xóa dòng) hay chỉ lưu một bộ số liệu tổng hợp?
3. Thành viên của dịch vụ sẽ là danh sách riêng theo từng dịch vụ, hay chỉ hiển thị/tái sử dụng danh sách thành viên chung của DigitalSales?
4. Phần công việc/tiến độ của `ProductProjectOverview` có đưa vào form dịch vụ không, hay tiếp tục dùng tab Tiến trình & Checklist chung của DigitalSales?
5. “Một form” được hiểu là một modal/form cho từng dịch vụ, hay một form duy nhất sửa đồng thời toàn bộ danh sách dịch vụ của hồ sơ DigitalSales?
6. Có cho phép bổ sung bảng/stored procedure mới và cập nhật trực tiếp DB Demo trong lần thực hiện này không?
7. Số tiền hiển thị/nhập theo đơn vị VNĐ như DigitalSales hiện tại hay theo triệu VNĐ như màn dự án cũ?

## 4. Câu trả lời & Quyết định
- Mỗi dịch vụ quản lý đầy đủ thông tin chung, nhiều dòng chi phí, nhiều dòng doanh thu thực thu và danh sách thành viên riêng.
- Không đưa hợp đồng vào giai đoạn này.
- Không đưa công việc/tiến độ vào form dịch vụ; tiếp tục sử dụng tab Tiến trình & Checklist chung của DigitalSales.
- Khi thêm mới hoặc chỉnh sửa, mở một form duy nhất cho đúng một dịch vụ; các nhóm dữ liệu nằm trên cùng form và không chia tab.
- Được phép bổ sung bảng/stored procedure mới và cập nhật trực tiếp DB Demo.
- Số tiền hiển thị và nhập theo đơn vị triệu VNĐ.
- Chỉ thay đổi source gốc; không đồng bộ sang `CenIT.Solution.TOC.WebApp` và `publish_source`.

## 5. Checklist dự kiến

### Chuẩn bị
- [x] Đối chiếu tab sản phẩm hiện tại của DigitalSales.
- [x] Đối chiếu dữ liệu và giao diện dịch vụ của ProjectOverview/ProductProjectOverview.
- [x] Chốt nhóm dữ liệu, quan hệ dữ liệu và cách bố trí form.

### Thực hiện
- [x] Cập nhật model, Biz và Cache cho chi phí, doanh thu thực thu và thành viên riêng theo dịch vụ.
- [x] Bổ sung ba bảng dữ liệu con và stored procedure tải/lưu tổng hợp/xóa mềm.
- [x] Thiết kế lại danh sách dịch vụ dạng card, hiển thị doanh thu dự kiến, thực thu, chi phí, lợi nhuận và số thành viên.
- [x] Tạo modal một form cho một dịch vụ, chia section dọc và hỗ trợ thêm/xóa nhiều dòng ngay trên form.
- [x] Thực hiện validation tại Controller, trả partial form khi dữ liệu không hợp lệ và submit AJAX khép kín.
- [x] Bổ sung App_Message cho toàn bộ nhãn/thông báo mới và cập nhật DB Demo.

### Kiểm tra / Nghiệm thu
- [x] Kiểm thử transaction lưu nhiều dòng, tổng hợp số liệu, dữ liệu sai và xóa mềm liên đới trên DB Demo.
- [x] Kiểm tra cú pháp/biên dịch ba Razor view và cú pháp JavaScript inline.
- [x] Chạy test mới 28/28, hồi quy DigitalSales Management 44/44 và Workflow 30/30.
- [x] Build Core.Cate và Modules.Cate thành công, 0 lỗi biên dịch.
- [x] Xác nhận không thay đổi các file tương ứng trong WebApp và publish_source.

### Ghi chú
- DB Demo đã được cập nhật bằng `Database/DigitalSalesProductDetail.sql`; dữ liệu doanh thu thực tế hiện hữu được bảo toàn thành một lần ghi nhận ban đầu.
- `ActualRevenue` của dịch vụ được tính tự động từ tổng các dòng doanh thu thực thu; tổng doanh thu của hồ sơ DigitalSales được cập nhật trong cùng transaction.
- Thẩm định UI theo `ui-ux-designer` và `ui-visual-validator`: bố cục dùng Ace Admin tokens, responsive, có trạng thái dữ liệu/trống/loading qua cơ chế hiện hữu, modal cuộn và khóa nút khi lưu. Không chụp runtime vì người dùng yêu cầu không đồng bộ source sang WebApp đang chạy.
- Không sửa `CenIT.Solution.TOC.WebApp` và `publish_source`; không publish/deploy.

---

# 2026-09-14 Vấn đề: Lỗi Razor lúc 10:38:55.207 PM tại chi tiết DigitalSales

## 1. Mô tả vấn đề
- Đọc lỗi trong `2026-09-14_LogsFile.log` lúc `10:38:55.207 PM` và sửa lỗi.

## 2. Phân tích ban đầu
- Log báo `Unexpected "if" keyword after "@" character` khi `DigitalSales/Detail.cshtml` render partial `_DetailTracking` tại dòng 163.
- Build `MvcBuildViews` định vị chính xác lỗi tại `_DetailTracking.cshtml:143`.
- Dòng lỗi là `@if (realTaskCount == 0)` đang nằm trực tiếp trong block C# của vòng `foreach`; Razor yêu cầu `if (...)` không có tiền tố `@`.
- Phạm vi sửa chỉ là cú pháp Razor trong partial Tiến trình/Checklist; không thay đổi nghiệp vụ hoặc DB.

## 3. Câu hỏi làm rõ
1. Người dùng yêu cầu sửa ngay; mặc định chỉ sửa nguyên nhân trực tiếp, thêm kiểm thử và build, không publish/deploy.

## 4. Câu trả lời & Quyết định
- Áp dụng giả định mặc định nêu trên để xử lý ngay.

## 5. Checklist

### Chuẩn bị
- [x] Đọc stack trace đúng thời điểm `10:38:55.207 PM`.
- [x] Dùng `MvcBuildViews` xác định đúng file và dòng lỗi.

### Thực hiện
- [x] Bỏ tiền tố `@` thừa trước `if (realTaskCount == 0)` trong `_DetailTracking.cshtml`.
- [x] Đồng bộ partial theo Triple Mirroring và UTF-8 BOM.
- [x] Bổ sung kiểm thử hồi quy cho cấu trúc Razor này.

### Kiểm tra / Nghiệm thu
- [x] Chạy kiểm tra Triple Mirroring và test DigitalSales.
- [x] Build Razor bằng `MvcBuildViews` xác nhận không còn lỗi `_DetailTracking`.
- [x] Build chuẩn WebApp thành công, không publish/deploy.

### Ghi chú
- Không sửa các lỗi Razor ngoài phạm vi nếu chúng xuất hiện tiếp sau khi lỗi `_DetailTracking` được loại bỏ.
- Kết quả: DigitalSales Review 41/41 PASS; `MvcBuildViews` không còn lỗi `_DetailTracking`; WebApp build chuẩn thành công.
- `MvcBuildViews` toàn site vẫn còn 5 lỗi model/view tại `Areas/Sys/Views/User` không liên quan yêu cầu này.
- Không publish source và không deploy.

---

# 2026-09-14 Vấn đề: Đồng bộ giao diện search rà soát DigitalSales theo search cũ

## 1. Mô tả vấn đề
- Cập nhật ô search của rà soát DigitalSales có style và nội dung tương tự phần search rà soát cũ theo ảnh tham chiếu.
- Bỏ bộ lọc “Loại hình”.

## 2. Phân tích ban đầu
- Bối cảnh: partial `_SearchDigitalSales.cshtml` hiện dùng grid Bootstrap bốn cột, có bộ lọc Loại hình và có hai nút Tìm kiếm/Đặt lại.
- Search cũ dùng card header xanh, body `p-2`, hai hàng flex: hàng đầu gồm Từ khóa/Đợt rà soát/Trạng thái; hàng sau gồm Phòng ban/Nhân viên/Trạng thái rà soát/nút Tìm kiếm.
- Mục tiêu: giữ nguyên nghiệp vụ lọc DigitalSales nhưng đưa bố cục, kích thước và nhãn về cùng chuẩn giao diện cũ; loại bỏ Loại hình khỏi giao diện và state JavaScript.
- Ràng buộc: trạng thái DigitalSales hiện được tải lại theo Loại hình; khi bỏ bộ lọc này phải xác định cách biểu diễn đồng thời trạng thái Cơ hội và Dự án.
- Rủi ro: các trạng thái của hai loại có thể trùng tên hoặc khác mã; danh sách gộp không có nhãn nhóm có thể gây khó hiểu.
- Phương án sơ bộ: hiển thị toàn bộ trạng thái trong một dropdown, có thể gắn tiền tố/nhóm Cơ hội và Dự án; giữ cơ chế tự tìm khi đổi dropdown/radio như search cũ.

## 3. Câu hỏi làm rõ
1. Dropdown “Trạng thái” sau khi bỏ Loại hình sẽ hiển thị toàn bộ trạng thái Cơ hội và Dự án; có cần ghi tiền tố `Cơ hội - ...` và `Dự án - ...` để phân biệt không?
2. Có bỏ nút “Đặt lại” và chỉ giữ một nút “Tìm kiếm” bên phải đúng như ảnh không?
3. Có giữ hành vi tự động tìm khi đổi Đợt rà soát, Trạng thái, Phòng ban, Nhân viên hoặc Đã/Chưa rà soát như chức năng cũ không?
4. Bố cục desktop áp dụng đúng hai hàng `3 ô` và `3 bộ lọc + nút`; trên màn hình nhỏ cho phép tự xuống hàng, đúng không?

## 4. Câu trả lời & Quyết định
- Giữ giao diện tổng thể tương tự màn rà soát cũ trong ảnh: card tìm kiếm hai hàng và bảng dữ liệu ngay bên dưới.
- Chỉ hiển thị một nội dung DigitalSales, không hiển thị hai tab Cơ hội/Dự án.
- Bỏ bộ lọc Loại hình; dùng một dropdown trạng thái chung của DigitalSales.
- Chỉ giữ nút “Tìm kiếm” ở cuối hàng thứ hai; giữ hành vi lọc khi đổi điều kiện như chức năng cũ.
- Cho phép các ô tự xuống hàng trên màn hình nhỏ.

## 5. Checklist

### Chuẩn bị
- [x] Đối chiếu cấu trúc `_SearchProject.cshtml` cũ và `_SearchDigitalSales.cshtml` hiện tại.
- [x] Kiểm tra nguồn dữ liệu trạng thái chung của DigitalSales.

### Thực hiện
- [x] Chuyển search DigitalSales sang card hai hàng flex theo giao diện rà soát cũ.
- [x] Bố trí hàng đầu gồm Từ khóa, Đợt rà soát và Trạng thái.
- [x] Bố trí hàng sau gồm Phòng ban, Nhân viên, Trạng thái rà soát và nút Tìm kiếm.
- [x] Loại bỏ bộ lọc Loại hình và nút Đặt lại khỏi View/JavaScript/state request.
- [x] Giữ màn hình một nội dung DigitalSales, không bổ sung tab Cơ hội/Dự án.
- [x] Đồng bộ Triple Mirroring và chuẩn hóa UTF-8 BOM.

### Kiểm tra / Nghiệm thu
- [x] Kiểm tra cú pháp JavaScript và cấu trúc responsive.
- [x] Kiểm tra request danh sách không còn gửi `BusinessType`.
- [x] Chạy bộ test DigitalSales Review.
- [x] Build các project liên quan, không publish/deploy.

### Ghi chú
- Thay đổi chỉ áp dụng cho search rà soát DigitalSales; không khôi phục hai tab cũ.
- Kết quả: DigitalSales Review 38/38 PASS; Core.Cate, Modules.Cate và WebApp build thành công.
- Đã xác thực cấu trúc/style theo code và ảnh tham chiếu bằng `ui-visual-validator`; chưa chụp ảnh runtime sau sửa vì URL local chuyển tới trang đăng nhập khi không có phiên xác thực.
- Không publish source và không deploy.

---

# 2026-09-15 Vấn đề: Không chọn được thành viên/loại chi phí và modal sản phẩm không cuộn

## 1. Mô tả vấn đề
- Trong modal thêm/cập nhật sản phẩm / dịch vụ số của DigitalSales, người dùng không chọn được thành viên, không chọn được loại chi phí và không cuộn xuống được vùng thêm doanh thu.

## 2. Phân tích ban đầu
- Bối cảnh: lỗi nằm trong form sản phẩm tải động qua `_ProductModal.cshtml`, `_ProductForm.cshtml` và JavaScript của `DigitalSales/Detail`.
- Mục tiêu: các dropdown trong bảng chi tiết chọn được dữ liệu, modal cuộn được đến toàn bộ nội dung và footer lưu luôn sử dụng được.
- Dữ liệu nguồn: DB Demo trả về 2 loại chi phí và 802 nhân viên từ các stored procedure hiện hành, nên danh mục không rỗng ở tầng DB.
- Nguyên nhân UI thứ nhất: dropdown sản phẩm và vai trò được khởi tạo Select2, nhưng dropdown thành viên và loại chi phí chỉ là `<select>` thường, không có class/init tương ứng; trong bảng chúng bị co hẹp và hoạt động không thống nhất.
- Nguyên nhân UI thứ hai: `<form>` đang nằm giữa `.modal-content` và `.modal-body`. Cấu trúc này phá chuỗi flex/overflow mà `modal-dialog-scrollable` của Bootstrap yêu cầu, làm phần dưới bị cắt và không tạo vùng cuộn đúng.
- Rủi ro: khởi tạo Select2 cả trong partial và lần nữa trong callback mở modal có thể gây khởi tạo trùng; dropdown nạp động cần đặt đúng `dropdownParent` để không nằm sau modal hoặc bị cắt.
- Phương án sơ bộ: đưa form thành phần tử `.modal-content`, giữ header/body/footer là con trực tiếp; chuẩn hóa thành viên, vai trò và loại chi phí bằng Select2 có `dropdownParent`; gom việc khởi tạo sau sự kiện modal hiển thị và xử lý riêng các dòng thêm động.
- Ràng buộc kế thừa: chưa đồng bộ WebApp/`publish_source`, chưa publish/deploy nếu người dùng không thay đổi quyết định.

## 3. Câu hỏi làm rõ
1. Có chuyển cả Thành viên, Vai trò và Loại chi phí sang Select2 có tìm kiếm, cùng cách hiển thị với dropdown Sản phẩm không?
2. Modal có giữ header và footer cố định, chỉ cuộn phần nội dung ở giữa không?
3. Có giữ kích thước modal `modal-xl` như hiện tại không?
4. Khi thêm một dòng Thành viên/Chi phí/Doanh thu, có cần tự cuộn dòng mới vào vùng nhìn thấy không?
5. Có tiếp tục không đồng bộ WebApp/`publish_source` và không publish/deploy không?

## 4. Câu trả lời & Quyết định
- Chuyển Thành viên, Vai trò và Loại chi phí sang Select2 có tìm kiếm.
- Giữ header/footer cố định và chỉ cuộn nội dung modal ở giữa.
- Giảm kích thước modal từ `modal-xl` xuống `modal-lg`.
- Tự cuộn dòng mới vào vùng nhìn thấy sau khi thêm Thành viên/Chi phí/Doanh thu.
- Không đồng bộ WebApp/`publish_source`, không publish/deploy.

## 5. Checklist

### Chuẩn bị
- [x] Kiểm tra cấu trúc modal, partial form và luồng khởi tạo JavaScript.
- [x] Xác nhận nguồn DB có dữ liệu loại chi phí và nhân viên.
- [x] Đối chiếu cách dùng Select2/dropdown động trong các chức năng hiện có.

### Thực hiện
- [x] Đưa form thành `.modal-content`, giữ header/body/footer đúng cấu trúc scrollable và giảm modal xuống `modal-lg`.
- [x] Chuyển Thành viên, Vai trò và Loại chi phí sang Select2 có tìm kiếm, đúng `dropdownParent`.
- [x] Ngăn khởi tạo Select2 trùng bằng cách khởi tạo form một lần sau sự kiện `shown.bs.modal`.
- [x] Tự cuộn dòng mới vào vùng nhìn thấy sau khi thêm.

### Kiểm tra / Nghiệm thu
- [x] Xác nhận bằng code và test rằng Thành viên, Vai trò và Loại chi phí đều được Select2 khởi tạo trong modal.
- [x] Xác nhận cấu trúc Bootstrap có form là `.modal-content`, body là vùng cuộn giữa header/footer cố định.
- [x] Xác nhận dòng thêm động giữ đúng tên collection, được khởi tạo control và gọi tự cuộn.
- [x] Kiểm tra JavaScript của trang và script trong Razor partial đều hợp lệ.
- [x] Chạy bộ test DigitalSales Product Detail đạt 31/31.
- [x] Build riêng `Modules.Cate` thành công, không publish/deploy.

### Ghi chú
- `ui-ux-designer` định hướng giữ phân cấp card hiện tại, thu modal về `lg` và chuẩn hóa tương tác dropdown/dòng động.
- `ui-visual-validator` xác nhận cấu trúc code mới không còn lỗi overflow do form trung gian; chưa có screenshot runtime sau sửa vì không đồng bộ view sang WebApp.
- Target build của project có hậu kỳ copy view sang WebApp; các thay đổi UI mới bị copy ngoài ý muốn đã được hoàn nguyên riêng ở WebApp. `publish_source` không bị tác động.
- Các warning dependency có sẵn vẫn xuất hiện khi build; không có lỗi biên dịch.

---

# 2026-09-15 Vấn đề: Select2 bị render trùng và bỏ thành viên khỏi dịch vụ DigitalSales

## 1. Mô tả vấn đề
- Select2 trong dòng chi tiết sản phẩm bị lỗi UI, hiển thị hai control chồng nhau như ảnh.
- Bỏ phần Thành viên thực hiện khỏi form sản phẩm / dịch vụ vì thành viên sẽ được quản lý từ bên ngoài.

## 2. Phân tích ban đầu
- Bối cảnh: form sản phẩm có template ẩn để thêm dòng động; framework toàn hệ thống tự gọi `_initSelectElement()` trên mọi `<select>` không có class `none-select2`.
- Nguyên nhân Select2: framework khởi tạo cả select trong template ẩn. Khi clone `<tr>`, markup container Select2 đã sinh cũng bị clone; code modal tiếp tục khởi tạo select mới nên xuất hiện hai control chồng nhau.
- Mục tiêu UI: mỗi trường chỉ có đúng một Select2, đủ chiều rộng, dropdown nằm đúng lớp modal và dòng động vẫn hoạt động.
- Phạm vi Thành viên hiện tại trải qua view, controller validation/load, model/Biz/cache, stored procedure, bảng DB và chỉ số `MemberCount` trên card dịch vụ.
- Rủi ro dữ liệu: nếu chỉ xóa UI nhưng vẫn gửi danh sách rỗng vào `RM_DigitalSalesProduct_SaveDetail`, stored procedure hiện tại sẽ soft-delete toàn bộ thành viên dịch vụ đã lưu trước đó.
- Phương án Select2: thêm `none-select2` cho các select do modal quản lý, bảo đảm template ẩn không bị framework khởi tạo; sau khi clone mới gỡ marker và khởi tạo đúng một lần.
- Phương án Thành viên an toàn: bỏ khỏi form/card/controller và không cho thao tác lưu sản phẩm thay đổi dữ liệu thành viên cũ; giữ bảng/model/SP đọc để tương thích, trừ khi người dùng yêu cầu xóa hẳn dữ liệu/cấu trúc.
- Ràng buộc kế thừa: không đồng bộ WebApp/`publish_source`, không publish/deploy.

## 3. Câu hỏi làm rõ
1. “Bỏ phần thành viên” là bỏ khỏi cả form sản phẩm và chỉ số Thành viên trên card danh sách dịch vụ, đúng không?
2. Dữ liệu thành viên riêng theo dịch vụ đã tồn tại có giữ nguyên trong DB nhưng không còn hiển thị/chỉnh sửa, hay soft-delete hết? Khuyến nghị: giữ nguyên để không mất dữ liệu.
3. Có giữ model/bảng/stored procedure thành viên để tương thích và chỉ ngừng load/validate/save từ chức năng sản phẩm không?
4. Với Sản phẩm và Loại chi phí, có áp dụng cơ chế Select2 riêng bằng `none-select2` để loại bỏ hoàn toàn khởi tạo tự động/trùng lặp không?
5. Tiếp tục chỉ sửa source `Modules.Cate`/`Core.Cate` và stored DB khi cần, không đồng bộ WebApp/`publish_source`, không publish/deploy, đúng không?

## 4. Câu trả lời & Quyết định
- Bỏ Thành viên khỏi cả form và chỉ số hiển thị trên card dịch vụ.
- Giữ nguyên dữ liệu thành viên dịch vụ đã tồn tại trong DB, không hiển thị/chỉnh sửa và không soft-delete khi lưu sản phẩm.
- Giữ model, bảng và stored procedure thành viên để tương thích; ngừng load/validate/save thành viên trong chức năng sản phẩm.
- Cô lập Select2 của Sản phẩm và Loại chi phí bằng class `none-select2`.
- Chỉ sửa source liên quan và stored DB khi cần; không đồng bộ WebApp/`publish_source`, không publish/deploy.

## 5. Checklist

### Chuẩn bị
- [x] Đối chiếu ảnh lỗi với DOM/template và luồng khởi tạo Select2 của framework.
- [x] Xác định `_initSelectElement()` tự khởi tạo tất cả select không có `none-select2`.
- [x] Kiểm tra luồng load/validate/save và ảnh hưởng dữ liệu khi bỏ Thành viên.

### Thực hiện
- [x] Gắn `none-select2` cho cả 3 select do modal quản lý, bao gồm select trong template ẩn.
- [x] Bỏ phần Thành viên khỏi form và chỉ số trên card dịch vụ.
- [x] Ngừng load/chuẩn bị dropdown/validate thành viên trong controller sản phẩm.
- [x] Điều chỉnh Biz/stored procedure lưu tổng hợp để không đọc hoặc thay đổi dữ liệu thành viên cũ.
- [x] Cập nhật test hiện có theo nghiệp vụ mới.

### Kiểm tra / Nghiệm thu
- [x] Xác nhận mọi select đều có `none-select2`, template ẩn không bị auto-init và dòng clone chỉ được modal khởi tạo một lần.
- [x] Xác nhận form/card không còn phần Thành viên hoặc `MemberCount`.
- [x] Xác nhận stored procedure thực tế trên DB không còn `UPDATE/INSERT RM_DigitalSalesProductMember`.
- [x] Xác nhận message trạng thái rỗng đã đổi thành “Thêm dịch vụ để theo dõi doanh thu và chi phí.”
- [x] Chạy test DigitalSales Product Detail đạt 35/35 và kiểm tra JavaScript hợp lệ.
- [x] Build `Modules.Cate` thành công với `PostBuildEvent` vô hiệu hóa, không đồng bộ/publish/deploy.

### Ghi chú
- Giữ nguyên model, bảng, stored procedure đọc và đăng ký procedure thành viên để tương thích dữ liệu cũ.
- Tham số `@MembersXml` vẫn được giữ trong procedure lưu để không phá contract gọi cũ, nhưng không còn được xử lý.
- DB Demo hiện không có bản ghi thành viên dịch vụ đang hoạt động; trước/sau migration đều bằng 0 và procedure mới bảo đảm các bản ghi tương lai/cũ không bị thao tác bởi form sản phẩm.
- `ui-ux-designer` định hướng loại bỏ hoàn toàn khối Thành viên khỏi luồng sản phẩm để tránh hai nguồn quản lý trùng nhau.
- `ui-visual-validator` xác nhận nguyên nhân container trùng đã được chặn tại nguồn bằng marker `none-select2`; chưa chụp screenshot runtime vì không đồng bộ sang WebApp.

---

# 2026-09-15 Vấn đề: Sắp xếp lại form và chuẩn hóa date picker/Select2 dịch vụ DigitalSales

## 1. Mô tả vấn đề
- Các trường ngày/thời gian trong form sản phẩm / dịch vụ cần chọn bằng date picker.
- Bố cục phần Thông tin dịch vụ cần gọn theo hàng: Sản phẩm và Gói cước cùng một hàng; Số lượng, Doanh thu dự kiến, Thời hạn bắt đầu và Thời hạn kết thúc cùng một hàng; Ghi chú một hàng riêng.
- Select2 Loại chi phí hiện cao hơn các input còn lại trong cùng dòng và cần được thu gọn cho đồng bộ.

## 2. Phân tích ban đầu
- Form hiện chia Sản phẩm 8 cột + Số lượng 4 cột, Gói cước 8 cột + Doanh thu 4 cột; StartDate, EndDate và textarea Ghi chú nằm chung hàng nên chưa đúng bố cục yêu cầu.
- Các trường StartDate, EndDate, PaymentDate, ReceivedDate và ReceivedTime đã mang class `date-picker`; modal có khởi tạo plugin khi mở và sau khi thêm dòng động. Cần chuẩn hóa lại cách hiển thị/kích hoạt để người dùng chọn ngày trực tiếp và bảo đảm các dòng được thêm mới cũng hoạt động.
- CSS modal đang ép mọi Select2 cao 38px. Quy tắc này phù hợp Select2 Sản phẩm nhưng làm Select2 Loại chi phí trong bảng (`form-control-sm`) cao hơn các input nhỏ cùng hàng.
- Hướng xử lý dự kiến: bố cục responsive 6/6 cho Sản phẩm/Gói cước, 3/3/3/3 cho bốn trường chỉ số và thời hạn, 12/12 cho Ghi chú; thêm biến thể CSS compact riêng cho Select2 chi phí.
- Ràng buộc kế thừa: chỉ sửa source liên quan, không đồng bộ WebApp/`publish_source`, không publish/deploy.

## 3. Câu hỏi làm rõ
1. Trường “Thời gian ghi nhận” của Doanh thu thực thu cần date picker chỉ ngày `dd/MM/yyyy`, hay date-time picker gồm cả giờ/phút `dd/MM/yyyy HH:mm`? Khuyến nghị dùng date-time picker vì tên trường là thời gian ghi nhận.
2. Bố cục desktop xác nhận là: Sản phẩm/Gói cước mỗi trường 6 cột; Số lượng/Doanh thu dự kiến/Bắt đầu/Kết thúc mỗi trường 3 cột; Ghi chú toàn hàng, và tự xuống dòng trên màn hình nhỏ?
3. Có áp dụng picker cho toàn bộ StartDate, EndDate, Ngày chi, Ngày nhận và Thời gian ghi nhận, kể cả các dòng Chi phí/Doanh thu thêm động không?
4. Chỉ thu nhỏ Select2 Loại chi phí theo chiều cao `form-control-sm`, còn Select2 Sản phẩm giữ chiều cao chuẩn hiện tại, đúng không?
5. Tiếp tục không đồng bộ WebApp/`publish_source` và không publish/deploy, đúng không?

## 4. Câu trả lời & Quyết định
- Tất cả trường thời gian dùng date picker định dạng `dd/MM/yyyy`, kể cả “Thời gian ghi nhận”.
- Bố cục desktop: Sản phẩm/Gói cước 6/6; Số lượng/Doanh thu dự kiến/Bắt đầu/Kết thúc 3/3/3/3; Ghi chú toàn hàng và responsive tự xuống dòng trên màn hình nhỏ.
- Áp dụng date picker cho StartDate, EndDate, Ngày chi, Ngày nhận và Thời gian ghi nhận, bao gồm cả dòng thêm động.
- Chỉ thu nhỏ Select2 Loại chi phí; Select2 Sản phẩm giữ chiều cao chuẩn.
- Không đồng bộ WebApp/`publish_source`, không publish/deploy.

## 5. Checklist dự kiến

### Chuẩn bị
- [x] Kiểm tra bố cục partial form hiện tại.
- [x] Kiểm tra luồng khởi tạo date picker cho form ban đầu và dòng động.
- [x] Xác định CSS 38px đang áp dụng chung cho mọi Select2 trong modal.

### Thực hiện
- [x] Sắp xếp lại các trường Thông tin dịch vụ theo bố cục được xác nhận.
- [x] Chuẩn hóa picker cho tất cả trường ngày/thời gian trong phạm vi.
- [x] Tạo kiểu Select2 compact riêng cho Loại chi phí.
- [x] Cập nhật kiểm thử giao diện/cấu trúc liên quan.

### Kiểm tra / Nghiệm thu
- [x] Xác nhận date picker được khởi tạo một lần cho dữ liệu có sẵn và được khởi tạo sau khi thêm dòng động.
- [x] Xác nhận bố cục responsive và Select2 Loại chi phí cao 31px đồng bộ `form-control-sm`.
- [x] Chạy test DigitalSales Product Detail đạt 41/41.
- [x] Build riêng `Modules.Cate` thành công, 0 lỗi, với `PostBuildEvent` bị vô hiệu hóa.

### Ghi chú
- Date picker dùng `dd/mm/yyyy`, ngôn ngữ Việt, bắt đầu tuần vào thứ Hai, có nút hôm nay và tự đóng sau khi chọn.
- Các cảnh báo dependency/reference có sẵn vẫn xuất hiện khi build; không phát sinh lỗi biên dịch.
- `ui-ux-designer` được dùng để tổ chức lại mật độ trường nhập theo các hàng 6/6, 3/3/3/3 và một hàng ghi chú riêng.
- `ui-visual-validator` xác nhận bằng code/test các breakpoint Bootstrap, số lượng trường date picker và chiều cao Select2 compact; không kiểm tra runtime bằng screenshot vì không đồng bộ view sang WebApp.

---

# 2026-09-15 Vấn đề: Date picker và chiều cao Select2 chưa có hiệu lực trên UI runtime

## 1. Mô tả vấn đề
- Sau thay đổi bố cục, UI thực tế vẫn không mở được date picker.
- Select2 Loại chi phí vẫn giữ chiều cao cũ, chưa đồng bộ với input cùng hàng.
- Người dùng yêu cầu xác nhận lại việc kiểm thử sau sửa.

## 2. Phân tích ban đầu
- Liên quan: mục “Sắp xếp lại form và chuẩn hóa date picker/Select2 dịch vụ DigitalSales” cùng ngày.
- Kết quả trước chỉ là test tĩnh trên nội dung source và build module; chưa phải kiểm thử trình duyệt/runtime. Vì vậy kết luận nghiệm thu UI trước đó không đủ bằng chứng và ảnh mới được xác định là trạng thái **KHÔNG ĐẠT** theo `ui-visual-validator`.
- Nguyên nhân chắc chắn của chiều cao Select2: `_ProductModal.css` đã được tạo và nằm trong project nhưng không được `<link>` ở `Detail.cshtml`; trình duyệt không tải file nên rule compact 31px không thể áp dụng.
- Date picker hiện được khởi tạo trong script inline của partial tải qua AJAX. Ảnh runtime cho thấy hiệu ứng plugin không xuất hiện; cần chuyển phần khởi tạo control sang `DigitalSalesDetail.js` là file luôn được trang Detail tải, đồng thời thêm dấu hiệu lịch trực quan và kiểm tra sự kiện mở picker trong trình duyệt.
- Phát hiện phạm vi: bản `Modules.Cate` và WebApp hiện đang giống nhau ở các file Product Form/Modal/CSS, cho thấy lần build vừa rồi vẫn kích hoạt hậu kỳ copy dù lệnh đã cố vô hiệu hóa. Không có thay đổi mới trong `publish_source` từ phần form này.
- Muốn nghiệm thu thật tại `http://crm.git`, cần cho phép cập nhật các file DigitalSales cần thiết ở WebApp hoặc có một cơ chế chạy trực tiếp từ `Modules.Cate`.

## 3. Câu hỏi làm rõ
1. Có cho phép chuyển logic khởi tạo Select2/date picker của form sản phẩm từ script inline AJAX sang `DigitalSalesDetail.js` luôn được trang tải, đồng thời liên kết `_ProductModal.css` trong `Detail.cshtml` không? Khuyến nghị: Có.
2. Các trường ngày có cần hiển thị biểu tượng lịch và mở lịch khi bấm cả biểu tượng lẫn ô nhập không? Khuyến nghị: Có để người dùng nhận biết rõ đây là date picker.
3. Để kiểm thử UI thật trên `http://crm.git`, có cho phép đồng bộ riêng các file DigitalSales sửa lần này sang `CenIT.Solution.TOC.WebApp` nhưng vẫn tuyệt đối không cập nhật `publish_source` và không publish/deploy không?

## 4. Câu trả lời & Quyết định
- Chuyển logic khởi tạo Select2/date picker sang `DigitalSalesDetail.js` và liên kết `_ProductModal.css` trực tiếp từ `Detail.cshtml`.
- Hiển thị biểu tượng lịch; bấm vào biểu tượng hoặc vùng ô nhập đều mở date picker.
- Cho phép đồng bộ riêng các file DigitalSales sửa lần này sang WebApp để kiểm thử trực tiếp trên `http://crm.git`; không cập nhật `publish_source`, không publish/deploy.

## 5. Checklist dự kiến

### Chuẩn bị
- [x] Đối chiếu ảnh runtime với markup và CSS hiện tại.
- [x] Xác nhận `_ProductModal.css` chưa được tải trong trang Detail.
- [x] Xác định test trước chỉ kiểm tra source/build, chưa kiểm tra tương tác trình duyệt.

### Thực hiện
- [x] Liên kết CSS modal sản phẩm từ trang Detail kèm cache-busting.
- [x] Chuyển toàn bộ khởi tạo plugin, dòng động, tổng hợp tài chính và submit AJAX sang JavaScript chính của trang.
- [x] Loại bỏ script inline khỏi partial modal để không còn hai luồng khởi tạo.
- [x] Bổ sung biểu tượng lịch, click handler mở picker và z-index 1080 để popup nằm trên modal.
- [x] Gắn class container compact sau khi Select2 render để rule chiều cao không phụ thuộc selector sibling.
- [x] Đồng bộ đúng các file DigitalSales liên quan sang WebApp; không thay đổi `publish_source`.
- [x] Tạo kiểm thử trình duyệt headless cho modal sản phẩm, không ghi dữ liệu.

### Kiểm tra / Nghiệm thu
- [x] Kiểm tra trực tiếp trên `http://crm.git`: CSS modal được tải và mỗi Select2 chỉ khởi tạo một lần.
- [x] Đo trực tiếp Select2 Loại chi phí cao 31px, bằng đúng ô Số tiền 31px.
- [x] Kiểm tra date picker được gắn cho thời hạn có sẵn, Ngày chi động và cả hai trường ngày của dòng Doanh thu động.
- [x] Kiểm tra popup lịch thực sự hiển thị, kích thước 216x280px và z-index 1080 phía trên modal.
- [x] Kiểm tra biểu tượng lịch hiển thị trên các ô ngày.
- [x] Chạy test DigitalSales Product Detail đạt 43/43 và kiểm tra cú pháp JavaScript thành công.
- [x] Lưu ảnh bằng chứng runtime tại `tests/digital_sales/artifacts/product-modal-runtime.png`.
- [x] Không gửi form trong kiểm thử runtime để tránh thay đổi dữ liệu; luồng submit AJAX được kiểm tra bằng source/test tĩnh.

### Ghi chú
- Nguyên nhân gốc thứ nhất: `_ProductModal.css` không được trang Detail tải nên rule Select2 compact không có hiệu lực.
- Nguyên nhân gốc thứ hai: popup date picker ban đầu bị CSS toàn hệ thống ép z-index 998 nên nằm sau modal; selector đặc hiệu đã được ghi đè thành 1080.
- Kết luận `ui-visual-validator`: **ĐẠT** sau khi có cả số đo DOM runtime và ảnh popup lịch thực tế.
- Không build lại vì thay đổi lần này chỉ gồm Razor view, CSS và JavaScript; kiểm thử trực tiếp tại site đang chạy có giá trị xác nhận cao hơn build C# và tránh kích hoạt hậu kỳ copy ngoài phạm vi.

---

# 2026-09-15 Vấn đề: Chuẩn hóa cột hồ sơ tại ReviewBatchItem/Index theo danh sách DigitalSales

## 1. Mô tả vấn đề
- Đổi tiêu đề cột “Hồ sơ DigitalSales” thành “Hồ sơ KD sản phẩm DVS”.
- Nội dung hồ sơ trong `ReviewBatchItem/Index` hiển thị tương tự cột Cơ hội/Dự án tại `Cate/DigitalSales`.
- Bỏ Loại hình và Trạng thái khỏi phần hiển thị.
- Kiểm tra bộ lọc “Đã rà soát” vẫn giữ nguyên hành vi.

## 2. Phân tích ban đầu
- Tiêu đề cột hiện lấy từ message key `ReviewDigitalSales_Column_Record`; dữ liệu gốc trong `Database/DigitalSalesReview.sql` đang là “Hồ sơ DigitalSales”. Muốn đúng quy tắc hệ thống cần cập nhật message DB và script nguồn, không code cứng trong Razor.
- Bảng rà soát hiện có các cột riêng: STT, Hồ sơ, Loại hình, Khách hàng, Trạng thái, AM, Thông tin rà soát, Thao tác. JavaScript cột Hồ sơ chỉ hiển thị tên dạng link và mã dạng chữ nhỏ.
- Cột Cơ hội/Dự án tại `Cate/DigitalSales` có cấu trúc phong phú hơn: Loại hình + Trạng thái; tên hồ sơ; mã dạng badge; badge Trọng điểm/Quan tâm; danh sách sản phẩm/dịch vụ nếu có. Sau khi bỏ Loại hình/Trạng thái, phạm vi có thể là chỉ tái sử dụng kiểu Tên + Mã hoặc lấy thêm Trọng điểm/Quan tâm/Sản phẩm dịch vụ, việc này ảnh hưởng model và stored procedure.
- Bộ lọc “Đã rà soát” hiện lưu/khôi phục qua localStorage, gửi `IsReviewed` trong AJAX và stored procedure lọc sau khi tính trạng thái rà soát. Việc bỏ cột hiển thị không được thay đổi chuỗi xử lý này.
- Cụm “bỏ Loại hình và Trạng thái” có thể chỉ nói hai cột bảng hoặc bao gồm cả bộ lọc Trạng thái trong vùng tìm kiếm; cần xác nhận để tránh thay đổi hành vi lọc ngoài ý muốn.
- Sau phản hồi về kiểm thử ở hạng mục trước, tiêu chí nghiệm thu lần này phải gồm kiểm tra runtime trên `http://crm.git`, không chỉ test source.

## 3. Câu hỏi làm rõ
1. “Bỏ Loại hình và Trạng thái” là chỉ bỏ hai cột khỏi bảng kết quả, hay bỏ luôn ô lọc Trạng thái ở vùng tìm kiếm? Khuyến nghị: chỉ bỏ hai cột bảng để không làm mất khả năng lọc.
2. Trong cột “Hồ sơ KD sản phẩm DVS”, ngoài Tên và Mã theo style của `Cate/DigitalSales`, có hiển thị thêm badge Trọng điểm/Quan tâm và danh sách Sản phẩm/Dịch vụ số không? Khuyến nghị: hiển thị đầy đủ các nội dung này, chỉ bỏ badge Loại hình/Trạng thái.
3. Tên hồ sơ vẫn là liên kết mở `DigitalSales/Detail` và giữ nguyên quy tắc: chưa rà soát thì kèm `reviewBatchID`, đã rà soát thì chỉ xem chi tiết, đúng không?
4. Có cập nhật message “Hồ sơ KD sản phẩm DVS” vào cả script DB nguồn và DB Demo đang chạy không?
5. Có đồng bộ riêng các file ReviewBatchItem/DigitalSales liên quan sang WebApp để kiểm thử trực tiếp trên `http://crm.git`, nhưng không cập nhật `publish_source` và không deploy không?

## 4. Câu trả lời & Quyết định
- Chỉ bỏ hai cột Loại hình và Trạng thái khỏi bảng; giữ nguyên ô lọc Trạng thái.
- Cột Hồ sơ KD sản phẩm DVS hiển thị tên, mã, trạng thái, badge Trọng điểm/Quan tâm và danh sách Sản phẩm/Dịch vụ số; không hiển thị Loại hình.
- Giữ nguyên quy tắc liên kết tên: hồ sơ chưa rà soát mở Detail kèm `reviewBatchID`, hồ sơ đã rà soát mở Detail để xem.
- Cập nhật message vào script DB nguồn và DB Demo.
- Đồng bộ các file liên quan sang WebApp để kiểm thử runtime; không cập nhật `publish_source`, không publish/deploy.

## 5. Checklist dự kiến

### Chuẩn bị
- [x] Đối chiếu cấu trúc bảng ReviewBatchItem với DataTable DigitalSales.
- [x] Xác định message key và stored procedure cung cấp dữ liệu.
- [x] Kiểm tra đầy đủ chuỗi lọc `IsReviewed` từ UI đến DB.

### Thực hiện
- [x] Cập nhật message tiêu đề theo cơ chế `Sys_Messages`.
- [x] Chuẩn hóa renderer cột hồ sơ theo phạm vi được xác nhận.
- [x] Bỏ đúng các cột Loại hình/Trạng thái và cập nhật ánh xạ sắp xếp DataTable/stored procedure.
- [x] Giữ ô lọc Trạng thái và sửa chuyển đổi `True/False` để hai lựa chọn tình trạng rà soát gửi đúng giá trị boolean.
- [x] Đồng bộ WebApp và cập nhật DB Demo.

### Kiểm tra / Nghiệm thu
- [x] Test tĩnh đạt 50/50 cho tiêu đề, số cột, renderer, dữ liệu DB và tham số `IsReviewed`.
- [x] Kiểm tra runtime trên `http://crm.git`: Chưa rà soát trả 2/2 dòng đúng; Đã rà soát trả 1/1 dòng đúng.
- [x] Kiểm tra liên kết tên/nút Rà soát hoặc Xem chi tiết giữ nguyên quy tắc theo trạng thái rà soát.
- [x] Chụp ảnh tại `tests/digital_sales/artifacts/review-list-runtime.png`; giao diện 6 cột không vỡ bố cục.
- [x] Build `Core.Cate` thành công, 0 lỗi; DLL/PDB được đồng bộ sang WebApp để model runtime nhận trường mới.
- [x] Không cập nhật `publish_source`, không publish/deploy.

### Ghi chú
- Kiểm thử runtime phát hiện Razor sinh giá trị radio `True/False` nhưng JavaScript cũ so sánh cứng với `"true"`, khiến lựa chọn Đã rà soát vẫn gửi `false`. Đã sửa bằng so sánh không phân biệt hoa/thường ở Module và WebApp.
- Kết luận `ui-visual-validator`: **ĐẠT**; tiêu đề, nội dung cột hồ sơ, bộ lọc và bố cục đã được xác nhận trực tiếp trên site local.

---

# 2026-09-15 Vấn đề: YC6 - Bổ sung trạng thái Kết luận rà soát

## 1. Mô tả vấn đề
- Trong chức năng Rà soát, bổ sung trạng thái “Kết luận rà soát báo cáo dự án/cơ hội kinh doanh”.
- Có ba giá trị: Chấp nhận, Quan tâm, Không chấp nhận.
- Yêu cầu hiện tại là phân tích ảnh hưởng đối với chức năng rà soát đang có, chưa triển khai code.

## 2. Phân tích ban đầu
- Bối cảnh hiện tại: chức năng `ReviewBatchItem` đã được chuyển sang rà soát một loại hồ sơ duy nhất là `DigitalSales`; một hồ sơ có thể có nhiều lịch sử rà soát theo đợt và theo cấp người rà soát. Form hiện lưu Nội dung rà soát, cờ Xác nhận rà soát và tệp đính kèm.
- Dữ liệu hiện có: `RM_ReviewHistory.IsConfirmed` và `ReviewAction` chỉ mô tả hành động “Đã xác nhận/Đã cho ý kiến”. Đây không phải kết luận nghiệp vụ Chấp nhận/Quan tâm/Không chấp nhận, vì vậy không nên tái sử dụng hai trường này cho YC6.
- Điểm lưu phù hợp phụ thuộc ý nghĩa nghiệp vụ: nếu mỗi lần/cấp rà soát có kết luận riêng thì trạng thái phải nằm tại `RM_ReviewHistory`; nếu mỗi hồ sơ trong một đợt chỉ có một kết luận cuối cùng thì cần lưu hoặc tổng hợp tại `RM_ReviewBatchItem`. Với dữ liệu nhiều cấp hiện tại, chỉ thêm một trường mà không quy định kết luận nào có hiệu lực sẽ làm báo cáo không xác định.
- Phạm vi kỹ thuật có thể bị ảnh hưởng: model form và history, partial `_ReviewForm`, action tạo/sửa lịch sử, Biz/Cache, stored procedure lưu/lấy lịch sử, danh sách rà soát, tab Lịch sử rà soát, báo cáo `ReviewReport`, xuất Excel, message `Sys_Messages`, migration/script DB và kiểm thử.
- Phát hiện liên quan: báo cáo `ReviewReport` và stored procedure `RM_Review_Report_Get` hiện vẫn đọc mô hình cũ gồm Cơ hội/Dự án qua `ObjectType=1/2`, trong khi luồng rà soát hiện tại đã dùng DigitalSales với `ObjectType IS NULL`. Nếu YC6 phải xuất hiện trong báo cáo hiện hành thì cần đồng thời xác nhận có chuyển báo cáo sang DigitalSales hay vẫn duy trì báo cáo dữ liệu cũ.
- Dữ liệu Demo hiện có 15 lịch sử rà soát: 14 bản ghi xác nhận và 1 bản ghi cho ý kiến. Khi bổ sung trường mới cần cho phép `NULL` đối với dữ liệu cũ hoặc có quy tắc chuyển đổi; không thể suy ra ba kết luận mới từ `IsConfirmed` một cách đáng tin cậy.
- Rủi ro nghiệp vụ: “Quan tâm” có thể chỉ là kết luận của lần rà soát hoặc có thể phải tác động đến cờ Quan tâm (`IsFollowed`) của DigitalSales. Tự động liên kết hai khái niệm khi chưa xác nhận có thể làm thay đổi danh sách theo dõi ngoài ý muốn.
- Phương án sơ bộ được khuyến nghị: thêm `ReviewConclusion` kiểu `TINYINT NULL` trên từng `RM_ReviewHistory` (1 Chấp nhận, 2 Quan tâm, 3 Không chấp nhận), giữ nguyên `IsConfirmed`; hiển thị kết luận trên form và lịch sử. Kết luận tổng hợp của hồ sơ/đợt được xác định theo quy tắc cấp rà soát do nghiệp vụ chốt, sau đó mới đưa vào danh sách/báo cáo.

## 3. Câu hỏi làm rõ
1. YC6 áp dụng cho chức năng rà soát DigitalSales hiện tại, hay vẫn phải áp dụng cho báo cáo Cơ hội/Dự án cũ? Khuyến nghị: áp dụng cho DigitalSales và chuyển báo cáo rà soát sang cùng nguồn DigitalSales để tránh hai mô hình lệch nhau.
2. Mỗi lần/cấp rà soát có một kết luận riêng, hay mỗi hồ sơ trong một đợt chỉ có duy nhất một kết luận cuối cùng? Khuyến nghị: lưu kết luận theo từng lịch sử rà soát để bảo toàn dấu vết.
3. Kết luận có bắt buộc khi bấm Lưu/Lưu và tiếp tục không? Khuyến nghị: bắt buộc khi chọn “Xác nhận rà soát”; nếu chỉ “Cho ý kiến” thì được để trống.
4. Có giữ nguyên checkbox “Xác nhận rà soát” và bổ sung trường Kết luận riêng không? Khuyến nghị: giữ nguyên vì hai trường có ý nghĩa khác nhau.
5. Kết luận cần hiển thị ở đâu: form, tab lịch sử, cột Thông tin rà soát, báo cáo web và file Excel? Khuyến nghị: hiển thị đầy đủ ở cả năm vị trí; chỉ thêm bộ lọc kết luận nếu nghiệp vụ cần tra cứu.
6. Khi một hồ sơ có nhiều kết luận theo nhiều cấp/lần rà soát, kết luận tổng hợp dùng bản ghi nào: cấp có thẩm quyền cao nhất, lần xác nhận mới nhất hay kết luận mới nhất bất kể cấp? Khuyến nghị: cấp có thẩm quyền cao nhất đã xác nhận, nếu cùng cấp thì lấy lần mới nhất.
7. Giá trị “Quan tâm” chỉ là kết luận trong đợt rà soát hay phải đồng thời bật cờ Quan tâm (`IsFollowed`) trên DigitalSales? Khuyến nghị: chỉ lưu kết luận, không tự thay đổi cờ hồ sơ.

## 4. Câu trả lời & Quyết định
- Chọn phương án B: tạo bảng liên kết riêng `RM_DigitalSalesProductContract(SalesProductID, ContractID)` để giữ nguyên quan hệ `RM_Contracts.ProductProjectID`.
- Các quyết định còn lại giữ nguyên: `TotalAmount` sau VAT, clone đầy đủ CRUD/file/nhắc hạn/trạng thái, tự lấy khách hàng DigitalSales, cập nhật doanh thu ngay, chỉ ẩn Số lượng và đồng bộ DB/runtime.
- YC6 chỉ áp dụng cho chức năng rà soát DigitalSales hiện tại; báo cáo rà soát cần chuyển sang cùng nguồn DigitalSales.
- Mỗi DigitalSales trong một đợt chỉ có một kết luận cuối cùng có hiệu lực. Các lần rà soát vẫn lưu giá trị đã chọn trong lịch sử để bảo toàn dấu vết; kết luận cuối được tổng hợp, không ghi đè mất lịch sử.
- Kết luận bắt buộc khi chọn “Xác nhận rà soát”; nếu chỉ “Cho ý kiến” thì được để trống.
- Giữ nguyên checkbox `IsConfirmed`; bổ sung Kết luận rà soát thành trường nghiệp vụ riêng.
- Hiển thị kết luận tại form, tab lịch sử, cột Thông tin rà soát, báo cáo web và file Excel.
- Nếu có nhiều kết luận đã xác nhận, lấy kết luận của cấp có thẩm quyền cao nhất; cùng cấp lấy bản ghi mới nhất. Theo cơ chế cấp hiện tại, số cấp nhỏ hơn có thẩm quyền cao hơn (`2` cao hơn `3`, `3` cao hơn `4`).
- “Quan tâm” chỉ là kết luận rà soát, không tự động bật hoặc thay đổi `IsFollowed` của DigitalSales.
- Dữ liệu lịch sử cũ giữ kết luận `NULL`; không suy diễn từ `IsConfirmed`.

## 5. Checklist

### Chuẩn bị
- [x] [Bắt buộc] Xác định mã cố định `1 = Chấp nhận`, `2 = Quan tâm`, `3 = Không chấp nhận` và bổ sung message `Sys_Messages` cho nhãn, lựa chọn, validation và nội dung báo cáo.
- [x] [Bắt buộc] Đối chiếu toàn bộ stored procedure tạo/sửa/đọc lịch sử và báo cáo để không bỏ sót luồng cập nhật kết luận.
- [x] [Bắt buộc] Thiết lập dữ liệu cũ có `ReviewConclusion = NULL`, không chuyển đổi tự động từ `IsConfirmed`.

### Thực hiện
- [x] [Bắt buộc] Bổ sung cột nullable `ReviewConclusion TINYINT` vào `RM_ReviewHistory` bằng script DB có thể chạy lặp an toàn và kiểm tra miền giá trị 1–3.
- [x] [Bắt buộc] Bổ sung `ReviewConclusion` vào model form/history/report; dùng display name từ `Sys_Messages`, không code cứng tiêu đề form.
- [x] [Bắt buộc] Bổ sung trường chọn Kết luận rà soát trong `_ReviewForm`, dùng chung cho tạo mới và chỉnh sửa lịch sử.
- [x] [Bắt buộc] Validate tại controller: khi `IsConfirmed = true`, chỉ chấp nhận kết luận 1–3; khi `IsConfirmed = false`, cho phép `NULL`.
- [x] [Bắt buộc] Cập nhật Biz và stored procedure lưu mới/sửa lịch sử để ghi `ReviewConclusion`; giữ nguyên ý nghĩa của `IsConfirmed` và `ReviewAction`.
- [x] [Bắt buộc] Cập nhật stored procedure lấy lịch sử để trả kết luận của từng lần rà soát.
- [x] [Bắt buộc] Tổng hợp một kết luận cuối cho mỗi DigitalSales trong từng đợt từ các lịch sử đã xác nhận: ưu tiên `ReviewLevel` nhỏ nhất, cùng cấp ưu tiên `CreatedDate/ReviewHistoryID` mới nhất.
- [x] [Bắt buộc] Hiển thị kết luận cuối tại cột Thông tin rà soát và hiển thị kết luận từng lần trong tab Lịch sử rà soát.
- [x] [Bắt buộc] Chuyển `ReviewReport` và `RM_Review_Report_Get` từ nguồn Cơ hội/Dự án cũ sang DigitalSales, bổ sung kết luận cuối trên báo cáo web.
- [x] [Bắt buộc] Bổ sung kết luận cuối vào file Excel báo cáo rà soát.
- [x] [Bắt buộc] Giữ “Quan tâm” độc lập, không cập nhật `RM_DigitalSalesFollow`/`IsFollowed`.
- [x] [Bắt buộc] Đồng bộ các file runtime tương ứng giữa Module, WebApp và `publish_source`; cập nhật script DB nguồn và DB Demo.

### Kiểm tra / Nghiệm thu
- [x] Kiểm tra runtime không cho lưu xác nhận khi chưa chọn kết luận; constraint DB từ chối mã ngoài 1–3.
- [x] Kiểm tra stored procedure trong transaction vẫn cho lưu “Cho ý kiến” với kết luận trống.
- [x] Kiểm tra hai nút Lưu/Lưu và tiếp tục dùng chung form và truyền `ReviewConclusion` vào cùng action lưu.
- [x] Kiểm tra chỉnh sửa lịch sử cập nhật trường kết luận và kết luận tổng hợp được truy vấn động, không lưu bản sao.
- [x] Kiểm tra DB trong transaction: cấp 2 thắng cấp 3/4 và bản ghi mới nhất thắng khi cùng cấp.
- [x] Kiểm tra 15 lịch sử cũ giữ kết luận `NULL`; danh sách và báo cáo không lỗi.
- [x] Kiểm tra form, lịch sử, danh sách, báo cáo web và Excel có đầy đủ ba nhãn kết luận.
- [x] Kiểm tra code và DB không cập nhật cờ Quan tâm của DigitalSales khi kết luận là “Quan tâm”.
- [x] Build `Core.Cate` và `Modules.Cate` thành công 0 lỗi; test tĩnh đạt 63/63 và test runtime đạt 10/10.

### Ghi chú
- Kết luận cuối nên được truy vấn/tổng hợp từ lịch sử thay vì lưu thêm một bản sao tại `RM_ReviewBatchItem`; cách này tránh lệch dữ liệu khi người dùng sửa lịch sử.
- Việc chuyển báo cáo cũ sang DigitalSales là một phần của YC6 vì người dùng xác nhận YC6 chỉ áp dụng cho DigitalSales nhưng yêu cầu kết luận phải xuất hiện trong báo cáo web và Excel.
- Script `Database/DigitalSalesReview.sql` đã chạy thành công 15 batch trên DB Demo; có cột và check constraint hợp lệ, đầy đủ message kết luận.
- Kiểm thử ghi DB dùng transaction và rollback; không để lại bản ghi rà soát thử. Kết quả: thiếu kết luận khi xác nhận trả `-1`, dữ liệu hợp lệ trả ID, “Cho ý kiến” không kết luận vẫn trả ID.
- Kiểm thử runtime không gửi dữ liệu hợp lệ để tránh thay đổi hồ sơ thật; đã xác nhận validation inline, ba lựa chọn, báo cáo DigitalSales và không có lỗi JavaScript console.
- Ảnh kiểm thử runtime: `tests/digital_sales/artifacts/review-conclusion-runtime.png`.
- Kết luận `ui-visual-validator`: **ĐẠT**; validation nằm trọn trong viewport (879–899px), footer vẫn truy cập được (925–992px), modal body cuộn đúng khi nội dung cao hơn vùng hiển thị và không vỡ bố cục.

---

# 2026-09-15 Vấn đề: Tối giản CKEditor và chuyển Kết luận rà soát sang radio

## 1. Mô tả vấn đề
- Thanh công cụ CKEditor tại form rà soát đang hiển thị quá nhiều chức năng, chiếm nhiều diện tích của panel bên phải.
- Trường Kết luận rà soát hiện là danh sách chọn; yêu cầu chuyển thành nhóm radio.

## 2. Phân tích ban đầu
- Bối cảnh: thay đổi nằm tại partial dùng chung `_ReviewForm.cshtml` của `ReviewBatchItem`, nên sẽ tác động đồng thời form thêm mới và chỉnh sửa lịch sử rà soát.
- Mục tiêu: giảm chiều cao toolbar để ưu tiên vùng nhập nội dung và làm ba kết luận nhìn thấy/chọn trực tiếp mà không cần mở dropdown.
- Phạm vi: chỉ thay cấu hình toolbar CKEditor và cách render trường `ReviewConclusion`; giữ nguyên mã `1 = Chấp nhận`, `2 = Quan tâm`, `3 = Không chấp nhận`, model, controller validation và DB.
- Ràng buộc: nhãn phải tiếp tục lấy từ model/`Sys_Messages`; radio phải bind đúng `ReviewConclusion`; khi xác nhận rà soát mà chưa chọn, validation inline hiện tại vẫn phải hoạt động.
- Hiện trạng kỹ thuật: CKEditor đang không khai báo `toolbar`, vì vậy dùng toàn bộ công cụ mặc định và bung thành nhiều hàng. Trong source đã có cấu hình gọn gồm Đậm/Nghiêng/Gạch chân, danh sách, ảnh/bảng/liên kết, định dạng và phóng to có thể tái sử dụng.
- Rủi ro / giả định: đặt sẵn một kết luận mặc định có thể khiến người dùng lưu nhầm; vô hiệu hóa radio theo checkbox `IsConfirmed` có thể làm thay đổi quy tắc đã chốt là “Cho ý kiến” được phép có hoặc không có kết luận.
- Phương án sơ bộ: khai báo toolbar tối giản một hàng hoặc tự xuống tối đa hai hàng; hiển thị ba radio nằm ngang, cho phép xuống dòng ở panel hẹp và không chọn mặc định.

## 3. Câu hỏi làm rõ
1. Bộ công cụ CKEditor có giữ theo cấu hình gọn đề xuất: Đậm, Nghiêng, Gạch chân, danh sách số/chấm, ảnh, bảng, liên kết, định dạng đoạn và phóng to không? Hay chỉ giữ Đậm, Nghiêng, Gạch chân và danh sách?
2. Ba radio hiển thị nằm ngang trên một hàng và tự xuống dòng khi panel hẹp, đúng không?
3. Có dùng màu nhận diện cho nhãn: Chấp nhận màu xanh, Quan tâm màu cam, Không chấp nhận màu đỏ không? Khuyến nghị: có, nhưng giữ radio theo style sẵn của hệ thống.
4. Giữ không chọn sẵn kết luận để người rà soát phải chọn rõ ràng khi `Xác nhận rà soát`, đúng không?
5. Khi bỏ chọn `Xác nhận rà soát`, radio vẫn được phép chọn và không bị tự xóa, đúng theo quy tắc đã chốt trước đó phải không?

## 4. Câu trả lời & Quyết định
- Giữ các công cụ CKEditor thường dùng; chỉ bỏ các công cụ ít sử dụng.
- Hiển thị ba radio nằm ngang và tự xuống dòng khi panel hẹp.
- Dùng màu nhận diện: Chấp nhận xanh, Quan tâm cam, Không chấp nhận đỏ.
- Form rà soát mới mặc định chọn Chấp nhận.
- Khi bỏ chọn Xác nhận rà soát, giữ nguyên kết luận đã chọn.

## 5. Checklist

### Chuẩn bị
- [x] Chốt bộ nút CKEditor cần giữ và cách trình bày radio.

### Thực hiện
- [x] Cấu hình toolbar CKEditor gọn cho cả form thêm mới và chỉnh sửa.
- [x] Chuyển `ReviewConclusion` từ dropdown sang ba `RadioButtonFor`, giữ nhãn từ `Sys_Messages`.
- [x] Bổ sung style responsive và màu nhận diện theo quyết định được xác nhận.
- [x] Đồng bộ đúng các bản source/runtime theo phạm vi triển khai hiện hành.

### Kiểm tra / Nghiệm thu
- [x] Kiểm tra tĩnh toolbar chỉ còn các nhóm công cụ thường dùng; bộ test DigitalSales Review đạt 65/65.
- [x] Kiểm tra source radio bind cùng thuộc tính `ReviewConclusion` cho thêm mới/sửa và ba bản view đồng nhất SHA-256.
- [x] Giữ nguyên validation controller khi xác nhận nhưng kết luận không hợp lệ hoặc để trống.
- [ ] Kiểm tra panel hẹp không vỡ bố cục, footer và vùng nhập nội dung vẫn truy cập được.

### Ghi chú
- Không thay đổi schema DB hoặc mã ba kết luận.
- Build `Modules.Cate` hoàn tất, 0 lỗi biên dịch; còn các warning dependency đã tồn tại của solution. Lần build đầu thiếu `SolutionDir` làm post-build lỗi đường dẫn, đã chạy lại với `SolutionDir` chính xác và thành công.
- Chưa kiểm thử trực quan bằng trình duyệt trong lượt này; cần xác nhận runtime để tick tiêu chí responsive cuối cùng.

---

# 2026-09-15 Vấn đề: Hoàn thiện báo cáo rà soát theo DigitalSales

## 1. Mô tả vấn đề
- Tiếp tục cập nhật chức năng `ReviewReport` để báo cáo rà soát hoàn toàn theo DigitalSales và phản ánh các thông tin mới đã bổ sung.

## 2. Phân tích ban đầu
- Bối cảnh: báo cáo web và Excel đã được chuyển nguồn cơ bản sang `RM_DigitalSales`, có liên kết chi tiết và cột Kết luận cuối. Tuy nhiên model vẫn còn tên trường di sản `ObjectType/ObjectID/ObjectCode/ObjectName`, giao diện chỉ có bộ lọc Đợt rà soát và ba cột nội dung theo cấp 4/3/2.
- Dữ liệu sẵn có ở danh sách rà soát DigitalSales phong phú hơn báo cáo: loại hình, trạng thái, khách hàng, phòng ban, AM, doanh thu dự kiến, cờ trọng điểm/quan tâm và danh sách sản phẩm/dịch vụ. Stored procedure báo cáo hiện chỉ trả mã, tên, nội dung theo cấp và kết luận cuối.
- Các thông tin mới của nghiệp vụ rà soát gồm: kết luận từng lịch sử, kết luận cuối theo cấp có thẩm quyền cao nhất, trạng thái xác nhận/cho ý kiến và liên kết về `DigitalSales/Detail`. Hiện báo cáo chỉ thể hiện kết luận cuối; mỗi ô cấp chưa thể hiện kết luận riêng của lần rà soát đó.
- Bộ lọc báo cáo hiện chưa đồng bộ với màn hình rà soát: chưa có phòng ban, nhân viên/AM, trạng thái DigitalSales, tình trạng đã rà soát và kết luận cuối. Việc thêm tất cả có thể làm màn hình nặng và cần mở rộng tham số Biz/stored procedure.
- File Excel đang có 6 cột giống bảng web. Nếu bổ sung thông tin DigitalSales hoặc kết luận theo từng cấp, cần chốt cấu trúc cột để web và Excel không lệch nhau.
- Ràng buộc: nhãn phải lấy từ `Sys_Messages`; search card và controls theo `MVC_RULES`; view/JS phải đồng bộ ba nơi; stored procedure và DB Demo phải được cập nhật đồng nhất; không dùng lại `ObjectType` vì nguồn mặc định là DigitalSales.
- Rủi ro: đưa quá nhiều thông tin vào ba ô nội dung cấp rà soát sẽ làm bảng rất rộng; hợp lý hơn là gom nhận diện hồ sơ vào một cột, giữ kết luận cuối riêng và hiển thị kết luận từng cấp ngay trong ô cấp tương ứng.
- Phương án khuyến nghị: đổi model báo cáo sang tên trường DigitalSales rõ nghĩa; cột hồ sơ dùng cấu trúc giống danh sách rà soát (tên, mã, trạng thái, badge, dịch vụ); bổ sung Khách hàng, AM/Phòng ban, Doanh thu dự kiến; trong mỗi cấp hiển thị người/ngày/nội dung/trạng thái/kết luận; đồng bộ Excel và bổ sung bộ lọc nghiệp vụ cần thiết.

## 3. Câu hỏi làm rõ
1. Cột Hồ sơ trên báo cáo có hiển thị giống danh sách rà soát hiện tại: tên, mã, trạng thái, badge Trọng điểm/Quan tâm và sản phẩm/dịch vụ; vẫn bỏ Loại hình, đúng không?
2. Có bổ sung các cột riêng `Khách hàng`, `AM/Phòng ban` và `Doanh thu dự kiến (triệu VNĐ)` không? Khuyến nghị: có để báo cáo đủ thông tin quản trị.
3. Trong từng cột cấp 4/3/2, có hiển thị thêm kết luận riêng của lần rà soát bên cạnh người rà soát, ngày, nội dung và Xác nhận/Cho ý kiến không? Khuyến nghị: có; vẫn giữ cột Kết luận cuối riêng.
4. Bộ lọc cần bổ sung ngoài Đợt rà soát gồm những mục nào: Trạng thái DigitalSales, Phòng ban, AM, tình trạng Chưa/Đã rà soát và Kết luận cuối? Khuyến nghị: bổ sung đủ năm mục, không thêm Loại hình.
5. File Excel phải có cùng dữ liệu/cột và áp dụng đúng các bộ lọc đang chọn trên màn hình web, đúng không?
6. Có đổi toàn bộ tên trường model/backend di sản `ObjectID/ObjectCode/ObjectName` thành `DigitalSalesID/Code/Title` để code đúng nghiệp vụ mới không? Khuyến nghị: có, đồng thời loại bỏ `ObjectType/ObjectTypeName` khỏi báo cáo.
7. Phạm vi triển khai lần này có cập nhật script DB, chạy DB Demo, đồng bộ WebApp và `publish_source`, build và test runtime nhưng không deploy không?

## 4. Câu trả lời & Quyết định
- Hiển thị Kết luận cuối và tổng số lượt tại header mỗi đợt.
- Giữ đầy đủ người rà soát, thời gian, cấp, Xác nhận/Cho ý kiến, kết luận, nội dung và tệp cho từng lịch sử.
- Thu gọn nội dung khoảng 2–3 dòng; chỉ render đầy đủ HTML/ảnh khi bấm Xem thêm.
- Giữ quyền sửa hiện tại; hiển thị nút icon nhỏ có tooltip.
- Đồng bộ Module, WebApp và `publish_source`, build/test nhưng không deploy.

## 5. Checklist

### Chuẩn bị
- [ ] Chốt cấu trúc cột báo cáo, bộ lọc và phạm vi Excel.
- [ ] Đối chiếu quyền dữ liệu báo cáo với quyền xem DigitalSales hiện tại.

### Thực hiện
- [ ] Chuẩn hóa model/Biz/stored procedure báo cáo sang DigitalSales.
- [ ] Bổ sung dữ liệu hồ sơ và kết luận từng cấp theo phạm vi đã chốt.
- [ ] Cập nhật bộ lọc và DataTable báo cáo theo `MVC_RULES`.
- [ ] Cập nhật xuất Excel đồng nhất với báo cáo web và bộ lọc.
- [ ] Bổ sung message DB, cập nhật script nguồn và DB Demo.
- [ ] Đồng bộ các view liên quan giữa Module, WebApp và `publish_source`.

### Kiểm tra / Nghiệm thu
- [ ] Build Core/Module không có lỗi mới.
- [ ] Kiểm tra stored procedure phân trang, tìm kiếm, sắp xếp và lọc đúng.
- [ ] Kiểm tra báo cáo web và Excel trả cùng phạm vi dữ liệu.
- [ ] Kiểm tra hồ sơ cũ chưa có kết luận không gây lỗi.
- [ ] Kiểm tra runtime không lỗi JavaScript và bảng responsive không vỡ bố cục.

### Ghi chú
- Hủy phạm vi theo yêu cầu người dùng ngày 2026-09-15: không tiếp tục sửa `ReviewReport`; chuyển sang cập nhật tab lịch sử rà soát.

---

# 2026-09-15 Vấn đề: Cập nhật UI tab Lịch sử rà soát DigitalSales

## 1. Mô tả vấn đề
- Không tiếp tục chỉnh sửa báo cáo rà soát.
- Cập nhật `tab-review-history` theo các thông tin rà soát mới bổ sung và tổ chức lại UI hợp lý hơn.

## 2. Phân tích ban đầu
- Bối cảnh: tab `tab-review-history` trong `DigitalSales/Detail` dùng partial chung `ReviewBatchItem/_ReviewHistory.cshtml`, nhóm các lịch sử theo đợt rà soát và hiển thị dạng timeline.
- Thông tin mới `ReviewConclusion` đã được đưa vào từng lịch sử nhưng hiện đang ghép chung trên một dòng dài với thời gian, cấp, người rà soát và trạng thái; trên khung hẹp dễ rối và có nguy cơ chồng nút chỉnh sửa.
- Mỗi đợt có thể có nhiều lịch sử/cấp rà soát. Dữ liệu hiện tại đủ để tính và hiển thị “Kết luận cuối” ngay ở phần đầu đợt theo quy tắc đã chốt: cấp có thẩm quyền cao nhất đã xác nhận, cùng cấp lấy bản ghi mới nhất.
- Phần nội dung hiện có bất nhất: cắt preview ở 150 ký tự nhưng chỉ hiện nút mở rộng từ 100 ký tự; nếu có ảnh thì render toàn bộ HTML vào `preview-image`, sau đó lại có bản nội dung đầy đủ ẩn, dễ lặp chữ và chiếm diện tích.
- Tệp đính kèm đang hiển thị dạng badge và nút sửa nằm tuyệt đối góc phải. Cần tách metadata, nội dung, tệp và hành động thành các vùng rõ ràng để quét nhanh.
- Trạng thái rỗng đã có nhưng còn đơn giản. Nên giữ phong cách Ace Admin hiện tại, không tạo design system/màu riêng; sử dụng các class success/warning/danger sẵn có cho kết luận.
- Phạm vi dự kiến chỉ gồm partial lịch sử, CSS và nếu cần JavaScript mở rộng/thu gọn; không sửa `ReviewReport`, schema DB hoặc stored procedure vì dữ liệu cần thiết đã có.
- Phương án khuyến nghị: mỗi đợt là một khối có tiêu đề, số lượt và badge Kết luận cuối; bên trong là các card theo thời gian với hàng đầu gồm avatar/icon, người rà soát và thời gian, hàng badge riêng cho cấp/trạng thái/kết luận, nội dung xem trước gọn, tệp đính kèm và nút sửa không chồng lấn.

## 3. Câu hỏi làm rõ
1. Phần đầu mỗi đợt có hiển thị `Kết luận cuối` và tổng số lượt rà soát không? Khuyến nghị: có.
2. Mỗi lịch sử có giữ đủ: người rà soát, thời gian, cấp rà soát, Xác nhận/Cho ý kiến, kết luận, nội dung và tệp đính kèm không? Khuyến nghị: có.
3. Nội dung rà soát hiển thị bản rút gọn khoảng 2–3 dòng, bấm “Xem thêm” mới render đầy đủ HTML/ảnh để tab gọn hơn, đúng không?
4. Giữ nút chỉnh sửa cho lịch sử do chính người dùng tạo, nhưng chuyển thành nút icon nhỏ ở góc phải card và có tooltip, đúng không?
5. Có đồng bộ Module, WebApp và `publish_source`, build/test runtime nhưng không publish/deploy không?

## 4. Câu trả lời & Quyết định
- Chờ người dùng xác nhận.

## 5. Checklist

### Chuẩn bị
- [x] Chốt nội dung phần tổng hợp đợt và cách thu gọn nội dung lịch sử.

### Thực hiện
- [x] Tổ chức lại header từng đợt và hiển thị kết luận cuối theo đúng quy tắc nghiệp vụ.
- [x] Tách metadata, trạng thái, kết luận, nội dung và tệp thành các vùng rõ ràng trong card lịch sử.
- [x] Sửa cơ chế xem trước/xem thêm để không render trùng nội dung hoặc ảnh.
- [x] Điều chỉnh nút sửa và trạng thái rỗng theo UI Ace Admin hiện tại.
- [x] Đồng bộ partial/CSS/JS theo phạm vi được xác nhận.

### Kiểm tra / Nghiệm thu
- [x] Kiểm tra logic kết luận cuối ưu tiên cấp nhỏ nhất, cùng cấp ưu tiên thời gian/ID mới nhất.
- [x] Kiểm tra nhánh lịch sử không có kết luận vẫn render card mà không yêu cầu badge kết luận.
- [x] Kiểm tra source nội dung chỉ có một khối HTML đầy đủ ẩn và không còn `preview-image` gây render lặp.
- [x] Kiểm tra nút sửa vẫn phụ thuộc `CanEdit`, nằm trong header flex và có tooltip.
- [x] Kiểm tra bộ đếm badge sau reload dùng selector `.review-history-entry`; test tĩnh DigitalSales Review đạt 68/68.

### Ghi chú
- Không sửa chức năng `ReviewReport` trong phạm vi này.
- Build `Modules.Cate` thành công, 0 lỗi biên dịch; các warning dependency là hiện trạng cũ của solution.
- Module, WebApp và `publish_source` đồng nhất SHA-256, giữ UTF-8 BOM.
- `ui-visual-validator`: chưa thể kết luận ĐẠT trực quan vì môi trường kiểm thử tự động chưa có biến đăng nhập `CRM_TEST_USER/CRM_TEST_PASSWORD`; đã kiểm tra code-level về overflow, text truncate, flex-wrap, ảnh/table responsive, trạng thái data/empty và không hard-code màu mới.
- Không publish/deploy.

---

# 2026-09-16 Vấn đề: Rà soát tự mở làm co/vỡ giao diện DigitalSales Detail

## 1. Mô tả vấn đề
- Tại `Cate/DigitalSales/Detail/67?reviewBatchID=24`, panel rà soát tự mở khi vào chi tiết.
- Cách bố trí hiện tại đưa panel vào cùng CSS Grid với vùng chi tiết, làm vùng chi tiết bị thu hẹp và vỡ bố cục.
- Cần vẫn tự mở rà soát, không bắt người dùng bấm thêm nút, nhưng không làm co vùng chi tiết.

## 2. Phân tích ban đầu
- Bối cảnh: `Detail.cshtml` tạo `#reviewSplitView` gồm `.review-detail-pane` và `#reviewFormPane`; khi có `reviewBatchID`, script tự kích hoạt `reviewBatchAutoTrigger` sau 300ms.
- Nguyên nhân layout: ở màn hình từ 992px, `.review-split-active` chia grid thành `minmax(0, 1fr)` và `minmax(360px, 34%)`. Panel rà soát vì vậy chiếm cố định khoảng 34% chiều rộng, buộc các card/nhóm tab chi tiết vốn có chiều rộng tối thiểu bị ép lại. CSS panel còn đặt dialog `position: static` trong grid nên không còn là lớp phủ độc lập.
- Mục tiêu: giữ chi tiết ở chiều rộng tự nhiên, panel rà soát tự mở và có vùng cuộn riêng; người dùng vẫn có thể thu gọn panel để xem toàn bộ chi tiết nếu cần.
- Phạm vi: CSS layout của panel/grid, trạng thái mở tự động và breakpoint; giữ nguyên action Lưu/Lưu và tiếp tục, nội dung form và dữ liệu rà soát.
- Ràng buộc: không quay lại cơ chế phải bấm nút mở; không để panel vượt viewport; panel phải có footer cố định và body cuộn riêng; màn hình hẹp phải không tạo horizontal overflow.
- Rủi ro / giả định: panel dạng overlay sẽ che một phần bên phải chi tiết khi mở. Đây là đánh đổi cần xác nhận; bù lại vùng chi tiết không bị co/vỡ và người dùng có thể thu gọn panel bằng nút mũi tên hiện có.
- Phương án sơ bộ:
  - Khuyến nghị: chuyển panel sang fixed overlay bên phải trên desktop, dùng chiều rộng giới hạn `clamp`/`min`, giữ `#reviewSplitView` một cột; tự mở khi có `reviewBatchID`, thu gọn thành rail 48px khi cần.
  - Phương án B: giữ grid nhưng chỉ dành một cột nhỏ hơn. Cách này vẫn làm chi tiết co, chỉ giảm mức độ vỡ.
  - Phương án C: mở panel toàn màn hình dạng modal trên mọi kích thước. Không phù hợp vì che hoàn toàn chi tiết và làm mất ngữ cảnh rà soát.

## 3. Câu hỏi làm rõ
1. Có chấp nhận panel rà soát dạng lớp phủ bên phải, che một phần nội dung bên dưới nhưng không làm thay đổi chiều rộng vùng chi tiết không? Khuyến nghị: có.
2. Trên desktop, chiều rộng panel nên theo hướng nào: cố định khoảng 760px như hiện tại, hay responsive khoảng 38–42vw với giới hạn 560–760px? Khuyến nghị: responsive, tối đa 760px.
3. Khi tự mở, panel để trạng thái mở đầy đủ; người dùng vẫn có thể bấm mũi tên để thu gọn thành rail 48px, đúng không?
4. Trên màn hình dưới 992px, panel có chuyển thành overlay gần/toàn màn hình, còn vùng chi tiết phía sau giữ nguyên và không scroll ngang, đúng không?
5. Có giữ nguyên tự động mở khi có `reviewBatchID` và không thêm thao tác mở thủ công không?

## 4. Câu trả lời & Quyết định
- Chờ người dùng xác nhận hướng overlay và kích thước responsive.

## 5. Checklist

### Chuẩn bị
- [ ] Xác nhận phương án overlay và breakpoint.

### Thực hiện
- [ ] Tách panel rà soát khỏi grid chiều rộng nội dung bằng fixed overlay.
- [ ] Giữ tự động mở theo `reviewBatchID` và rail thu gọn hiện có.
- [ ] Chuẩn hóa chiều rộng responsive, z-index, body scroll và footer cố định.
- [ ] Điều chỉnh breakpoint mobile để không phát sinh scroll ngang.
- [ ] Đồng bộ CSS/view/JS giữa Module, WebApp và `publish_source`.

### Kiểm tra / Nghiệm thu
- [ ] Kiểm tra chi tiết không bị giảm chiều rộng khi panel mở.
- [ ] Kiểm tra panel tự mở không cần click và vẫn lưu rà soát bình thường.
- [ ] Kiểm tra thu gọn/mở lại panel không làm thay đổi layout chi tiết.
- [ ] Kiểm tra viewport desktop/tablet/mobile không overflow ngang.
- [ ] Chụp screenshot runtime và kiểm tra không có lỗi console.

### Ghi chú
- Chưa sửa code trước khi người dùng chốt phương án overlay.

---

# 2026-09-16 Vấn đề: Ẩn Chi phí và Doanh thu thực thu trong form sản phẩm DigitalSales

## 1. Mô tả vấn đề
- Trong form cập nhật sản phẩm/dịch vụ số tại `DigitalSales/Detail`, không cần nhập thông tin Chi phí và Doanh thu thực thu nữa.
- Yêu cầu chỉ ẩn giao diện, không xóa model, dữ liệu DB, stored procedure hay logic lưu hiện tại.

## 2. Phân tích ban đầu
- Bối cảnh: `_ProductForm.cshtml` đang render hai card `Chi phí dịch vụ` và `Doanh thu thực thu`, kèm template dòng động và phần `Tổng hợp tài chính` có các chỉ số dựa trên doanh thu/chi phí.
- Ràng buộc dữ liệu: controller vẫn nạp `Costs`/`Revenues` khi sửa và action `SaveProduct` vẫn validate/lưu hai collection. Nếu xóa markup hoặc không gửi các hidden/index hiện có, lần sửa sản phẩm có thể làm mất dữ liệu cũ.
- Phạm vi an toàn: ẩn hai card bằng wrapper/CSS hoặc điều kiện hiển thị giao diện; giữ nguyên model, controller, JavaScript, templates và stored procedure để dữ liệu hiện tại không bị xóa.
- Điểm cần xác định: “không cần thông tin chi phí và doanh thu thực thu” có thể chỉ áp dụng hai bảng nhập liệu, hoặc bao gồm cả các ô Tổng hợp tài chính như Doanh thu thực thu, Tổng chi phí và Lợi nhuận. Doanh thu dự kiến vẫn thuộc thông tin sản phẩm và không nên ẩn nếu chưa có yêu cầu.
- Phương án sơ bộ: mặc định ẩn hai section nhập liệu bằng class UI (`d-none`/class riêng) nhưng vẫn giữ DOM và dữ liệu binding; cân nhắc ẩn các chỉ số tổng hợp phụ thuộc hai section, giữ lại Doanh thu dự kiến nếu cần.

## 3. Câu hỏi làm rõ
1. Có ẩn cả phần Tổng hợp tài chính gồm Doanh thu thực thu, Tổng chi phí và Lợi nhuận không, hay chỉ ẩn hai card nhập liệu Chi phí và Doanh thu thực thu? Khuyến nghị: ẩn cả các chỉ số phụ thuộc dữ liệu đã ẩn, giữ Doanh thu dự kiến.
2. Dữ liệu Chi phí/Doanh thu thực thu đã lưu trước đây vẫn giữ nguyên trong DB và không được xóa khi người dùng sửa/lưu sản phẩm, đúng không? Khuyến nghị: đúng; chỉ ẩn UI và không thay đổi collection khi submit.
3. Có áp dụng việc ẩn này cho cả thêm mới và chỉnh sửa sản phẩm/dịch vụ số trong `DigitalSales/Detail`, đồng thời đồng bộ Module/WebApp/`publish_source`, build/test nhưng không deploy không?

## 4. Câu trả lời & Quyết định
- Ẩn cả card Chi phí, Doanh thu thực thu và Tổng hợp tài chính; giữ lại trường Doanh thu dự kiến trong thông tin sản phẩm.
- Giữ nguyên dữ liệu cũ trong DB và không xóa collection khi sửa/lưu.
- Áp dụng cho thêm mới và chỉnh sửa, đồng bộ ba bản source, build/test nhưng không deploy.

## 5. Checklist

### Chuẩn bị
- [x] Xác định các section/chỉ số cần ẩn và kiểm tra đường đi dữ liệu khi submit.

### Thực hiện
- [x] Ẩn giao diện Chi phí, Doanh thu thực thu và Tổng hợp tài chính theo phạm vi được chốt.
- [x] Giữ nguyên model, hidden fields, controller validation và logic lưu dữ liệu.
- [x] Bảo đảm không xóa collection cũ khi sửa/lưu sản phẩm.
- [x] Đồng bộ view giữa ba bản source.

### Kiểm tra / Nghiệm thu
- [x] Form không còn hiển thị phần đã chốt ẩn (`d-none` + `aria-hidden`).
- [x] Form vẫn giữ trường Doanh thu dự kiến và toàn bộ thông tin sản phẩm chính.
- [x] Markup/hidden fields của sản phẩm cũ vẫn tồn tại để bảo toàn dữ liệu khi submit.
- [x] Không thay đổi schema DB, controller, stored procedure hoặc JavaScript lưu.

### Ghi chú
- Chưa sửa code trước khi chốt có ẩn Tổng hợp tài chính hay không.
- Test tĩnh sản phẩm: 40 pass, 3 fail BOM ở `Core.Cate/Biz/RM_DigitalSalesBiz.cs`, `Modules.Cate/Areas/Cate/Controllers/DigitalSalesController.cs` và `Modules.Cate/App_Data/Modules/Cate_StoredProcedures.xml`; đây là các lỗi tồn tại trước thay đổi view.

---

# 2026-09-16 Vấn đề: Quản lý nhiều hợp đồng theo từng sản phẩm DigitalSales

## 1. Mô tả vấn đề
- Clone tab Hợp đồng của `Cate/ProductProjectOverview/Index` vào `Cate/DigitalSales/Detail/{id}#tab-products`.
- Mỗi sản phẩm/dịch vụ số có thể có nhiều hợp đồng; cho phép thêm mới, xem và chỉnh sửa hợp đồng ngay bên trong từng sản phẩm.
- Form sản phẩm bỏ Số lượng, Chi phí và Lợi nhuận; đổi Doanh thu thực thu thành Doanh thu thực tế; doanh thu thực tế bằng tổng giá trị hợp đồng.

## 2. Phân tích ban đầu
- Bối cảnh: ProductProjectOverview đang dùng `RM_ContractsModel`, `RM_ContractsController`, các stored procedure `RM_Contracts_*` và partial `_Contracts.cshtml`; quan hệ hiện tại của hợp đồng là `ProductProjectID`.
- DigitalSales hiện có sản phẩm `RM_DigitalSalesProductModel` và form `_ProductForm.cshtml`; các collection Costs/Revenues vừa được ẩn giao diện nhưng vẫn còn model/controller/DB để bảo toàn dữ liệu cũ.
- Mỗi sản phẩm DigitalSales cần một khóa liên kết riêng. Không thể dùng trực tiếp `ProductProjectID` vì sản phẩm DigitalSales không phải `RM_ProductProject`; cần mở rộng quan hệ hợp đồng (khuyến nghị thêm nullable `SalesProductID`/`DigitalSalesProductID` vào `RM_Contracts` và stored procedure riêng hoặc mở rộng SP có tham số loại quan hệ).
- Doanh thu thực tế phải tính từ `SUM(RM_Contracts.TotalAmount)` của các hợp đồng còn hiệu lực/không xóa thuộc đúng sản phẩm; cần chốt tổng trước VAT hay tổng sau VAT. Theo tab Hợp đồng hiện tại, “Tổng” là `TotalAmount` sau VAT.
- Clone đầy đủ tab cũ có thể kéo theo danh sách hóa đơn, file đính kèm, nhắc hạn, trạng thái hợp đồng và quyền sửa/xóa. Đây là phạm vi lớn hơn việc chỉ hiển thị thẻ hợp đồng.
- UI sản phẩm sau thay đổi nên tập trung vào: sản phẩm/gói cước, thời hạn, doanh thu dự kiến, doanh thu thực tế tính từ hợp đồng và danh sách hợp đồng dạng card gọn; không hiển thị số lượng, chi phí, lợi nhuận.
- Rủi ro dữ liệu: xóa hoặc tái sử dụng collection Revenues để lưu hợp đồng sẽ làm mất dữ liệu cũ/đảo nghĩa nghiệp vụ; hợp đồng phải có model/quan hệ riêng.
- Phạm vi kỹ thuật có thể ảnh hưởng: Core model/Biz/Cache, DigitalSalesController, contract controller/Biz, stored procedure và migration DB, partial/modal/JS/CSS, message Sys_Messages, ba bản source và kiểm thử runtime.

## 3. Câu hỏi làm rõ
0. Vì `RM_Contracts` hiện chỉ có `ProductProjectID`, quan hệ DigitalSales sẽ dùng cách nào: (A) thêm nullable `DigitalSalesProductID` trực tiếp vào `RM_Contracts`, hoặc (B) tạo bảng liên kết riêng `RM_DigitalSalesProductContract(SalesProductID, ContractID)`? Khuyến nghị: B để không thay đổi nghĩa khóa cũ và không ảnh hưởng hợp đồng dự án.
1. Quan hệ hợp đồng với sản phẩm DigitalSales có chấp nhận mở rộng bảng `RM_Contracts` bằng khóa nullable `DigitalSalesProductID`/`SalesProductID` và thêm các stored procedure truy vấn/lưu theo khóa này không? Khuyến nghị: có, tái sử dụng toàn bộ model/trường/file/nhắc hạn hợp đồng hiện tại.
2. Doanh thu thực tế tính bằng `TotalAmount` sau VAT như dòng “Tổng” của tab Hợp đồng cũ, hay bằng `ContractValue` trước VAT? Khuyến nghị: `TotalAmount` sau VAT.
3. “Clone phần hợp đồng” có bao gồm đầy đủ file đính kèm, danh sách hóa đơn, nhắc hạn, trạng thái hợp đồng và xóa mềm như ProductProjectOverview không, hay chỉ thêm/xem/sửa thông tin và file? Khuyến nghị: đầy đủ CRUD, file và xóa mềm; danh sách hóa đơn giữ liên kết nếu không phát sinh thay đổi schema.
4. Hợp đồng DigitalSales có bắt buộc chọn khách hàng theo DigitalSales hiện tại và tự điền khách hàng/sản phẩm, hay vẫn cho chọn lại như form hợp đồng dự án?
5. Khi sửa/xóa hợp đồng, có cần cập nhật ngay Doanh thu thực tế trên card sản phẩm và tổng ở danh sách DigitalSales không? Khuyến nghị: có, reload card và danh sách sau khi lưu thành công.
6. Bỏ Số lượng có chỉ ẩn giao diện hay xóa khỏi binding/lưu trữ? Khuyến nghị: chỉ ẩn để không ảnh hưởng dữ liệu cũ, tương tự Chi phí/Doanh thu thực thu.
7. Có đồng bộ Module, WebApp, `publish_source`, cập nhật DB Demo, build/test nhưng chưa publish/deploy không?

## 4. Câu trả lời & Quyết định
- Chờ người dùng xác nhận quan hệ DB, cách tính giá trị hợp đồng và phạm vi clone.

## 5. Checklist

### Chuẩn bị
- [ ] Đối chiếu toàn bộ model/controller/view/SP của tab Hợp đồng ProductProjectOverview.
- [ ] Chốt khóa liên kết và quy tắc tính Doanh thu thực tế.

### Thực hiện
- [x] Bổ sung bước đầu mô hình quan hệ hợp đồng với DigitalSalesProduct (`DigitalSalesProductID` nullable và danh sách hợp đồng trên sản phẩm).
- [x] Tạo script bảng/liên kết và stored procedure đọc/liên kết/bỏ liên kết hợp đồng; đăng ký đủ ở source, WebApp và `publish_source`.
- [x] Nạp danh sách hợp đồng liên kết và tổng `TotalAmount` (quy đổi triệu VNĐ) vào model sản phẩm; hiển thị danh sách trong form sản phẩm.
- [ ] Tạo luồng lấy/thêm/sửa/xóa mềm hợp đồng theo từng sản phẩm.
- [x] Hiển thị danh sách hợp đồng trong card sản phẩm và form sản phẩm; giữ liên kết chỉnh sửa hợp đồng hiện hữu.
- [x] Tính Doanh thu thực tế động từ tổng `TotalAmount` của các hợp đồng liên kết.
- [x] Ẩn Số lượng, Chi phí, Lợi nhuận và hiển thị Doanh thu thực tế/Số hợp đồng theo phạm vi chốt.
- [x] Đồng bộ DB script và registry Module/WebApp/`publish_source`.

### Kiểm tra / Nghiệm thu
- [ ] Kiểm tra một sản phẩm có nhiều hợp đồng.
- [ ] Kiểm tra thêm/sửa/xóa mềm hợp đồng và file đính kèm.
- [ ] Kiểm tra tổng Doanh thu thực tế sau VAT cập nhật ngay.
- [ ] Kiểm tra dữ liệu chi phí/doanh thu cũ không bị mất.
- [ ] Build/test runtime và kiểm tra responsive card/modal.

### Ghi chú
- Chưa sửa code trước khi chốt 7 câu hỏi trên.

---

# 2026-09-16 Vấn đề: Không tải danh sách sản phẩm sau khi lưu

## 1. Mô tả vấn đề
Sau khi triển khai phần hợp đồng DigitalSales, lưu sản phẩm/dịch vụ thành công nhưng `crm.git` không tải được danh sách sản phẩm/dịch vụ.

## 2. Phân tích ban đầu
- Bối cảnh: `RM_DigitalSalesBiz.GetProductsBySalesID` vừa được bổ sung truy vấn `RM_DigitalSalesProductContract_GetByProductID` cho từng sản phẩm.
- Khả năng cao: stored procedure mới chưa được chạy trên database của `crm.git`; exception ở truy vấn hợp đồng làm toàn bộ danh sách sản phẩm bị trả rỗng qua nhánh bắt lỗi của `GetByID`.
- Mục tiêu: dữ liệu sản phẩm vẫn hiển thị dù database chưa có hợp đồng; sau đó xác nhận stored procedure chạy đúng trên site.
- Phạm vi: chỉ luồng tải tab Sản phẩm/Dịch vụ số và script DB liên kết hợp đồng.

## 3. Câu hỏi làm rõ
- Không cần chờ: yêu cầu đã nêu rõ cần sửa và test lại trực tiếp trên `http://crm.git/`.

## 4. Quyết định
- Tách lỗi truy vấn hợp đồng khỏi truy vấn danh sách sản phẩm, ghi log thay vì làm trống toàn bộ danh sách; kiểm tra/rà soát script DB trước khi test runtime.

## 5. Checklist
### Thực hiện
- [x] Kiểm tra log/runtime và xác nhận nguyên nhân: registry procedure chưa được khởi tạo và script ban đầu dùng sai cột `RM_Contracts.CustomerID`.
- [x] Bảo vệ luồng tải danh sách sản phẩm khi stored procedure hợp đồng chưa khả dụng.
- [x] Kiểm tra, sửa script DB và chạy thành công trên môi trường `crm.git`.
### Kiểm tra / Nghiệm thu
- [x] Xác nhận tải `Cate/DigitalSales/Detail/69` trên `crm.git` hiển thị 4 sản phẩm sau đăng nhập.
- [x] Xác nhận markup hợp đồng/Số hợp đồng được render khi stored procedure khả dụng.

---

# 2026-09-16 Yêu cầu: Thao tác hợp đồng trực tiếp theo sản phẩm DigitalSales

## 1. Mô tả vấn đề
Người dùng yêu cầu hiển thị danh sách hợp đồng trong từng Sản phẩm/Dịch vụ số, có thể sửa từng hợp đồng và thêm hợp đồng mới ngay tại sản phẩm.

## 2. Phân tích ban đầu
- Bối cảnh: quan hệ nhiều-nhiều được lưu trong `RM_DigitalSalesProductContract`; màn hình hiện mới hiển thị số lượng/tóm tắt.
- Mục tiêu: tái sử dụng popup CRUD hợp đồng hiện hữu, nhưng tạo hợp đồng không được nhầm `SalesProductID` với `ProductProjectID`.
- Quyết định: thêm tham số `digitalSalesProductId` cho action thêm mới, lưu hợp đồng với `ProductProjectID = 0` (cột cho phép null) rồi tạo liên kết bằng SP; sửa/xóa tiếp tục dùng action hợp đồng có sẵn.

## 3. Câu hỏi làm rõ
- Không cần chờ: yêu cầu và phạm vi đã rõ; “trong mỗi hợp đồng” được hiểu là trong từng thẻ Sản phẩm/Dịch vụ số.

## 4. Checklist
### Thực hiện
- [x] Bổ sung API liên kết hợp đồng mới vào sản phẩm.
- [x] Bổ sung action thêm hợp đồng từ sản phẩm DigitalSales.
- [x] Cập nhật thẻ sản phẩm hiển thị danh sách, nút Thêm mới và nút Sửa hợp đồng.
### Kiểm tra / Nghiệm thu
- [x] Kiểm tra popup thêm hợp đồng trên `crm.git`: action nhận đúng `DigitalSalesProductID` và không dùng nhầm `ProductProjectID`.
- [ ] Kiểm tra thêm/sửa thực tế và tải lại danh sách sau khi có hợp đồng mẫu.

---

# 2026-09-16 Yêu cầu: Tinh gọn thẻ sản phẩm DigitalSales

## 1. Mô tả vấn đề
Bỏ chỉ số “Số hợp đồng” và làm giao diện sản phẩm gọn hơn.

## 2. Phân tích & quyết định
- Danh sách hợp đồng đã thể hiện trực tiếp số lượng thực tế nên không cần KPI trùng lặp.
- Giữ hai chỉ số cần thiết: Doanh thu dự kiến và Doanh thu thực tế; gom gói cước/thời hạn thành một hàng thông tin ngắn.

## 3. Checklist
- [x] Bỏ chỉ số Số hợp đồng.
- [x] Gom thông tin sản phẩm và rút gọn khoảng cách hiển thị.
- [x] Đồng bộ ba bản view và kiểm tra render trên `crm.git` (4 sản phẩm, còn nút Thêm hợp đồng và Doanh thu thực tế).

---

# 2026-09-16 Lỗi: Lưu hợp đồng DigitalSales trả về HTTP 500

## 1. Mô tả vấn đề
Lưu hợp đồng từ sản phẩm DigitalSales bị lỗi HTTP 500.

## 2. Phân tích & quyết định
- Log xác nhận `Modules.Cate.dll` gọi `RM_DigitalSalesCache.LinkProductContract` nhưng `Core.Cate.dll` đang chạy chưa có hàm này.
- Nguyên nhân là hai DLL triển khai lệch phiên bản, không phải lỗi dữ liệu hợp đồng.
- Cập nhật lại `Core.Cate.dll` cùng phiên bản với `Modules.Cate.dll` và khởi động lại ứng dụng.

## 3. Checklist
- [x] Đọc log và xác định lỗi binary mismatch.
- [x] Cập nhật DLL Core.Cate và khởi động lại `crm.git`.
- [x] Kiểm tra popup thêm hợp đồng sau khởi động lại.
- [ ] Kiểm tra lưu bằng dữ liệu hợp đồng thực tế.

---

# 2026-09-16 Lỗi: Thiếu callback JavaScript khi lưu hợp đồng DigitalSales

## 1. Mô tả vấn đề
Sau khi lưu hợp đồng từ DigitalSales, trình duyệt báo `Contracts_OnProcessSuccess is not defined`.

## 2. Phân tích & quyết định
- Callback cũ chỉ được nạp tại màn hình Hợp đồng/ProjectOverview, còn DigitalSalesDetail không nạp file đó.
- Tạo callback riêng `DigitalSalesContract_OnProcessSuccess` trong `DigitalSalesDetail.js`; callback đóng modal, hiển thị thông báo và tải lại tab Sản phẩm.
- Popup hợp đồng nhận diện `DigitalSalesProductID` để gọi callback mới, còn luồng hợp đồng dự án giữ callback cũ.

## 3. Checklist
- [x] Tạo callback riêng cho hợp đồng DigitalSales.
- [x] Cập nhật popup thêm/sửa để chọn callback theo ngữ cảnh.
- [x] Đồng bộ view/JS và build Modules.Cate.
- [ ] Kiểm tra lưu hợp đồng thực tế trên trình duyệt sau khi làm mới trang.

---

# 2026-09-16 Lỗi: Nút Lưu popup hợp đồng không submit

## 1. Mô tả vấn đề
Sau khi xử lý callback, nhấn nút Lưu trên popup hợp đồng DigitalSales không gửi form.

## 2. Phân tích & quyết định
- Nút `#btnSave` của layout nằm ở footer, ngoài thẻ form `AddContracts`/`EditContracts`.
- Bổ sung binding click theo mẫu form DigitalSales: chặn hành vi mặc định, gọi `$form.submit()`, validate trước submit và khóa/mở lại nút khi gửi.

## 3. Checklist
- [x] Xác định nút Save nằm ngoài form và chưa được bind submit.
- [x] Bổ sung binding cho popup thêm/sửa hợp đồng.
- [x] Đồng bộ view và xác nhận popup `crm.git` trả về script binding.
- [ ] Kiểm tra lưu hợp đồng thực tế trên trình duyệt.

---

# 2026-09-16 Lỗi: Lưu thành công nhưng popup hợp đồng không đóng

## 1. Mô tả vấn đề
Hợp đồng được lưu nhưng modal không đóng và không hiện thông báo thành công.

## 2. Phân tích & quyết định
- Callback chỉ chọn modal theo `#ModalContent`, trong khi popup DigitalSales có thể nằm ở `#modalContainer`/root.
- Bổ sung chuỗi fallback tìm modal theo ID trực tiếp rồi modal đang hiển thị; chỉ gọi callback hoàn tất sau sự kiện `hidden.bs.modal`.

## 3. Checklist
- [x] Sửa selector modal có fallback theo ngữ cảnh DigitalSales.
- [x] Đồng bộ JavaScript và kiểm tra endpoint script trên `crm.git`.
- [ ] Kiểm tra lưu hợp đồng thực tế: hiển thị thông báo, đóng popup, tải lại danh sách.

---

# 2026-09-16 Lỗi: Doanh thu thực tế chưa cập nhật theo hợp đồng

## 1. Mô tả vấn đề
Sau khi lưu hợp đồng thành công, doanh thu thực tế của sản phẩm/Dịch vụ số chưa hiển thị đúng.

## 2. Phân tích & quyết định
- `RM_Contracts.TotalAmount` đang được lưu theo đơn vị triệu VNĐ, đồng nhất với chỉ tiêu hiển thị trên thẻ sản phẩm.
- Phần tổng doanh thu đã chia thêm 1.000.000, khiến hợp đồng 100 triệu hiển thị thành `0.00`.
- Dùng trực tiếp tổng `TotalAmount` làm `ContractRevenueMillion`; vẫn duy trì `ActualRevenue` quy đổi về VNĐ cho các xử lý nội bộ tương thích.

## 3. Checklist
- [x] Sửa phép tính tổng doanh thu hợp đồng trong `RM_DigitalSalesBiz`.
- [x] Build Core.Cate và cập nhật DLL WebApp cục bộ.
- [x] Kiểm tra `Cate/DigitalSales/Detail/69`: hợp đồng `HD26032701` và doanh thu thực tế `100.00` cùng được hiển thị.

---

# 2026-09-16 Cập nhật: Hiển thị doanh thu và ghi chú dịch vụ

## 1. Mô tả vấn đề
Người dùng yêu cầu hiển thị đơn vị Triệu VNĐ cạnh từng chỉ tiêu doanh thu, dùng vị trí dòng đơn vị cũ để hiển thị ghi chú dịch vụ, và bỏ hợp đồng khỏi popup thêm/sửa dịch vụ.

## 2. Phân tích & quyết định
- Đơn vị phải nằm ngay cạnh doanh thu dự kiến và doanh thu thực tế để không phụ thuộc dòng chú thích chung.
- Ghi chú luôn hiển thị tại vị trí dòng đơn vị cũ; không có dữ liệu thì hiển thị dấu `—`.
- Hợp đồng vẫn quản lý trên thẻ sản phẩm ở trang chi tiết, không xuất hiện trong form thêm/sửa dịch vụ.

## 3. Checklist
- [x] Hiển thị `Triệu VNĐ` cạnh hai giá trị doanh thu.
- [x] Thay dòng đơn vị bằng ghi chú dịch vụ.
- [x] Bỏ khối hợp đồng trong `_ProductForm`.
- [x] Đồng bộ view sang WebApp và publish_source.

---

# 2026-09-16 Cập nhật: Đồng bộ thẻ tổng doanh thu thực tế theo hợp đồng

## 1. Mô tả vấn đề
Thẻ tổng `Doanh thu thực tế (Ký HĐ)` trên chi tiết DigitalSales vẫn hiển thị giá trị cũ, chưa đồng bộ với doanh thu thực tế của các sản phẩm từ hợp đồng liên kết.

## 2. Phân tích & quyết định
- Thẻ tổng dùng `TotalActualRevenue` từ hồ sơ, trong khi từng sản phẩm đã tính `ContractRevenueMillion` từ `RM_Contracts.TotalAmount`.
- Khi tải hồ sơ, tính lại `TotalActualRevenue` bằng tổng doanh thu hợp đồng của tất cả sản phẩm và quy đổi về VNĐ.
- Khi sửa một hợp đồng trong ngữ cảnh DigitalSales, gọi lại liên kết để làm mới cache, bảo đảm thẻ tổng cập nhật ngay sau lần tải lại.

## 3. Checklist
- [x] Tính `TotalActualRevenue` từ tổng hợp đồng liên kết trong `RM_DigitalSalesBiz.GetByID`.
- [x] Làm mới cache DigitalSales sau khi sửa hợp đồng.
- [x] Build Core.Cate và Modules.Cate, cập nhật DLL WebApp cục bộ.
- [x] Kiểm tra `Cate/DigitalSales/Detail/69`: thẻ tổng hiển thị `100,000,000 VNĐ` và thẻ sản phẩm hiển thị `100.00 Triệu VNĐ`.

---

# 2026-09-16 Cập nhật: Mở rà soát định kỳ bằng modal theo yêu cầu

## 1. Mô tả vấn đề
Panel rà soát tự mở trên trang chi tiết DigitalSales và chia đôi layout, làm giao diện chi tiết bị thu hẹp/vỡ.

## 2. Phân tích & quyết định
- Không tự mở biểu mẫu khi URL có `reviewBatchID`.
- Hiển thị nút `Rà soát` tại cụm thao tác khi hồ sơ được truy cập từ một đợt rà soát.
- Mở biểu mẫu bằng modal phủ lên trang; giữ nguyên quy tắc lưu hiện có, đồng thời bỏ hoàn toàn vùng layout chia đôi.

## 3. Checklist
- [x] Thay trigger ẩn/tự click bằng nút `Rà soát` hiển thị.
- [x] Bỏ `reviewSplitView`, `reviewFormPane` và script tự mở modal.
- [x] Cập nhật script modal không neo vào panel bên phải của layout.
- [x] Đồng bộ view sang WebApp và publish_source.
- [x] Kiểm tra `Cate/DigitalSales/Detail/67?reviewBatchID=24`: có nút rà soát, không có trigger tự mở hoặc split layout.

---

# 2026-09-16 Cập nhật: Khôi phục công cụ CKEditor và nút đóng modal rà soát

## 1. Mô tả vấn đề
Sau khi chuyển rà soát thành modal mở bằng nút, người dùng yêu cầu dùng lại đầy đủ công cụ CKEditor và hiển thị nút đóng modal.

## 2. Phân tích & quyết định
- Modal không còn làm vỡ layout nên không cần giới hạn thanh công cụ hay chiều cao vùng soạn thảo.
- Dùng cấu hình toolbar mặc định của CKEditor và tăng chiều cao vùng nội dung lên 360px.
- Khôi phục nút đóng `×` ở header; vẫn ẩn nút Hủy footer để giữ hai thao tác nghiệp vụ chính là Lưu và Lưu và tiếp tục.

## 3. Checklist
- [x] Bỏ cấu hình toolbar rút gọn của CKEditor.
- [x] Khôi phục hiển thị nút đóng header modal.
- [x] Đồng bộ các view/CSS sang WebApp và publish_source.
- [x] Kiểm tra response `ReviewBatch`: toolbar mặc định, chiều cao 360px và chỉ loại bỏ dismiss ở footer.

---

# 2026-09-16 Cập nhật: Nút rà soát dạng nút nổi mép phải

## 1. Mô tả vấn đề
Người dùng yêu cầu nút mở modal rà soát có kiểu tương tự nút nổi ở mép phải trong ảnh minh họa.

## 2. Phân tích & quyết định
- Thay nút trong thanh thao tác bằng nút nổi cố định ở mép phải để không chiếm chiều rộng header.
- Nút dùng biểu tượng clipboard, nhãn ngắn và tooltip; hover mở rộng để dễ nhận biết.
- Chỉ render khi URL thuộc một đợt rà soát (`reviewBatchID > 0`).

## 3. Checklist
- [x] Thay nút header bằng `ds-review-floating-trigger`.
- [x] Bổ sung CSS nút nổi mép phải và cách hiển thị trên màn hình nhỏ.
- [x] Đồng bộ view/CSS sang WebApp và publish_source.
- [x] Kiểm tra `Detail/67?reviewBatchID=24`: có nút nổi, không còn nút header và CSS fixed-right hợp lệ.

---

# 2026-09-16 Điều chỉnh: Giảm chiều cao CKEditor rà soát

- Giảm chiều cao vùng soạn thảo CKEditor từ 360px xuống 260px trong cấu hình và CSS override.
- [x] Đồng bộ sang WebApp và publish_source.

---

# 2026-09-16 Rà soát: Thông báo và email DigitalSales

## 1. Mô tả vấn đề
Kiểm tra phần DigitalSales mới đã xử lý gửi thông báo và gửi email hay chưa.

## 2. Phân tích & kết quả
- `DigitalSalesController` hiện chỉ gọi `NotificationService.PushDigitalSalesNotification` trong `AddDiscussion`, khi có người dùng được nhắc đến trong trao đổi.
- `NotificationService` lưu thông báo nội bộ và phát tín hiệu realtime; đường dẫn thông báo DigitalSales trỏ tới tab trao đổi của hồ sơ.
- Không tìm thấy lời gọi gửi email hay template email dành cho DigitalSales trong controller, service và view liên quan.
- Các luồng tạo/sửa hồ sơ, chuyển trạng thái, sản phẩm/hợp đồng, thành viên, tiến trình và rà soát hiện chưa phát thông báo nội bộ hoặc email.

## 3. Checklist
- [x] Rà soát điểm gọi NotificationService trong DigitalSales.
- [x] Rà soát lời gọi email/template mang ngữ cảnh DigitalSales.
- [x] Xác nhận phạm vi thông báo đang có và các luồng còn thiếu.

---

# 2026-09-16 Phân tích: Kế thừa MailTemplate và SMSTemplate cho DigitalSales

## 1. Mô tả vấn đề
Rà soát hai cấu hình `Sys/MailTemplate` và `Sys/SMSTemplate` để chọn phần phù hợp chuyển sang DigitalSales.

## 2. Kết quả phân tích
- Mail hiện có 14 template active; SMS có 8 template active. Các template đều hướng tới cơ hội, dự án, công việc, kế hoạch hoặc hợp đồng.
- Nên kế thừa cấu trúc/luồng của `COHOIKINHDOANH_CAPNHATTRANGTHAI`, `THONGBAONGUOITHAMGIA`, `XOANGUOITHAMGIA`, `CONGVIEC_KHOITAO`, `CONGVIEC_CAPNHAT` và `HOPDONG_DENHANXUATHOADON`, nhưng tạo mã template DigitalSales riêng vì nội dung/đường dẫn/đối tượng khác.
- `DUAN_COHOI_NHACNHOCAPNHAT` có tham số tổng quát `ObjectName`, `ObjectType`, `FullName`, `DetailUrl`; có thể tái sử dụng trực tiếp cho nhắc cập nhật định kỳ DigitalSales.
- SMS nên giới hạn cho phân công mới và nhắc hạn/quá hạn; không nên gửi SMS khi sửa hồ sơ, trao đổi hoặc chuyển trạng thái thông thường để tránh spam.

## 3. Danh sách đề xuất
- [ ] Tạo mail DigitalSales chuyển trạng thái.
- [ ] Tạo mail/SMS DigitalSales thêm thành viên.
- [ ] Tạo mail/SMS DigitalSales giao hoặc nhắc tiến trình.
- [ ] Điều chỉnh mail/SMS nhắc hợp đồng theo DigitalSales, thay nhãn Dự án bằng Hồ sơ KD SPDV số.
- [ ] Dùng trực tiếp mail nhắc cập nhật định kỳ với tham số tổng quát.

---

# 2026-09-16 Cập nhật: Hiển thị hợp đồng DigitalSales tại Cate/RM_Contracts

## 1. Mô tả vấn đề
Cập nhật danh sách/chi tiết hợp đồng để nhận diện hợp đồng mới phát sinh từ DigitalSales, đồng thời bổ sung tên hồ sơ KD SPDV số có thể bấm mở trang chi tiết.

## 2. Phân tích & quyết định
- Hợp đồng DigitalSales không dùng `ProductProjectID`; liên kết qua `RM_DigitalSalesProductContract` đến `RM_DigitalSalesProduct`, nên stored procedure hợp đồng cũ không lấy được hồ sơ, sản phẩm hoặc khách hàng tương ứng.
- Giữ nguyên dữ liệu và cách hiển thị hợp đồng dự án cũ; dùng `OUTER APPLY` lấy liên kết DigitalSales khi có, sau đó `COALESCE` thông tin sản phẩm/khách hàng theo dự án hoặc DigitalSales.
- Bổ sung cột Hồ sơ KD SPDV số trong danh sách. Cột này hiển thị tên, mã hồ sơ và liên kết đến `Cate/DigitalSales/Detail/{id}`; hợp đồng không thuộc DigitalSales hiển thị dấu gạch ngang.

## 3. Checklist
### Thực hiện
- [x] Bổ sung các trường DigitalSales vào `RM_ContractsModel`.
- [x] Cập nhật `RM_Contracts_Get` và `RM_Contracts_GetById` trong script database để lấy liên kết DigitalSales.
- [x] Cập nhật danh sách và chi tiết hợp đồng, đồng bộ view/JS sang WebApp và publish_source.
- [x] Chạy script stored procedure trên database demo.
### Kiểm tra / Nghiệm thu
- [x] Build Core.Cate và Modules.Cate thành công, DLL WebApp được cập nhật.
- [x] Kiểm tra `RM_Contracts_Get` với hợp đồng `HD26032701`: trả về DigitalSalesID 69, mã/tên hồ sơ, sản phẩm và khách hàng.
- [x] Kiểm tra site local `Cate/RM_Contracts/Get` và `ViewDetail/87`: dữ liệu trả về đầy đủ, liên kết DigitalSales hợp lệ.

---

# 2026-09-16 Điều chỉnh: Danh sách hợp đồng chỉ dành cho DigitalSales

## 1. Mô tả vấn đề
Danh sách `Cate/RM_Contracts` vẫn còn hợp đồng dự án cũ và có quá nhiều cột, khiến các thông tin quan trọng bị phân tán.

## 2. Phân tích & quyết định
- Trang này được dùng tiếp cho hợp đồng DigitalSales nên chỉ trả về bản ghi có liên kết `RM_DigitalSalesProductContract` còn hiệu lực; hợp đồng dự án cũ không còn xuất hiện tại đây.
- Gộp dữ liệu vào các nhóm: Hợp đồng; Hồ sơ KD SPDV số/Dịch vụ; Khách hàng; Giá trị/Thời gian; Trạng thái.
- Mã hợp đồng, tên hồ sơ và tổng giá trị được làm nổi bật; ngày ký, mã hồ sơ và sản phẩm là thông tin phụ trợ trong cùng cột.

## 3. Checklist
### Thực hiện
- [x] Lọc `RM_Contracts_Get` theo `DigitalSalesID IS NOT NULL`.
- [x] Gộp lại các cột DataTable và cập nhật renderer để hiển thị theo nhóm.
- [x] Đồng bộ view/JS sang WebApp và publish_source.
### Kiểm tra / Nghiệm thu
- [x] Chạy lại stored procedure trên database demo.
- [x] Kiểm tra stored procedure và `http://crm.git/Cate/RM_Contracts/Get`: 1 hợp đồng trả về, không có bản ghi thiếu DigitalSalesID.

---

# 2026-09-16 Cập nhật: Chọn phương thức đăng nhập SSO hoặc CRM

## 1. Mô tả vấn đề
Trang đăng nhập cần cho phép người dùng chủ động chọn đăng nhập qua SSO hoặc đăng nhập thông thường bằng tài khoản CRM.

## 2. Phân tích & quyết định
- Trước đây host trùng `App_HostUrl` tự chuyển ngay sang SSO, khiến không thể dùng xác thực CRM trên site chính/demo.
- Giữ nhận diện site qua `App_HostUrl`; tại host SSO, trang Login hiển thị nút SSO và form tài khoản CRM. Local/IP vẫn chỉ dùng form CRM như trước.
- Form CRM gửi cờ `loginMode=local`; controller chỉ cho phép bỏ qua redirect SSO khi cờ này được chọn rõ ràng.

## 3. Checklist
### Thực hiện
- [x] Cập nhật GET Login để hiển thị lựa chọn thay vì tự điều hướng SSO.
- [x] Bổ sung nút SSO, ngăn cách trực quan và form tài khoản CRM.
- [x] Cập nhật POST Login xử lý cờ lựa chọn đăng nhập thông thường.
- [x] Đồng bộ các view Login sang publish_source.
### Kiểm tra / Nghiệm thu
- [x] Build WebApp thành công; chỉ có warning dependency có sẵn.
- [x] Kiểm tra `crm.git`: form chứa cờ `loginMode` và đăng nhập CRM thành công.

---

# 2026-09-16 Điều chỉnh: Không phân biệt host khi chọn đăng nhập

## 1. Mô tả vấn đề
Người dùng yêu cầu lựa chọn đăng nhập SSO hoặc tài khoản/mật khẩu CRM phải giống nhau trên mọi host, không dựa vào local, demo hay site chính.

## 2. Phân tích & quyết định
- Bỏ điều kiện `App_HostUrl` khỏi quyết định hiển thị và xử lý hai phương thức đăng nhập.
- GET Login luôn hiển thị hai lựa chọn; nút SSO mới chuyển sang cổng SSO, form CRM luôn xác thực theo luồng cũ.
- Callback SSO vẫn được xử lý ở mọi host. Logout chỉ gọi hủy ticket SSO nếu phiên hiện tại thực sự có ticket SSO.

## 3. Checklist
### Thực hiện
- [x] Cập nhật luồng Login không phụ thuộc `App_HostUrl`.
- [x] Cập nhật logout nhận biết phiên SSO qua ticket thay vì host.
### Kiểm tra / Nghiệm thu
- [x] Build WebApp thành công, chỉ có warning dependency có sẵn.
- [x] Kiểm tra `crm.git`: hiển thị nút SSO, liên kết bắt đầu SSO và form CRM; đăng nhập CRM thành công.

---

# 2026-09-16 Cải thiện UI: Trang đăng nhập hai phương thức

## 1. Mô tả vấn đề
Giao diện đăng nhập sau khi thêm lựa chọn SSO/CRM thiếu phân cấp, logo quá lớn, phần hỗ trợ dài và làm form mất cân đối.

## 2. Phân tích & quyết định
- Dùng card trắng trung tâm với đổ bóng nhẹ trên nền thương hiệu để tách vùng thao tác rõ ràng.
- Thu nhỏ logo; chuẩn hóa hai nút đăng nhập cao 48px; phân cách hai luồng bằng divider ngắn gọn.
- Thu gọn thông tin hỗ trợ vào mục mở rộng để không gây nhiễu luồng đăng nhập chính.

## 3. Checklist
### Thực hiện
- [x] Áp dụng card responsive, spacing và phân cấp mới cho trang Login.
- [x] Tối giản phần hỗ trợ nhưng giữ hotline, email và hướng dẫn đổi mật khẩu.
- [x] Đồng bộ views sang publish_source.
### Kiểm tra / Nghiệm thu
- [x] Chụp và kiểm tra thực tế tại 1440x1000: card cân giữa, không tràn, các nút đạt chiều cao tối thiểu 48px và thông tin hỗ trợ được thu gọn.

---

# 2026-09-16 Điều chỉnh: Giữ bố cục đăng nhập cũ

- Khôi phục bố cục và nội dung hỗ trợ như trước khi cải tiến UI.
- Chỉ thay nút SSO: nhãn `Đăng nhập bằng VNPT SSO`, dùng cùng kiểu xanh/gradient với nút Đăng nhập CRM.
- [x] Đồng bộ views sang publish_source và kiểm tra HTML tại `crm.git`.

---

# 2026-09-16 Cập nhật: Ngày ký hợp đồng DigitalSales

- Bổ sung cột `Ngày ký` trong danh sách hợp đồng của từng sản phẩm/dịch vụ tại `DigitalSales/Detail`.
- Hiển thị theo định dạng `dd/MM/yyyy`; hợp đồng chưa có ngày ký hiển thị dấu gạch ngang.
- [x] Đồng bộ source, WebApp và publish_source; kiểm tra `DigitalSales/Detail/69` có tiêu đề và dữ liệu hợp đồng.

---

# 2026-09-16 Quy ước commit: Visual Studio dtbcache

- Các file `*.csproj.dtbcache.json` trong thư mục `.vs` là cache cục bộ của Visual Studio, không phải mã nguồn hay cấu hình triển khai.
- [x] Quyết định: không đưa các file này vào commit dù chúng đang hiện thay đổi do đã từng được Git theo dõi.

---

# 2026-09-16 Điều chỉnh UI: Modal chọn khách hàng DigitalSales

- Thu gọn riêng modal tra cứu/chọn khách hàng trong form Hồ sơ KD SPDV số từ `modal-lg` xuống `modal-md`.
- Giảm vùng kết quả cuộn từ 380px xuống 300px để modal thấp hơn, vẫn giữ đầy đủ cột và có cuộn ngang/dọc khi cần.
- [x] Đồng bộ source, WebApp, publish_source; kiểm tra HTML form Edit DigitalSales trên site local.

---

# 2026-09-16 Phân tích: Kích thước modal Bootstrap

- Bootstrap của hệ thống chỉ có các cỡ chuẩn: mặc định (không class), `modal-sm`, `modal-lg`, `modal-xl`; `modal-md` không phải class Bootstrap chuẩn.
- `modal-md` chỉ được định nghĩa trong CSS riêng của ProductService (70%) và không được nạp tại DigitalSales, nên tại đây nó rơi về kích thước mặc định như `modal-sm`.
- Cần tạo class riêng, ví dụ `digital-sales-customer-modal`, đặt `width/max-width` cụ thể (khuyến nghị 760px, tối đa `calc(100vw - 32px)`) thay vì tái dùng các cỡ không ổn định.

---

# 2026-09-16 Đồng bộ kích thước: Modal chọn khách hàng DigitalSales

- Modal Chỉnh sửa Hồ sơ KD SPDV số được tạo bởi `openEditSalesModal`, dùng `modal-xl` với `max-width: 1024px`.
- [x] Áp dụng đúng `modal-xl` và `max-width: 1024px` cho modal chọn khách hàng, đồng bộ source/WebApp/publish_source.
- [x] Kiểm tra HTML form Edit DigitalSales trên site local có kích thước 1024px.

---

# 2026-09-17 Phân tích lỗi: Xem chi tiết Timeline chuyển trạng thái DigitalSales

## 1. Mô tả vấn đề
Khi mở chi tiết từ Timeline cập nhật chuyển trạng thái tại `Cate/DigitalSales/Detail/98#tab-tracking`, hệ thống báo lỗi server.

## 2. Kết quả phân tích
- Log lúc 14:49:41 báo `Illegal characters in path` tại `DigitalSalesController.ParseAttachmentFiles`, được gọi từ `StatusTimelineDetailModal`.
- Timeline ID 258 của hồ sơ 98 lưu `AttachmentPath` theo JSON array (`[{"FileName":...,"FilePath":...}]`), trong khi `ParseAttachmentFiles` chỉ xử lý chuỗi path ngăn cách bởi dấu `;` hoặc `,`.
- Hàm hiện đưa nguyên đoạn JSON/chunks có ký tự `{`, `"`, `:` vào `Path.GetFileName`, nên .NET ném lỗi và modal không render.

## 3. Hướng xử lý đề xuất
- Cập nhật `ParseAttachmentFiles` nhận diện JSON array, deserialize để lấy `FilePath`/`FileName`; giữ fallback cho dữ liệu path cũ ngăn cách `;` hoặc `,`.
- Kiểm tra lại Timeline 258 và các bản ghi task/todo dùng định dạng JSON tương tự.

## 4. Thực hiện và kiểm tra
- [x] Cập nhật `ParseAttachmentFiles` để deserialize `ActivityAttachmentItem` khi dữ liệu là JSON array; giữ nguyên cách đọc chuỗi đường dẫn cũ.
- [x] Bổ sung fallback an toàn khi lấy tên file để một giá trị đường dẫn lỗi không làm hỏng modal Timeline.
- [x] Build thành công `Modules.Cate` (chỉ còn các cảnh báo dependency có sẵn).
- [x] Test trên `http://crm.git/`: `StatusTimelineDetailModal?digitalSalesId=98&timelineId=258` trả HTTP 200 và hiển thị `Checklist_thuc_hien.xlsx`.

---

# 2026-09-17 Vấn đề: Thông báo và email cho DigitalSales

## 1. Mô tả vấn đề
Bổ sung trước cơ chế gửi thông báo và email cho các sự kiện: thêm/xóa thành viên, khởi tạo hồ sơ DigitalSales và chuyển trạng thái; tái sử dụng cơ chế từ Dự án/Cơ hội nhưng điều chỉnh nội dung phù hợp DigitalSales.

## 2. Phân tích ban đầu
- Bối cảnh: DigitalSales đã có luồng tạo hồ sơ, quản lý thành viên và Timeline chuyển trạng thái nhưng cần đồng bộ truyền thông như nghiệp vụ Dự án/Cơ hội.
- Mục tiêu: người liên quan nhận được thông báo trong hệ thống và email với ngữ cảnh hồ sơ KD sản phẩm/dịch vụ số.
- Phạm vi dự kiến: tái dùng service/template/queue sẵn có; bổ sung điểm gọi tại 4 nhóm sự kiện, không thay đổi dữ liệu nghiệp vụ chính.
- Ràng buộc: cần giữ đúng đối tượng nhận và quy tắc gửi email hiện hành để tránh gửi nhầm hoặc gửi trùng.
- Rủi ro / giả định: cơ chế Dự án và Cơ hội có thể khác nhau về người nhận, mã template và điều kiện gửi; cần chốt chính sách nhận trước khi code.

## 3. Câu hỏi làm rõ
1. Khi khởi tạo DigitalSales, gửi cho ai: chỉ người tạo/AM chủ trì, hay cả thành viên được tạo cùng lúc và cấp quản lý?
2. Khi thêm/xóa thành viên, có gửi cho chính thành viên bị thêm/xóa và AM chủ trì/người thao tác không? Với thành viên bị xóa, có vẫn gửi email/thông báo báo đã rút khỏi hồ sơ không?
3. Khi chuyển trạng thái, người nhận là toàn bộ thành viên hiện tại, hay chỉ AM chủ trì và người tạo? Có cần thông báo cho thành viên vừa bị xóa ở các lần chuyển trạng thái sau không?

## 4. Câu trả lời & Quyết định
1. Khởi tạo hồ sơ → gửi AM và quản lý.
2. Thêm/xóa thành viên → chỉ gửi chính thành viên được thêm hoặc bị xóa.
3. Chuyển trạng thái → gửi toàn bộ thành viên hiện tại và quản lý.
4. Quản lý → nhận email theo cơ chế CC chung, không tạo thông báo/tin gửi riêng từng quản lý.

## 5. Checklist
### Chuẩn bị
- [x] Rà soát các điểm gửi thông báo/email của Dự án và Cơ hội cùng các template, biến thay thế và cách lấy quản lý.
### Thực hiện
- [x] Tạo `DigitalSalesMailService`, phân tách người nhận chính (thông báo + email) và quản lý (chỉ CC email một lần).
- [x] Gắn thông báo vào khởi tạo hồ sơ, thêm/xóa thành viên và chuyển trạng thái.
- [x] Bổ sung bốn mã cấu hình AppSettings, template mail dùng chung và script `Database/DigitalSales_NotificationMail.sql`.
### Kiểm tra / Nghiệm thu
- [x] Build thành công `Modules.Cate` (chỉ còn cảnh báo dependency có sẵn).
- [x] Khởi tạo và kiểm tra đủ 4/4 mẫu email DigitalSales đang hoạt động trên DB demo.
- [x] Xác nhận theo code: danh sách quản lý chỉ truyền vào CC; `Push` notification chỉ nhận danh sách người nhận chính.

## 6. Cập nhật quyết định gửi email
- [x] Mỗi sự kiện chỉ gửi một email chính tới AM chủ trì.
- [x] CC gồm toàn bộ thành viên hiện tại và các quản lý liên quan; AM được loại khỏi CC để không nhận trùng.
- [x] Thông báo chuông không thay đổi: vẫn theo đối tượng nhận nghiệp vụ đã chốt trước đó.
- [x] Build lại `Modules.Cate` thành công.

## 7. Điều kiện CC quản lý
- [x] Chỉ thêm quản lý vào CC khi `CONFIG_SEND_MAIL_TO_MANAGER` có giá trị `true`; thành viên vẫn được CC независимо cấu hình này.

---

# 2026-09-18 Vấn đề: Hướng dẫn tương tác cho DigitalSales

## 1. Mô tả vấn đề
Tạo hướng dẫn kiểu tooltip cho màn hình chi tiết DigitalSales. Lần đầu người dùng vào màn hình, giao diện bị phủ tối và hiển thị hướng dẫn tuần tự từng phần; sau khi kết thúc, lưu trạng thái xuống DB để không tự hiện lại. Cơ chế sẽ tái sử dụng cho nhiều màn hình, trước mắt áp dụng DigitalSales/Detail.

## 2. Phân tích ban đầu
- Mục tiêu: giúp người dùng mới nắm luồng thao tác mà không ảnh hưởng người đã xem.
- Phạm vi: component hướng dẫn dùng chung, trạng thái theo từng người dùng và từng mã màn hình, endpoint lưu/đọc trạng thái, cấu hình bước đầu cho Detail DigitalSales.
- Ràng buộc: phải hoạt động khi các tab/nút render động; không được đánh dấu đã xem khi người dùng chỉ đóng dở.
- Rủi ro: tooltip cần selector ổn định và cơ chế mở lại để người dùng xem lại hướng dẫn.

## 3. Câu hỏi làm rõ
1. Hoàn thành hướng dẫn mới đánh dấu đã xem, hay bấm Bỏ qua/Đóng cũng không hiện lại?
2. Có cần nút "Hướng dẫn" để người dùng chủ động xem lại sau này không?
3. Chỉ hướng dẫn các chức năng người dùng có quyền thao tác, hay vẫn giới thiệu cả khu vực chỉ xem?

## 4. Câu trả lời & Quyết định
1. Trạng thái đã xem được lưu theo từng người dùng và từng mã màn hình ngay khi tour tự mở ở lần truy cập đầu tiên; từ lần vào sau không tự hiển thị lại.
2. Có nút **Hướng dẫn** để người dùng tự mở lại tour bất cứ lúc nào; mở lại không thay đổi trạng thái đã xem.
3. Tour giới thiệu cả khu vực chỉ xem và khu vực thao tác.
4. Nội dung từng bước giai đoạn đầu dùng placeholder `Hướng dẫn abc` để cập nhật chi tiết sau.

## 5. Checklist
### Thực hiện
- [x] Tạo stored procedure/table lưu trạng thái xem hướng dẫn theo người dùng và màn hình.
- [x] Bổ sung component overlay/tooltip dùng lại được và gắn tại `Cate/DigitalSales/Detail`.
- [x] Thêm nút xem lại hướng dẫn, auto-tour lần đầu và các bước cho tiêu đề, thao tác, chỉ số, tab, vùng chỉ xem.
### Kiểm tra / Nghiệm thu
- [x] Build `Modules.Cate` thành công (chỉ còn cảnh báo dependency có sẵn).
- [x] Kiểm tra endpoint lưu trạng thái và HTML runtime: lần vào đầu `autoStart=true`, sau khi lưu `autoStart=false`; xóa dữ liệu test sau kiểm tra.

---

# 2026-09-18 Vấn đề: Hướng dẫn riêng theo tab DigitalSales

## 1. Mô tả vấn đề
Khi người dùng chọn từng tab trong trang chi tiết DigitalSales, tab đó có tour hướng dẫn riêng. Việc đã xem tour tổng quan của trang không được coi là đã xem tour của các tab.

## 2. Phân tích ban đầu
- Bối cảnh: component tour hiện dùng một mã trạng thái chung `Cate.DigitalSales.Detail` và chỉ tự mở khi vào trang.
- Mục tiêu: tách độc lập trạng thái xem giữa tour tổng quan và từng tab, để có thể bổ sung/chỉnh nội dung tab mà không ảnh hưởng các tab khác.
- Phạm vi: dùng lại table `Sys_UserGuideState`, endpoint và component hiện có; chỉ thêm mã màn hình và danh sách bước riêng cho mỗi tab.
- Ràng buộc: tour của tab chỉ bắt đầu khi tab trở thành tab đang hiển thị; không tự mở lại sau khi đã xem; nút Hướng dẫn vẫn phải mở lại đúng ngữ cảnh tab hiện tại.
- Rủi ro / giả định: cần chốt tab nào tự mở lần đầu và tab nào chỉ mở khi người dùng bấm nút Hướng dẫn, tránh mở tooltip liên tiếp khi người dùng chuyển nhiều tab.

## 3. Câu hỏi làm rõ
1. Khi người dùng lần đầu bấm một tab chưa xem, có tự động mở hướng dẫn của tab đó ngay không, hay chỉ đổi ngữ cảnh của nút **Hướng dẫn** để họ tự bấm?
2. Có áp dụng cho cả 5 tab hiện có (Tổng quan, Sản phẩm, Tiến trình, Trao đổi, Lịch sử rà soát) không?

## 4. Câu trả lời & Quyết định
1. Khi người dùng lần đầu chọn một tab chưa xem, tự động mở ngay tour riêng của tab đó.
2. Áp dụng cho toàn bộ 5 tab hiện có.
3. Giữ một nút **Hướng dẫn** duy nhất ở thanh hành động. Nút luôn mở lại tour tương ứng tab đang active; không cần tạo nút riêng cho từng tab.

## 5. Checklist
### Chuẩn bị
- [x] Khai báo mã hướng dẫn độc lập cho tổng quan và 5 tab DigitalSales.
### Thực hiện
- [x] Theo dõi sự kiện chuyển tab, kiểm tra trạng thái DB và tự mở đúng tour khi tab chưa được xem.
- [x] Cấu hình danh sách bước riêng cho từng tab, không đánh dấu chéo giữa các tab.
- [x] Cập nhật nút Hướng dẫn chung để nhận diện tab active và mở lại đúng tour.
### Kiểm tra / Nghiệm thu
- [x] Build `Modules.Cate` thành công; kiểm tra cú pháp JavaScript.
- [x] Kiểm tra endpoint runtime: mã tab Sản phẩm được lưu/đọc độc lập với tour tổng quan; xóa dữ liệu test sau kiểm tra.
- [ ] Kiểm tra trực quan lần đầu mở từng tab và nút Hướng dẫn tại từng tab trên trình duyệt.

---

# 2026-09-18 Vấn đề: Chia nhỏ bước hướng dẫn theo tab DigitalSales

## 1. Mô tả vấn đề
Mỗi tab trong DigitalSales/Detail cần có nhiều bước hướng dẫn tuần tự. Người dùng nhấn **Tiếp theo** để chuyển bước; tại bước cuối nút hiển thị **Đã hiểu** để kết thúc.

## 2. Phân tích ban đầu
- Bối cảnh: hiện mỗi tab mới có một bước placeholder, nhưng component tour đã hỗ trợ mảng `steps`, nút điều hướng và tự đóng ở bước cuối.
- Mục tiêu: hướng dẫn theo từng nhóm thông tin/thao tác nhỏ trong tab, tránh dồn nội dung vào một tooltip lớn.
- Phạm vi: bổ sung selector/bước con cho đủ 5 tab; đổi nhãn nút kết thúc từ `Hoàn thành` thành `Đã hiểu`.
- Ràng buộc: selector cần tồn tại và hiển thị theo quyền/dữ liệu; bước không có phần tử hiển thị sẽ được bỏ qua an toàn.
- Rủi ro / giả định: các tab có cấu trúc khác nhau theo dữ liệu, nên cần xác định các khu vực cần giới thiệu thay vì chỉ chọn số lượng bước tùy ý.

## 3. Câu hỏi làm rõ
1. Anh muốn tôi tự chia theo các khối giao diện hiện có (tiêu đề tab, danh sách/chỉ số, nút thêm/chỉnh sửa, lịch sử), hay anh sẽ chỉ rõ từng khu vực cần hướng dẫn?
2. Có muốn giữ nội dung `Hướng dẫn abc` cho tất cả bước mới để tự cập nhật sau không?

## 4. Câu trả lời & Quyết định
1. Tự chia theo các khối giao diện hiện có trong từng tab.
2. Giữ `Hướng dẫn abc` cho toàn bộ bước mới.

## 5. Checklist
### Thực hiện
- [x] Chia tab Tổng quan thành các bước thông tin hồ sơ, phân công, thành viên, mô tả và tệp đính kèm.
- [x] Chia các tab Sản phẩm, Tiến trình, Trao đổi và Lịch sử rà soát theo các khối dữ liệu/thao tác hiện có.
- [x] Đổi nhãn điều hướng thành `Tiếp theo`; đổi nhãn bước cuối thành `Đã hiểu`.
### Kiểm tra / Nghiệm thu
- [x] Kiểm tra cú pháp JavaScript và build `Modules.Cate` thành công (chỉ còn cảnh báo dependency có sẵn).
- [x] Xác nhận script đa bước đã được sao chép sang WebApp và local site trả về bản mới.
- [ ] Kiểm tra trực quan tour từng tab trên trình duyệt.

---

# 2026-09-18 Vấn đề: Sửa font và đồng bộ giao diện Sys/SharedDocument

## 1. Mô tả vấn đề
Khắc phục lỗi font chữ và đồng bộ giao diện trang `Sys/SharedDocument`.

## 2. Phân tích ban đầu
- Bối cảnh: source `Modules.Sys` có trang Index, Search, CSS/JS riêng; WebApp đang có bản sao tương ứng.
- Phát hiện: nhiều chuỗi tiếng Việt hard-code trong `SharedDocument.js` và comment/view bị mojibake (`Táº£i vá»`, `Chi tiáº¿t`…), đây có thể trực tiếp tạo lỗi font ở cột thao tác/thông báo.
- Mục tiêu: chuẩn hóa UTF-8 và các text UI; căn chỉnh lại search card, bảng danh sách, badge và action theo giao diện danh mục hiện hành.
- Ràng buộc: message lấy qua `Sys_Messages` cần giữ nguyên; chỉ sửa hard-code lỗi mã hóa và style/layout của SharedDocument, không thay đổi dữ liệu tài liệu hay quyền.
- Rủi ro / giả định: “đồng bộ giao diện” chưa có màn hình đối chiếu cụ thể; cần chốt mức độ làm mới để tránh thay đổi ngoài ý muốn.

## 3. Câu hỏi làm rõ
1. Anh muốn đồng bộ theo phong cách danh mục hiện tại (search card xanh, bảng/card Ace, button action nhỏ) hay theo một màn hình cụ thể? Nếu có, vui lòng cho URL tham chiếu.
2. Có áp dụng luôn cho các modal Thêm/Sửa/Xem chi tiết của SharedDocument hay chỉ trang danh sách?

## 4. Câu trả lời & Quyết định
1. Đồng bộ theo phong cách danh mục hiện tại của hệ thống.
2. Chỉ cập nhật trang danh sách; giữ nguyên các modal hiện có.

## 5. Checklist
### Thực hiện
- [x] Chuẩn hóa hiển thị chuỗi tiếng Việt trong script SharedDocument: dữ liệu/tên tài liệu/danh mục và tooltip action được tự phục hồi từ mojibake về UTF-8 khi render.
- [x] Cập nhật search card, bảng danh sách, badge và action theo giao diện danh mục hiện hành.
- [x] Giữ nguyên luồng dữ liệu, action và các modal Thêm/Sửa/Xem chi tiết.
### Kiểm tra / Nghiệm thu
- [x] Build `Modules.Sys` thành công (chỉ còn cảnh báo dependency có sẵn).
- [x] Kiểm tra source được copy sang WebApp, trang `Sys/SharedDocument` trả HTTP 200 và có layout mới; kiểm tra hàm phục hồi mã hóa với các nhãn action lỗi font.

## 6. Cập nhật nguyên nhân lỗi font
- [x] Xác định `Sys_Messages` trên DB demo có hai bản ghi `Label_TuNgay` và `Label_DenNgay` bị mojibake, nên label ở form search lỗi dù view dùng message key đúng.
- [x] Tạo `Database/SharedDocument_FontFix.sql` và chạy trên DB demo với UTF-8 để sửa hai message thành `Từ ngày` / `Đến ngày`.
- [x] Bổ sung chuẩn hóa text phía client cho label/placeholder search và tooltip action, giúp màn hình hiển thị đúng cả khi cache message cũ còn tồn tại.
- [x] Build lại `Modules.Sys`; xác nhận local site đang phục vụ script chuẩn hóa mới.

## 7. Tinh chỉnh giao diện danh sách
- [x] Bỏ header `Danh sách tài liệu biểu mẫu dùng chung` theo yêu cầu để giảm phần tiêu đề lặp.
- [x] Bổ sung CSS Select2 32px, đồng bộ chiều cao với textbox search.
# 2026-09-18 Vấn đề: Đồng bộ search SharedDocument theo CSS chung

## 1. Mô tả vấn đề
Tiếp tục tối ưu `Sys/SharedDocument`: dùng CSS chung đang áp dụng tại `Cate/Customer` và đưa các nút tìm kiếm/làm mới lên cùng hàng điều kiện tìm kiếm.

## 2. Phân tích ban đầu
- Bối cảnh: giao diện trước đó dùng CSS cục bộ cho ô input/Select2 nên không đồng bộ với style chung của hệ thống.
- Mục tiêu: dùng cấu trúc `Search_Box`, `Group`, `Search_button` chung để Select2 cùng chiều cao textbox và thao tác tìm kiếm không chiếm thêm một hàng.
- Phạm vi: partial `_Search`, CSS riêng của SharedDocument và link CSS trùng lặp trên trang Index.

## 4. Câu trả lời & Quyết định
1. Tham khảo `Cate/Customer` → dùng đúng các class CSS chung đã có.
2. Nút thao tác cùng hàng → đặt trong `Group Group_button` cuối search bar.

## 5. Checklist
### Thực hiện
- [x] Thay cấu trúc vùng tìm kiếm sang `Search_Box`/`Group`/`Search_button` chuẩn.
- [x] Xóa CSS cục bộ ép chiều cao Select2/input và link CSS nội dung trùng lặp.
- [ ] Build Modules.Sys và kiểm tra các file mirror WebApp.

---

# 2026-09-18 Vấn đề: Hiển thị ngày và tooltip action SharedDocument

## 1. Mô tả vấn đề
Ngày đăng hiển thị chuỗi JSON `/Date(...)`; tooltip action bị lỗi mã hóa tiếng Việt.

## 2. Phân tích & quyết định
- JSON từ server có thể dùng Microsoft JSON date nên không thể render trực tiếp.
- Tooltip hard-code cũ mang mã hóa sai; gán lại theo `data-modal-id` sau mỗi lần DataTable render sẽ áp dụng được cho cả helper button chung.

## 5. Checklist
- [x] Format ngày đăng về `dd/MM/yyyy HH:mm`.
- [x] Gán tooltip UTF-8 cho Tải về, Chi tiết, Chỉnh sửa và Xóa.
- [x] Build Modules.Sys và kiểm tra file mirror WebApp.

---

# 2026-09-20 Lỗi: Chỉnh sửa Trạng thái và Quy trình bị lỗi 404 (DigitalSalesWorkflow)

## 1. Mô tả vấn đề
Tại `http://crm.git/Cate/DigitalSalesWorkflow`, khi người dùng bấm nút chỉnh sửa (icon cây bút) trên Trạng thái hoặc Quy trình thì bị văng lỗi 404 Not Found (`/Error/NotFound`).

## 2. Phân tích nguyên nhân gốc rễ (Root Cause Analysis)
1. **Frontend:**
   - Trong `_StatusList.cshtml` và `_ProcessList.cshtml`, thẻ bọc các nút hành động khai báo `onclick="event.stopPropagation();"`.
   - Cơ chế mở popup modal của hệ thống (`TSFramework.js`) bắt sự kiện qua event delegation `$(document).on("click", "[data-modal]", ...)`.
   - `event.stopPropagation()` chặn việc nổi bọt (bubbling) lên `document`, khiến trình xử lý modal không chạy và lệnh `return false;` không được gọi. Trình duyệt thực hiện điều hướng GET thông thường tới URL `/Cate/DigitalSalesWorkflow/EditStatus/{id}`.
2. **Backend:**
   - Trong `DigitalSalesWorkflowController.cs`, các Action GET mở modal (`EditStatus`, `EditProcess`, `EditProgress`, `AddStatus`, `AddProcess`, `AddProgress`) đều trang trí thuộc tính `[AjaxOnly]`.
   - Khi request GET thông thường từ trình duyệt gửi lên mà không có header `X-Requested-With: XMLHttpRequest`, `AjaxOnlyAttribute` kiểm tra `!Request.IsAjaxRequest()` và tự động redirect sang `/Error/NotFound` (lỗi 404).

## 3. Giải pháp & Quyết định kỹ thuật
- **Controller:** Bỏ `[AjaxOnly]` trên các Action `[HttpGet]` mở modal Form theo đúng chuẩn tham chiếu của `RM_BusinessOpportunityController` và `ProjectController`. Vẫn giữ nguyên `[ActionType]` phân quyền và `[AjaxOnly]`, `[ValidateAntiForgeryToken]` trên các Action POST.
- **View:** Bỏ `onclick="event.stopPropagation();"` trên thẻ bao `.status-actions` và `.process-actions`. Chuyển các nút Sửa/Xóa sang gọi hàm điều hướng modal chuyên trách: `DigitalSalesWorkflow.openEditStatus(...)`, `openEditProcess(...)`, `openEditProgress(...)`.
- **JavaScript:** Bổ sung các hàm `openEditStatus`, `openDeleteStatus`, `openEditProcess`, `openDeleteProcess`, `openEditProgress`, `openDeleteProgress`; kiểm tra `closest(".action-buttons")` trong `selectStatus` và `selectProcess` để tránh xung đột chọn dòng; bắt sự kiện click nút `#btnSave` và `#btnConfirm` trong modal footer để submit form mượt mà.
- **Biên dịch & Đồng bộ:** Biên dịch `Modules.Cate` (0 errors), đồng bộ Triple Mirroring sang `CenIT.Solution.TOC.WebApp` và `publish_source` với UTF-8 BOM.

## 4. Checklist
- [x] Sửa `DigitalSalesWorkflowController.cs`: bỏ `[AjaxOnly]` trên các Action GET modal.
- [x] Sửa `_StatusList.cshtml`, `_ProcessList.cshtml`, `_ProgressList.cshtml`: bỏ `event.stopPropagation()`, gắn `openEdit...` và `openDelete...`.
- [x] Sửa `DigitalSalesWorkflow.js`: thêm các hàm mở modal, chặn click va chạm chọn dòng, bind `#btnSave`/`#btnConfirm`.
- [x] Biên dịch `Modules.Cate.csproj` thành công (0 errors).
- [x] Đồng bộ Triple Mirroring sang `CenIT.Solution.TOC.WebApp` và `publish_source`.
- [x] Đảm bảo 100% tệp UTF-8 with BOM.
- [x] Kiểm thử tự động qua script PowerShell: GET trực tiếp và AJAX đều trả về response hợp lệ, không còn lỗi 404.

---

# 2026-09-20 Lỗi: Trùng lặp form submit khi thêm mới, cập nhật, xóa Trạng thái / Quy trình / Tiến trình (DigitalSalesWorkflow)

## 1. Mô tả vấn đề
Tại màn hình `http://crm.git/Cate/DigitalSalesWorkflow`:
- Khi Thêm mới Trạng thái / Quy trình / Tiến trình: Hiển thị đồng thời thông báo "Thêm mới thành công" và "Dữ liệu đã tồn tại".
- Khi Xóa: Hiển thị đồng thời thông báo "Xóa thành công" và "Dữ liệu không tồn tại".

## 2. Phân tích nguyên nhân gốc rễ (Root Cause Analysis)
1. Trong `DigitalSalesWorkflow.js`, việc gắn thêm 2 listener ủy quyền submit trên `document`:
   `$(document).on("click", ".modal #btnSave", ...)` và `$(document).on("click", ".modal #btnConfirm", ...)`
   đã gây xung đột trực tiếp với hàm `_initElement()` của `TSFramework.js` (hàm này vốn đã tự động gán listener submit lên tất cả `button[type='submit']` nằm ngoài form).
2. Khi người dùng click nút "Lưu" hoặc "Đồng ý", cả 2 listener đều kích hoạt, làm `$form.submit()` được gọi 2 lần liên tiếp.
3. `jquery.unobtrusive-ajax` gửi 2 request AJAX POST song song tới server:
   - Request 1 xử lý trước thành công -> trả về Success.
   - Request 2 xử lý sau vài mili-giây -> bản ghi đã được lưu/xóa -> server trả về lỗi `DataExisted` (-9) hoặc `DataNotExist`.
   - Giao diện nhận cả 2 kết quả và hiển thị cùng lúc 2 Toastr thông báo.

## 3. Giải pháp & Quyết định kỹ thuật
- **JavaScript (`DigitalSalesWorkflow.js`):**
  - Gỡ bỏ hoàn toàn 2 listener delegated thừa trên `document`.
  - Bổ sung `onFormBegin`: kiểm tra cờ `$form.data("submitting")`, ngắt request trùng và vô hiệu hóa nút submit (`prop("disabled", true)`).
  - Bổ sung `onFormComplete`: dọn dẹp cờ submitting và khôi phục nút submit.
  - Cải tiến selector active modal và phục hồi nút submit khi validation thất bại hoặc server trả lỗi.
- **View Modal:**
  - Bổ sung `OnBegin = "DigitalSalesWorkflow.onFormBegin"`, `OnComplete = "DigitalSalesWorkflow.onFormComplete"` trong `AjaxOptions` của `_StatusModal.cshtml`, `_ProcessModal.cshtml`, `_ProgressModal.cshtml`, `_DeleteConfirm.cshtml`.
  - Thêm lời gọi `_initElement()` ngay khi modal view được nạp.
- **Đồng bộ & Biên dịch:**
  - Triple Mirroring đồng bộ 3 nơi (`Modules.Cate`, `CenIT.Solution.TOC.WebApp`, `publish_source`).
  - 100% tệp UTF-8 with BOM.
  - Biên dịch MSBuild `Modules.Cate.csproj` thành công (0 errors).

## 4. Checklist
- [x] Gỡ bỏ listener delegated `.modal #btnSave` và `#btnConfirm` trong `DigitalSalesWorkflow.js`.
- [x] Bổ sung `onFormBegin` và `onFormComplete` chống double submission.
- [x] Cập nhật `AjaxOptions` trên 4 modal views.
- [x] Đồng bộ Triple Mirroring và UTF-8 BOM.
- [x] Biên dịch MSBuild thành công.
- [x] Kiểm thử 4 tầng test suite PASS 100%.


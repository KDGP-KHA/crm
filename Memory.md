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

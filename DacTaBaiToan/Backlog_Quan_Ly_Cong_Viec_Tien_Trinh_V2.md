# PRODUCT BACKLOG / TASK BREAKDOWN: QUẢN LÝ CÔNG VIỆC CON TRONG TIẾN TRÌNH (V2)

## 1. TỔNG QUAN (OVERVIEW)
Tài liệu phân tích và đặc tả backlog kỹ thuật cho phân hệ quản lý Công việc con (Sub-tasks) thuộc Tiến trình (Milestone/Process) trong Dự án hoặc Cơ hội bán hàng. 

Phiên bản cập nhật bổ sung tính năng **Báo cáo tiến độ/kết quả công việc (Task Reporting)** sử dụng trình soạn thảo Rich Text (CKEditor), đính kèm nhiều file, chuyển đổi trạng thái (loại trừ trạng thái hiện tại) và ghi vết toàn bộ vào Audit Log.

---

## 2. DANH SÁCH USER STORIES & TASKS (BACKLOG ITEMS)

### Epic: Quản lý Công việc con (Sub-tasks Management)

---

### [US-01] Thêm mới công việc con vào tiến trình
**User Story:**  
*Là một* Quản lý dự án / Phụ trách tiến trình,  
*Tôi muốn* tạo mới công việc con trong tiến trình,  
*Để* phân rã đầu việc cụ thể, chỉ định nhân sự và thiết lập deadline.

#### Tiêu chí chấp nhận (Acceptance Criteria - AC):
1. **Mã công việc (Task Code):**
   - Sinh tự động: `CV + YY + MM + XXXXXXX` (VD: `CV26090000001`).
   - Duy nhất (Unique constraint) trên toàn hệ thống.
2. **Form Thêm mới (Modal):**
   - **Tên công việc:** Textarea (cho phép xuống dòng, placeholder trực quan).
   - **Người thực hiện:** Dropdown chọn từ danh sách nhân sự tham gia dự án/cơ hội.
   - **Trạng thái:** Mặc định `Chưa thực hiện`. Tùy chọn: `Chưa thực hiện`, `Đang thực hiện`, `Đã xong`.
   - **Ngày bắt đầu & Deadline:** Datetimepicker.
   - **Số ngày thực hiện:** Input number ($\ge 1$).
   - **Ràng buộc:** Deadline công việc con $\le$ Deadline của Tiến trình cha. `Ngày bắt đầu <= Deadline`.
   - **Ghi chú / Kết quả ban đầu:** Textarea.
   - **File đính kèm:** Cho phép chọn/tải lên nhiều file (Multiple upload).
3. Sau khi lưu: Đóng modal, tự động nạp lại danh sách công việc trên lưới (Grid).

---

### [US-02] Cập nhật thông tin công việc con
**User Story:**  
*Là một* Người thực hiện hoặc Quản lý,  
*Tôi muốn* chỉnh sửa thông tin chi tiết của công việc,  
*Để* cập nhật đúng tiến độ và phân công khi có phát sinh.

#### Tiêu chí chấp nhận (Acceptance Criteria - AC):
1. Khi ở trạng thái `Chưa thực hiện` hoặc `Đang thực hiện`: Cho phép mở modal sửa và nạp đầy đủ dữ liệu cũ.
2. Khi ở trạng thái `Đã xong` (Hoàn thành):
   - Hệ thống tự động **Khóa (Read-only)**, không cho phép sửa trực tiếp.
   - Nút **"Mở khóa" (Unlock)**: Yêu cầu nhập **Lý do mở khóa** (bắt buộc). Ghi nhận người mở khóa, thời gian và lý do vào Audit Log.

---

### [US-03] [NEW] Báo cáo tiến độ & kết quả công việc (Task Progress Report)
**User Story:**  
*Là một* Người thực hiện công việc con,  
*Tôi muốn* viết báo cáo định kỳ/kết quả thực hiện, đính kèm chứng từ/sản phẩm và cập nhật trạng thái mới,  
*Để* quản lý nắm bắt tiến độ thực tế mà không làm mất vết lịch sử các lần báo cáo.

#### Tiêu chí chấp nhận (Acceptance Criteria - AC):
1. **Nút thao tác:** Bổ sung nút/icon action **"Báo cáo" (Report)** trên từng dòng công việc ở Grid và bên trong chi tiết công việc.
2. **Modal Báo cáo công việc gồm các thành phần:**
   - **Thông tin tóm tắt:** Hiển thị Mã CV, Tên CV, Trạng thái hiện tại (Read-only tag).
   - **Nội dung báo cáo:** 
     - Tích hợp trình soạn thảo WYSIWYG **CKEditor** (hỗ trợ định dạng văn bản, bold/italic, bullet list, chèn bảng, chèn ảnh inline nếu cần).
     - Trường bắt buộc nhập (Validation: không được để trống hoặc chỉ chứa khoảng trắng/thẻ HTML rỗng).
   - **Đính kèm tài liệu liên quan:** Cho phép kéo thả / upload nhiều file liên quan (PDF, Word, Excel, Image, Zip...).
   - **Cập nhật trạng thái mới:**
     - Dropdown chọn trạng thái chuyển tiếp.
     - **Quy tắc quan trọng:** Dropdown **KHÔNG hiển thị trạng thái hiện tại** của công việc (Ví dụ: Nếu đang là `Chưa thực hiện`, dropdown chỉ hiển thị `Đang thực hiện` và `Đã xong`. Nếu đang là `Đang thực hiện`, chỉ hiển thị `Chưa thực hiện` và `Đã xong`).
3. **Audit Log & Lưu vết:**
   - Khi bấm **"Gửi báo cáo"**: 
     - Lưu nội dung báo cáo và danh sách file đính kèm vào bảng `task_reports`.
     - Cập nhật trạng thái mới của công việc vào bảng `tasks`.
     - Ghi nhận một bản ghi Audit Log đặc biệt: Loại hành động `REPORT` (Gồm: Người báo cáo, Thời gian, Nội dung báo cáo tóm tắt/full, Danh sách file đính kèm, Trạng thái cũ -> Trạng thái mới).
     - Nếu trạng thái chuyển sang `Đã xong`: Tự động cập nhật `actual_completed_date = NOW()` và bật cờ `is_locked = true`.

---

### [US-04] Lịch sử thao tác (Audit Log)
**User Story:**  
*Là một* Giám sát / Quản lý dự án,  
*Tôi muốn* xem dòng thời gian toàn bộ thay đổi của công việc,  
*Để* minh bạch trách nhiệm và kiểm soát tiến độ.

#### Tiêu chí chấp nhận (Acceptance Criteria - AC):
1. Modal Timeline Audit Log hiển thị chi tiết theo thứ tự thời gian mới nhất:
   - **Thêm mới:** Người tạo, thời điểm tạo, thông tin khởi tạo.
   - **Cập nhật:** Thay đổi trường nào, `Giá trị cũ` -> `Giá trị mới`.
   - **Mở khóa:** Ai mở khóa, thời gian, lý do mở khóa.
   - **Báo cáo (Report):** 
     - Người gửi báo cáo, thời gian gửi.
     - Nội dung báo cáo (xem formatted HTML từ CKEditor).
     - File đính kèm đợt báo cáo (cho phép click tải xuống).
     - Thay đổi trạng thái: `[Trạng thái cũ] -> [Trạng thái mới]`.

---

### [US-05] Hiển thị lưới danh sách & Trạng thái xử lý hạn
**User Story:**  
*Là một* Thành viên theo dõi tiến trình,  
*Tôi muốn* nhìn thấy trạng thái thời hạn của các đầu việc trực quan trên lưới,  
*Để* nhận diện ngay công việc nào đang bị trễ hạn.

#### Tiêu chí chấp nhận (Acceptance Criteria - AC):
1. Badge màu sắc trực quan:
   - **CHƯA TỚI HẠN (Xanh dương / Xám):** Chưa xong và Ngày hiện tại $<$ Deadline.
   - **ĐÚNG HẠN (Xanh lá):** Đã xong và Ngày hoàn thành thực tế $\le$ Deadline.
   - **TRỄ HẠN (Đỏ):** (Chưa xong và Ngày hiện tại $>$ Deadline) HOẶC (Đã xong nhưng Ngày hoàn thành thực tế $>$ Deadline).
2. Action buttons trên dòng: `[Báo cáo]`, `[Chỉnh sửa]`, `[Lịch sử]`, `[Xóa]`.

---

### [US-06] Tối ưu hóa giao diện (UI/UX & Typography)
**User Story:**  
*Là một* Người dùng,  
*Tôi muốn* font chữ hài hòa, các modal hiển thị cân đối và responsive,  
*Để* thao tác thuận tiện, không bị rối mắt.

#### Tiêu chí chấp nhận (Acceptance Criteria - AC):
1. Chuẩn hóa font hệ thống: Inter / Roboto / Segoe UI.
2. Phân cấp cỡ chữ: Header 16-18px Bold, Label 13-14px Medium, Body/Input 13-14px Regular.
3. Đồng bộ giao diện modal Báo cáo với CKEditor (toolbar tinh gọn, chiều cao editor vừa vặn ~200-250px).

---

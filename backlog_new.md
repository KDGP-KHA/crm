# 📋 PRODUCT BACKLOG: CẢI TIẾN TOÀN DIỆN TIẾN TRÌNH & CÔNG VIỆC (DIGITAL SALES)

> **Tài liệu Backlog Kỹ thuật & Nghiệp vụ**  
> **Dự án:** CENIT TOC CRM - Phân hệ Kinh doanh Giải pháp số (`DigitalSales`)  
> **Tệp cấu hình:** `backlog_new.md` (Phiên bản v2 - Đã cập nhật theo góp ý chi tiết của Người Dùng)  
> **Trạng thái:** 🟡 *Chờ Người Dùng Xem Xét & Thống Nhất (TUYỆT ĐỐI KHÔNG THỰC HIỆN CODE)*  
> **Ngày cập nhật:** 22/09/2026  

---

## 📑 MỤC LỤC TỔNG QUAN

1. [Bối Cảnh & Kiến Trúc Phân Cấp Nhiệm Vụ](#1-bối-cảnh--kiến-trúc-phân-cấp-nhiệm-vụ)
2. [Bảng Phân Rã Toàn Diện 7 Hạng Mục Backlog (User Stories & Tasks)](#2-bảng-phân-rã-toàn-diện-7-hạng-mục-backlog-user-stories--tasks)
3. [Đặc Tả Chi Tiết Từng User Story](#3-đặc-tả-chi-tiết-từng-user-story)
   - [US-01: Tách Biệt Chức Năng Báo Cáo Kết Quả & Ghi Log Tương Tự Chỉnh Sửa](#us-01-tách-biệt-chức-năng-báo-cáo-kết-quả--ghi-log-tương-tự-chỉnh-sửa)
   - [US-02: Phân Quyền Nút Sửa Theo Người Tạo & Cập Nhật Ngày Hoàn Thành (>= Ngày Bắt Đầu)](#us-02-phân-quyền-nút-sửa-theo-người-tạo--cập-nhật-ngày-hoàn-thành--ngày-bắt-đầu)
   - [US-03: Tạo Công Việc Con Bên Trong Công Việc & Cải Tiến File Mẫu Import Excel](#us-03-tạo-công-việc-con-bên-trong-công-việc--cải-tiến-file-mẫu-import-excel)
   - [US-04: Phân Quyền Nút Xóa – Chỉ QTHT Mới Được Xóa Đầu Việc Đã Hoàn Thành](#us-04-phân-quyền-nút-xóa--chỉ-qtht-mới-được-xóa-đầu-việc-đã-hoàn-thành)
   - [US-05: Xem Trước (Preview) Tệp PDF Trực Tiếp Tương Tự Hình Ảnh](#us-05-xem-trước-preview-tệp-pdf-trực-tiếp-tương-tự-hình-ảnh)
   - [US-07: Chuẩn Hóa Cụm Thao Tác Thành Nút Dropdown Dấu Ba Chấm (...)](#us-07-chuẩn-hóa-cụm-thao-tác-thành-nút-dropdown-dấu-ba-chấm-)
   - [US-08: Cải Tiến Chức Năng Đổi Quy Trình](#us-08-cải-tiến-chức-năng-đổi-quy-trình-chỉ-đổi-quy-trình-không-add-thêm-tiến-trình-vào-checklist)
4. [Các Điểm Thiết Kế & Phương Án Cần Xác Nhận (Open Decisions)](#4-các-điểm-thiết-kế--phương-án-cần-xác-nhận-open-decisions)
5. [Kế Hoạch & Ma Trận Phân Quyền Tổng Hợp](#5-kế-hoạch--ma-trận-phân-quyền-tổng-hợp)

---

## 1. BỐI CẢNH & KIẾN TRÚC PHÂN CẤP NHIỆM VỤ

Trong phân hệ Quản lý hồ sơ Kinh doanh giải pháp số (`DigitalSales`), cây thực hiện công việc được cấu trúc phân cấp:
- **Cấp 1:** Trạng thái hồ sơ (`Status`)
- **Cấp 2:** Quy trình triển khai (`Process`)
- **Cấp 3:** Tiến trình thực hiện (`Progress` - `ParentID = null/0`)
- **Cấp 4:** Công việc / Nhiệm vụ (`Task` - `ParentID = TrackingID của tiến trình`)
- **Cấp 5:** Công việc con (`Subtask` - `ParentID = TrackingID của Task cha`)

Hệ thống cần cải tiến toàn diện 8 hạng mục nhằm:
1. **Minh bạch hóa luồng Báo cáo:** Tách riêng luồng nộp kết quả thực hiện, lưu vết lịch sử (Log) đầy đủ như khi chỉnh sửa.
2. **Siết chặt quyền Sửa đổi ban đầu:** Chỉ người trực tiếp tạo và QTHT mới được sửa thông tin cấu hình, đồng thời cho phép nhập Ngày hoàn thành (`>= Ngày bắt đầu`).
3. **Phân rã đa tầng linh hoạt:** Cho phép tạo việc con lồng nhau và cải tiến file Excel import theo chuẩn WBS Text (`1, 1.1, 1.2`) để nhận diện chính xác cấu trúc cha - con.
4. **Bảo vệ an toàn dữ liệu lịch sử:** Các đầu việc đã "Hoàn thành" chỉ duy nhất nhóm Quản trị hệ thống (QTHT) mới được phép xóa.
5. **Tiện ích hóa trải nghiệm tài liệu:** Xem trước trực tiếp tệp PDF ngay trên trình duyệt mà không cần tải file về máy.
6. **Tách biệt luồng Cập nhật trạng thái:** Phân quyền rõ ràng cho 3 đối tượng: Người được gán, Người tạo và Quản trị hệ thống (QTHT).
7. **Gọn gàng hóa giao diện (Clean UI):** Tự động gom cụm thao tác thành nút Dropdown ba chấm (`...`) khi có từ 2 chức năng trở lên, loại bỏ tình trạng tràn nút làm vỡ layout bảng.
8. **Cải tiến Đổi Quy Trình tinh gọn:** Chỉ đổi thông tin quy trình cho trạng thái, **tuyệt đối không tự động add thêm các tiến trình của quy trình mới vào checklist**.

---

## 2. BẢNG PHÂN RÃ TOÀN DIỆN 8 HẠNG MỤC BACKLOG (USER STORIES & TASKS)

| Mã Story | Tên Hạng Mục (Story Title) | Mức Độ Ưu Tiên | Độ Phức Tạp | Tệp / Thành Phần Ảnh Hưởng |
| :---: | :--- | :---: | :---: | :--- |
| **US-01** | Tách biệt chức năng Báo cáo tiến độ trên Tiến trình/Task/Subtask & Ghi Log như chỉnh sửa | **ĐÃ HOÀN THÀNH** | Trung bình | `_DetailTracking.cshtml`, `_TrackingReportModal.cshtml`, `DigitalSalesDetail.js`, `DigitalSalesController.cs`, `RM_DigitalSalesTrackingLog` |
| **US-02** | Phân quyền nút Sửa (Người tạo & QTHT) & Cho phép cập nhật Ngày Hoàn thành (`>= Ngày Bắt đầu`) | **ĐÃ HOÀN THÀNH** | Trung bình | `_DetailTracking.cshtml`, `_TrackingForm.cshtml`, `_TodoModal.cshtml`, `DigitalSalesDetail.js`, `DigitalSalesController.cs` |
| **US-03** | Tạo công việc con bên trong công việc & Cải tiến tệp Excel mẫu Import WBS Text | **Rất Cao (P0)** | Cao | `_DetailTracking.cshtml`, `_TodoModal.cshtml`, `_ImportTodoModal.cshtml`, `DigitalSalesController.cs`, `XLWorkbook` |
| **US-04** | Phân quyền nút Xóa: Người tạo xóa việc chưa xong; Việc ĐÃ HOÀN THÀNH chỉ QTHT mới được xóa | **ĐÃ HOÀN THÀNH** | Trung bình | `_DetailTracking.cshtml`, `DigitalSalesDetail.js`, `DigitalSalesController.cs` |
| **US-05** | Xem trước (Preview) file PDF trực tiếp tương tự như hình ảnh | **ĐÃ HOÀN THÀNH** | Trung bình | `_DetailAttachments.cshtml`, `_DetailDiscussions.cshtml`, `DigitalSalesDetail.js`, `DigitalSalesController.cs` |
| **US-07** | Chuẩn hóa cụm thao tác: Nếu từ 2 chức năng trở lên thì gộp thành nút Dropdown dấu ba chấm (`...`) | **ĐÃ HOÀN THÀNH** | Trung bình | `_DetailTracking.cshtml`, `_DetailTracking.css`, `DigitalSalesDetail.js` |
| **US-08** | Cải tiến chức năng Đổi Quy Trình: Chỉ đổi quy trình, KHÔNG tự động add thêm tiến trình vào checklist | **ĐÃ HOÀN THÀNH** | Thấp - Trung bình | `_ChangeProcessModal.cshtml`, `DigitalSalesController.cs`, `RM_DigitalSalesBiz.cs`, `RM_DigitalSalesCache.cs` |

---

## 3. ĐẶC TẢ CHI TIẾT TỪNG USER STORY

### US-01: Tách Biệt Chức Năng Báo Cáo Kết Quả & Ghi Log Tương Tự Chỉnh Sửa

#### 1. Mô tả bài toán
- Hiện tại, để cập nhật kết quả và tệp đính kèm minh chứng, người dùng phải mở form Chỉnh sửa. Việc này gây rủi ro sửa nhầm các thông tin quản lý khác (tên việc, người thực hiện, thời hạn...).
- Cần tách hẳn thành một chức năng riêng gọi là **"Báo cáo kết quả"**, hiển thị ở **toàn bộ các cấp**: Tiến trình (Progress), Công việc (Task), Công việc con (Subtask).
- **Yêu cầu then chốt:** Thao tác Báo cáo kết quả **bắt buộc phải ghi nhận vào Nhật ký lịch sử thao tác (`RM_DigitalSalesTrackingLog`)** tương tự như khi Chỉnh sửa, bao gồm: người báo cáo, nội dung báo cáo cũ/mới, danh sách file đính kèm bổ sung, thời gian báo cáo.

#### 2. Tiêu chí chấp nhận (Acceptance Criteria - AC)
- [ ] **AC 1.1 (Phạm vi & Phân quyền hiển thị nút Báo cáo):**
  - Chức năng Báo cáo kết quả xuất hiện trên tất cả các cấp trong cây theo dõi:
    + Cấp 3: Tiến trình (`tree-progress-row`).
    + Cấp 4: Công việc (`tree-todo-row`).
    + Cấp 5: Công việc con (`tree-subtask-row`).
  - **Phân quyền thực hiện:** Chỉ có **(1) Người được phân công (`AssignedUser`)**, **(2) Người tạo (`Creator`)**, và **(3) Nhóm QTHT** mới thấy và được thực hiện báo cáo. Thành viên hồ sơ thông thường (`Member`) **KHÔNG** thấy chức năng này.
- [ ] **AC 1.2 (Giao diện Modal Báo cáo `_TrackingReportModal.cshtml`):**
  - **Tiêu đề:** *"Báo cáo kết quả thực hiện: [Mã việc] - [Tên việc]"*.
  - **Thông tin tham chiếu (Chỉ đọc - Readonly):** Tên đầu việc, Người phụ trách, Hạn hoàn thành.
  - **Nhập liệu (Chỉ 2 nội dung cốt lõi):**
    1. *Nội dung báo cáo kết quả:* TextArea / RichText (`ResultNote`).
    2. *Tệp đính kèm minh chứng:* Cho phép upload nhiều file (PDF, Word, Excel, Hình ảnh, Zip...), đồng thời liệt kê danh sách các tệp đã báo cáo trước đó kèm link tải và link xem trước.
- [ ] **AC 1.3 (Ghi nhận Log Lịch Sử Đầy Đủ):**
  - Khi lưu báo cáo thành công, Controller gọi `RM_DigitalSalesBiz.SaveTrackingLog()`:
    + Hành động: `"Báo cáo kết quả"` / `"Cập nhật kết quả thực hiện"`.
    + Chi tiết thay đổi: Ghi rõ nội dung báo cáo mới, tệp đính kèm mới tải lên.
    + Khi người dùng bấm nút *"Xem Log"* (`fa-history`), thông tin báo cáo kết quả này phải xuất hiện chuẩn xác trong danh sách lịch sử.

---

### US-02: Phân Quyền Nút Sửa (Người Tạo & QTHT) & Cập Nhật Ngày Hoàn Thành (>= Ngày Bắt Đầu)

#### 1. Mô tả bài toán
1. Chỉ người trực tiếp tạo ra đầu việc HOẶC người thuộc nhóm Quản trị hệ thống (QTHT) mới được thấy nút Chỉnh sửa và có quyền chỉnh sửa các thông tin ban đầu của đầu việc đó.
2. Trong form Chỉnh sửa, cho phép chọn/cập nhật Ngày Hoàn Thành (`CompletedDate`), với ràng buộc: Ngày Hoàn Thành không được nhỏ hơn Ngày Bắt Đầu.

#### 2. Tiêu chí chấp nhận (Acceptance Criteria - AC)
- [x] **AC 2.1 (Phân quyền nút Sửa trên UI):**
  - Tại mỗi dòng tiến trình / task / subtask:
    `canEditTask = isQTHT || string.Equals(task.CreatedBy, User.UserName, StringComparison.OrdinalIgnoreCase);`
  - Chỉ hiển thị chức năng "Chỉnh sửa" nếu `canEditTask == true` (Người tạo hoặc QTHT). Thành viên khác không phải người tạo/QTHT sẽ **không nhìn thấy** nút Chỉnh sửa.
- [x] **AC 2.2 (Kiểm soát Backend Chống Bypass):**
  - Tại các Action `SaveTracking` / `SaveTodo`: Khi sửa bản ghi (`TrackingID > 0`), hệ thống kiểm tra `isQTHT || oldTask.CreatedBy == User.UserName`. Nếu không khớp, từ chối và trả về lỗi: *"Chỉ người tạo hoặc Quản trị hệ thống (QTHT) mới có quyền chỉnh sửa đầu việc này!"*.
- [x] **AC 2.3 (Cập nhật Ngày Hoàn Thành & Bắt lỗi Ràng Buộc):**
  - Mở khóa trường **Ngày hoàn thành (`CompletedDate`)** thành điều khiển `date-picker`.
  - **Ràng buộc:** `CompletedDate >= StartDate`.
  - *Client Validation:* Nếu chọn Ngày hoàn thành nhỏ hơn Ngày bắt đầu -> Báo viền đỏ, hiển thị thông báo lỗi ngay dưới ô nhập và ngăn chặn bấm Lưu.
  - *Server Validation:* `if (model.CompletedDate.HasValue && model.CompletedDate.Value.Date < model.StartDate.Date)` -> Chặn lưu và trả về thông báo lỗi.

---

### US-03: Tạo Công Việc Con Bên Trong Công Việc & Cải Tiến File Mẫu Import Excel

#### 1. Mô tả bài toán
1. Xây dựng chức năng cho phép tạo Công việc con (Subtask) bên trong một Công việc (Task).
2. Tệp Excel mẫu Import công việc phải được thiết kế lại để thể hiện rõ ràng công việc con nào trực thuộc công việc nào.

#### 2. Tiêu chí chấp nhận (Acceptance Criteria - AC)
- [ ] **AC 3.1 (Tạo Công Việc Con trên Web):**
  - Có nút hành động `(+)` hoặc tùy chọn *"Tạo công việc con"* bên trong menu của Công việc.
  - Modal tạo việc con tự động nhận diện và hiển thị thông tin Công việc cha (`ParentID = Task.TrackingID`).
  - Cây danh mục hiển thị thụt lề cấp 5 rõ ràng với ký hiệu nhánh phân cấp (`└─ └─`).
- [ ] **AC 3.2 (Cải tiến File Excel Mẫu Import – Đầy Đủ Cột Mã Tiến Trình & STT Phân Cấp WBS Text):**
  - **Mục tiêu:** Đảm bảo hệ thống nhận diện **đồng thời 2 quan hệ**:
    1. Công việc thuộc **Tiến trình nào** trong quy trình (qua cột *Mã tiến trình*).
    2. Công việc con thuộc **Công việc nào** bên trong tiến trình đó (qua cột *STT Phân cấp WBS* dạng Text `@`).
  - **ĐẶC TẢ KỸ THUẬT QUAN TRỌNG (ĐỊNH DẠNG TEXT `@`):**
    + Cả 2 cột: **Mã tiến trình** (Cột B) và **STT Phân cấp** (Cột C) **bắt buộc định dạng Text (Chuỗi ký tự - `@`)** trên tệp Excel mẫu để triệt tiêu 100% rủi ro Excel tự động nhận diện sai dấu chấm thành số thập phân hoặc ngày tháng.
    + Dòng header hướng dẫn: *"Mã tiến trình tra cứu ở Sheet 2; STT Phân cấp định dạng Text: 1 (việc chính), 1.1 (việc con), 1.1.1 (subtask)"*.
  - **Cấu trúc cột Sheet 1 (Dữ liệu import):**
    | STT | Mã tiến trình (*) | STT Phân Cấp (Text `@`) | Tên công việc / Việc con (*) | Người thực hiện | Ngày bắt đầu (*) | Số ngày (*) | Hạn xử lý | Ghi chú |
    | :---: | :---: | :---: | :--- | :--- | :---: | :---: | :---: | :--- |
    | 1 | `PR26090001` | `1` | Khảo sát hiện trạng kỹ thuật hạ tầng | tan.tran | 22/09/2026 | 3 | 25/09/2026 | Việc chính của PR01 |
    | 2 | `PR26090001` | `1.1` | Kiểm tra phòng máy chủ | nguyen.van.a | 22/09/2026 | 1 | 23/09/2026 | Việc con của mục 1 |
    | 3 | `PR26090001` | `1.2` | Đo kiểm suy hao sợi quang | nguyen.van.b | 23/09/2026 | 2 | 25/09/2026 | Việc con của mục 1 |
    | 4 | `PR26090001` | `1.2.1` | Lập biên bản đo kiểm | nguyen.van.b | 24/09/2026 | 1 | 25/09/2026 | Việc con của mục 1.2 |
    | 5 | `PR26090002` | `1` | Xây dựng giải pháp kỹ thuật tổng thể | tan.tran | 25/09/2026 | 5 | 30/09/2026 | Việc chính của PR02 |
    | 6 | `PR26090002` | `1.1` | Vẽ sơ đồ topo mạng | le.thi.c | 25/09/2026 | 2 | 27/09/2026 | Việc con của mục 1 (PR02) |
  - **Sheet 2 (Danh sách Tiến trình tra cứu):** Liệt kê đầy đủ Danh sách Tiến trình của Quy trình (Mã tiến trình, Tên tiến trình, Người thực hiện, Ngày bắt đầu, Hạn tối đa) để người dùng copy mã tiến trình sang Sheet 1.
- [ ] **AC 3.3 (Xử lý Import Server-side Thông Minh):**
  - Action `PreviewImportTodo` và `ConfirmImportTodo`:
    + Đọc cột **Mã tiến trình**: Tìm tiến trình cha trực tiếp trong CSDL theo `TrackingCode` (hoặc `TrackingID`).
    + Đọc cột **STT Phân cấp** dưới dạng String nguyên bản: `cell.GetString().Trim()`.
    + Trong phạm vi từng Mã tiến trình:
      * Nếu STT là `"1"`, `"2"`: Là **Task chính** của tiến trình (`ParentID = Tiến trình.TrackingID`).
      * Nếu STT có dạng `"1.1"`: Tự động gán làm việc con của Task `"1"` cùng tiến trình (`ParentID = Task 1.TrackingID`).
      * Nếu STT có dạng `"1.2.1"`: Tự động gán làm việc con của Task `"1.2"` cùng tiến trình.
    + Thẩm định ràng buộc thời hạn: Thời hạn của việc con không được vượt quá hạn của việc cha và hạn tối đa của tiến trình.
    + Lưu chính xác quan hệ phân cấp đa tầng vào CSDL `RM_DigitalSalesTracking`.

---

### US-04: Phân Quyền Nút Xóa – Người Tạo Xóa Việc Chưa Xong, Duy Nhất QTHT Xóa Việc Đã Xong

#### 1. Mô tả bài toán
- **Quy tắc xóa việc CHƯA HOÀN THÀNH (`Status != 3`):** Chỉ **Người tạo đầu việc** (`CreatedBy == CurrentUserName`) HOẶC **Quản trị hệ thống (QTHT)** mới xuất hiện nút Xóa và có quyền xóa. Thành viên hồ sơ thông thường KHÔNG có quyền xóa.
- **Quy tắc đặc thù (Đầu việc ĐÃ HOÀN THÀNH - `Status == 3`):** Khóa chặt toàn bộ: **DUY NHẤT người thuộc nhóm QTHT mới xuất hiện nút Xóa và thao tác Xóa được.** Người tạo cũng KHÔNG được xóa khi đã hoàn thành.

#### 2. Tiêu chí chấp nhận (Acceptance Criteria - AC)
- [x] **AC 4.1 (Điều kiện hiển thị nút Xóa trên giao diện):**
  - Xét một đầu việc bất kỳ (`task`):
    - **Trường hợp A (Đã Hoàn Thành - `task.Status == 3`):**
      - Chỉ hiển thị nút Xóa khi `isQTHT == true`.
      - Nếu `!isQTHT`: Tuyệt đối **KHÔNG hiển thị nút Xóa** (kể cả là người tạo ra đầu việc đó).
    - **Trường hợp B (Chưa Hoàn Thành - `task.Status != 3`):**
      - Chỉ hiển thị nút Xóa khi `isQTHT || string.Equals(task.CreatedBy, User.UserName, StringComparison.OrdinalIgnoreCase)`.
      - Các thành viên khác: Tuyệt đối **KHÔNG hiển thị nút Xóa**.
- [x] **AC 4.2 (Bảo vệ Backend Controller):**
  - Tại Action xử lý xóa `DeleteTrackingTask` / `DeleteTodoItem`:
    ```csharp
    var task = _salesCache.GetTrackingByID(trackingId);
    if (task != null)
    {
        bool isQTHT = IsUserQTHT(CurrentUserName, CurrentUserId);
        bool isCreator = string.Equals(task.CreatedBy, CurrentUserName, StringComparison.OrdinalIgnoreCase);

        if (task.Status == 3)
        {
            // Nếu đã hoàn thành: DUY NHẤT QTHT mới được xóa
            if (!isQTHT)
            {
                return Json(new { 
                    status = false, 
                    message = "Đầu việc này đã hoàn thành! Chỉ thành viên thuộc nhóm Quản trị hệ thống (QTHT) mới có quyền xóa." 
                });
            }
        }
        else
        {
            // Nếu chưa hoàn thành: Người tạo hoặc QTHT mới được xóa
            if (!isQTHT && !isCreator)
            {
                return Json(new { 
                    status = false, 
                    message = "Bạn không phải người tạo hoặc QTHT nên không có quyền xóa đầu việc này!" 
                });
            }
        }
    }
    ```

---

### US-05: Xem Trước (Preview) Tệp PDF Trực Tiếp Tương Tự Hình Ảnh

#### 1. Mô tả bài toán
Hiện tại tệp PDF chỉ có nút "Tải về", người dùng buộc phải tải file về máy rồi mở xem bên ngoài trình duyệt. Cần cung cấp khả năng xem trước (preview) tệp PDF trực tiếp ngay trong giao diện web tương tự như khi xem ảnh.

#### 2. Tiêu chí chấp nhận (Acceptance Criteria - AC)
- [x] **AC 5.1 (Modal Preview PDF Chuẩn Giao Diện):**
  - Xây dựng modal `#modalPdfPreview` với thanh tiêu đề hiển thị tên tệp, nút *"Mở tab mới"*, nút *"Tải về"* và nút *"Đóng"*.
  - Nội dung modal nhúng thẻ `<iframe>` tải trực tiếp stream từ action `/Cate/DigitalSales/ViewAttachment?filePath=...`.
  - Sử dụng viewer mặc định của trình duyệt (hỗ trợ phóng to, thu nhỏ, cuộn trang, in ấn).
- [x] **AC 5.2 (Tích hợp Nút Xem Trước PDF tại Các Vị Trí):**
  - Tab Tệp đính kèm (`_DetailAttachments.cshtml`): Bổ sung nút icon con mắt `fa-eye` trên các card file `.pdf`, click thumbnail box mở trực tiếp preview PDF.
  - Tab Thảo luận (`_DetailDiscussions.cshtml`): Cho phép click xem trước trực tiếp các file PDF đính kèm.
  - Modal Báo cáo kết quả (`_TrackingReportModal.cshtml`), Form chỉnh sửa (`_TrackingForm.cshtml`), Lịch sử thay đổi (`_TrackingLogsModal.cshtml`), và Modal chi tiết Timeline trạng thái (`_StatusTimelineDetailModal.cshtml`): Bổ sung nút icon con mắt `fa-eye` xem trước trực tiếp PDF.

---

### US-07: Chuẩn Hóa Cụm Thao Tác Thành Nút Dropdown Dấu Ba Chấm (...)

#### 1. Mô tả bài toán
Hiện tại trên mỗi dòng tiến trình / task / subtask có rất nhiều chức năng tiềm năng:
1. Xem Log lịch sử (`fa-history`)
2. Báo cáo kết quả (`fa-clipboard-check`)
3. Cập nhật trạng thái (`fa-tasks` / `fa-toggle-on`)
4. Thêm việc con (`fa-plus`)
5. Chỉnh sửa (`fa-pencil-alt`)
6. Mở khóa (`fa-unlock-alt`)
7. Xóa (`fa-trash-alt`)
Nếu hiển thị dàn trải tất cả các nút icon ra hàng ngang sẽ gây rối mắt, chiếm diện tích và dễ vỡ giao diện bảng dữ liệu.

#### 2. Tiêu chí chấp nhận (Acceptance Criteria - AC)
- [ ] **AC 7.1 (Quy tắc Gom Nút Thông Minh):**
  - **Trường hợp có 1 chức năng hợp lệ:** Hiển thị trực tiếp nút icon đơn lẻ đó để bấm nhanh 1 chạm.
  - **Trường hợp có TỪ 2 CHỨC NĂNG HỢP LỆ TRỞ LÊN:**
    - Tự động gom toàn bộ vào một **Nút bấm Dropdown dấu ba chấm** (`...` - icon `fa-ellipsis-v` hoặc `fa-ellipsis-h`).
    - Nút ba chấm có giao diện tròn/bo góc gọn gàng: `btn btn-xs btn-white border-1 brc-grey-m2 text-secondary-d2`.
- [ ] **AC 7.2 (Giao diện Menu Dropdown khi Bấm Vào):**
  - Menu xổ xuống (`dropdown-menu dropdown-menu-right`) hiển thị danh sách các chức năng kèm icon màu sắc và tên chữ rõ ràng:
    + 📝 *Báo cáo kết quả* (`fa-clipboard-check text-purple`)
    + 🔄 *Cập nhật trạng thái* (`fa-exchange-alt text-info`)
    + ➕ *Thêm công việc con* (`fa-plus text-success`)
    + ✏️ *Chỉnh sửa* (`fa-pencil-alt text-primary`)
    + 📜 *Xem lịch sử log* (`fa-history text-secondary`)
    + 🔓 *Mở khóa* (`fa-unlock-alt text-warning`)
    + 🗑️ *Xóa* (`fa-trash-alt text-danger`)
  - Chống kẹt z-index hoặc bị che khuất bởi thanh cuộn bảng (`table-responsive`): Sử dụng Bootstrap Dropdown hoặc điều chỉnh positioning phù hợp.

---

### US-08: Cải Tiến Chức Năng Đổi Quy Trình (Chỉ Đổi Quy Trình, KHÔNG Add Thêm Tiến Trình Vào Checklist)

#### 1. Mô tả bài toán
- **Hiện trạng bất cập:** Khi người dùng sử dụng chức năng "Đổi quy trình" (`openChangeProcessModal` ➔ `SaveChangeProcess` ➔ `ChangeProcessOfStatus`), hệ thống hiện tại đang tự động đọc danh mục tiến trình mẫu của quy trình mới (`GetProgressesByProcess(newProcessId)`) và **tự động chèn thêm hàng loạt các tiến trình đó vào checklist**, đồng thời xóa/làm xáo trộn các tiến trình cũ.
- **Yêu cầu cải tiến nghiệp vụ:** Thao tác tự động add thêm tiến trình là **không cần thiết** và gây dư thừa dữ liệu. Người dùng chỉ muốn **đổi lại thông tin quy trình áp dụng** cho giai đoạn/trạng thái đó, giữ nguyên trạng thái danh sách công việc hiện tại.

#### 2. Tiêu chí chấp nhận (Acceptance Criteria - AC)
- [x] **AC 8.1 (Giao diện Modal Đổi Quy Trình `_ChangeProcessModal.cshtml`):**
  - Giữ nguyên giao diện cho phép chọn quy trình mới từ danh sách các quy trình khả dụng của trạng thái hiện tại.
  - Hiển thị thông báo hướng dẫn rõ ràng: *"Hệ thống sẽ cập nhật quy trình áp dụng mới, giữ nguyên vẹn toàn bộ danh sách tiến trình và công việc hiện có trong hồ sơ."*
- [x] **AC 8.2 (Cải tiến Logic Xử Lý Backend `ChangeProcessOfStatus` & `SaveChangeProcess`):**
  - **LOẠI BỎ HOÀN TOÀN** đoạn code gọi `GetProgressesByProcess()` và vòng lặp tự động `SaveTracking(pg)` thêm tiến trình mẫu vào checklist.
  - **LOẠI BỎ** việc tự động xóa các tiến trình cũ (`DeleteTracking`).
  - **Hành vi mới chuẩn mực:**
    1. Cập nhật mã quy trình mới (`ProcessID = newProcessId`) cho trạng thái / timeline tương ứng.
    2. Cập nhật `ProcessID = newProcessId` cho các tiến trình hiện có thuộc trạng thái đó (để nhóm hiển thị đúng tiêu đề quy trình mới trên cây checklist).
    3. Toàn bộ tiến trình, công việc con, kết quả báo cáo và tệp đính kèm đã có được **bảo toàn nguyên vẹn 100%**.
    4. Ghi nhận lịch sử log thao tác: *"Đổi quy trình thực hiện từ [Quy trình cũ] sang [Quy trình mới]"*.

---

## 4. QUYẾT ĐỊNH THIẾT KẾ ĐÃ THỐNG NHẤT VỚI NGƯỜI DÙNG (APPROVED DECISIONS)

### ✅ Quyết định Thiết Kế File Excel Mẫu Import Công Việc Con (US-03)
- **Lựa chọn chính thức:** **Kết hợp Cột "Mã tiến trình (*)" và Cột "STT Phân cấp WBS" dạng Text (`1`, `1.1`, `1.2`, `1.2.1`)**.
- **Giải quyết trọn vẹn 2 bài toán định danh:**
  1. **Xác định thuộc Tiến trình nào:** Thông qua cột **`Mã tiến trình (*)`** (Cột B, tra cứu mã tại Sheet 2 của quy trình).
  2. **Xác định công việc con thuộc Công việc nào:** Thông qua cột **`STT Phân cấp (Text `@`)`** (Cột C, nhập `1` cho việc chính, `1.1`, `1.2` cho việc con của mục 1).
- **Quy chuẩn kỹ thuật cốt lõi theo yêu cầu của Người Dùng:**
  1. **Bắt buộc định dạng Cột Mã tiến trình và Cột STT Phân Cấp là TEXT (`@`):** 
     - Khi xuất file mẫu bằng C# (ClosedXML), gán thuộc tính: `ws.Column(2).Style.NumberFormat.Format = "@"; ws.Column(3).Style.NumberFormat.Format = "@";`.
     - Điều này đảm bảo khi người dùng gõ mã hoặc gõ `1.1`, `1.2`, `1.10` trên Excel thì Excel giữ nguyên định dạng chuỗi ký tự (Text), **hoàn toàn không bị tự động chuyển đổi sai lệch** thành số thập phân (do khác biệt dấu chấm/phẩy giữa các phiên bản Windows) hoặc bị biến thành ngày tháng (Date).
  2. **Cách đọc dữ liệu Backend an toàn:**
     - Sử dụng hàm đọc chuỗi nguyên bản: `cell.GetString().Trim()`.
     - Phân tích mã tiến trình để xác định tiến trình cha, và phân tích dấu chấm của STT phân cấp để tự động xây dựng cây phân cấp cha - con bên trong tiến trình đó.
  3. **Độ tiện lợi đạt tối đa:** Người dùng dễ dàng copy mã tiến trình kéo xuống cho cả nhóm việc, và gõ số thứ tự `1`, `1.1`, `1.2` trực quan, không cần chuyển tab tra cứu nhiều lần.

---

## 5. KẾ HOẠCH & MA TRẬN PHÂN QUYỀN TỔNG HỢP

### 📋 BẢNG MA TRẬN PHÂN QUYỀN TỔNG HỢP

| Chức năng | Người được phân công (`AssignedUser`) | Người tạo đầu việc (`Creator`) | Thành viên hồ sơ (`Member`) | Quản trị hệ thống (`QTHT`) |
| :--- | :---: | :---: | :---: | :---: |
| **1. Báo cáo kết quả (US-01)** | ✅ Có | ✅ Có | ❌ **Không** | ✅ Có |
| **2. Xem Log lịch sử** | ✅ Có | ✅ Có | ✅ Có | ✅ Có |
| **3. Chỉnh sửa thông tin (US-02)** | ❌ Không | ✅ **Có** | ❌ **Không** | ✅ **Có** |
| **4. Xóa việc CHƯA hoàn thành (US-04)** | ❌ Không | ✅ **Có** | ❌ **Không** | ✅ **Có** |
| **5. Xóa việc ĐÃ HOÀN THÀNH (US-04)** | ❌ **Cấm** | ❌ **Cấm** | ❌ **Cấm** | ✅ **Duy nhất QTHT** |
| **6. Xem trước PDF (US-05)** | ✅ Có | ✅ Có | ✅ Có | ✅ Có |
| **7. Đổi quy trình (US-08)** | ❌ Không | ✅ **Có** | ❌ **Không** | ✅ **Có** |

---

> ⚠️ **CAM KẾT TUÂN THỦ:**  
> Toàn bộ nội dung trên đã được cập nhật chính xác theo toàn bộ phản hồi của Người Dùng. **Tác nhân AI TUYỆT ĐỐI KHÔNG THỰC HIỆN BẤT KỲ DÒNG CODE NÀO**, chờ đợi sự kiểm tra và chỉ đạo tiếp theo từ Quý người dùng!

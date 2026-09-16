# HƯỚNG DẪN KIỂM THỬ VÀ TÀI LIỆU HƯỚNG DẪN THỰC HIỆN
## PHÂN HỆ TIẾN TRÌNH & CHECKLIST DỰ ÁN (DIGITAL SALES 360)

---

## MỤC LỤC
1. [TỔNG QUAN PHÂN HỆ TIẾN TRÌNH & CHECKLIST](#1-tổng-quan-phân-hệ-tiến-trình--checklist)
2. [KẾT QUẢ RÀ SOÁT ĐẶC TẢ VÀ CẬP NHẬT MÃ NGUỒN](#2-kết-quả-rà-soát-đặc-tả-và-cập-nhật-mã-nguồn)
3. [PHẦN I: HƯỚNG DẪN VÀ KỊCH BẢN KIỂM THỬ (TEST GUIDE & TEST CASES)](#phần-i-hướng-dẫn-và-kịch-bản-kiểm-thử-test-guide--test-cases)
   - [TC-01: Kiểm thử Tự động khởi tạo Trạng thái & Quy trình (US-01)](#tc-01-tự-động-khởi-tạo-trạng-thái--quy-trình)
   - [TC-02: Kiểm thử Chuyển trạng thái & Tự động hoàn thành chu kỳ trước (US-02)](#tc-02-tự-động-hoàn-thành-chu-kỳ-trước-khi-chuyển-trạng-thái)
   - [TC-03: Kiểm thử Giao diện Cây 4 Tầng & Chống mất dòng Quy trình khi xóa hết tiến trình (US-03)](#tc-03-giao-diện-cây-4-tầng--bảo-toàn-dòng-trạng-thái-khi-xóa-tiến-trình)
   - [TC-04: Kiểm thử Tải file mẫu & Import Tiến trình từ Excel (US-04)](#tc-04-tải-file-mẫu--import-tiến-trình-từ-excel)
   - [TC-05: Kiểm thử Tải file mẫu & Import Công việc con (Todo List) Excel 2 Sheet (US-05)](#tc-05-tải-file-mẫu--import-công-việc-con-excel-2-sheet)
   - [TC-06: Kiểm thử Thao tác Tiến trình & Audit Log (US-06 & US-07)](#tc-06-thao-tác-tiến-trình--audit-log)
   - [TC-07: Kiểm thử Nghiệp vụ Mở khóa Tiến trình đã Hoàn thành (US-08)](#tc-07-mở-khóa-tiến-trình-đã-hoàn-thành)
   - [TC-08: Kiểm thử Quản lý Công việc con / Todo List (US-09)](#tc-08-quản-lý-công-việc-con--todo-list)
   - [TC-09: Kiểm thử Đổi Quy trình thực hiện cho Trạng thái (US-01/US-03)](#tc-09-đổi-quy-trình-thực-hiện-cho-trạng-thái)
   - [TC-10: Kiểm thử Tự động hoàn thành Công việc con khi Tiến trình Hoàn thành & Lưu Log thao tác (US-10)](#tc-10-tự-động-hoàn-thành-công-việc-con-khi-tiến-trình-hoàn-thành--lưu-log-thao-tác)
    - [TC-11: Kiểm thử Bảo toàn Trạng thái không có Tiến trình trong lịch sử khi Chuyển trạng thái (US-02)](#tc-11-bảo-toàn-trạng-thái-không-có-tiến-trình-trong-lịch-sử-khi-chuyển-trạng-thái)
    - [TC-12: Kiểm thử Cột Hoàn thành hiển thị Người hoàn thành & Ngày giờ (US-11)](#tc-12-kiểm-thử-cột-hoàn-thành-hiển-thị-người-hoàn-thành--ngày-giờ)
4. [PHẦN II: TÀI LIỆU HƯỚNG DẪN THỰC HIỆN / SỬ DỤNG CHO NGƯỜI DÙNG](#phần-ii-tài-liệu-hướng-dẫn-thực-hiện--sử-dụng-cho-người-dùng)
   - [1. Xem và Điều hướng cây Checklist 360](#1-xem-và-điều-hướng-cây-checklist-360)
   - [2. Chuyển trạng thái và khởi tạo quy trình](#2-chuyển-trạng-thái-và-khởi-tạo-quy-trình)
   - [3. Thêm mới và quản lý tiến trình thủ công](#3-thêm-mới-và-quản-lý-tiến-trình-thủ-công)
   - [4. Import hàng loạt tiến trình từ file Excel](#4-import-hàng-loạt-tiến-trình-từ-file-excel)
   - [5. Import hàng loạt công việc con (Todo) từ file Excel](#5-import-hàng-loạt-công-việc-con-todo-từ-file-excel)
   - [6. Báo cáo hoàn thành và Mở khóa cập nhật tiến trình](#6-báo-cáo-hoàn-thành-và-mở-khóa-cập-nhật-tiến-trình)
   - [7. Tra cứu lịch sử thao tác (Audit Log)](#7-tra-cứu-lịch-sử-thao-tác-audit-log)
   - [8. Theo dõi thông tin Hoàn thành trực tiếp trên bảng Checklist](#8-theo-dõi-thông-tin-hoàn-thành-trực-tiếp-trên-bảng-checklist)

---

## 1. TỔNG QUAN PHÂN HỆ TIẾN TRÌNH & CHECKLIST

Hệ thống **Tiến trình & Checklist** trong module **Digital Sales (Cơ hội & Dự án 360)** cung cấp giải pháp quản trị toàn diện vòng đời cơ hội bán hàng và dự án công nghệ:
- **Mô hình Cây phân cấp 4 tầng (Tree Grid 4 Levels):**
  - **Cấp 1 - Giai đoạn / Trạng thái:** Đại diện cho các bước trong phễu bán hàng (1. Chưa nắm bắt $\rightarrow$ 2. Tiếp cận $\rightarrow$ 3. Đang tư vấn $\rightarrow$ ... $\rightarrow$ 7. Ký kết). Hỗ trợ hiển thị nhiều chu kỳ nếu trạng thái xuất hiện lặp lại.
  - **Cấp 2 - Quy trình thực hiện (Process):** Mỗi trạng thái gắn với một hoặc nhiều quy trình công việc chuẩn. Có biểu tượng chỉnh sửa (cây bút chì) và badge *"Có thể đổi quy trình"* khi có từ 2 quy trình trở lên.
  - **Cấp 3 - Tiến trình công việc (Milestones / Progress):** Các đầu việc chính trong quy trình, có mã tiến trình tự động sinh (`PRyymmxxxxxx`), người thực hiện, thời gian bắt đầu, số ngày, hạn chót, trạng thái thực hiện, báo cáo kết quả (CKEditor) và tệp đính kèm.
  - **Cấp 4 - Công việc con (Sub-tasks / Todo list):** Các bước chi tiết của tiến trình, có checkbox đánh dấu hoàn thành nhanh, thời hạn chót không được vượt quá thời hạn tiến trình cha.

---

## 2. KẾT QUẢ RÀ SOÁT ĐẶC TẢ VÀ CẬP NHẬT MÃ NGUỒN

Đối chiếu giữa tài liệu đặc tả `TienTrinh va Checklist.docx`, tài liệu backlog `Backlog_TienTrinh_va_Checklist.md` với mã nguồn hệ thống hiện tại:

| STT | Hạng mục nghiệp vụ | Yêu cầu đặc tả | Hiện trạng trước rà soát | Giải pháp đã khắc phục & Chuẩn hóa | Trạng thái |
| :---: | :--- | :--- | :--- | :--- | :--- | :---: |
| 1 | **Bảo toàn dòng Trạng thái & Quy trình** | Khi hồ sơ mới tạo hoặc khi xóa hết tiến trình, KHÔNG ĐƯỢC làm mất dòng Trạng thái và dòng Quy trình (P58-P61, P71). Phải hiển thị thông báo "Quy trình chưa có tiến trình nào" kèm nút Thêm tiến trình & Import tiến trình. | Khi xóa hết tiến trình, query DB trả về rỗng làm view rơi vào block rỗng generic, làm biến mất dòng Trạng thái & Quy trình. | Bổ sung `EnsureCurrentStatusTrackingPlaceholder` trong `RM_DigitalSalesBiz.cs`. Khi danh sách rỗng, tự động inject placeholder của trạng thái hiện tại giúp cây luôn render dòng Trạng thái và Quy trình chuẩn. | **ĐÃ XỬ LÝ** |
| 2 | **Cỡ chữ & Font layout tiến trình** | Text số lượng tiến trình tăng 2px (P35). Các cột thông tin tiến trình (mã, người thực hiện, ngày, trạng thái) đồng nhất cỡ chữ với tên tiến trình (P37). | Badge số lượng tiến trình dùng class `text-80` (~11-12px) quá nhỏ. | Cập nhật CSS `.tree-process-task-count` lên `0.98rem` (~14.5px), thêm `.tree-status-task-count` `0.88rem`, đồng bộ `.font-progress-cell` = `0.92rem` bằng font `.tree-progress-title`. | **ĐÃ XỬ LÝ** |
| 3 | **Stored Procedure Unicode** | Dữ liệu trạng thái và quy trình trong SP phải hiển thị tiếng Việt có dấu chuẩn xác 100%. | SP `RM_DigitalSalesTracking_GetBySalesID` chứa chuỗi mã hóa sai (Mojibake: `ChÆ°a thá»±c hiá»‡n`, `HoÃ n thÃ nh`...). | Đã chạy ALTER SP với mã nguồn UTF-8 chuẩn hóa toàn bộ các nhãn: *Checklist tiến trình dự án, Chưa thực hiện, Đang thực hiện, Hoàn thành, Quá hạn, Khác*. | **ĐÃ XỬ LÝ** |
| 4 | **Nghiệp vụ Mở khóa tiến trình (US-08)** | Nút Mở khóa cho tiến trình đã hoàn thành, cho phép cập nhật lại nội dung. Trạng thái giữ nguyên Hoàn thành, không cho chọn lại trạng thái, không có nút Xóa. Phải ghi log lý do mở khóa. | Có nút Mở khóa nhưng form submit từng báo lỗi 500 do thiếu action hoặc lỗi binding context. | Đã hoàn thiện Modal mở khóa, Action `SubmitUnlockProgress` ghi log ActivityType = 5 kèm lý do mở khóa, sau đó client tự động mở Modal Edit với trạng thái readonly "Hoàn thành". | **ĐÃ XỬ LÝ** |
| 5 | **Import Tiến trình & Todo List** | Hỗ trợ tải file mẫu Excel (Tiến trình 1 sheet, Todo list 2 sheet), preview kiểm tra hợp lệ, hiển thị cột lỗi, nút Xóa dữ liệu và Xác nhận lưu. | Đã có action controller và view modal nhưng cần hướng dẫn test chi tiết. | Đã hoàn thiện toàn diện, tích hợp ClosedXML sinh file Excel mẫu chuẩn đẹp, preview phân tích màu sắc trực quan. | **ĐÃ XỬ LÝ** |
| 6 | **Audit Log khi Chuyển trạng thái** | Toàn bộ tiến trình và công việc con của trạng thái cũ tự động hoàn thành, ghi log User, thời gian và trạng thái mới. | Đã xây dựng trong `ChangeStatus` nhưng cần xác thực luồng dữ liệu DB. | Xác nhận hoạt động chuẩn xác qua SP `RM_DigitalSalesTracking_UpdateStatus` và bảng `RM_DigitalSalesActivity`. | **ĐÃ XỬ LÝ** |
| 7 | **Tự động hoàn thành công việc con khi Tiến trình Hoàn thành (US-10)** | Khi Tiến trình cập nhật Hoàn thành, các công việc con chưa hoàn thành phải chuyển Hoàn thành và lưu log đầy đủ. | Trước đây chỉ cập nhật tiến trình cha, các công việc con chưa làm vẫn giữ nguyên trạng thái cũ. | Đã cập nhật SP `RM_DigitalSalesTracking_UpdateStatus` và `RM_DigitalSalesTracking_Save` tự động cascade `Status = 3` cho các việc con, đồng thời tự động ghi nhận Activity Log (`ActivityType = 3`) cho từng việc con và tiến trình cha. | **ĐÃ XỬ LÝ** |
| 8 | **Cột Hoàn thành trên bảng Checklist (US-11)** | Bổ sung thêm 1 Cột Hoàn thành gồm tên người Hoàn thành và Thời gian - Ngày hoàn thành. | Chưa có cột Hoàn thành trên bảng Checklist, phải mở Log mới xem được ai hoàn thành. | Bổ sung cột Hoàn thành (`th` 160px) giữa cột Trạng thái và Thao tác. Hiển thị Tên người hoàn thành (in đậm) và Ngày giờ (`dd/MM/yyyy HH:mm`) kèm icon đồng hồ xanh lá. Thêm trường `CompletedBy`, `CompletedByName` trong DB và SP `RM_DigitalSalesTracking_GetBySalesID`. Cập nhật colspan bảng từ 8/7 lên 9/8. | **ĐÃ XỬ LÝ** |

---

## PHẦN I: HƯỚNG DẪN VÀ KỊCH BẢN KIỂM THỬ (TEST GUIDE & TEST CASES)

### TC-01: Tự động khởi tạo Trạng thái & Quy trình
- **Mục tiêu:** Xác minh khi tạo mới Cơ hội ở trạng thái mặc định "Chưa nắm bắt" (hoặc bất kỳ trạng thái nào), hệ thống tự động gán Quy trình mặc định và khởi tạo danh sách tiến trình chuẩn.
- **Tiền điều kiện:** Đã cấu hình Quy trình và Tiến trình trong module Cấu hình quy trình (Workflow).
- **Các bước thực hiện:**
  1. Vào menu **Cơ hội & Dự án** $\rightarrow$ Bấm nút **+ Thêm mới**.
  2. Nhập Tiêu đề cơ hội, chọn Khách hàng, chọn Trạng thái là "1. Chưa nắm bắt".
  3. Bấm **Lưu**.
  4. Mở màn hình Chi tiết 360 của cơ hội vừa tạo $\rightarrow$ Chọn tab **Tiến trình & Checklist**.
- **Kết quả mong đợi (Pass Criteria):**
  - Cây hiển thị Cấp 1: Giai đoạn "1. Chưa nắm bắt".
  - Cấp 2 hiển thị: "QUY TRÌNH: [TÊN QUY TRÌNH MẶC ĐỊNH]".
  - Cấp 3 hiển thị đầy đủ các đầu mục tiến trình mẫu đã cấu hình sẵn trong quy trình.
  - Header bảng có badge tổng số tiến trình: `0/N` (0 hoàn thành trên N công việc).
  - Không có bất kỳ lỗi Console JavaScript nào.

---

### TC-02: Tự động hoàn thành chu kỳ trước khi Chuyển trạng thái
- **Mục tiêu:** Xác minh nghiệp vụ: Khi chuyển sang trạng thái mới, toàn bộ tiến trình và công việc con của trạng thái trước đó đều tự động đánh dấu Hoàn thành kèm Audit log.
- **Các bước thực hiện:**
  1. Tại màn hình Chi tiết cơ hội, quan sát trạng thái hiện tại (ví dụ: đang có 3 tiến trình ở trạng thái "Chưa làm" hoặc "Đang làm").
  2. Bấm nút **Chuyển trạng thái** trên thanh công cụ đầu trang.
  3. Trong modal Chuyển trạng thái: Chọn Trạng thái mới (ví dụ: "2. Tiếp cận"), nhập Ghi chú chuyển trạng thái, đính kèm tệp nếu có.
  4. Chọn Quy trình thực hiện cho trạng thái mới $\rightarrow$ Bấm **Xác nhận**.
  5. Xem lại tab **Tiến trình & Checklist** và tab **Lịch sử hoạt động (Activity)**.
- **Kết quả mong đợi:**
  - Toàn bộ tiến trình và việc nhỏ của trạng thái cũ chuyển sang trạng thái **Hoàn thành** (Badge xanh lá, Checkbox được tick).
  - Bấm vào icon **Lịch sử (Log thao tác)** của tiến trình cũ: Thấy dòng log *"Tự động hoàn thành khi chuyển trạng thái sang [Tên trạng thái mới]"*, hiển thị chính xác tên người dùng và thời gian bấm xác nhận.
  - Cây hiển thị thêm nhánh Cấp 1 của Trạng thái mới và Cấp 2 của Quy trình mới với các tiến trình mới được khởi tạo ở trạng thái "Chưa làm".

---

### TC-03: Giao diện Cây 4 Tầng & Bảo toàn dòng Trạng thái khi xóa tiến trình
- **Mục tiêu:** Đảm bảo khi xóa hết toàn bộ tiến trình trong một quy trình, dòng Trạng thái và dòng Quy trình **KHÔNG BỊ MẤT**.
- **Các bước thực hiện:**
  1. Trong tab **Tiến trình & Checklist**, tìm đến quy trình của trạng thái hiện tại.
  2. Bấm nút icon **Thùng rác (Xóa)** trên từng dòng tiến trình để xóa hết toàn bộ tiến trình của quy trình đó.
  3. Xác nhận xóa trên modal cảnh báo.
  4. Quan sát giao diện bảng sau khi xóa xong.
- **Kết quả mong đợi:**
  - Dòng **Trạng thái** (Cấp 1) và dòng **Quy trình** (Cấp 2) VẪN HIỂN THỊ NGUYÊN VẸN.
  - Bên dưới dòng Quy trình, hiển thị dòng thông báo nền sáng viền xám:
    `"Quy trình [Tên quy trình] chưa có tiến trình nào."`
  - Hiển thị nút **+ Thêm tiến trình ngay** (màu xanh lá) và nút **Import tiến trình** (màu xanh dương).
  - Trên thanh tiêu đề dòng Quy trình: Nút **Import công việc** tự động ẩn đi (vì chưa có tiến trình cha để import công việc con).
  - Badge số lượng tiến trình hiển thị: `0 tiến trình` với cỡ font to rõ (`0.98rem`).

---

### TC-04: Tải file mẫu & Import Tiến trình từ Excel
- **Mục tiêu:** Xác minh luồng tải file Excel mẫu, điền dữ liệu, upload preview kiểm tra lỗi và xác nhận lưu tiến trình vào hệ thống.
- **Các bước thực hiện:**
  1. Trên dòng Quy trình cần import, bấm nút **Import tiến trình**.
  2. Trong modal hiện ra, bấm vào liên kết **Tải file mẫu Excel**.
  3. Mở file Excel vừa tải về (`Mau_Import_TienTrinh_...xlsx`), quan sát các cột: *STT, Tên tiến trình (*), Người thực hiện, Ngày bắt đầu (*), Số ngày thực hiện (*), Ghi chú*.
  4. Thêm 3 dòng dữ liệu: 2 dòng chuẩn và 1 dòng cố tình bỏ trống *Tên tiến trình* để test bắt lỗi.
  5. Lưu file và chọn file vào ô upload trong modal $\rightarrow$ Bấm **Tải lên & Phân tích**.
  6. Quan sát bảng dữ liệu Preview:
     - Dòng hợp lệ hiển thị badge xanh lá *"Hợp lệ"*.
     - Dòng thiếu tên tiến trình hiển thị badge đỏ *"Lỗi"* và cột Thông tin lỗi ghi rõ *"Tên tiến trình không được để trống"*.
  7. Bấm nút **Xóa dữ liệu** $\rightarrow$ Bảng preview được xóa sạch để chọn lại file.
  8. Sửa lại dòng lỗi trong file Excel, tải lên lại $\rightarrow$ Bấm nút **Xác nhận Import**.
- **Kết quả mong đợi:**
  - Hệ thống thông báo Import thành công số lượng bản ghi tương ứng.
  - Modal tự đóng, cây Checklist tự động làm mới và xuất hiện các tiến trình vừa import với mã tiến trình được sinh tự động.

---

### TC-05: Tải file mẫu & Import Công việc con (Excel 2 Sheet)
- **Mục tiêu:** Xác minh chức năng import công việc con vào đúng tiến trình cha thông qua Mã tiến trình tra cứu ở Sheet 2.
- **Các bước thực hiện:**
  1. Trên dòng Quy trình đã có tiến trình, bấm nút **Import công việc**.
  2. Bấm **Tải file mẫu Excel (2 Sheet)**.
  3. Mở file Excel:
     - **Sheet 1 ("Du lieu Cong viec"):** Có các cột: *STT, Mã tiến trình (*), Tên công việc con (*), Người thực hiện, Ngày bắt đầu (*), Hạn xử lý (Deadline) (*), Ghi chú*.
     - **Sheet 2 ("DS Tien trinh tra cuu"):** Danh sách sẵn các tiến trình đang có của quy trình này (Mã tiến trình, Tên, Người thực hiện, Hạn chót...).
  4. Copy Mã tiến trình từ Sheet 2 dán vào cột *Mã tiến trình (*)* của Sheet 1, nhập Tên việc con, Ngày bắt đầu, Hạn xử lý $\le$ Hạn chót của tiến trình cha.
  5. Thêm 1 dòng thử nghiệm có Hạn xử lý lớn hơn hạn chót của tiến trình cha hoặc Mã tiến trình sai.
  6. Tải file lên và bấm **Tải lên & Phân tích**.
- **Kết quả mong đợi:**
  - Hệ thống phân tích đối chiếu mã tiến trình cha:
    - Nếu mã tiến trình không tồn tại: Báo lỗi *"Mã tiến trình không tồn tại trong quy trình này"*.
    - Nếu hạn xử lý vượt quá hạn tiến trình cha: Cảnh báo thời gian.
  - Khi dữ liệu chuẩn, bấm **Xác nhận Import**: Toàn bộ công việc con được lưu thành công vào cây phân cấp dưới tiến trình tương ứng.

---

### TC-06: Thao tác Tiến trình & Audit Log
- **Mục tiêu:** Xác minh khả năng xem Log thao tác, sửa tiến trình qua trình soạn thảo CKEditor và đính kèm nhiều file.
- **Các bước thực hiện:**
  1. Tại dòng tiến trình đang ở trạng thái "Chưa làm" hoặc "Đang làm", bấm icon **Cây bút chì (Chỉnh sửa)**.
  2. Kiểm tra giao diện modal:
     - Cột *Mã tiến trình*: Readonly, có icon barcode.
     - Cột *Nội dung thực hiện*: Hiển thị trình soạn thảo CKEditor đầy đủ công cụ định dạng.
     - Cột *Tệp đính kèm*: Cho phép chọn nhiều tệp tin cùng lúc (ảnh, docx, pdf, zip).
  3. Nhập nội dung có in đậm, màu sắc, đính kèm 2 file $\rightarrow$ Bấm **Lưu thay đổi**.
  4. Sau khi lưu, bấm icon **Lịch sử (fa-history)** ở cột Thao tác của tiến trình đó.
- **Kết quả mong đợi:**
  - Modal Lịch sử mở ra hiển thị danh sách dòng thời gian (Timeline) các lần thao tác: ai sửa, vào lúc nào, nội dung thay đổi là gì, danh sách tệp đã đính kèm.
  - Định dạng HTML từ CKEditor hiển thị đẹp mắt, không vỡ layout, không dính thẻ mã nguồn thô.

---

### TC-07: Mở khóa Tiến trình đã Hoàn thành
- **Mục tiêu:** Xác minh quy trình mở khóa tiến trình đã hoàn thành để cập nhật bổ sung nội dung mà không làm sai lệch trạng thái Hoàn thành.
- **Các bước thực hiện:**
  1. Tìm một tiến trình có trạng thái **Hoàn thành** (Status = 3).
  2. Quan sát cột Thao tác: **KHÔNG CÓ nút Sửa và KHÔNG CÓ nút Xóa**. Chỉ có icon **Log thao tác** và icon **Ổ khóa mở (Mở khóa tiến trình)**.
  3. Bấm icon **Mở khóa tiến trình**.
  4. Modal *Xác nhận mở khóa tiến trình* hiển thị:
     - Để trống ô *Lý do mở khóa* $\rightarrow$ Bấm Xác nhận: Hệ thống hiển thị viền đỏ cảnh báo và thông báo yêu cầu nhập lý do.
     - Nhập lý do: *"Bổ sung biên bản nghiệm thu đợt 1"* $\rightarrow$ Bấm **Xác nhận mở khóa**.
- **Kết quả mong đợi:**
  - Hệ thống ghi nhận Audit log vào hệ thống với nội dung: *"Mở khóa tiến trình: [Tên tiến trình] | Lý do mở khóa: Bổ sung biên bản nghiệm thu đợt 1"*.
  - Modal Mở khóa đóng lại, hệ thống **TỰ ĐỘNG MỞ MODAL CHỈNH SỬA TIẾN TRÌNH**.
  - Trong modal Chỉnh sửa:
    - Ô Trạng thái bị khóa readonly: Hiển thị badge xanh *"Hoàn thành"* kèm ghi chú *"Tiến trình đã hoàn thành, không thể thay đổi trạng thái"*.
    - Cho phép người dùng chỉnh sửa Nội dung thực hiện (CKEditor), đính kèm thêm tệp, điều chỉnh ghi chú.
  - Bấm **Lưu thay đổi** $\rightarrow$ Tiến trình lưu thành công và VẪN GIỮ NGUYÊN TRẠNG THÁI HOÀN THÀNH.
  - Bấm nút **Log thao tác**: Thấy đầy đủ dòng log mở khóa và dòng log cập nhật nội dung sau khi mở khóa.

---

### TC-08: Kiểm thử Quản lý Công việc con / Todo List
- **Mục tiêu:** Kiểm tra thêm, sửa, xóa và đánh dấu hoàn thành trực tiếp công việc con trên cây.
- **Các bước thực hiện:**
  1. Bấm vào dấu **(+) màu cam** nằm ngay sau tên của một tiến trình $\rightarrow$ Modal Thêm công việc con mở ra.
  2. Nhập Tên việc con, Người thực hiện, Ngày bắt đầu, Hạn chót (Deadline) $\le$ Deadline tiến trình cha $\rightarrow$ Bấm Lưu.
  3. Quan sát dòng công việc con xuất hiện thụt đầu dòng (Cấp 4) có ký hiệu nhánh `└─`.
  4. Tick vào checkbox tròn trước tên việc con:
     - Hệ thống gửi AJAX xác nhận hoàn thành, checkbox chuyển sang màu xanh, tên việc con gạch ngang nhẹ hoặc đổi màu hoàn thành.
     - Badge tổng số việc con trên dòng tiến trình cập nhật lại.
  5. Thử sửa việc con, xóa việc con bằng các icon thao tác tương ứng trên dòng.
- **Kết quả mong đợi (Pass Criteria):**
  - Giao diện dòng công việc con hiển thị chuẩn mực và đồng bộ 100% với dòng tiến trình:
    - **Cột Tên công việc:** Thụt lề `└─`, checkbox tròn, tên việc font chữ chuẩn to rõ (`0.92rem`), hoàn toàn không có các nút text inline (`[Báo cáo]`, `[Xác nhận]`, `[Mở khóa]`, `[Log]`) chèn vào cột.
    - **Cột Mã:** Hiển thị Badge viền xanh chuẩn `CVyymmxxxxxx` đồng bộ hoàn toàn với mã `PRyymmxxxxxx` của tiến trình.
    - **Cột Người thực hiện, Bắt đầu, Hạn chót:** Áp dụng cỡ chữ chuẩn `0.92rem` (`font-progress-cell`).
    - **Cột Tổng ngày:** Hiển thị Badge số ngày xử lý (`N ngày`) thay vì hiển thị dấu gạch ngang (`—`).
    - **Cột Trạng thái:** Hiển thị Badge trạng thái chuẩn (`font-bold`, to rõ đồng bộ với tiến trình).
    - **Cột Thao tác:** Nhóm icon thao tác chuẩn ở cột Thao tác cuối cùng gồm `[Log thao tác]`, `[Chỉnh sửa]`, `[Xóa]` (hoặc `[Mở khóa]` khi trạng thái là Hoàn thành, tự động ẩn nút Xóa khi Hoàn thành).

---

### TC-09: Đổi Quy trình thực hiện cho Trạng thái
- **Mục tiêu:** Kiểm tra tính năng đổi quy trình khi một trạng thái có từ 2 quy trình trở lên (P64-P67).
- **Các bước thực hiện:**
  1. Chọn cơ hội đang ở trạng thái có $\ge 2$ quy trình (ví dụ trạng thái có cấu hình 2 quy trình song song hoặc phương án thay thế).
  2. Quan sát dòng Quy trình: Xuất hiện icon **Cây bút chì** cạnh tên quy trình và badge *"Có thể đổi quy trình"*.
  3. Bấm vào icon bút chì hoặc badge $\rightarrow$ Modal Đổi quy trình hiển thị.
  4. Chọn Quy trình mới $\rightarrow$ Bấm Lưu.
- **Kết quả mong đợi:**
  - Quy trình được cập nhật sang quy trình mới.
  - Cây Checklist làm mới hiển thị tên Quy trình mới và các tiến trình tương ứng của quy trình mới đó.

---

### TC-10: Tự động hoàn thành Công việc con khi Tiến trình Hoàn thành & Lưu Log thao tác
- **Mục tiêu:** Xác minh khi một Tiến trình cha chuyển sang trạng thái **Hoàn thành**, tất cả các công việc con chưa hoàn thành (Chưa làm, Đang làm) của tiến trình đó đều tự động chuyển sang **Hoàn thành** và sinh Activity Log đầy đủ.
- **Các bước thực hiện:**
  1. Chọn một tiến trình có ít nhất 2 công việc con (ví dụ 1 việc đang ở trạng thái *Chưa làm*, 1 việc đang ở trạng thái *Đang làm*).
  2. Thực hiện cập nhật Tiến trình cha sang trạng thái **Hoàn thành** bằng một trong các cách:
     - Cách A: Nhấp vào nút **Báo cáo** trên dòng tiến trình $\rightarrow$ Tick chọn checkbox *Hoàn thành* $\rightarrow$ Nhập nội dung kết quả $\rightarrow$ Bấm **Xác nhận báo cáo**.
     - Cách B: Nhấp vào nút **Xác nhận** (`actionConfirmTracking`) hoặc tick checkbox trước tên tiến trình cha.
     - Cách C: Bấm **Chỉnh sửa** tiến trình $\rightarrow$ Chọn trạng thái *Hoàn thành* $\rightarrow$ Bấm **Lưu**.
- **Kết quả mong đợi:**
  - **Trạng thái trên giao diện:**
    - Cả Tiến trình cha và tất cả Công việc con đều tự động chuyển sang badge xanh **Hoàn thành**.
    - Các checkbox của công việc con tự động tick chọn hoàn thành.
    - Cột Ngày hoàn thành / Ngày chót được cập nhật theo thời gian thực tế.
  - **Nhật ký thao tác (Audit Log):**
    - Nhấp nút **Log** trên dòng Tiến trình cha:
      - Thẻ hồ sơ hiển thị bảng thống kê: *Danh sách công việc con ($N/N$ hoàn thành)*.
      - Dòng thời gian hiển thị sự kiện hoàn thành của tiến trình cha (ghi chú số lượng việc con tự động hoàn thành).
      - Dòng thời gian hiển thị chi tiết từng sự kiện hoàn thành của các công việc con có badge cam `[Công việc con]`: *Tự động hoàn thành công việc: [Tên việc] (theo tiến trình cha: [Tên tiến trình])*.
    - Nhấp nút **Log** trên từng dòng Công việc con:
      - Modal hiển thị thông tin công việc con và dòng lịch sử: *Tự động hoàn thành công việc: [Tên việc] (theo tiến trình cha: [Tên tiến trình])*.

---

### TC-11: Bảo toàn Trạng thái không có Tiến trình trong lịch sử khi Chuyển trạng thái (US-02)
- **Mục tiêu:** Xác minh khi hồ sơ chuyển sang một trạng thái không có tiến trình nào (0 tiến trình), sau đó tiếp tục chuyển sang trạng thái mới có tiến trình, dòng trạng thái không có tiến trình trước đó **VẪN ĐƯỢC BẢO TOÀN NGUYÊN VẸN TRÊN CÂY CHECKLIST**, không bị biến mất.
- **Các bước thực hiện:**
  1. Giả sử hồ sơ đang ở trạng thái A (có các tiến trình đã thực hiện hoặc hoàn thành).
  2. Bấm **Chuyển trạng thái** sang trạng thái B (ví dụ: "Đang tiếp cận").
  3. Trong modal chuyển trạng thái: Xóa hết toàn bộ tiến trình gợi ý (hoặc chọn quy trình chưa có tiến trình mẫu) để trạng thái B có **0 tiến trình**. Bấm **Xác nhận**.
  4. Quan sát tab Checklist: Trạng thái B xuất hiện trên cây với Quy trình tương ứng, hiển thị thông báo *"Quy trình [...] chưa có tiến trình nào"* và badge `0 tiến trình`.
  5. Tiếp tục bấm **Chuyển trạng thái** sang trạng thái C (ví dụ: "Giai đoạn Hình thành dự án").
  6. Trong modal chuyển trạng thái: Giữ nguyên danh sách tiến trình mẫu của trạng thái C (ví dụ có 2 tiến trình) $\rightarrow$ Bấm **Xác nhận**.
  7. Quan sát lại cây Checklist của hồ sơ sau khi cập nhật.
- **Kết quả mong đợi (Pass Criteria):**
  - Cây Checklist hiển thị đầy đủ cả 3 giai đoạn theo đúng thứ tự thời gian:
    1. **Khối trạng thái A:** Chứa các tiến trình của giai đoạn A đã hoàn thành.
    2. **Khối trạng thái B:** VẪN ĐƯỢC GIỮ LẠI NGUYÊN VẸN TRÊN CÂY với tiêu đề trạng thái B, dòng Quy trình và thông báo *"Quy trình [...] chưa có tiến trình nào"*, tuyệt đối không bị biến mất.
    3. **Khối trạng thái C:** Chứa các tiến trình mới của giai đoạn C ở trạng thái *Chưa làm*.
  - Header tổng số công việc (`badgeChecklistSummary`) tính toán chính xác tổng số tiến trình thực tế của toàn bộ các giai đoạn (loại trừ placeholder rỗng).
  - Không phát sinh bất kỳ lỗi Console JavaScript nào.

---


### TC-12: Kiểm thử Cột Hoàn thành hiển thị Người hoàn thành & Ngày giờ (US-11)
- **Mục tiêu:** Xác minh cột "Hoàn thành" hiển thị đúng vị trí (giữa Trạng thái và Thao tác) và thể hiện chính xác Tên người hoàn thành cùng Thời gian - Ngày hoàn thành của cả Tiến trình và Công việc con.
- **Các bước thực hiện:**
  1. Mở màn hình Chi tiết 360 của cơ hội (ví dụ cơ hội số 97) $ightarrow$ Chọn tab **Tiến trình & Checklist**.
  2. Quan sát cấu trúc bảng: Giữa cột **Trạng thái** và cột **Thao tác** có xuất hiện cột **Hoàn thành** (độ rộng 160px, căn giữa).
  3. Quan sát các dòng Tiến trình và Công việc con:
     - Đối với các dòng có trạng thái **Hoàn thành** (Status = 3):
       - Dòng trên: Hiển thị Họ và tên người thực hiện hoàn thành in đậm (ví dụ: **Trần Duy Tân**).
       - Dòng dưới: Hiển thị icon đồng hồ xanh lá (`fa-clock text-success`) và thời gian thực hiện hoàn thành theo định dạng `dd/MM/yyyy HH:mm` (ví dụ: `15/09/2026 20:31`).
     - Đối với các dòng có trạng thái **Chưa làm** hoặc **Đang làm**:
       - Cột Hoàn thành hiển thị ký tự gạch ngang xám (`—`).
  4. Thực hiện hoàn thành một công việc con hoặc tiến trình đang ở trạng thái Chưa làm:
     - Bấm tick checkbox hoặc chọn Chỉnh sửa $ightarrow$ Đổi trạng thái sang Hoàn thành $ightarrow$ Bấm Lưu.
  5. Quan sát lại dòng vừa thao tác: Cột Hoàn thành ngay lập tức hiển thị tên tài khoản người dùng hiện tại và thời gian ngày giờ vừa xác nhận.
- **Kết quả mong đợi:**
  - Cột Hoàn thành hiển thị đúng vị trí, layout bảng ngay ngắn, các dòng gom nhóm trạng thái và quy trình trải đều `colspan` 9 cột chuẩn xác.
  - Thông tin người và ngày giờ hiển thị đầy đủ, không bị rỗng (`null` hay undefined).
  - Không có bất kỳ lỗi Console JavaScript nào.

---

## PHẦN II: TÀI LIỆU HƯỚNG DẪN THỰC HIỆN / SỬ DỤNG CHO NGƯỜI DÙNG

### 1. Xem và Điều hướng cây Checklist 360
- Khi truy cập vào màn hình **Chi tiết Cơ hội / Dự án**, chọn thẻ tab **Tiến trình & Checklist**.
- Giao diện cung cấp thanh công cụ:
  - **Mở rộng tất cả (`Expand All`):** Mở bung toàn bộ 4 cấp độ để xem toàn cảnh chi tiết.
  - **Thu gọn tất cả (`Collapse All`):** Thu gọn về cấp Giai đoạn để nhìn tổng quan phễu.
  - **Click vào từng dòng Trạng thái / Quy trình:** Để đóng hoặc mở riêng từng nhánh mong muốn.

### 2. Chuyển trạng thái và khởi tạo quy trình
- Khi cơ hội tiến triển sang giai đoạn tiếp theo (ví dụ từ *Chưa nắm bắt* sang *Tiếp cận*, hoặc sang *Lập phương án*):
  - Bấm nút **Chuyển trạng thái** ở góc trên bên phải màn hình.
  - Chọn trạng thái đích, nhập nội dung báo cáo và đính kèm file biên bản/tài liệu liên quan.
  - Chọn Quy trình sẽ áp dụng cho giai đoạn mới. Có thể xem trước danh sách tiến trình sẽ sinh ra.
  - Bấm **Xác nhận**: Hệ thống sẽ tự động chốt hoàn thành toàn bộ công việc của giai đoạn trước và mở ra các đầu việc của giai đoạn mới.

### 3. Thêm mới và quản lý tiến trình thủ công
- Để bổ sung thêm một đầu việc phát sinh vào quy trình hiện tại:
  - Bấm nút **+ Thêm tiến trình** trên thanh tiêu đề của dòng Quy trình (hoặc nút **+ Thêm tiến trình ngay** nếu quy trình đang trống).
  - Điền tên tiến trình, phân công người chịu trách nhiệm, ngày bắt đầu và số ngày thực hiện dự kiến.
  - Hệ thống tự động tính toán ra Hạn chót (Deadline = Bắt đầu + Số ngày).
  - Bấm **Lưu** để đưa tiến trình vào danh sách.

### 4. Import hàng loạt tiến trình từ file Excel
- Phù hợp khi dự án có sẵn danh sách kế hoạch triển khai chi tiết:
  1. Bấm **Import tiến trình** trên dòng Quy trình.
  2. Tải file mẫu Excel về máy tính.
  3. Nhập danh sách tiến trình theo đúng định dạng cột:
     - Không thay đổi tên cột hoặc cấu trúc file.
     - Cột Ngày bắt đầu nhập định dạng `dd/MM/yyyy`.
     - Cột Số ngày là số nguyên dương $\ge 1$.
  4. Đính kèm file lên hệ thống $\rightarrow$ Bấm **Tải lên & Phân tích**.
  5. Kiểm tra kết quả phân tích: Nếu có dòng báo đỏ, sửa lại trong Excel và tải lại. Khi tất cả hợp lệ, bấm **Xác nhận Import**.

### 5. Import hàng loạt công việc con (Todo) từ file Excel
- Dùng để nạp danh sách các việc nhỏ chi tiết gắn vào các tiến trình cha:
  1. Bấm **Import công việc** (chỉ hiển thị khi quy trình đã có ít nhất 1 tiến trình).
  2. Tải file mẫu Excel 2 Sheet:
     - Xem mã tiến trình tại **Sheet 2** (ví dụ: `PR2609000101`).
     - Điền dữ liệu vào **Sheet 1**: Dán đúng Mã tiến trình vào cột tương ứng.
  3. Tải file lên kiểm tra và bấm **Xác nhận Import**.

### 6. Báo cáo hoàn thành và Mở khóa cập nhật tiến trình
- **Báo cáo tiến độ / Hoàn thành:**
  - Bấm nút **Chỉnh sửa** trên dòng tiến trình.
  - Cập nhật kết quả chi tiết bằng khung soạn thảo (hỗ trợ chèn bảng, link, danh sách).
  - Đính kèm tệp tin kết quả.
  - Chọn trạng thái sang **Hoàn thành** $\rightarrow$ Bấm **Lưu thay đổi**.
- **Khi cần hiệu chỉnh tiến trình đã hoàn thành:**
  - Sau khi hoàn thành, tiến trình sẽ được khóa an toàn để đảm bảo tính toàn vẹn dữ liệu.
  - Nếu cần cập nhật lại nội dung hoặc đính kèm thêm file: Bấm icon **Mở khóa tiến trình (ổ khóa mở)**.
  - Nhập rõ lý do mở khóa $\rightarrow$ Bấm **Xác nhận**.
  - Hệ thống ghi nhận lịch sử mở khóa và tự động mở lại màn hình chỉnh sửa cho phép cập nhật nội dung. Trạng thái Hoàn thành vẫn được giữ nguyên vẹn.

### 7. Tra cứu lịch sử thao tác (Audit Log)
- Tại mỗi dòng tiến trình, bấm icon **Đồng hồ / Lịch sử (`fa-history`)** để xem toàn bộ nhật ký:
  - Ai đã tạo tiến trình vào thời gian nào.
  - Ai đã cập nhật tiến độ, nội dung báo cáo từng lần.
  - Ai đã mở khóa tiến trình và lý do mở khóa là gì.
  - Toàn bộ tệp đính kèm qua các lần cập nhật đều được lưu trữ và có thể tải về bất kỳ lúc nào.

### 8. Theo dõi thông tin Hoàn thành trực tiếp trên bảng Checklist
- Cột **Hoàn thành** được bố trí ngay trước cột Thao tác, giúp người quản lý và các thành viên nắm bắt ngay ai là người hoàn tất công việc và vào thời gian nào:
  - **Tên người hoàn thành:** In đậm rõ ràng.
  - **Thời gian hoàn thành:** Hiển thị ngày giờ chi tiết (`dd/MM/yyyy HH:mm`) kèm biểu tượng đồng hồ xanh lá.
  - **Dấu gạch ngang (`—`):** Thể hiện đầu mục công việc vẫn đang được xử lý hoặc chưa bắt đầu.

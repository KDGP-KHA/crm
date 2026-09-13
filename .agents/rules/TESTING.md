---
trigger: always_on
---

# 🧪 Strict Testing & Quality Assurance Rules

Bạn phải tuân thủ nghiêm ngặt quy trình kiểm thử và xác minh sau đây cho mọi thay đổi mã nguồn:

---

## 1. Nguyên tắc viết Test (Test Design)
- **Bao phủ 3 tầng kịch bản:** Mọi tính năng/hàm logic mới phải có test cho:
  1. *Happy Path:* Trường hợp đầu vào chuẩn, hoạt động đúng mong đợi.
  2. *Edge Cases:* Dữ liệu biên (null, undefined, rỗng, số âm, mảng rỗng, chuỗi quá dài, ký tự đặc biệt, thẻ HTML từ CKEditor).
  3. *Error Handling:* Trường hợp lỗi (sai mật khẩu, mất mạng, token hết hạn, dữ liệu không hợp lệ) và kiểm tra xem có throw đúng mã lỗi/thông báo không.
- **Assertion có ý nghĩa:** Tuyệt đối không viết test rỗng hoặc assert hình thức (ví dụ: `expect(res).toBeDefined()`). Phải assert chính xác dữ liệu trả về và trạng thái mong muốn.
- **Mocking chuẩn:** Mock toàn bộ các phụ thuộc ngoại vi (Database, Network API, Third-party service, File system) để bộ test có thể chạy độc lập, cô lập và ổn định khi chạy unit test. Khi chạy integration/verification test, phải kiểm tra luồng dữ liệu thực.

---

## 2. Quy chuẩn Kiểm thử Giao diện Web, Runtime JavaScript & AJAX
- **CẤM NGHIỆM THU NẾU CÒN LỖI CONSOLE (No Console Errors):**
  - Mọi thao tác tương tác người dùng (chọn dropdown Loại hình, chuyển tab, mở modal, submit form, tìm kiếm) PHẢI đảm bảo không văng bất kỳ lỗi Console nào (`Uncaught ReferenceError: ... is not defined`, `TypeError`, `SyntaxError`).
  - Mọi hàm JavaScript được gọi từ sự kiện trên View/Partial View (`onchange`, `onclick`) PHẢI được kiểm tra tồn tại trên scope trước khi người dùng tương tác.
  - Mọi script tag nhúng file `.js` tự viết phải có cache-busting timestamp: `?v=@DateTime.Now.Ticks` để chống client chạy file JS cũ từ cache trình duyệt.
- **Kiểm thử Form Submission & Chống lỗi 500 (Safe Form Lifecycle):**
  - BẮT BUỘC kiểm thử việc submit form:
    - Khi submit hợp lệ: Phải trả về JSON `{ status: true }`, hiển thị đúng Toastr thông báo, đóng modal mượt mà, không kẹt backdrop.
    - Khi có lỗi nghiệp vụ: Phải hiển thị thông báo lỗi rõ ràng từ `App_Message`, không được làm sập trang.
    - Tuyệt đối KHÔNG để xảy ra tình trạng submit native làm sập trang sang `/Error/Error` (HTTP 500) do thiếu thuộc tính `action` hoặc mất event `e.preventDefault()`.
  - Mọi form có trường nhập nội dung từ trình soạn thảo (CKEditor/Summernote) PHẢI được kiểm thử lưu chuỗi có chứa các thẻ HTML (`<p>`, `<div>`, `<br>`, `<b>`). Phải xác minh C# Model có thuộc tính `[AllowHtml]` và `Web.config` có `requestValidationMode="2.0"` để không bị `HttpRequestValidationException`.
  - **Kiểm thử An toàn Model Binding & DisplayName (Anti-Null DisplayName):** Mọi Model C# dùng làm tham số nhận dữ liệu Form POST/PUT PHẢI được xác minh:
    - Không có bất kỳ property nào có `CustomDisplayName` hoặc `DisplayName` trả về `null`.
    - Gán thử vào `ValidationContext.DisplayName` thành công, không được quăng `ArgumentNullException: Value cannot be null. Parameter name: value`.
  - **Kiểm thử Chống xung đột DOM ID (Anti-DOM ID Collision):**
    - Kiểm tra toàn bộ các phần tử `[id]` giữa trang danh sách (View cha, `_Search`) và các Partial View Modal (`_Add`, `_Edit`).
    - CẤM TUYỆT ĐỐI việc trùng ID các control dữ liệu (như `CustomerID`, `StatusID`, `EmployeeID`), vì selector jQuery `$('#CustomerID')` sẽ trỏ sai vào trang ngoài, khiến form modal gửi dữ liệu rỗng/0 lên server dù người dùng đã chọn trên giao diện.
    - Bắt buộc các trường trong Modal phải có hậu tố phân biệt: `CustomerID_Add`, `CustomerID_Edit`.
- **Kiểm thử phản hồi AJAX Network:**
  - Kiểm tra toàn bộ các request AJAX: Bắt buộc mã trạng thái `200 OK`. Cấm tuyệt đối mã lỗi HTTP 404, 500 hoặc 302 Redirect sang trang lỗi.

---

## 3. Quy trình Thực thi, Sửa lỗi & Nghiêm cấm "Đoán mò"
- **NGHIÊM CẤM "CODE XONG KHÔNG TEST LẠI":**
  - **Cấm tuyệt đối** việc chỉ biên dịch C# thành công (Build 0 errors) rồi tự ý kết luận nhiệm vụ hoàn thành. Biên dịch C# chỉ chứng minh cú pháp hợp lệ, KHÔNG chứng minh mã chạy đúng trên giao diện và luồng dữ liệu.
  - Bắt buộc PHẢI chạy script kiểm thử thực tế (PowerShell test script vào DB/Service, hoặc gọi API/kiểm tra view) xác nhận luồng chạy trơn tru trước khi bàn giao.
- **Quy tắc phân tích nguyên nhân gốc rễ (Root Cause Analysis):**
  - Khi test FAIL hoặc phát hiện lỗi: Đọc kỹ stack trace, xác định chính xác dòng bị lỗi và lý do logic trước khi sửa.
  - **TUYỆT ĐỐI KHÔNG sửa file Test** để làm cho bài test PASS, trừ khi yêu cầu nghiệp vụ thực sự thay đổi. Trách nhiệm của bạn là sửa file Implementation (mã nguồn chính).
  - Không được đoán mò hoặc thử các cách sửa ngẫu nhiên lặp đi lặp lại.
- **Kiểm tra hồi quy (Regression Check):** Sau khi tính năng mới PASS, phải chạy lại toàn bộ test suite liên quan trong module để đảm bảo không làm hỏng tính năng cũ.

---

## 4. Giới hạn Vòng lặp & Báo cáo
- **Giới hạn 3 lần thử:** Nếu sau 3 lần tự sửa mà test vẫn FAIL:
  - DỪNG vòng lặp tự sửa lại.
  - Trích dẫn log lỗi và stack trace chính xác.
  - Giải thích cho người dùng: (1) Bạn đang muốn làm gì, (2) Lỗi thực sự nằm ở đâu (môi trường, logic, hay dependency), (3) Đề xuất hướng giải quyết.
- Chỉ xem nhiệm vụ là HOÀN THÀNH khi:
  - Tất cả các test đều PASS 100%.
  - Không còn bất kỳ lỗi runtime, JavaScript console error hay lỗi 500 nào.
  - Không còn bất kỳ cảnh báo Linting hay Type Error nào.

---

## 5. Bộ Kiểm thử Tự động 5 Tầng Bắt buộc (5-Layer Verification Suite)
Mỗi module trước khi bàn giao PHẢI vượt qua bộ kiểm thử 5 tầng được tự động hóa bằng script kiểm thử (PowerShell / Node.js):
1. **Tầng 1 (Compile & Build):** Biên dịch toàn bộ các project liên quan (`Core.*`, `Modules.*`) bằng MSBuild đạt `0 Error(s)`.
2. **Tầng 2 (Triple Mirroring & UTF-8 BOM):**
   - Mọi tệp View (`.cshtml`), Script (`.js`), Style (`.css`) phải tồn tại đồng nhất về nội dung (Hash MD5 khớp nhau) ở cả 3 cây thư mục: `Modules.*`, `publish_source`, và `WebApp`.
   - 100% tệp Razor `.cshtml` phải có tiền tố `UTF-8 with BOM` (`0xEF, 0xBB, 0xBF`) để máy chủ IIS không lỗi font tiếng Việt.
3. **Tầng 3 (DOM ID Collision Scanner):** Quét phân tích HTML của view cha (`Index`, `_Search`) và các partial view modal (`_Add`, `_Edit`, `_ChangeStatusModal`...) để khẳng định không trùng bất kỳ ID input/select/textarea nào.
4. **Tầng 4 (Sys_Messages DB Coverage):** Quét toàn bộ các chuỗi `LabelKey` được gọi trong Controller/Model, đối chiếu trực tiếp với CSDL SQL Server bảng `Sys_Messages` để khẳng định 100% key đều tồn tại và trả về message tiếng Việt hợp lệ.
5. **Tầng 5 (Clean Code, No Inline Styles & No Hardcoded UI Text):**
   - Quét mã nguồn `.cs` đảm bảo không có chuỗi tiếng Việt hardcoded trực tiếp cho thông báo, tiêu đề, hoặc lỗi validation.
   - Quét toàn bộ tệp Razor `.cshtml` đảm bảo **100% KHÔNG có thẻ `<style>` nội tuyến**, mọi style phải được tách riêng thành tệp `.css` đặt cùng thư mục với view tương ứng và nhúng kèm timestamp `?v=@DateTime.Now.Ticks`.

---

## 6. Quy chuẩn Báo Cáo Đánh Giá Tự Thân (Self-Evaluation Report)
Sau khi hoàn thành MỖI yêu cầu từ người dùng, tác nhân AI BẮT BUỘC phải thực hiện bước Đánh giá Tự thân và xuất bảng đánh giá chi tiết theo cấu trúc:
```markdown
### 📋 BÁO CÁO TỰ ĐÁNH GIÁ THỰC HIỆN YÊU CẦU (Self-Evaluation Report)
| Tiêu chí | Mục tiêu | Kết quả thực tế | Đánh giá |
| :--- | :--- | :--- | :--- |
| **1. Tính chính xác nghiệp vụ** | Đáp ứng 100% yêu cầu người dùng đặt ra | [Mô tả chi tiết] | ĐẠT / CHƯA ĐẠT |
| **2. Chất lượng giao diện UI/UX** | Font chữ chuẩn (+1px), không vỡ layout, micro-interactions | [Mô tả chi tiết] | ĐẠT / CHƯA ĐẠT |
| **3. Tuân thủ Rules & Standards** | 100% text qua Sys_Messages, file lưu đúng /Contents/Uploads/ | [Mô tả chi tiết] | ĐẠT / CHƯA ĐẠT |
| **4. Kết quả Test Suite 5 Tầng** | Pass 100% tất cả 5 tầng kiểm thử | [Mô tả chi tiết] | ĐẠT / CHƯA ĐẠT |
| **5. An toàn hồi quy (Regression)** | Không làm hỏng các tính năng khác trong hệ thống | [Mô tả chi tiết] | ĐẠT / CHƯA ĐẠT |
```
Nếu có bất kỳ tiêu chí nào CHƯA ĐẠT, tác nhân AI PHẢI tự động sửa chữa ngay lập tức trước khi thông báo tới người dùng.
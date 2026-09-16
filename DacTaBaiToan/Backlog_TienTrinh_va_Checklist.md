# PRODUCT BACKLOG: QUẢN LÝ TIẾN TRÌNH & CHECKLIST HỒ SƠ KINH DOANH

Tài liệu Product Backlog chi tiết được xây dựng theo tiêu chuẩn Agile/Scrum dựa trên tài liệu đặc tả chức năng phân hệ **Tiến trình & Checklist**.

---

## 1. TỔNG QUAN HỆ THỐNG & ĐẶC TẢ NGHIỆP VỤ

- **Phân hệ:** Quản lý Hồ sơ Kinh doanh số / Cơ hội kinh doanh.
- **Tính năng trọng tâm:** Quản trị Tiến trình công việc, Checklist nhiệm vụ, Cơ chế chuyển đổi trạng thái cơ hội gắn liền với hoàn thành tự động và ghi nhận Audit Log.
- **Cấu trúc phân cấp dữ liệu:**
  $$\text{Hồ sơ Cơ hội} \longrightarrow \text{Trạng thái (Status)} \longrightarrow \text{Quy trình (Process)} \longrightarrow \text{Tiến trình (Stage/Phase)} \longrightarrow \text{Công việc con (Task/Sub-task)}$$

---

## 2. DANH SÁCH USER STORIES (PRODUCT BACKLOG ITEMS)

### Epic 1: Quản lý Trạng thái Cơ hội & Tác động Checklist

#### US-01: Tự động khởi tạo Checklist khi tạo mới Cơ hội
* **User Story:** Là Nhân viên kinh doanh, tôi muốn khi tạo mới cơ hội ở trạng thái "Chưa nắm bắt", hệ thống tự động sinh cấu trúc Trạng thái và Quy trình tương ứng trong tab Checklist để chuẩn bị phân bổ công việc.
* **Độ ưu tiên:** Must-Have
* **Ước lượng:** 2 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. Khi một Cơ hội mới được tạo với trạng thái ban đầu là `CHƯA NẮM BẮT`, hệ thống tự động thêm block Trạng thái `CHƯA NẮM BẮT` vào tab *Tiến trình & Checklist*.
  2. Tự động liên kết và hiển thị block `QUY TRÌNH` mặc định tương ứng với trạng thái `CHƯA NẮM BẮT`.
  3. Cây phân cấp sẵn sàng tiếp nhận danh sách Tiến trình đầu tiên.

---

#### US-02: Chuyển trạng thái Cơ hội & Tự động kết thúc tiến trình trạng thái cũ
* **User Story:** Là Nhân viên kinh doanh, tôi muốn khi xác nhận chuyển trạng thái hồ sơ kinh doanh, toàn bộ tiến trình và công việc của trạng thái trước đó được tự động đánh dấu hoàn thành và đưa cấu trúc của trạng thái mới vào Checklist.
* **Độ ưu tiên:** Must-Have
* **Ước lượng:** 5 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. **Hiển thị modal chuyển trạng thái:**
     - Cho phép chọn trạng thái mới (bắt buộc).
     - Cho phép nhập file đính kèm minh chứng và ghi chú/lý do chuyển (bắt buộc).
     - Danh sách tiến trình công việc của trạng thái mới hiển thị cho phép thêm, sửa tên, phân công người thực hiện, ngày bắt đầu, số ngày xử lý, tính hạn chót, hoặc xóa dòng trước khi xác nhận.
     - Chỉ những tiến trình còn tồn tại trong danh sách này khi bấm *Xác nhận chuyển* mới được đưa vào Checklist (nếu người dùng xóa hết thì không sinh tiến trình mới).
  2. **Xử lý khi bấm Xác nhận chuyển:**
     - Toàn bộ Tiến trình và Công việc con thuộc Quy trình của **Trạng thái trước đó** tự động chuyển sang trạng thái `Hoàn thành`.
     - *Thời gian hoàn thành* được gán bằng thời gian thực tế người dùng bấm xác nhận.
     - *Người hoàn thành* được ghi nhận là User đang thực hiện thao tác.
     - Tự động ghi nhận thông tin đóng này vào **Audit Log** của từng tiến trình/công việc và ghi nhận vào dòng thời gian (Timeline) của hồ sơ.
  3. **Chuyển vòng lặp:** Nếu quy trình quay vòng (ví dụ $A \rightarrow B \rightarrow C \rightarrow D \rightarrow B$), khối Trạng thái $B$ mới sẽ được sinh thêm độc lập trên Checklist với mã tiến trình mới.
  4. **Bảo toàn trạng thái không có tiến trình trên Checklist (Preserve Zero-Task Status in History):**
      - Khi chuyển sang một trạng thái mà trạng thái đó không có tiến trình nào (0 tiến trình - do người dùng xóa hết tiến trình hoặc quy trình chưa có tiến trình mẫu), hệ thống tự động lưu giữ 1 bản ghi cấu trúc trạng thái & quy trình gắn với `TimelineID` tương ứng.
      - Khi tiếp tục chuyển sang các trạng thái tiếp theo (kể cả trạng thái có nhiều tiến trình), dòng Trạng thái và Quy trình của trạng thái 0 tiến trình trước đó **BẮT BUỘC PHẢI ĐƯỢC GIỮ LẠI NGUYÊN VẸN** trên cây Checklist theo đúng dòng thời gian (Timeline), tuyệt đối không để biến mất khỏi giao diện Checklist.
      - Trên giao diện của trạng thái 0 tiến trình này trong quá khứ, hệ thống hiển thị thông báo "Quy trình [Tên quy trình] chưa có tiến trình nào" kèm badge `0 tiến trình`, bảo đảm phản ánh chính xác 100% toàn bộ lịch sử các giai đoạn hồ sơ đã từng đi qua.

---

### Epic 2: Giao diện & Thao tác trên Quy trình (Checklist Layout)

#### US-03: Tái cấu trúc layout thanh công cụ trên dòng Quy trình
* **User Story:** Là Người dùng, tôi muốn các nút hành động trên dòng Quy trình được căn chỉnh về phía bên phải và kích thước font chữ hiển thị rõ ràng hơn để tối ưu thao tác.
* **Độ ưu tiên:** Must-Have
* **Ước lượng:** 2 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. Khối các nút hành động gồm `+ Thêm tiến trình`, `Import tiến trình`, `Import công việc` được căn phải (align right) trên dòng Quy trình.
  2. Badge/Text hiển thị số lượng tiến trình (ví dụ: `3 tiến trình`) được tăng kích thước font thêm `2px` so với cỡ chữ mặc định.
  3. Nút `Import công việc` chỉ hiển thị khi Quy trình đó **có ít nhất 1 tiến trình**. Nếu chưa có tiến trình nào, nút này phải được ẩn đi.
  4. Nếu người dùng xóa hết toàn bộ tiến trình trong bảng, hệ thống **vẫn giữ nguyên** dòng Trạng thái và dòng Quy trình, tuyệt đối không tự động ẩn đi.

---

### Epic 3: Tính năng Import Dữ liệu Excel

#### US-04: Import danh sách Tiến trình từ file Excel
* **User Story:** Là Quản lý / Phụ trách hồ sơ, tôi muốn import danh sách nhiều tiến trình từ file Excel vào đúng quy trình để tiết kiệm thời gian nhập liệu thủ công.
* **Độ ưu tiên:** Should-Have
* **Ước lượng:** 5 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. Nhấp nút `Import tiến trình` trên dòng quy trình mở modal Import gồm: Link/nút Tải file mẫu (`.xlsx`), Vùng tải lên file dữ liệu.
  2. Khi người dùng tải file lên, hệ thống hiển thị bảng xem trước (preview) dữ liệu.
  3. Cột **Thông tin lỗi** hiển thị tập trung các lỗi kiểm tra tính hợp lệ:
     - Tên tiến trình (bắt buộc, không được trùng lặp trong cùng một quy trình).
     - Định dạng ngày bắt đầu hợp lệ (`DD/MM/YYYY`).
     - Số ngày xử lý phải là số nguyên dương $> 0$.
     - Người thực hiện (phải nằm trong danh sách thành viên hợp lệ của dự án/hồ sơ).
     - Ghi chú/Nội dung.
  4. Nút `Xóa dữ liệu Import`: Cho phép xóa sạch dữ liệu vừa tải lên để tải lại.
  5. Nút `Xác nhận dữ liệu Import`: Chỉ kích hoạt khi dữ liệu hợp lệ (không còn lỗi); lưu toàn bộ danh sách tiến trình vào đúng Quy trình đang thao tác trên Checklist.

---

#### US-05: Import danh sách Công việc con theo Tiến trình từ file Excel
* **User Story:** Là Quản lý / Phụ trách hồ sơ, tôi muốn import danh sách các công việc con phân bổ cho từng tiến trình thông qua file Excel có 2 sheet.
* **Độ ưu tiên:** Could-Have
* **Ước lượng:** 5 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. Nút `Import công việc` mở modal import công việc con.
  2. File Excel mẫu khi tải về bao gồm **2 sheet**:
     - **Sheet 1 (Dữ liệu Công việc):** Gồm các cột thông tin công việc con và cột `Mã Tiến trình`.
     - **Sheet 2 (Danh sách Tiến trình):** Danh sách tra cứu các tiến trình hiện có trong Quy trình hiện tại (gồm cột `Mã Tiến trình`, `Tên Tiến trình`).
  3. Cơ chế đọc file, kiểm tra validate dữ liệu, kiểm tra tính tồn tại của `Mã Tiến trình` trên hệ thống, cột cảnh báo lỗi, nút xóa dữ liệu và nút xác nhận thực hiện đồng bộ theo quy chuẩn của US-04.

---

### Epic 4: Quản lý Chi tiết Tiến trình & Công việc con

#### US-06: Chuẩn hóa hiển thị dòng Tiến trình & Xem Log thao tác
* **User Story:** Là Người dùng, tôi muốn xem thông tin các dòng tiến trình có font chữ đồng nhất và có thể tra cứu lịch sử thao tác của từng tiến trình.
* **Độ ưu tiên:** Should-Have
* **Ước lượng:** 3 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. Tất cả các cột thông tin text trên row tiến trình có cỡ font bằng với font của cột Tên tiến trình.
  2. Cột **Thao tác** bổ sung action/icon: `Log thao tác`.
  3. Khi click vào `Log thao tác`: Hiển thị modal/drawer chứa toàn bộ dòng thời gian ghi nhận các thay đổi, lịch sử tạo, sửa, hoàn thành, mở khóa kèm thông tin user và timestamp.

---

#### US-07: Cập nhật chi tiết Tiến trình
* **User Story:** Là Người phụ trách tiến trình, tôi muốn cập nhật chi tiết nội dung, tệp đính kèm và hạn xử lý của tiến trình để phản ánh tiến độ thực tế.
* **Độ ưu tiên:** Must-Have
* **Ước lượng:** 5 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. Click icon Edit mở modal **Cập nhật Tiến trình** gồm các trường:
     - `Mã Tiến trình`: Readonly (sinh tự động dạng mã, ví dụ: `PR2609000328`).
     - `Tên tiến trình (*)`: Textarea, bắt buộc.
     - `Người thực hiện (*)`: Dropdown chọn người dùng được gán trong hồ sơ dịch vụ. Nếu chưa có ai thì để trống.
     - `Ngày bắt đầu (*)`: Datepicker, bắt buộc.
     - `Số ngày xử lý (*)`: Input number, bắt buộc.
     - `Hạn xử lý`: Readonly, tự động tính toán theo công thức:
       $$\text{Hạn xử lý} = \text{Ngày bắt đầu} + \text{Số ngày xử lý}$$
     - `Trạng thái (*)`: Dropdown trạng thái **chỉ gồm 3 trạng thái nghiệp vụ chuẩn**: `Chưa thực hiện` (1), `Đang thực hiện` (2), `Hoàn thành` (3). **TUYỆT ĐỐI KHÔNG đưa "Quá hạn" vào danh sách lựa chọn** (Quá hạn là chỉ báo cảnh báo trực quan tính toán tự động dựa trên hạn chót với ngày hiện tại khi chưa hoàn thành, không phải là trạng thái nghiệp vụ có thể chọn thủ công).
     - `Ngày hoàn thành`: Tự động ghi nhận ngày giờ thực tế khi người dùng chọn Trạng thái là `Hoàn thành`.
     - `Nội dung thực hiện (*)`: Trình soạn thảo văn bản phong phú (CKEditor).
     - `Tệp đính kèm`: Cho phép upload đồng thời nhiều file.
  2. Bấm nút `Lưu tiến trình`: Cập nhật dữ liệu vào cơ sở dữ liệu và ghi nhận 1 bản ghi vào Audit Log.
  3. **Kiểm soát nút Xóa:** Nếu tiến trình đã ở trạng thái `Hoàn thành`, nút `Xóa` phải bị ẩn hoàn toàn để bảo đảm tính toàn vẹn dữ liệu.
  4. **Bảo toàn layout bảng Checklist:** Không hiển thị dòng nội dung ghi chú/HTML dưới tên tiến trình trên bảng Checklist để giữ bảng dữ liệu luôn gọn gàng, chuẩn mực. Toàn bộ nội dung thực hiện và tệp đính kèm được tra cứu tập trung qua modal *Log thao tác tiến trình* và *Báo cáo*.

---

#### US-08: Mở khóa Tiến trình đã Hoàn thành
* **User Story:** Là Quản trị viên / Người có thẩm quyền, tôi muốn mở khóa một tiến trình đã hoàn thành khi có phát sinh cần bổ sung thông tin và ghi nhận lý do rõ ràng.
* **Độ ưu tiên:** Should-Have
* **Ước lượng:** 3 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. Đối với dòng Tiến trình đã có trạng thái `Hoàn thành`, hiển thị nút `Mở khóa`.
  2. Bấm nút `Mở khóa` sẽ kích hoạt modal yêu cầu nhập **Lý do mở khóa** (bắt buộc).
  3. Sau khi xác nhận lý do:
     - Lưu thông tin lý do mở khóa vào Audit Log.
     - Tiến trình được mở khóa (cho phép người dùng bổ sung thông tin).
     - Tự động mở modal Edit cập nhật tiến trình với trạng thái giữ nguyên là `Hoàn thành` (readonly, không cho phép đổi lại trạng thái).
     - **Làm sạch vùng tệp đính kèm (Clear Files on Unlock):** Vì bản chất của thao tác này là **cập nhật thêm thông tin bổ sung** chứ không phải sửa đổi hay ghi đè dữ liệu cũ, vùng Tệp đính kèm trong modal được **để trống (clear toàn bộ badge file cũ)**, sẵn sàng nhận các tệp đính kèm mới.
     - **Bảo toàn tệp cũ:** Các tệp tin đã đính kèm trong các lần báo cáo/hoàn thành trước đó vẫn được lưu trữ nguyên vẹn trong hệ thống và tra cứu đầy đủ trong modal *Log thao tác tiến trình*.
     - Khi bấm Lưu: Tệp mới được nối tiếp vào danh sách tài liệu của tiến trình và ghi nhận riêng vào Activity Log của lần cập nhật bổ sung này.

---

#### US-09: Quản lý Công việc con (Sub-tasks) thuộc Tiến trình
* **User Story:** Là Người thực hiện, tôi muốn tạo và quản lý các công việc con bên dưới mỗi tiến trình để phân rã đầu việc chi tiết.
* **Độ ưu tiên:** Must-Have
* **Ước lượng:** 5 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. Trên mỗi dòng Tiến trình có nút `+` (Thêm mới công việc).
  2. Dữ liệu và thuộc tính của Công việc con bao gồm đầy đủ các trường tương tự như Tiến trình (Mã công việc, Tên, Người thực hiện, Ngày bắt đầu, Số ngày xử lý, Hạn xử lý, Nội dung thực hiện CKEditor, Đính kèm file, Trạng thái).
  3. Áp dụng đầy đủ các nghiệp vụ của Tiến trình cho Công việc con:
     - Ghi nhận Audit Log riêng biệt.
     - Tự động chuyển `Hoàn thành` khi chuyển trạng thái hồ sơ kinh doanh.
     - Có nút `Mở khóa` kèm modal nhập lý do khi đã hoàn thành.
     - Ẩn nút xóa khi công việc đã ở trạng thái Hoàn thành.
   4. **Chuẩn hóa giao diện hiển thị bảng Checklist đồng nhất 100% với dòng Tiến trình cha:**
      - **Cột Tên công việc con:** Hiển thị thụt lề nhánh cây (`└─`), checkbox hoàn thành, tên công việc với font chữ chuẩn to rõ (`0.92rem`). **Tuyệt đối KHÔNG chèn các nút bấm text inline** (`[Báo cáo]`, `[Xác nhận]`, `[Mở khóa]`, `[Log]`) vào cột Tên công việc làm vỡ cấu trúc và lệch giao diện so với tiến trình cha.
      - **Cột Mã công việc:** Hiển thị dưới dạng Badge chuẩn có viền và màu sắc đồng bộ (`CVyymmxxxxxx`) giống như mã tiến trình (`PRyymmxxxxxx`), font chữ chuẩn rõ ràng (`font-progress-cell`).
      - **Cột Người thực hiện, Bắt đầu, Hạn chót:** Áp dụng cỡ chữ chuẩn (`font-progress-cell` = `0.92rem`), không dùng cỡ chữ thu nhỏ (`text-75`, `text-85`).
      - **Cột Tổng ngày:** Tính toán và hiển thị Badge số ngày xử lý chuẩn (`N ngày`) thay vì hiển thị gạch ngang (`—`).
      - **Cột Trạng thái:** Hiển thị Badge trạng thái chuẩn (`font-bold`, cỡ chuẩn đồng bộ với tiến trình).
      - **Cột Thao tác:** Đồng bộ nhóm nút thao tác chuẩn ở cột Thao tác cuối cùng gồm:
        - `[Log thao tác]` (`fa-history`): Xem toàn bộ lịch sử thao tác của công việc con.
        - `[Mở khóa]` (`fa-unlock-alt`): Hiển thị khi công việc con đã `Hoàn thành`, mở modal nhập lý do mở khóa.
        - `[Chỉnh sửa]` (`fa-pencil-alt`): Hiển thị khi công việc con chưa hoàn thành.
        - `[Xóa]` (`fa-trash-alt`): Hiển thị khi công việc con chưa hoàn thành (ẩn đi khi đã Hoàn thành).
   5. **Quy chuẩn kích thước Modal Thêm/Sửa công việc con:**
      - Modal thêm và sửa công việc con (`_TodoModal`) được mở rộng đạt kích thước chuẩn `800px` (`modal-lg`, `data_width="800"`).
      - Các cột Ngày bắt đầu, Số ngày xử lý, Hạn xử lý được chia đều cân đối (`col-md-4`), đảm bảo hiển thị trọn vẹn datepicker không bị cắt cụt ký tự.

---

#### US-10: Tự động hoàn thành các Công việc con khi Tiến trình cha Hoàn thành & Lưu Log thao tác
* **User Story:** Là Người phụ trách tiến trình / Quản lý, tôi muốn khi cập nhật một Tiến trình sang trạng thái `Hoàn thành`, tất cả các Công việc con (Checklist/Sub-tasks) chưa hoàn thành thuộc tiến trình đó cũng sẽ được tự động chuyển sang `Hoàn thành` và ghi nhận nhật ký thao tác (Audit Log) đầy đủ để đảm bảo tính nhất quán dữ liệu và tiết kiệm thời gian thao tác lặp lại.
* **Độ ưu tiên:** Must-Have
* **Ước lượng:** 3 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. **Kích hoạt tự động (Cascade Completion Trigger):**
     - Khi Tiến trình cha được cập nhật sang trạng thái `Hoàn thành` (thông qua: Báo cáo kết quả tiến độ, Checkbox hoàn thành, Nút Xác nhận hoàn thành, hoặc Modal Chỉnh sửa tiến trình).
     - Hệ thống tự động quét toàn bộ danh sách Công việc con trực thuộc Tiến trình đó (`ParentID = TrackingID`).
     - Với tất cả công việc con đang ở trạng thái `Chưa làm` (Status = 1) hoặc `Đang làm` (Status = 2):
       - Tự động cập nhật `Status = 3` (Hoàn thành).
       - Tự động gán `CompletedDate` bằng thời gian thực tế thực hiện thao tác (`GETDATE()`).
       - Cập nhật thông tin người chỉnh sửa gần nhất (`LastModifiedBy`, `LastModifiedDate`).
  2. **Ghi nhận Log thao tác (Audit Logging):**
     - **Đối với từng công việc con:** Tự động sinh 1 bản ghi Activity Log (`ActivityType = 3`):
       $$N\text{'Tự động hoàn thành công việc: <b>[Tên việc con]</b> (theo tiến trình cha: [Tên tiến trình cha])'}$$
       Gắn `ReferenceID = ChildTrackingID`, `ActionBy`, `ActionByName` là người dùng đang thao tác.
     - **Đối với Tiến trình cha:** Log hoàn thành tiến trình ghi nhận số lượng công việc con đã được hoàn thành tự động đính kèm (Ví dụ: *Đã hoàn thành tiến trình: X (Tự động hoàn thành 2 công việc con đính kèm)*).
  3. **Tra cứu & Hiển thị Nhật ký (Tracking Logs Modal):**
     - Modal `Log thao tác tiến trình` của Tiến trình cha hiển thị danh sách công việc con, tỷ lệ hoàn thành ($N/M$), đồng thời dòng thời gian hiển thị đầy đủ cả log của tiến trình cha lẫn log tự động hoàn thành của các công việc con với badge phân biệt trực quan (`[Công việc con]`, `[Tiến trình]`).
     - Dòng công việc con trên bảng Checklist có nút `Log` riêng biệt để tra cứu trực tiếp lịch sử của từng việc nhỏ.

#### US-11: Hiển thị Cột Hoàn thành trên bảng Checklist & Tiến trình
* **User Story:** Là Người quản lý / Nhân viên theo dõi tiến độ, tôi muốn bảng Checklist có thêm cột "Hoàn thành" nằm ngay giữa cột "Trạng thái" và "Thao tác", hiển thị rõ ràng Tên người hoàn thành và Ngày - Giờ hoàn thành của cả Tiến trình và Công việc con để nắm bắt tức thời ai là người chốt việc và vào thời điểm nào mà không cần phải mở xem Log.
* **Độ ưu tiên:** Must-Have
* **Ước lượng:** 3 Story Points
* **Tiêu chí chấp nhận (Acceptance Criteria - AC):**
  1. **Vị trí cột trên bảng Checklist:** Nằm giữa cột `Trạng thái` và cột `Thao tác` với độ rộng chuẩn `160px`, căn giữa (`text-center`).
  2. **Nội dung hiển thị:**
     - Khi Tiến trình hoặc Công việc con ở trạng thái `Hoàn thành` (Status = 3):
       - Dòng 1: **Tên người Hoàn thành** (in đậm `font-weight-bold`, text đen `text-dark`, ví dụ: *Trần Duy Tân*).
       - Dòng 2: **Thời gian - Ngày hoàn thành** kèm icon đồng hồ màu xanh lá (`<i class="fa fa-clock text-success mr-1"></i>`), định dạng `dd/MM/yyyy HH:mm` (ví dụ: *15/09/2026 20:31*).
     - Khi Tiến trình hoặc Công việc con chưa hoàn thành (`Chưa làm`, `Đang làm`): Hiển thị dấu gạch ngang xám (`—`).
  3. **Cơ chế lưu vết và truy vấn dữ liệu:**
     - Lưu trữ trực tiếp trường `CompletedBy` (tên tài khoản người hoàn thành) và `CompletedDate` (thời điểm hoàn thành) trong bảng `dbo.RM_DigitalSalesTracking`.
     - Stored Procedure `dbo.RM_DigitalSalesTracking_GetBySalesID` tự động JOIN với bảng người dùng `Sys_User` để lấy ra họ tên đầy đủ (`CompletedByName`).
     - Stored Procedure `dbo.RM_DigitalSalesTracking_UpdateStatus` và cơ chế chuyển trạng thái tự động cập nhật `CompletedBy` và `CompletedDate = GETDATE()` khi chuyển trạng thái sang `Hoàn thành` (Status = 3) cho cả tiến trình cha và các công việc con cascade.
  4. **Đồng bộ Colspan:** Toàn bộ các dòng Cấp 1 (Trạng thái), Cấp 2 (Quy trình), Dòng thông báo quy trình rỗng và Dòng bảng rỗng đều được điều chỉnh tăng `colspan` từ 8/7 lên 9/8 tương ứng để bảng không bị lệch cấu trúc.

---

## 3. BẢNG TỔNG HỢP SPRINT PLANNING & ĐỘ ƯU TIÊN

| Mã US | Tên User Story | Module | Ước lượng (SP) | Mức độ ưu tiên | Sprint khuyến nghị |
| :--- | :--- | :---: | :---: | :---: | :---: |
| **US-01** | Tự động khởi tạo Checklist khi tạo mới Cơ hội | Workflow / Status | 2 | **Must-Have** | Sprint 1 |
| **US-02** | Chuyển trạng thái Cơ hội & Tự động kết thúc tiến trình cũ | Workflow / Status | 5 | **Must-Have** | Sprint 1 |
| **US-03** | Tái cấu trúc layout thanh công cụ trên dòng Quy trình | UI/UX Checklist | 2 | **Must-Have** | Sprint 1 |
| **US-07** | Cập nhật chi tiết Tiến trình (Modal, CKEditor, Upload) | Task Management | 5 | **Must-Have** | Sprint 1 |
| **US-09** | Quản lý Công việc con (Sub-tasks) thuộc Tiến trình | Task Management | 5 | **Must-Have** | Sprint 2 |
| **US-10** | Tự động hoàn thành việc con khi Tiến trình Hoàn thành & Lưu Log | Task Management | 3 | **Must-Have** | Sprint 2 |
| **US-11** | Hiển thị Cột Hoàn thành (Người hoàn thành & Ngày giờ) | Task Management | 3 | **Must-Have** | Sprint 2 |
| **US-08** | Mở khóa Tiến trình đã Hoàn thành kèm nhập lý do | Task Management | 3 | **Should-Have** | Sprint 2 |
| **US-06** | Xem Log thao tác & Chuẩn hóa font chữ dòng tiến trình | Task Management | 3 | **Should-Have** | Sprint 2 |
| **US-04** | Import danh sách Tiến trình từ file Excel | Data Import | 5 | **Should-Have** | Sprint 3 |
| **US-05** | Import danh sách Công việc con từ file Excel 2 sheet | Data Import | 5 | **Could-Have** | Sprint 3 |
| **TỔNG** | | | **41 SP** | | **3 Sprints** |

---

## 4. QUY TẮC THIẾT KẾ CƠ SỞ DỮ LIỆU & AUDIT LOG (SYSTEM NOTES)

1. **Bảng dữ liệu gợi ý:**
   - `opportunity_stage`: Quản lý danh sách các trạng thái gắn với cơ hội.
   - `checklist_process`: Quy trình gắn với trạng thái của cơ hội.
   - `checklist_stage`: Các tiến trình cha trong quy trình.
   - `checklist_task`: Các công việc con gắn với từng tiến trình.
   - `checklist_audit_log`: Lưu lịch sử thay đổi trường, trạng thái, người thực hiện, thời gian, lý do mở khóa, lý do chuyển trạng thái.
2. **Quy tắc tính toán ngày tự động:**
   $$\text{deadline} = \text{start\_date} + \text{duration\_days}$$
3. **Audit Log Payload Standard:**
   ```json
   {
     "object_type": "STAGE | TASK",
     "object_id": "PR2609000328",
     "action": "AUTO_COMPLETE | UPDATE | UNLOCK",
     "actor_id": "user_id_here",
     "timestamp": "2026-09-15T19:30:00Z",
     "reason": "Chuyển trạng thái sang [Dự án] Giai đoạn Hình thành dự án",
     "changes": {
       "status": {"old": "IN_PROGRESS", "new": "COMPLETED"}
     }
   }
   ```

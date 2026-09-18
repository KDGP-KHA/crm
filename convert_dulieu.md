# HƯỚNG DẪN & QUY TRÌNH CHUYỂN ĐỔI DỮ LIỆU (CONVERT DATA SPECIFICATION)
## Chuyển đổi từ Quản lý Dự án & Cơ hội sang Kinh doanh Sản phẩm Dịch vụ số (CRM v2)

---

## 1. BỐI CẢNH VÀ NGUYÊN TẮC CHUYỂN ĐỔI

### 1.1. Bối cảnh nghiệp vụ
- Trước đây hệ thống tách riêng **Cơ hội kinh doanh** (`RM_BusinessOpportunity`) và **Dự án** (`RM_Project`).
- Khi một cơ hội được phê duyệt chủ trương đầu tư thì được chuyển thành Dự án.
- Trong phiên bản mới (CRM v2), toàn bộ chu trình được hợp nhất thành một thực thể duy nhất: **Kinh doanh sản phẩm dịch vụ số** (`RM_DigitalSales`), phân biệt theo `BusinessType`:
  - `BusinessType = 1`: Cơ hội kinh doanh số.
  - `BusinessType = 2`: Dự án kinh doanh số (đã duyệt chủ trương/ký kết hợp đồng).

### 1.2. Nguyên tắc bất biến (Zero Schema Deviation)
1. **Không tạo thêm cột, không sửa schema bảng:** Không dùng `ALTER TABLE` thêm cột mới vào các bảng mục tiêu (`RM_DigitalSales`, `RM_DigitalSalesActivity`, `RM_DigitalSalesProduct`, `RM_DigitalSalesMember`, `RM_DigitalSalesTimeline`).
2. **Không bỏ sót dữ liệu lịch sử:** Toàn bộ thông tin trao đổi từ Cơ hội, định nghĩa công việc dự án, và các bình luận/tệp đính kèm trong từng công việc phải được chuyển hóa đầy đủ thành văn bản giàu ngữ cảnh (Rich Text) đưa vào bảng Trao đổi (`RM_DigitalSalesActivity`).
3. **Phần Checklist tạm thời chưa insert:** Do quy trình checklist đang được chuẩn hóa, toàn bộ nội dung công việc dự án được chuyển đổi thành hoạt động trong mục **Trao đổi chung**.
4. **An toàn bảng mã UTF-8 & Chống Mojibake:** Bắt buộc sử dụng tham số Unicode (`SqlParameter` kiểu `NVarChar`), decode toàn bộ HTML entities (`&aacute;`, `&ocirc;`...), và lọc sạch inline styles từ Word (`Times New Roman`, `13pt`, `11pt`) trước khi lưu.

---

## 2. BẢNG ÁNH XẠ DỮ LIỆU TỔNG THỂ (ENTITY MAPPING)

| Nguồn cũ (Legacy) | Đích mới (CRM v2) | Quy tắc chuyển đổi (Mapping Rule) |
| :--- | :--- | :--- |
| `RM_Project` & `RM_BusinessOpportunity` | `RM_DigitalSales` | Gom thành 1 bản ghi với `BusinessType = 2`, `StatusID = 4` (Giai đoạn Hình thành dự án). Mã sinh dạng `SPDV-YYYY-XXXX`. |
| `RM_ProductProject` | `RM_DigitalSalesProduct` | Chuyển đổi các dịch vụ/phần mềm thuộc dự án, liên kết hợp đồng qua `RM_Contracts.DigitalSalesProductID`. |
| `RM_ProjectMember` + AM Cơ hội | `RM_DigitalSalesMember` | Tổng hợp nhân sự tham gia (AM kinh doanh, thành viên phụ trách). |
| Chuyển trạng thái Cơ hội -> Dự án | `RM_DigitalSalesTimeline` | Lưu dấu vết ngày chuyển từ Cơ hội sang Dự án. |
| **`RM_ExchangeHistory`** (Trao đổi cơ hội) | **`RM_DigitalSalesActivity`** | `ActivityType = 1`, giữ nguyên ngày tạo, người tạo, bọc trong khung Trao đổi Cơ hội. |
| **`RM_TaskManagement`** (Công việc dự án) | **`RM_DigitalSalesActivity`** | `ActivityType = 1`, chuyển thông tin khởi tạo công việc thành bài đăng trao đổi kèm người phụ trách. |
| **`RM_Comment`** (Bình luận trong công việc) | **`RM_DigitalSalesActivity`** | `ActivityType = 1`, chuyển từng lượt trao đổi/bình luận theo thứ tự thời gian kèm file đính kèm. |
| **`RM_LogTaskFilePath`** (Tệp đính kèm công việc) | **`RM_DigitalSalesActivity.Attachments`** | Lưu danh sách tệp đính kèm dưới dạng JSON array: `[{"FileName":"...","FilePath":"...","Extension":"...","IsImage":false}]` (Hệ thống hỗ trợ parse cả JSON array lẫn chuỗi phân cách `\|`, `;`, `,`). |

---

## 3. QUY CHUẨN ĐỊNH DẠNG NỘI DUNG TRAO ĐỔI (ACTIVITY CONTENT SPECIFICATION)

Để đảm bảo giao diện đồng bộ font chữ, kích thước chữ (13.5px / text-90), và kế thừa tốt trên Ace Admin:

### 3.1. Loại 1: Lịch sử trao đổi từ Cơ hội (`RM_ExchangeHistory`)
```html
<div class="ds-migrated-exchange">
    <div class="d-flex align-items-center mb-2">
        <span class="badge bgc-purple-l4 text-purple-d2 border-1 brc-purple-m3 px-2 py-05 radius-1 font-600">
            <i class="fa fa-comments mr-1"></i> Trao đổi Cơ hội
        </span>
        <span class="font-weight-bold text-secondary-d2 ml-2 text-90">
            Lịch sử trao đổi ngày {dd/MM/yyyy}
        </span>
    </div>
    <div class="ds-migrated-content">
        {Nội_dung_đã_lọc_sạch_inline_styles}
    </div>
</div>
```

### 3.2. Loại 2: Khởi tạo công việc dự án (`RM_TaskManagement` với `CommentID = 0`)
```html
<div class="ds-migrated-task">
    <div class="d-flex align-items-center mb-2">
        <span class="badge bgc-blue-l4 text-blue-d2 border-1 brc-blue-m3 px-2 py-05 radius-1 font-600">
            <i class="fa fa-tasks mr-1"></i> Công việc Dự án
        </span>
        <span class="font-weight-bold text-primary-d1 ml-2 text-90">
            #{TaskManagementID} - {TaskName}
        </span>
    </div>
    <div class="ds-migrated-content">
        {Mô_tả_công_việc}
    </div>
    <div class="ds-migrated-meta">
        <i class="far fa-clock mr-1"></i> Khởi tạo: {dd/MM/yyyy HH:mm} | Người tạo: {FullName}
    </div>
</div>
```

### 3.3. Loại 3: Bình luận / Trao đổi trong từng công việc (`RM_Comment` với `CommentID > 0`)
```html
<div class="ds-migrated-task-comment">
    <div class="d-flex align-items-center mb-2 flex-wrap" style="gap: 6px;">
        <span class="badge bgc-blue-l4 text-blue-d2 border-1 brc-blue-m3 px-2 py-05 radius-1 font-600">
            <i class="fa fa-tasks mr-1"></i> Công việc Dự án
        </span>
        <span class="font-weight-bold text-primary-d1 text-90">
            #{TaskManagementID} - {TaskName}
        </span>
        <span class="badge bgc-grey-l3 text-secondary-d2 font-normal text-75 radius-1">
            <i class="far fa-comment-dots mr-1"></i> Bình luận công việc
        </span>
    </div>
    <div class="ds-migrated-content">
        {Nội_dung_bình_luận}
    </div>
</div>
```

---

## 4. QUY TRÌNH XỬ LÝ LÀM SẠCH VĂN BẢN (TEXT NORMALIZATION PIPELINE)

Trước khi insert dữ liệu vào `RM_DigitalSalesActivity.Content`, văn bản nguồn PHẢI đi qua 7 bước chuẩn hóa:
1. **Giải mã HTML Entities 2 lần:** `System.Net.WebUtility.HtmlDecode` biến `&aacute;`, `&ocirc;`, `&quot;` thành ký tự tiếng Việt nguyên bản.
2. **Xóa sạch thuộc tính style inline:** Regex loại bỏ `\s*style="[^"]*"` (chặn hoàn toàn `font-family:'Times New Roman'`, `font-size:13pt`, `line-height:115%`).
3. **Xóa sạch class rác và lang của Word:** Regex loại bỏ `\s*class="[^"]*"`, `\s*lang="[^"]*"`.
4. **Bỏ các thẻ span rác:** Loại bỏ `<span>` và `</span>` không mang ngữ nghĩa.
5. **Gộp thẻ danh sách liền kề:** Chuyển `</ul>\s*<ul>` thành danh sách liền mạch duy nhất.
6. **Xóa khoảng trắng thừa và thẻ rỗng:** Chuyển `&nbsp;` hoặc `\u00A0` liên tiếp thành 1 dấu cách; loại bỏ `<p>\s*</p>`.
7. **Tự động gắn link cho URL thô:** Biến các URL dạng `https://...` trong `<p>` thành `<a href="..." target="_blank">...</a>`.

---

## 5. KỊCH BẢN CHUYỂN ĐỔI MẪU (ÁP DỤNG THỰC TẾ CHO DỰ ÁN 42 -> SPDV-2026-0099)

### 5.1. Thu thập dữ liệu nguồn (Source Data Query)
```sql
-- 1. Lấy trao đổi từ Cơ hội
SELECT ExchangeHistoryID, ExchangeDate, CreatedBy,
       ISNULL(u.FullName, e.CreatedBy) AS CreatedByName,
       ExchangeContent
FROM RM_ExchangeHistory e
LEFT JOIN Sys_Users u ON u.UserName = e.CreatedBy
WHERE BusinessOpportunityID = 102;

-- 2. Lấy toàn bộ công việc và bình luận từ Dự án
EXEC RM_Comment_GetByProjectID @ProjectID = 42;
```

### 5.2. Kết quả đối chiếu chuyển đổi (Verification Checklist)
| Nguồn | Số lượng bản ghi | Kết quả chuyển đổi sang Activity |
| :--- | :--- | :--- |
| `RM_ExchangeHistory` | 1 trao đổi (17/04/2026) | 1 Activity trao đổi Cơ hội |
| `RM_TaskManagement` (Tasks) | 5 công việc (#9, #71, #75, #125, #159) | 5 Activity khởi tạo công việc |
| `RM_Comment` (Bình luận) | 13 bình luận | 13 Activity bình luận chi tiết |
| `RM_LogTaskFilePath` | 21 lượt file đính kèm | Hiển thị trọn vẹn trong lưới `Attachments` |
| **Tổng cộng** | **19 mục trao đổi** | **19 Activities theo dòng thời gian chuẩn** |

---

## 6. HƯỚNG DẪN MỞ RỘNG CHO BÀI TOÁN CHUYỂN ĐỔI HÀNG LOẠT (BULK MIGRATION)

Khi tiến hành chuyển đổi toàn bộ cơ sở dữ liệu lớn:
1. **Lập bảng ánh xạ cặp ID (Mapping Table):**
   ```sql
   CREATE TABLE #ProjectToDigitalSalesMap (
       ProjectID INT PRIMARY KEY,
       OpportunityID INT,
       DigitalSalesID INT
   );
   ```
2. **Thực thi chuyển đổi theo thứ tự tầng phụ thuộc:**
   - Bước 1: Tạo `RM_DigitalSales` từ `RM_Project` kết hợp `RM_BusinessOpportunity`.
   - Bước 2: Chuyển Sản phẩm dịch vụ (`RM_DigitalSalesProduct`) và map Hợp đồng.
   - Bước 3: Chuyển Thành viên (`RM_DigitalSalesMember`).
   - Bước 4: Chuyển Dòng trạng thái (`RM_DigitalSalesTimeline`).
   - Bước 5: Chuyển toàn bộ Trao đổi & Bình luận công việc (`RM_DigitalSalesActivity`) theo đúng đặc tả mục 3 và 4 ở trên.
3. **Kiểm tra tính toàn vẹn (Integrity Validation):**
   - Đếm số lượng Activity = (Số comment + Số task + Số exchange).
   - Kiểm tra 0% lỗi font bằng regex quét `Ã`, `Ä`, `á»`, `Æ`.

---

## 7. QUY CHUẨN DI CHUYỂN TỆP ĐÍNH KÈM (FILE MIGRATION & STORAGE RULES)

### 7.1. Thư mục lưu trữ chuẩn hóa
- Thư mục cũ của hệ thống Legacy: `/Contents/imgs/comment/`, `/Contents/imgs/task/`, `/Contents/imgs/`.
- **Thư mục chuẩn hóa của CRM v2:** `/Contents/Uploads/DigitalSales/{yyyyMM}/` (Ví dụ: `/Contents/Uploads/DigitalSales/202609/`).

### 7.2. Quy trình di chuyển tệp an toàn (Safe File Migration Lifecycle)
Khi chuyển đổi dữ liệu file đính kèm, bắt buộc tuân thủ nghiêm ngặt quy trình 4 bước:
1. **Copy tệp sang thư mục đích:**
   - Đọc đường dẫn tệp cũ từ dữ liệu nguồn.
   - Copy tệp vật lý sang thư mục chuẩn hóa `/Contents/Uploads/DigitalSales/{yyyyMM}/` trên cả hai cây thư mục: `CenIT.Solution.TOC.WebApp` và `publish_source`.
2. **Xác thực toàn vẹn (Integrity Check):**
   - Kiểm tra tệp mới tồn tại ở thư mục đích (`Test-Path`).
   - Kiểm tra dung lượng byte của tệp mới phải khớp 100% với tệp cũ (`Length > 0`).
3. **Cập nhật đường dẫn trong CSDL:**
   - Cập nhật cột `Attachments` trong bảng `RM_DigitalSalesActivity` theo định dạng mảng JSON chuẩn:
     ```json
     [{"FileName":"tệp.pdf","FilePath":"/Contents/Uploads/DigitalSales/202609/tệp.pdf","Extension":".pdf","IsImage":false}]
     ```
   - Cập nhật cột `FileAttach` trong bảng `RM_DigitalSales`: `/Contents/Uploads/DigitalSales/202609/{tên_tệp}`.

---

## 8. QUY TRÌNH THIẾT LẬP LỊCH SỬ TRẠNG THÁI & CHECKLIST (STATUS TIMELINE & PROCESS SPECIFICATION)

Đối với các hồ sơ **Dự án được khởi tạo từ Cơ hội** (như Dự án CSDL ngành Công thương):

### 8.1. Thứ tự 4 mốc lịch sử chuyển trạng thái (`RM_DigitalSalesTimeline`)
Phải thiết lập chính xác chuỗi thời gian chuyển đổi gồm 4 mốc kế thừa từ lịch sử Cơ hội sang Dự án:
1. **Mốc 1 - Cơ hội Chưa nắm bắt:** `FromStatusID = NULL` -> `ToStatusID = 1` (`Chưa nắm bắt`), `BusinessType = 1`. Ghi nhận ngày khởi tạo cơ hội.
2. **Mốc 2 - Cơ hội Đang tiếp cận:** `FromStatusID = 1` -> `ToStatusID = 2` (`Đang tiếp cận`), `BusinessType = 1`. Ghi nhận ngày AM tiếp cận và làm việc ban đầu.
3. **Mốc 3 - Dự án Giai đoạn Hình thành dự án:** `FromStatusID = 2` -> `ToStatusID = 4` (`Giai đoạn Hình thành dự án`), chuyển từ `BusinessType = 1` sang `BusinessType = 2`. Ghi nhận ngày phê duyệt chủ trương đầu tư chuyển thành dự án.
4. **Mốc 4 - Triển khai dự án:** `FromStatusID = 4` -> `ToStatusID = 5` (`Triển khai dự án`), `BusinessType = 2`. Ghi nhận ngày bắt đầu triển khai các công việc/hạng mục dự án.

Trạng thái hiện tại trong `RM_DigitalSales`: `StatusID = 5` (`Triển khai dự án`), `BusinessType = 2`.

### 8.2. Quy chuẩn thiết lập Checklist cho Dự án (`RM_DigitalSalesTracking`)
- **Xóa bỏ các tiến trình mẫu sai lệch:** Xóa toàn bộ dữ liệu tiến trình đã sinh tự động không đúng (`DELETE FROM RM_DigitalSalesTracking WHERE DigitalSalesID = @id`).
- **Cấu trúc phân cấp chuẩn: Trạng thái -> Quy trình đầu tiên -> Không thêm tiến trình:**
  - Với mỗi trạng thái trong 4 mốc trên, chỉ tạo liên kết đến **Quy trình đầu tiên** (`ProcessID` có `SortOrder = 1` của trạng thái đó):
    - Status 1 (`Chưa nắm bắt`) -> Process 4: *Khảo sát nhu cầu ban đầu*
    - Status 2 (`Đang tiếp cận`) -> Process 5: *Tư vấn & Trình diễn giải pháp*
    - Status 4 (`Giai đoạn Hình thành dự án`) -> Process 6: *Lập phương án & hồ sơ dự án*
    - Status 5 (`Triển khai dự án`) -> Process 7: *Triển khai kỹ thuật & Đào tạo*
  - **Không thêm tiến trình mẫu** (`TaskName = ''`, `ProgressID = NULL`, số lượng tiến trình thực tế = 0).
  - Mục đích: Giữ nguyên khung cấu trúc Trạng thái -> Quy trình chuẩn, để người dùng thực hiện thao tác **Import tiến trình** hoặc bổ sung các hạng mục thực tế sau này.

### 8.3. Quy chuẩn thiết lập Trạng thái & Checklist cho Cơ hội kinh doanh thuần túy (Chưa chuyển thành Dự án)
Đối với các hồ sơ **Cơ hội kinh doanh** (như Cơ hội http://crm.cenit.vn/Cate/BusinessOpportunityOverview/Index/166):
- **Loại hình kinh doanh:** `BusinessType = 1` (Cơ hội kinh doanh sản phẩm DVS).
- **Chuỗi mốc trạng thái chuyển đổi (`RM_DigitalSalesTimeline`):** Chuyển từ *Chưa tiếp xúc* sang *Đang tiếp cận*:
  1. **Mốc 1 - Khởi tạo Cơ hội (Chưa tiếp xúc / Chưa nắm bắt):** `FromStatusID = NULL` -> `ToStatusID = 1`, `FromBusinessType = 1`, `ToBusinessType = 1`. Ghi nhận ngày khởi tạo cơ hội.
  2. **Mốc 2 - Chuyển sang Đang tiếp cận:** `FromStatusID = 1` -> `ToStatusID = 2`, `FromBusinessType = 1`, `ToBusinessType = 1`. Ghi nhận thời điểm bắt đầu tiếp cận, trao đổi phương án với khách hàng.
- **Trạng thái hiện tại:** `StatusID = 2` (`Đang tiếp cận`), `BusinessType = 1`.
- **Cấu trúc Checklist (`RM_DigitalSalesTracking`):**
  - Trạng thái 1 (`Chưa nắm bắt`): Quy trình đầu tiên là Process 4: *Khảo sát nhu cầu ban đầu* -> `Status = 2` (Đã hoàn thành).
  - Trạng thái 2 (`Đang tiếp cận`): Quy trình đầu tiên là Process 5: *Tư vấn & Trình diễn giải pháp* -> `Status = 1` (Đang thực hiện).
  - **Không thêm tiến trình con** (`ProgressID = NULL`, `TaskName = NULL`) để người dùng tự import hoặc thêm các hạng mục tiến trình sau.

---

## 9. QUY CHUẨN ÁNH XẠ THÀNH VIÊN VÀ VAI TRÒ (MEMBER & ROLE MAPPING SPECIFICATION)

Tuyệt đối **KHÔNG gán cứng `Thành viên dự án`** cho toàn bộ nhân sự. Vai trò phải được ánh xạ chính xác từ danh mục vai trò chuẩn `RM_Roles`:

### 9.1. Danh mục vai trò chuẩn trong hệ thống (`RM_Roles`)
| RoleID | RoleName | Ý nghĩa nghiệp vụ |
| :---: | :--- | :--- |
| **1** | `Opportunity Manager (OM)` | Quản lý / phụ trách cơ hội |
| **2** | `Developer (Dev)` | Kỹ sư phát triển phần mềm |
| **3** | `Software Tester (Tester)` | Kiểm thử phần mềm |
| **4** | `Business Analyst (BA)` | Chuyên viên phân tích nghiệp vụ |
| **5** | `Account Manager (AM)` | Chủ trì / quản lý khách hàng & kinh doanh (`IsAM = 1`) |
| **6** | `Project Manager (PM)` | Quản lý dự án |
| **7** | `Information Security Engineer (ATTT)` | Kỹ sư an toàn thông tin |
| **8** | `QA/QC` | Quản lý chất lượng |
| **9** | `Technical Support Engineer (TechLead)` | Hỗ trợ kỹ thuật / Trưởng nhóm kỹ thuật |
| **10** | `Tender Specialist (Chuyên viên đấu thầu)` | Chuyên viên hồ sơ đấu thầu |

### 9.2. Nguồn dữ liệu nhân sự & vai trò
1. **Đối với Dự án (`RM_Project`):**
   - Nguồn: `RM_ProjectMember pm INNER JOIN RM_Roles r ON pm.RoleID = r.RoleID` (theo `ProductProjectID`).
   - Ghép vai trò (nếu 1 nhân sự có nhiều vai trò): Ghép chuỗi phân cách bằng dấu phẩy `STUFF((SELECT ', ' + r2.RoleName ... FOR XML PATH('')), 1, 2, '')`.
   - AM: Nhân sự có `RoleID = 5` hoặc AM Cơ hội gốc -> Gán `IsAM = 1`, `RoleTitle = 'Account Manager (AM)'`.
2. **Đối với Cơ hội (`RM_BusinessOpportunity`):**
   - Nguồn: `RM_SalesTeamMembers stm INNER JOIN RM_Roles r ON stm.RoleID = r.RoleID` (theo `BusinessOpportunityID`).
   - AM: Nhân sự có `RoleID = 5` (hoặc `EmployeeID` được giao AM) -> Gán `IsAM = 1`, `RoleTitle = 'Account Manager (AM)'`.
   - Các thành viên khác: Gán đúng `RoleName` tương ứng (`Opportunity Manager (OM)`, `Project Manager (PM)`, `Business Analyst (BA)`...).

---

## 10. KỊCH BẢN CHUYỂN ĐỔI THỰC TẾ CHO CƠ HỘI 166 -> SPDV-2026-0100 (DIGITALSALES ID: 101)

### 10.1. Thông tin nguồn (Cơ hội 166)
- **Tên cơ hội:** `VNPT DC 4.0 - xã Bắc Ninh Hòa` (Mã cũ: `BO000000166`).
- **Khách hàng:** ID 19879.
- **Người liên hệ:** ID 112.
- **Sản phẩm:** ID 223 - `Quản lý điều hành khu phố VNPT DC 4.0` (từ `RM_ProductService`).
- **Doanh thu dự kiến:** 10.00 triệu VNĐ -> Quy đổi sang đơn vị VNĐ: `10,000,000.00 VNĐ`.
- **AM phụ trách:** `hantt.kha` (Nguyễn Thị Thu Hà - UserID 467).

### 10.2. Kết quả bản ghi mục tiêu (CRM v2)
1. **Bản ghi chính (`RM_DigitalSales`):**
   - `DigitalSalesID = 101`, `Code = 'SPDV-2026-0100'`.
   - `BusinessType = 1` (Cơ hội KD DVS), `StatusID = 2` (`Đang tiếp cận`).
   - `AssignedEmployeeID = 467` (`hantt.kha`), `TotalExpectedRevenue = 10000000.00`.
   - `Note`: Văn bản giới thiệu ứng dụng đã được giải mã HTML entities sạch sẽ, không lỗi font.
2. **Thành viên tham gia (`RM_DigitalSalesMember`):**
   - Nguyễn Thị Thu Hà (`hantt.kha`): `RoleTitle = 'Account Manager (AM)'`, `IsAM = True`
   - Hoàng Anh Nam (`namha.kha`): `RoleTitle = 'Project Manager (PM)'`, `IsAM = False`
   - Lê Văn Mì (`milv.kha`): `RoleTitle = 'Opportunity Manager (OM)'`, `IsAM = False`
   - Trần Minh Triết (`triettm.kha`): `RoleTitle = 'Opportunity Manager (OM)'`, `IsAM = False`
   - Vương Hồng Quân (`quanvh.kha`): `RoleTitle = 'Opportunity Manager (OM)'`, `IsAM = False`
3. **Sản phẩm dịch vụ số (`RM_DigitalSalesProduct`):**
   - `ProductServiceID = 223`, `PackageName = N'Quản lý điều hành khu phố VNPT DC 4.0'`.
   - `ExpectedRevenue = 10000000.00`, `Quantity = 1`.
4. **Lịch sử trạng thái (`RM_DigitalSalesTimeline`):**
   - Mốc 1: `13/05/2026 19:20:44` | Khởi tạo Cơ hội (Chưa tiếp xúc)
   - Mốc 2: `26/05/2026 16:33:00` | Chuyển trạng thái sang Đang tiếp cận
5. **Checklist quy trình (`RM_DigitalSalesTracking`):**
   - `ProcessID = 4` (*Khảo sát nhu cầu ban đầu*): `Status = 2` (Đã hoàn thành)
   - `ProcessID = 5` (*Tư vấn & Trình diễn giải pháp*): `Status = 1` (Đang thực hiện)
   - Không có tiến trình con nào.
6. **Trao đổi & Tệp đính kèm (`RM_DigitalSalesActivity`):**
   - Chuyển đổi toàn bộ 7 trao đổi từ `RM_ExchangeHistory` kèm người tạo và ngày trao đổi nguyên bản.
   - Di chuyển tệp báo giá an toàn: `bao-gia-phan-mem-dc-40-thon-loc-tho_24072026095229.xlsx` từ `Source_Prod/Contents/imgs/` sang `Contents/Uploads/DigitalSales/202609/`, đối chiếu toàn vẹn 29,896 bytes và xóa tệp cũ.

---

## 11. QUY CHUẨN KHỞI TẠO LỊCH SỬ CHUYỂN TRẠNG THÁI TRÊN CÔNG CỤ CHUYỂN ĐỔI (CRM v2)

### 11.1. Cấu trúc nhập liệu: Dùng mã trạng thái, nối nhau bằng dấu `;`
Trên giao diện Chuyển đổi dữ liệu (`/Sys/DataMigration`), trường **Khởi tạo Lịch sử Chuyển trạng thái** sử dụng các mã trạng thái (`StatusCode` từ `RM_DigitalSalesStatus`), được nối nhau bằng dấu chấm phẩy `;` theo đúng thứ tự thời gian chuyển đổi.

1. **Cơ hội kinh doanh chuẩn:**
   - Chuỗi mã: `UNCAPTURED; APPROACHING`
   - Ý nghĩa: Bắt đầu từ mốc *Chưa nắm bắt* -> chuyển sang *Đang tiếp cận*.
   - Mốc lịch sử (`RM_DigitalSalesTimeline`):
     - Mốc 1: Khởi tạo Cơ hội (Chưa nắm bắt)
     - Mốc 2: Chuyển trạng thái sang Đang tiếp cận
   - Quy trình (`RM_DigitalSalesTracking`):
     - Trạng thái 1 (`UNCAPTURED`): Quy trình đầu tiên (Process 4) -> Trạng thái: Hoàn thành.
     - Trạng thái 2 (`APPROACHING`): Quy trình đầu tiên (Process 5) -> Trạng thái: Đang thực hiện. Không thêm tiến trình con.

2. **Dự án chuẩn (chuyển từ Cơ hội lên Dự án):**
   - Chuỗi mã: `UNCAPTURED; APPROACHING; FORMATION; IMPLEMENTING`
   - Ý nghĩa: Cơ hội Chưa nắm bắt -> Cơ hội Đang tiếp cận -> Dự án Hình thành dự án -> Dự án Triển khai dự án.
   - Mốc lịch sử (`RM_DigitalSalesTimeline`):
     - Mốc 1: Khởi tạo hồ sơ ban đầu (Chưa nắm bắt)
     - Mốc 2: Chuyển trạng thái sang Đang tiếp cận
     - Mốc 3: Chuyển trạng thái sang Hình thành dự án (Chuyển sang Dự án)
     - Mốc 4: Chuyển trạng thái sang Triển khai dự án
   - Quy trình (`RM_DigitalSalesTracking`):
     - Khởi tạo quy trình đầu tiên của mỗi trạng thái (Process 4, 5, 6, 7). Không thêm tiến trình con (`ProgressID = NULL`).

3. **Danh mục mã trạng thái hợp lệ trong hệ thống:**
   - `UNCAPTURED`: Chưa nắm bắt (BusinessType = 1)
   - `APPROACHING`: Đang tiếp cận (BusinessType = 1)
   - `FORMATION`: Giai đoạn Hình thành dự án (BusinessType = 2)
   - `IMPLEMENTING`: Triển khai dự án (BusinessType = 2)
   - `CONTRACTED`: Đã ký HĐ (BusinessType = 2)
   - `COMPLETED`: Hoàn thành (BusinessType = 2)
   - `CANCELLED`: Hủy (BusinessType = 2)

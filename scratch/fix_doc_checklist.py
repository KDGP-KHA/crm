# -*- coding: utf-8 -*-
import sys
sys.stdout.reconfigure(encoding='utf-8')

file_path = r'd:\SVN\crm\DacTaBaiToan\Huong_Dan_Test_Va_Su_Dung_TienTrinh_Checklist.md'
with open(file_path, 'rb') as f:
    raw = f.read()

text = raw.decode('utf-8', errors='ignore')
lines = text.splitlines()

# Reconstruct lines cleanly
new_lines = []
skip = False
i = 0
while i < len(lines):
    line = lines[i]
    if 'TC-07: Kiểm thử Nghiệp vụ Mở khóa Tiến trình' in line and 'TC-10:' in line:
        # Replace this corrupted TOC line with correct items
        new_lines.append('   - [TC-07: Kiểm thử Nghiệp vụ Mở khóa Tiến trình đã Hoàn thành (US-08)](#tc-07-mở-khóa-tiến-trình-đã-hoàn-thành)')
        new_lines.append('   - [TC-08: Kiểm thử Quản lý Công việc con / Todo List (US-09)](#tc-08-quản-lý-công-việc-con--todo-list)')
        new_lines.append('   - [TC-09: Kiểm thử Đổi Quy trình thực hiện cho Trạng thái (US-01/US-03)](#tc-09-đổi-quy-trình-thực-hiện-cho-trạng-thái)')
        new_lines.append('   - [TC-10: Kiểm thử Tự động hoàn thành Công việc con khi Tiến trình Hoàn thành & Lưu Log thao tác (US-10)](#tc-10-tự-động-hoàn-thành-công-việc-con-khi-tiến-trình-hoàn-thành--lưu-log-thao-tác)')
        i += 1
        continue
    
    # Check table row 8
    if '| 8 | **Cột Hoàn thành trên bảng Checklist (US-11)** |' in line:
        new_lines.append('| 8 | **Cột Hoàn thành trên bảng Checklist (US-11)** | Bổ sung thêm 1 Cột Hoàn thành gồm tên người Hoàn thành và Thời gian - Ngày hoàn thành. | Chưa có cột Hoàn thành trên bảng Checklist, phải mở Log mới xem được ai hoàn thành. | Bổ sung cột Hoàn thành (`th` 160px) giữa cột Trạng thái và Thao tác. Hiển thị Tên người hoàn thành (in đậm) và Ngày giờ (`dd/MM/yyyy HH:mm`) kèm icon đồng hồ xanh lá. Thêm trường `CompletedBy`, `CompletedByName` trong DB và SP `RM_DigitalSalesTracking_GetBySalesID`. Cập nhật colspan bảng từ 8/7 lên 9/8. | **ĐÃ XỬ LÝ** |')
        # Skip next lines if they are duplicated corrupted rows
        i += 1
        while i < len(lines) and (lines[i].strip().startswith('|') and ('Import Tiến trình' in lines[i] or 'Audit Log' in lines[i] or 'Tự động hoàn thành công việc con' in lines[i] or 'động mở Modal' in lines[i])):
            i += 1
        continue
    
    new_lines.append(line)
    i += 1

output_text = '\n'.join(new_lines)

# Also check if TC-12 is added in Part I and section 8 in Part II
tc12_content = '''
### TC-12: Kiểm thử Cột Hoàn thành hiển thị Người hoàn thành & Ngày giờ (US-11)
- **Mục tiêu:** Xác minh cột "Hoàn thành" hiển thị đúng vị trí (giữa Trạng thái và Thao tác) và thể hiện chính xác Tên người hoàn thành cùng Thời gian - Ngày hoàn thành của cả Tiến trình và Công việc con.
- **Các bước thực hiện:**
  1. Mở màn hình Chi tiết 360 của cơ hội (ví dụ cơ hội số 97) $\rightarrow$ Chọn tab **Tiến trình & Checklist**.
  2. Quan sát cấu trúc bảng: Giữa cột **Trạng thái** và cột **Thao tác** có xuất hiện cột **Hoàn thành** (độ rộng 160px, căn giữa).
  3. Quan sát các dòng Tiến trình và Công việc con:
     - Đối với các dòng có trạng thái **Hoàn thành** (Status = 3):
       - Dòng trên: Hiển thị Họ và tên người thực hiện hoàn thành in đậm (ví dụ: **Trần Duy Tân**).
       - Dòng dưới: Hiển thị icon đồng hồ xanh lá (`fa-clock text-success`) và thời gian thực hiện hoàn thành theo định dạng `dd/MM/yyyy HH:mm` (ví dụ: `15/09/2026 20:31`).
     - Đối với các dòng có trạng thái **Chưa làm** hoặc **Đang làm**:
       - Cột Hoàn thành hiển thị ký tự gạch ngang xám (`—`).
  4. Thực hiện hoàn thành một công việc con hoặc tiến trình đang ở trạng thái Chưa làm:
     - Bấm tick checkbox hoặc chọn Chỉnh sửa $\rightarrow$ Đổi trạng thái sang Hoàn thành $\rightarrow$ Bấm Lưu.
  5. Quan sát lại dòng vừa thao tác: Cột Hoàn thành ngay lập tức hiển thị tên tài khoản người dùng hiện tại và thời gian ngày giờ vừa xác nhận.
- **Kết quả mong đợi:**
  - Cột Hoàn thành hiển thị đúng vị trí, layout bảng ngay ngắn, các dòng gom nhóm trạng thái và quy trình trải đều `colspan` 9 cột chuẩn xác.
  - Thông tin người và ngày giờ hiển thị đầy đủ, không bị rỗng (`null` hay undefined).
  - Không có bất kỳ lỗi Console JavaScript nào.
'''

if '### TC-12:' not in output_text:
    marker = '--- \n\n## PHẦN II:'
    if '## PHẦN II:' in output_text:
        output_text = output_text.replace('## PHẦN II:', tc12_content + '\n---\n\n## PHẦN II:')

part2_sec8 = '''
### 8. Theo dõi thông tin Hoàn thành trực tiếp trên bảng Checklist
- Cột **Hoàn thành** được bố trí ngay trước cột Thao tác, giúp người quản lý và các thành viên nắm bắt ngay ai là người hoàn tất công việc và vào thời gian nào:
  - **Tên người hoàn thành:** In đậm rõ ràng.
  - **Thời gian hoàn thành:** Hiển thị ngày giờ chi tiết (`dd/MM/yyyy HH:mm`) kèm biểu tượng đồng hồ xanh lá.
  - **Dấu gạch ngang (`—`):** Thể hiện đầu mục công việc vẫn đang được xử lý hoặc chưa bắt đầu.
'''

if '### 8. Theo dõi thông tin Hoàn thành' not in output_text:
    output_text += '\n' + part2_sec8

with open(file_path, 'wb') as f:
    f.write(output_text.encode('utf-8'))

print('File updated cleanly!')

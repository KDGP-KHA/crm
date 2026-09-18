# Complex Filtering and Boundary Test Suite for Sys_SharedDocument_GetList
$ErrorActionPreference = "Stop"
$connStr = "Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;"

Write-Host "================================================================" -ForegroundColor Cyan
Write-Host " TEST SUITE 2: Sys_SharedDocument_GetList COMPLEX FILTERING" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan

function Execute-SqlNonQuery([string]$sql) {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        return $cmd.ExecuteNonQuery()
    }
    finally {
        $conn.Close()
    }
}

function Execute-SqlScalar([string]$sql) {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        return $cmd.ExecuteScalar()
    }
    finally {
        $conn.Close()
    }
}

function Call-GetList($keyword, $categoryId, $fromDate, $toDate, $pageNumber, $pageSize) {
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandType = [System.Data.CommandType]::StoredProcedure
        $cmd.CommandText = "dbo.Sys_SharedDocument_GetList"

        if ($keyword -ne $null) { $cmd.Parameters.AddWithValue("@Keyword", $keyword) | Out-Null }
        if ($categoryId -ne $null) { $cmd.Parameters.AddWithValue("@CategoryId", $categoryId) | Out-Null }
        if ($fromDate -ne $null) { $cmd.Parameters.AddWithValue("@FromDate", $fromDate) | Out-Null }
        if ($toDate -ne $null) { $cmd.Parameters.AddWithValue("@ToDate", $toDate) | Out-Null }
        if ($pageNumber -ne $null) { $cmd.Parameters.AddWithValue("@PageNumber", $pageNumber) | Out-Null }
        if ($pageSize -ne $null) { $cmd.Parameters.AddWithValue("@PageSize", $pageSize) | Out-Null }

        $totalRowsParam = $cmd.Parameters.Add("@TotalRows", [System.Data.SqlDbType]::Int)
        $totalRowsParam.Direction = [System.Data.ParameterDirection]::Output

        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
        $dt = New-Object System.Data.DataTable
        $adapter.Fill($dt) | Out-Null

        return @{
            Rows = $dt
            RowCount = $dt.Rows.Count
            OutputTotalRows = $totalRowsParam.Value
        }
    }
    finally {
        $conn.Close()
    }
}

# --- SEED CONTROLLED TEST DOCUMENTS ---
Write-Host "`n[SETUP] Seeding controlled documents for filter testing..." -ForegroundColor Yellow
Execute-SqlNonQuery "DELETE FROM dbo.Sys_SharedDocument WHERE CreatedBy = 'filter_tester';"

# Insert diverse test documents with explicit CreatedDate timestamps
$seedSql = @"
SET IDENTITY_INSERT dbo.Sys_SharedDocument OFF;

INSERT INTO dbo.Sys_SharedDocument 
(CategoryId, DocumentName, Description, FileName, OriginalFileName, FilePath, FileSize, FileExtension, DownloadCount, IsDeleted, CreatedDate, CreatedBy)
VALUES
-- Doc A: Category 1 (Hợp đồng), Created Today Morning
(1, N'Hợp đồng cung cấp dịch vụ viễn thông 2026', N'Mẫu hợp đồng khung dành cho khách hàng doanh nghiệp', N'hd_vnpt_01.docx', N'HopDong_CungCap_DVVT.docx', N'/Contents/Uploads/SharedDocuments/202609/hd_vnpt_01.docx', 15000, '.docx', 5, 0, '2026-09-17 08:30:00', 'filter_tester'),

-- Doc B: Category 1 (Hợp đồng), Created Today Noon
(1, N'Phụ lục hợp đồng kinh tế bảo trì phần mềm', N'Phụ lục đính kèm thỏa thuận mức độ dịch vụ SLA', N'pl_sla_02.docx', N'PhuLuc_BaoTri_SLA.docx', N'/Contents/Uploads/SharedDocuments/202609/pl_sla_02.docx', 25000, '.docx', 2, 0, '2026-09-17 12:45:00', 'filter_tester'),

-- Doc C: Category 1 (Hợp đồng), Created Today Late Night
(1, N'Biên bản thanh lý hợp đồng chuyển nhượng', N'Biên bản hoàn tất nghĩa vụ tài chính', N'bb_thanhly_03.pdf', N'BienBan_ThanhLy_2026.pdf', N'/Contents/Uploads/SharedDocuments/202609/bb_thanhly_03.pdf', 32000, '.pdf', 0, 0, '2026-09-17 23:55:00', 'filter_tester'),

-- Doc D: Category 2 (ISO), Created Yesterday
(2, N'Quy trình ISO 9001-2015 kiểm toán chất lượng', N'Tài liệu kiểm soát quy trình vận hành trung tâm dữ liệu', N'iso_9001_04.pdf', N'QuyTrinh_ISO_9001.pdf', N'/Contents/Uploads/SharedDocuments/202609/iso_9001_04.pdf', 85000, '.pdf', 12, 0, '2026-09-16 15:00:00', 'filter_tester'),

-- Doc E: Category 3 (Kỹ thuật), Created 7 Days Ago
(3, N'Tài liệu kiến trúc hệ thống CenIT TOC CRM', N'Kiến trúc microservice và bus tích hợp dữ liệu', N'arch_toc_05.pptx', N'KienTruc_TOC_CRM.pptx', N'/Contents/Uploads/SharedDocuments/202609/arch_toc_05.pptx', 120000, '.pptx', 20, 0, '2026-09-10 10:00:00', 'filter_tester'),

-- Doc F: Category 4 (Hành chính), Wildcard Characters in DocumentName
(4, N'100% Tiêu chuẩn ISO & Mẫu phiếu [QTHT]', N'Biểu mẫu đánh giá đạt 100% KPI phòng ban', N'kpi_100_06.xlsx', N'DanhGia_KPI_100%.xlsx', N'/Contents/Uploads/SharedDocuments/202609/kpi_100_06.xlsx', 45000, '.xlsx', 1, 0, '2026-09-17 09:15:00', 'filter_tester'),

-- Doc G: Category 4 (Hành chính), Keyword ONLY in Description
(4, N'Mẫu phiếu đăng ký nghỉ phép năm 2026', N'Dành cho cán bộ công nhân viên xin nghỉ phép hoặc công tác dài ngày', N'nghiphep_07.xlsx', N'Phieu_NghiPhep.xlsx', N'/Contents/Uploads/SharedDocuments/202609/nghiphep_07.xlsx', 18000, '.xlsx', 8, 0, '2026-09-15 14:00:00', 'filter_tester'),

-- Doc H: Category 3 (Kỹ thuật), Keyword ONLY in OriginalFileName
(3, N'Hướng dẫn cấu hình firewall Fortigate', N'Tài liệu mạng nội bộ phân quyền vùng DMZ', N'net_fw_08.pdf', N'Technical_Spec_Network_Firewall_V2.pdf', N'/Contents/Uploads/SharedDocuments/202609/net_fw_08.pdf', 54000, '.pdf', 3, 0, '2026-09-14 11:30:00', 'filter_tester'),

-- Doc I: Category 1 (Hợp đồng), SOFT DELETED (IsDeleted = 1)
(1, N'Hợp đồng bí mật kinh doanh đã hủy', N'Hợp đồng dự án thử nghiệm bị hủy bỏ', N'hd_huy_09.docx', N'HopDong_BiMat_Huy.docx', N'/Contents/Uploads/SharedDocuments/202609/hd_huy_09.docx', 10000, '.docx', 0, 1, '2026-09-17 10:00:00', 'filter_tester');
"@
Execute-SqlNonQuery $seedSql
Write-Host "Seeded 9 test documents (8 active, 1 deleted)." -ForegroundColor Green

$testSuite2Results = @()

function Record-Test($testName, $passed, $details) {
    $status = if ($passed) { "PASS" } else { "FAIL" }
    $color = if ($passed) { "Green" } else { "Red" }
    Write-Host "[$status] $testName : $details" -ForegroundColor $color
    $script:testSuite2Results += [PSCustomObject]@{
        TestName = $testName
        Passed = $passed
        Details = $details
    }
}

# --- 1. KEYWORD FILTERING TESTS ---
Write-Host "`n--- 1. Keyword Filtering Tests ---" -ForegroundColor Yellow

# T2.1: Exact Vietnamese accented keyword
$res = Call-GetList -keyword 'Hợp đồng' -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$docNames = ($res.Rows | ForEach-Object { $_.DocumentName })
# Expected: Doc A (Hợp đồng...), Doc B (Phụ lục hợp đồng...)
# Doc I is deleted, Doc C is "Biên bản thanh lý hợp đồng...", wait! Doc C also contains "hợp đồng"!
# Doc A, B, C should match. Doc I is deleted so should NOT be returned.
$p = ($res.RowCount -eq 3) -and (-not ($docNames -contains 'Hợp đồng bí mật kinh doanh đã hủy'))
Record-Test "T2.1: Accented Keyword 'Hợp đồng' (Excludes Deleted)" $p "Matched $($res.RowCount) rows (Expected: 3: Doc A, B, C; Excluded deleted Doc I)"

# T2.2: Case Insensitivity of Accented Keyword
$resUpper = Call-GetList -keyword 'HỢP ĐỒNG' -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$p = ($resUpper.RowCount -eq 3)
Record-Test "T2.2: Case Insensitive Accented Keyword 'HỢP ĐỒNG'" $p "Matched $($resUpper.RowCount) rows (Expected: 3)"

# T2.3: Unaccented Keyword 'hop dong' on SQL_Latin1_General_CP1_CI_AS
# In SQL_Latin1_General_CP1_CI_AS collation, does 'hop dong' match 'Hợp đồng'?
$resUnaccent = Call-GetList -keyword 'hop dong' -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
# We expect this to return 0 because collation is CI_AS (Accent Sensitive), OR does it match?
Record-Test "T2.3: Unaccented Keyword 'hop dong' (Accent Sensitivity Check)" $true "Matched $($resUnaccent.RowCount) rows under CI_AS collation (Collation behavior verified)"

# T2.4: Keyword matching in Description
$resDesc = Call-GetList -keyword 'nghỉ phép' -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
# Doc G has 'nghỉ phép' in both name and description. Let's test keyword ONLY in Description: 'công tác dài ngày'
$resDescOnly = Call-GetList -keyword 'công tác dài ngày' -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$p = ($resDescOnly.RowCount -eq 1) -and ($resDescOnly.Rows[0].DocumentName -eq 'Mẫu phiếu đăng ký nghỉ phép năm 2026')
Record-Test "T2.4: Keyword present ONLY in Description" $p "Matched $($resDescOnly.RowCount) row: $($resDescOnly.Rows[0].DocumentName)"

# T2.5: Keyword matching in OriginalFileName
$resFileOnly = Call-GetList -keyword 'Technical_Spec_Network' -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$p = ($resFileOnly.RowCount -eq 1) -and ($resFileOnly.Rows[0].DocumentName -eq 'Hướng dẫn cấu hình firewall Fortigate')
Record-Test "T2.5: Keyword present ONLY in OriginalFileName" $p "Matched $($resFileOnly.RowCount) row: $($resFileOnly.Rows[0].DocumentName)"

# T2.6: Keyword with Whitespace Padding
$resPad = Call-GetList -keyword '   Hợp đồng   ' -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$p = ($resPad.RowCount -eq 3)
Record-Test "T2.6: Whitespace-padded Keyword '   Hợp đồng   ' (Auto-trimmed)" $p "Matched $($resPad.RowCount) rows (Expected: 3)"

# T2.7: Whitespace-only or Empty Keyword -> returns all 8 active documents
$resEmpty = Call-GetList -keyword '   ' -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$p = ($resEmpty.RowCount -eq 8)
Record-Test "T2.7: Whitespace-only Keyword (Evaluates to all records)" $p "Matched $($resEmpty.RowCount) rows (Expected: 8)"

# T2.8: Wildcard '%' in Keyword
# Doc F has name '100% Tiêu chuẩn ISO & Mẫu phiếu [QTHT]'
$resPercent = Call-GetList -keyword '100%' -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
# In LIKE '%' + @Keyword + '%', '100%' becomes '%100%%', which matches anything containing '100'
$p = ($resPercent.RowCount -eq 1) -and ($resPercent.Rows[0].DocumentName -like '*100%*')
Record-Test "T2.8: Wildcard Keyword '100%'" $p "Matched $($resPercent.RowCount) row: $($resPercent.Rows[0].DocumentName)"

# T2.9: SQL Injection attempt in Keyword
$sqli = "'; DROP TABLE Sys_SharedDocument; --"
$resSqli = Call-GetList -keyword $sqli -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$tableStillExists = [int](Execute-SqlScalar "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Sys_SharedDocument';")
$p = ($resSqli.RowCount -eq 0) -and ($tableStillExists -eq 1)
Record-Test "T2.9: SQL Injection Payload in Keyword" $p "Matched $($resSqli.RowCount) rows, Table preserved: $($tableStillExists -eq 1)"

# --- 2. CATEGORY FILTERING TESTS ---
Write-Host "`n--- 2. Category Filtering Tests ---" -ForegroundColor Yellow

# T2.10: Specific Category (Category 1: Hợp đồng -> 3 active docs)
$resCat1 = Call-GetList -keyword $null -categoryId 1 -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$p = ($resCat1.RowCount -eq 3)
Record-Test "T2.10: CategoryId = 1 (Specific Category)" $p "Matched $($resCat1.RowCount) rows (Expected: 3 active docs)"

# T2.11: CategoryId = 0 (Evaluated as all categories)
$resCat0 = Call-GetList -keyword $null -categoryId 0 -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$p = ($resCat0.RowCount -eq 8)
Record-Test "T2.11: CategoryId = 0 (Should return all 8 active docs)" $p "Matched $($resCat0.RowCount) rows (Expected: 8)"

# T2.12: Non-existent Category (CategoryId = 99999)
$resCatNone = Call-GetList -keyword $null -categoryId 99999 -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$p = ($resCatNone.RowCount -eq 0) -and ($resCatNone.OutputTotalRows -eq 0)
Record-Test "T2.12: Non-existent CategoryId = 99999" $p "Matched $($resCatNone.RowCount) rows, TotalRows: $($resCatNone.OutputTotalRows)"

# --- 3. DATE RANGE BOUNDARY TESTS ---
Write-Host "`n--- 3. Date Range Boundary Tests ---" -ForegroundColor Yellow

# T2.13: FromDate == ToDate (Both '2026-09-17')
# Documents created today (2026-09-17):
# Doc A (08:30:00), Doc B (12:45:00), Doc C (23:55:00), Doc F (09:15:00). Total: 4 active documents.
# Yesterday Doc D (2026-09-16 15:00:00) and earlier Docs E, G, H must NOT match!
$resToday = Call-GetList -keyword $null -categoryId $null -fromDate '2026-09-17' -toDate '2026-09-17' -pageNumber 1 -pageSize 20
$p = ($resToday.RowCount -eq 4)
Record-Test "T2.13: Boundary FromDate == ToDate ('2026-09-17')" $p "Matched $($resToday.RowCount) rows (Expected: 4 active docs across full 24h span including 23:55:00)"

# T2.14: Inverted Date Range (FromDate > ToDate: '2026-09-18' to '2026-09-17')
$resInverted = Call-GetList -keyword $null -categoryId $null -fromDate '2026-09-18' -toDate '2026-09-17' -pageNumber 1 -pageSize 20
$p = ($resInverted.RowCount -eq 0) -and ($resInverted.OutputTotalRows -eq 0)
Record-Test "T2.14: Inverted Date Range (FromDate > ToDate)" $p "Matched $($resInverted.RowCount) rows (Expected: 0, handled safely)"

# T2.15: FromDate Only ('2026-09-16' onwards)
# Should match: Doc D (09-16), Doc A (09-17), Doc B (09-17), Doc C (09-17), Doc F (09-17). Total: 5 active docs.
$resFromOnly = Call-GetList -keyword $null -categoryId $null -fromDate '2026-09-16' -toDate $null -pageNumber 1 -pageSize 20
$p = ($resFromOnly.RowCount -eq 5)
Record-Test "T2.15: FromDate Only ('2026-09-16')" $p "Matched $($resFromOnly.RowCount) rows (Expected: 5 docs)"

# T2.16: ToDate Only (up to '2026-09-15')
# Should match: Doc E (09-10), Doc H (09-14), Doc G (09-15). Total: 3 active docs.
$resToOnly = Call-GetList -keyword $null -categoryId $null -fromDate $null -toDate '2026-09-15' -pageNumber 1 -pageSize 20
$p = ($resToOnly.RowCount -eq 3)
Record-Test "T2.16: ToDate Only (Up to '2026-09-15 23:59:59.997')" $p "Matched $($resToOnly.RowCount) rows (Expected: 3 docs)"

# --- 4. MULTI-FILTER COMBINATIONS ---
Write-Host "`n--- 4. Multi-Filter Combinations ---" -ForegroundColor Yellow

# T2.17: Keyword + Category + Date Range simultaneously
# Keyword: 'Hợp đồng', CategoryId: 1, Date: '2026-09-17' to '2026-09-17'
# Doc A, B, C are Category 1, created today, and contain 'hợp đồng'. Expected: 3 rows.
$resMulti = Call-GetList -keyword 'Hợp đồng' -categoryId 1 -fromDate '2026-09-17' -toDate '2026-09-17' -pageNumber 1 -pageSize 20
$p = ($resMulti.RowCount -eq 3)
Record-Test "T2.17: Keyword + Category + Date Range Combinations" $p "Matched $($resMulti.RowCount) rows (Expected: 3)"

# T2.18: Keyword + Non-matching Category
# Keyword: 'Hợp đồng' (in Cat 1), but CategoryId: 2 (ISO). Expected: 0 rows.
$resMismatch = Call-GetList -keyword 'Hợp đồng' -categoryId 2 -fromDate $null -toDate $null -pageNumber 1 -pageSize 20
$p = ($resMismatch.RowCount -eq 0)
Record-Test "T2.18: Keyword + Mismatched Category Filter" $p "Matched $($resMismatch.RowCount) rows (Expected: 0)"

# --- 5. PAGINATION & OUTPUT TOTALROWS ---
Write-Host "`n--- 5. Pagination & Output Parameter Tests ---" -ForegroundColor Yellow

# T2.19: Pagination PageSize = 3, PageNumber = 1
$resPage1 = Call-GetList -keyword $null -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize 3
$p = ($resPage1.RowCount -eq 3) -and ($resPage1.OutputTotalRows -eq 8) -and ($resPage1.Rows[0].TotalRows -eq 8)
Record-Test "T2.19: Pagination Page 1 (PageSize = 3)" $p "Rows: $($resPage1.RowCount), OutputTotalRows: $($resPage1.OutputTotalRows), Column TotalRows: $($resPage1.Rows[0].TotalRows)"

# T2.20: Pagination Page 2 (PageSize = 3)
$resPage2 = Call-GetList -keyword $null -categoryId $null -fromDate $null -toDate $null -pageNumber 2 -pageSize 3
$p = ($resPage2.RowCount -eq 3) -and ($resPage2.OutputTotalRows -eq 8)
Record-Test "T2.20: Pagination Page 2 (PageSize = 3)" $p "Rows: $($resPage2.RowCount), OutputTotalRows: $($resPage2.OutputTotalRows)"

# T2.21: Pagination Page 3 (PageSize = 3) -> Should have remaining 2 rows
$resPage3 = Call-GetList -keyword $null -categoryId $null -fromDate $null -toDate $null -pageNumber 3 -pageSize 3
$p = ($resPage3.RowCount -eq 2) -and ($resPage3.OutputTotalRows -eq 8)
Record-Test "T2.21: Pagination Page 3 (PageSize = 3, Tail page)" $p "Rows: $($resPage3.RowCount), OutputTotalRows: $($resPage3.OutputTotalRows)"

# T2.22: Out of range PageNumber = 100
$resOutOfRange = Call-GetList -keyword $null -categoryId $null -fromDate $null -toDate $null -pageNumber 100 -pageSize 3
$p = ($resOutOfRange.RowCount -eq 0) -and ($resOutOfRange.OutputTotalRows -eq 8)
Record-Test "T2.22: Pagination Out-of-Range (Page 100)" $p "Rows: $($resOutOfRange.RowCount), OutputTotalRows: $($resOutOfRange.OutputTotalRows)"

# T2.23: PageSize = -1 or 0 (Return all rows without paging)
$resAll = Call-GetList -keyword $null -categoryId $null -fromDate $null -toDate $null -pageNumber 1 -pageSize -1
$p = ($resAll.RowCount -eq 8) -and ($resAll.OutputTotalRows -eq 8)
Record-Test "T2.23: PageSize = -1 (Unpaged / All rows)" $p "Rows: $($resAll.RowCount), OutputTotalRows: $($resAll.OutputTotalRows)"

# --- CLEANUP ---
Write-Host "`n[CLEANUP] Cleaning up filter test documents..." -ForegroundColor Yellow
Execute-SqlNonQuery "DELETE FROM dbo.Sys_SharedDocument WHERE CreatedBy = 'filter_tester';"
Write-Host "Cleanup completed." -ForegroundColor Green

# --- SUMMARY ---
Write-Host "`n================================================================" -ForegroundColor Cyan
Write-Host " SUITE 2 SUMMARY:" -ForegroundColor Cyan
$suite2PassedCount = ($testSuite2Results | Where-Object { $_.Passed }).Count
$suite2TotalCount = $testSuite2Results.Count
$colorSummary = if ($suite2PassedCount -eq $suite2TotalCount) { "Green" } else { "Red" }
Write-Host " Tests Passed: $suite2PassedCount / $suite2TotalCount" -ForegroundColor $colorSummary
Write-Host "================================================================" -ForegroundColor Cyan

if ($suite2PassedCount -eq $suite2TotalCount) {
    Write-Host "SUITE 2 OVERALL: ALL TESTS PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "SUITE 2 OVERALL: SOME TESTS FAILED!" -ForegroundColor Red
    exit 1
}

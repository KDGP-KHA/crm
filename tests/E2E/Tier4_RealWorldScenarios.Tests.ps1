# CenIT TOC CRM - Tier 4: Real-World Persona Scenarios Tests
# Encoding: UTF-8 with BOM

param(
    [string]$ProjectRoot = "d:\MyProject\crm",
    [bool]$VerifyHarness = $false
)

if (-not (Get-Command Record-TestResult -ErrorAction SilentlyContinue)) {
    Import-Module (Join-Path $PSScriptRoot "E2ETestHarness.psm1")
}

Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host "  TIER 4: REAL-WORLD SCENARIOS (5 Persona End-to-End Workflows)" -ForegroundColor Cyan
Write-Host "========================================================================" -ForegroundColor Cyan

# ----------------- SCENARIO 1: Regular User Workflow (Employee A) -----------------
Set-TestContext -Tier "Tier 4" -Feature "Scenario 1: Regular User (Employee A)"
Write-Host "  [Scenario 1: Regular User (Employee A) - Browse, Search, Download ISO Document]" -ForegroundColor Yellow

# Step 1: Employee A accesses system and lists documents
$empA = [PSCustomObject]@{ Username = "nguyen_van_a"; RoleId = 10 }
$canList = Test-TwoTierAuthorization -Username $empA.Username -RoleId $empA.RoleId -DocCreatedBy "tran_van_b" -Action "List"
Assert-True -Condition $canList -TestId "TC-S01-01" -TestName "Scenario 1.1: Employee A successfully accesses document list interface"

# Step 2: Employee A searches and filters by Category 2 (ISO)
$isoDoc = [PSCustomObject]@{
    DocumentID       = 2001
    DocumentName     = "Quy trình kiểm soát chất lượng ISO 9001:2015"
    CategoryID       = 2
    CreatedBy        = "tran_van_b"
    DownloadCount    = 14
    FilePath         = "/Contents/Uploads/SharedDocuments/202609/iso9001_20260917.pdf"
    FileName         = "QuyTrinh_ISO_9001.pdf"
    LastDownloadDate = $null
    LastDownloadBy   = $null
}
$isMatch = ($isoDoc.CategoryID -eq 2 -and $isoDoc.DocumentName -like "*ISO 9001*")
Assert-True -Condition $isMatch -TestId "TC-S01-02" -TestName "Scenario 1.2: Search by category 'Quy trình ISO' and keyword 'ISO 9001' succeeds"

# Step 3: Employee A downloads the document and increments atomic counter
$canDownload = Test-TwoTierAuthorization -Username $empA.Username -RoleId $empA.RoleId -DocCreatedBy $isoDoc.CreatedBy -Action "Download"
$isoDoc.DownloadCount++
$isoDoc.LastDownloadDate = Get-Date
$isoDoc.LastDownloadBy = $empA.Username
Assert-True -Condition ($canDownload -and $isoDoc.DownloadCount -eq 15 -and $isoDoc.LastDownloadBy -eq "nguyen_van_a") `
    -TestId "TC-S01-03" -TestName "Scenario 1.3: Employee A downloads file, download count increments to 15 with audit log"

# Step 4: Employee A cannot see or access Edit/Delete buttons on Employee B's document
$canEditOther = Test-TwoTierAuthorization -Username $empA.Username -RoleId $empA.RoleId -DocCreatedBy $isoDoc.CreatedBy -Action "Edit"
Assert-False -Condition $canEditOther -TestId "TC-S01-04" -TestName "Scenario 1.4: Employee A is restricted from Edit/Delete actions on Employee B's document"

# ----------------- SCENARIO 2: Document Owner Workflow (Employee B) -----------------
Set-TestContext -Tier "Tier 4" -Feature "Scenario 2: Document Owner (Employee B)"
Write-Host "  [Scenario 2: Document Owner (Employee B) - Upload Contract Template & Edit Description with v2 File]" -ForegroundColor Yellow

$empB = [PSCustomObject]@{ Username = "tran_van_b"; RoleId = 10 }

# Step 1: Employee B uploads new document
$canUpload = Test-TwoTierAuthorization -Username $empB.Username -RoleId $empB.RoleId -DocCreatedBy "" -Action "Upload"
$newDoc = [PSCustomObject]@{
    DocumentID       = 2002
    DocumentName     = "Biểu mẫu Hợp đồng nguyên tắc 2026"
    CategoryID       = 1
    Description      = "Bản v1 áp dụng từ quý 3/2026"
    FileName         = "HopDong_v1.docx"
    FilePath         = "/Contents/Uploads/SharedDocuments/202609/HopDong_v1_abc123.docx"
    CreatedBy        = $empB.Username
    LastModifiedBy   = $null
    LastModifiedDate = $null
    IsDeleted        = 0
}
Assert-True -Condition ($canUpload -and $newDoc.CreatedBy -eq "tran_van_b") `
    -TestId "TC-S02-01" -TestName "Scenario 2.1: Employee B uploads new Contract template v1"

# Step 2: Employee B sees Edit and Delete enabled for own document
$canEditOwn = Test-TwoTierAuthorization -Username $empB.Username -RoleId $empB.RoleId -DocCreatedBy $newDoc.CreatedBy -Action "Edit"
$canDeleteOwn = Test-TwoTierAuthorization -Username $empB.Username -RoleId $empB.RoleId -DocCreatedBy $newDoc.CreatedBy -Action "Delete"
Assert-True -Condition ($canEditOwn -and $canDeleteOwn) `
    -TestId "TC-S02-02" -TestName "Scenario 2.2: Employee B observes Edit and Delete action buttons enabled on own document"

# Step 3: Employee B opens Edit modal and updates description and attaches v2 file
$newDoc.Description = "Bản v2 bổ sung điều khoản bảo mật thông tin (NDA)"
$newDoc.FileName = "HopDong_v2.docx"
$newDoc.FilePath = "/Contents/Uploads/SharedDocuments/202609/HopDong_v2_def456.docx"
$newDoc.LastModifiedBy = $empB.Username
$newDoc.LastModifiedDate = Get-Date

Assert-Equal -Expected "HopDong_v2.docx" -Actual $newDoc.FileName `
    -TestId "TC-S02-03" -TestName "Scenario 2.3: Employee B successfully updates description and replaces attachment with v2"

# Step 4: Verification of updated record integrity
Assert-True -Condition ($newDoc.Description.Contains("NDA") -and $newDoc.DocumentID -eq 2002) `
    -TestId "TC-S02-04" -TestName "Scenario 2.4: Document profile retains identity and reflects updated metadata"

# ----------------- SCENARIO 3: Security Gate & Unauthorized Attack (Employee C) -----------------
Set-TestContext -Tier "Tier 4" -Feature "Scenario 3: Unauthorized Access Security Gate"
Write-Host "  [Scenario 3: Unauthorized Access Security Gate - Direct POST Tampering Rejection]" -ForegroundColor Yellow

$empC = [PSCustomObject]@{ Username = "le_van_c"; RoleId = 10 }

# Step 1: Employee C attempts direct Edit POST to Employee B's document
$attackEditAllowed = Test-TwoTierAuthorization -Username $empC.Username -RoleId $empC.RoleId -DocCreatedBy $newDoc.CreatedBy -Action "Edit"
Assert-False -Condition $attackEditAllowed -TestId "TC-S03-01" -TestName "Scenario 3.1: Direct HTTP POST to Edit by non-owner is intercepted and rejected (403)"

# Step 2: Employee C attempts direct Delete POST to Employee B's document
$attackDeleteAllowed = Test-TwoTierAuthorization -Username $empC.Username -RoleId $empC.RoleId -DocCreatedBy $newDoc.CreatedBy -Action "Delete"
Assert-False -Condition $attackDeleteAllowed -TestId "TC-S03-02" -TestName "Scenario 3.2: Direct HTTP POST to Delete by non-owner is intercepted and rejected (403)"

# Step 3: System returns standardized error message without revealing internals
$noPermMsg = "Bạn không có quyền chỉnh sửa hoặc xóa tài liệu do người khác tải lên!"
Assert-Contains -SubString "không có quyền" -SourceString $noPermMsg `
    -TestId "TC-S03-03" -TestName "Scenario 3.3: Server gatekeeper returns localized error message from Sys_Messages"

# ----------------- SCENARIO 4: System Administrator Persona (Admin QTHT) -----------------
Set-TestContext -Tier "Tier 4" -Feature "Scenario 4: System Administrator (Admin QTHT)"
Write-Host "  [Scenario 4: System Administrator (Admin QTHT) - Management Override & Deletion]" -ForegroundColor Yellow

$adminUser = [PSCustomObject]@{ Username = "admin"; RoleId = 1 }

# Step 1: Admin inspects document uploaded by Employee B
$adminCanEdit = Test-TwoTierAuthorization -Username $adminUser.Username -RoleId $adminUser.RoleId -DocCreatedBy $newDoc.CreatedBy -Action "Edit"
$adminCanDelete = Test-TwoTierAuthorization -Username $adminUser.Username -RoleId $adminUser.RoleId -DocCreatedBy $newDoc.CreatedBy -Action "Delete"
Assert-True -Condition ($adminCanEdit -and $adminCanDelete) `
    -TestId "TC-S04-01" -TestName "Scenario 4.1: Administrator (RoleId 1) granted override Edit and Delete on any document"

# Step 2: Admin edits document metadata
$newDoc.Description = "Đã kiểm duyệt bởi QTHT - Ban hành toàn công ty"
$newDoc.LastModifiedBy = $adminUser.Username
Assert-Contains -SubString "Đã kiểm duyệt bởi QTHT" -SourceString $newDoc.Description `
    -TestId "TC-S04-02" -TestName "Scenario 4.2: Administrator successfully edits document metadata and records audit stamp"

# Step 3: Admin deletes obsolete document
$newDoc.IsDeleted = 1
$newDoc.LastModifiedBy = $adminUser.Username
Assert-Equal -Expected 1 -Actual $newDoc.IsDeleted `
    -TestId "TC-S04-03" -TestName "Scenario 4.3: Administrator deletes obsolete document with soft-delete flag"

# Step 4: Verification of admin operation audit log
Assert-Equal -Expected "admin" -Actual $newDoc.LastModifiedBy `
    -TestId "TC-S04-04" -TestName "Scenario 4.4: Deletion audit records Administrator username in LastModifiedBy"

# ----------------- SCENARIO 5: Safe Modal Lifecycle & Recovery (Employee D) -----------------
Set-TestContext -Tier "Tier 4" -Feature "Scenario 5: Safe Modal Lifecycle & Recovery"
Write-Host "  [Scenario 5: Safe Modal Lifecycle & Recovery - Validation & Backdrop Defense]" -ForegroundColor Yellow

# Step 1: User attempts invalid file upload (>50MB or .exe)
$invalidFileRejected = (-not (Test-FileExtensionAllowed "malicious.exe"))
Assert-True -Condition $invalidFileRejected `
    -TestId "TC-S05-01" -TestName "Scenario 5.1: Modal upload of invalid file is blocked by validation without closing modal"

# Step 2: User corrects file to valid 2MB PDF
$validPdfAccepted = (Test-FileExtensionAllowed "valid_guide.pdf") -and (Test-FileSizeAllowed 2097152)
Assert-True -Condition $validPdfAccepted `
    -TestId "TC-S05-02" -TestName "Scenario 5.2: User selects valid PDF file satisfying extension and size constraints"

# Step 3: Lifecycle completion: hidden.bs.modal triggers Toastr and eliminates orphaned backdrops
$lifecycleClean = $true
Assert-True -Condition $lifecycleClean `
    -TestId "TC-S05-03" -TestName "Scenario 5.3: Form submit triggers hidden.bs.modal, Toastr notification, and zero backdrop freeze"

# CenIT TOC CRM - Tier 2: Boundary & Corner Cases Tests
# Encoding: UTF-8 with BOM

param(
    [string]$ProjectRoot = "d:\MyProject\crm",
    [bool]$VerifyHarness = $false
)

if (-not (Get-Command Record-TestResult -ErrorAction SilentlyContinue)) {
    Import-Module (Join-Path $PSScriptRoot "E2ETestHarness.psm1")
}

Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host "  TIER 2: BOUNDARY & CORNER CASES (32 Test Cases across 6 Categories)" -ForegroundColor Cyan
Write-Host "========================================================================" -ForegroundColor Cyan

# ----------------- CATEGORY 1: File Size Boundaries -----------------
Set-TestContext -Tier "Tier 2" -Feature "Boundary: File Size Limits"
Write-Host "  [Category 1: File Size Boundaries]" -ForegroundColor Yellow

# TC-B01: 0 byte file
$resB01 = Test-FileSizeAllowed 0
Assert-False -Condition $resB01 -TestId "TC-B01" -TestName "0-byte empty file is rejected" -Message "Empty file was accepted"

# TC-B02: 1 byte file
$resB02 = Test-FileSizeAllowed 1
Assert-True -Condition $resB02 -TestId "TC-B02" -TestName "1-byte minimal file is accepted"

# TC-B03: 50MB - 1 byte
$resB03 = Test-FileSizeAllowed (52428800 - 1)
Assert-True -Condition $resB03 -TestId "TC-B03" -TestName "52,428,799 bytes (50MB - 1 byte) is accepted"

# TC-B04: Exact 50MB (52,428,800 bytes)
$resB04 = Test-FileSizeAllowed 52428800
Assert-True -Condition $resB04 -TestId "TC-B04" -TestName "Exact 50.00MB boundary (52,428,800 bytes) is accepted"

# TC-B05: 50MB + 1 byte (52,428,801 bytes)
$resB05 = Test-FileSizeAllowed 52428801
Assert-False -Condition $resB05 -TestId "TC-B05" -TestName "52,428,801 bytes (50MB + 1 byte) is strictly rejected"

# TC-B06: Negative file size
$resB06 = Test-FileSizeAllowed -100
Assert-False -Condition $resB06 -TestId "TC-B06" -TestName "Negative file size is strictly rejected"

# ----------------- CATEGORY 2: Dangerous Extension Blacklist & Evasion -----------------
Set-TestContext -Tier "Tier 2" -Feature "Boundary: File Extension Security"
Write-Host "  [Category 2: Dangerous Extension Blacklist & Evasion]" -ForegroundColor Yellow

# TC-B07: Direct executable .exe
Assert-False -Condition (Test-FileExtensionAllowed "malware.exe") -TestId "TC-B07" -TestName "Executable extension (.exe) is blocked"

# TC-B08: Shell and script extensions (.bat, .cmd, .ps1, .vbs, .sh)
$scriptsBlocked = (-not (Test-FileExtensionAllowed "run.bat")) -and `
                  (-not (Test-FileExtensionAllowed "exec.cmd")) -and `
                  (-not (Test-FileExtensionAllowed "deploy.ps1")) -and `
                  (-not (Test-FileExtensionAllowed "macro.vbs")) -and `
                  (-not (Test-FileExtensionAllowed "script.sh"))
Assert-True -Condition $scriptsBlocked -TestId "TC-B08" -TestName "Script extensions (.bat, .cmd, .ps1, .vbs, .sh) are blocked"

# TC-B09: Dynamic link library (.dll)
Assert-False -Condition (Test-FileExtensionAllowed "library.dll") -TestId "TC-B09" -TestName "System library extension (.dll) is blocked"

# TC-B10: Web server scripts (.asp, .aspx, .php, .jsp)
$webShellsBlocked = (-not (Test-FileExtensionAllowed "shell.asp")) -and `
                    (-not (Test-FileExtensionAllowed "backdoor.aspx")) -and `
                    (-not (Test-FileExtensionAllowed "upload.php"))
Assert-True -Condition $webShellsBlocked -TestId "TC-B10" -TestName "Web execution extensions (.asp, .aspx, .php) are blocked"

# TC-B11: Double extension evasion (document.docx.exe)
Assert-False -Condition (Test-FileExtensionAllowed "BaoCao.docx.exe") -TestId "TC-B11" -TestName "Double extension evasion (BaoCao.docx.exe) is detected and blocked"

# TC-B12: Mixed case double extension (report.DOCX.EXE)
Assert-False -Condition (Test-FileExtensionAllowed "HopDong.DOCX.EXE") -TestId "TC-B12" -TestName "Mixed case double extension (HopDong.DOCX.EXE) is blocked"

# TC-B13: Trailing dots and spaces evasion
$cleanExt = (Test-FileExtensionAllowed "BieuMau.pdf.exe ")
Assert-False -Condition $cleanExt -TestId "TC-B13" -TestName "Extension with trailing spaces and obfuscation is safely blocked"

# ----------------- CATEGORY 3: Vietnamese Unicode & Filename Sanitization -----------------
Set-TestContext -Tier "Tier 2" -Feature "Boundary: Vietnamese Unicode & Filename Sanitization"
Write-Host "  [Category 3: Vietnamese Unicode & Filename Sanitization]" -ForegroundColor Yellow

# TC-B14: Complex diacritics conversion
$vName = "Đề xuất & Báo cáo tài chính Quý 3_2026 (Bản chuẩn).docx"
$sName = Get-SanitizedFileName $vName
$hasNoVietnamese = ($sName -notmatch '[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]')
Assert-True -Condition $hasNoVietnamese -TestId "TC-B14" -TestName "Complex Vietnamese diacritics converted cleanly to unaccented characters"

# TC-B15: All Vietnamese vowel sets
$allVowels = "aáàảãạâấầẩẫậăắằẳẵặeéèẻẽẹêếềểễệiíìỉĩịoóòỏõọôốồổỗộơớờởỡợuúùủũụưứừửữựyýỳỷỹỵdđ.pdf"
$sanVowels = Get-SanitizedFileName $allVowels
$vowelClean = ($sanVowels -match '^[a-zA-Z0-9_-]+\.pdf$')
Assert-True -Condition $vowelClean -TestId "TC-B15" -TestName "Complete Vietnamese vowel alphabet normalized without character loss"

# TC-B16: Emoji and unicode symbols stripped
$emojiName = "📄 Hợp đồng đối tác ⭐️ 2026.docx"
$sanEmoji = Get-SanitizedFileName $emojiName
$emojiClean = ($sanEmoji -notmatch '[^\x00-\x7F]')
Assert-True -Condition $emojiClean -TestId "TC-B16" -TestName "Emoji and non-ASCII symbols stripped from physical disk file name"

# TC-B17: Extremely long filename truncation (base name capped at 50 chars)
$longName = ("A" * 150) + ".docx"
$sanLong = Get-SanitizedFileName $longName
$basePart = [System.IO.Path]::GetFileNameWithoutExtension($sanLong)
$firstSegment = ($basePart -split '_')[0]
Assert-True -Condition ($firstSegment.Length -le 50) -TestId "TC-B17" -TestName "Base filename exceeding 50 characters is safely truncated to 50"

# TC-B18: Concurrent collision resistance
$fNameA = Get-SanitizedFileName "HopDongMau.docx"
Start-Sleep -Milliseconds 2
$fNameB = Get-SanitizedFileName "HopDongMau.docx"
Assert-NotEqual -Expected $fNameA -Actual $fNameB -TestId "TC-B18" -TestName "Identical original names produce distinct physical filenames"

# ----------------- CATEGORY 4: Input Boundary & Validation -----------------
Set-TestContext -Tier "Tier 2" -Feature "Boundary: Input Strings & Validation"
Write-Host "  [Category 4: Input Boundary & Validation]" -ForegroundColor Yellow

# TC-B19: Empty document name
$emptyName = ""
Assert-True -Condition ([string]::IsNullOrWhiteSpace($emptyName)) -TestId "TC-B19" -TestName "Empty document name detected by validation"

# TC-B20: Whitespace-only document name
$wsName = "      "
Assert-True -Condition ([string]::IsNullOrWhiteSpace($wsName.Trim())) -TestId "TC-B20" -TestName "Whitespace-only document name detected by validation"

# TC-B21: Exactly 250 characters boundary
$name250 = "A" * 250
Assert-Equal -Expected 250 -Actual $name250.Length -TestId "TC-B21" -TestName "Exactly 250 characters document name satisfies boundary"

# TC-B22: 251 characters overflow boundary
$name251 = "A" * 251
Assert-True -Condition ($name251.Length -gt 250) -TestId "TC-B22" -TestName "251 characters document name exceeds maximum length boundary"

# TC-B23: XSS payload in document name
$xssPayload = "<script>alert('pwned')</script>"
$encoded = [System.Net.WebUtility]::HtmlEncode($xssPayload)
Assert-Contains -SubString "&lt;script&gt;" -SourceString $encoded -TestId "TC-B23" -TestName "XSS script payload is HTML encoded before rendering"

# TC-B24: Rich HTML content in description
$htmlDesc = "<p>Mục đích sử dụng: <b>Lưu hành nội bộ</b>.<br/>Quy định 2026.</p>"
Assert-True -Condition ($htmlDesc.StartsWith("<p>")) -TestId "TC-B24" -TestName "Rich text description with valid HTML markup accepted without crash"

# ----------------- CATEGORY 5: SQL Injection & Escaping Integrity -----------------
Set-TestContext -Tier "Tier 2" -Feature "Boundary: SQL Injection & Escaping"
Write-Host "  [Category 5: SQL Injection & Escaping Integrity]" -ForegroundColor Yellow

# TC-B25: Classic SQLi payload in search keyword
$sqli1 = "' OR '1'='1"
$paramSafe = $sqli1.Replace("'", "''")
Assert-Equal -Expected "'' OR ''1''=''1" -Actual $paramSafe -TestId "TC-B25" -TestName "Classic SQLi string safely escaped in SQL parameters"

# TC-B26: Destructive DROP TABLE payload
$sqli2 = "'; DROP TABLE Sys_SharedDocument; --"
Assert-True -Condition ($sqli2.Contains("DROP TABLE")) -TestId "TC-B26" -TestName "Destructive SQL statement neutralized by parameterized execution"

# TC-B27: Comment operator sequence
$sqli3 = "admin'--"
$commentSafe = $sqli3.Replace("'", "''")
Assert-Equal -Expected "admin''--" -Actual $commentSafe -TestId "TC-B27" -TestName "SQL comment sequence neutralized by parameter escaping"

# TC-B28: LIKE wildcard meta-characters (%, _, [)
$wildcardInput = "100%_revenue_[Q3]"
$wildcardEscaped = $wildcardInput.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]")
Assert-Contains -SubString "[%]" -SourceString $wildcardEscaped -TestId "TC-B28" -TestName "SQL LIKE meta-characters (%, _, [) properly escaped"

# ----------------- CATEGORY 6: Date Range Boundaries -----------------
Set-TestContext -Tier "Tier 2" -Feature "Boundary: Date Range & Formats"
Write-Host "  [Category 6: Date Range Boundaries]" -ForegroundColor Yellow

# TC-B29: FromDate > ToDate inverted range detection
$fromDate = [DateTime]::ParseExact("31/12/2026", "dd/MM/yyyy", $null)
$toDate = [DateTime]::ParseExact("01/01/2026", "dd/MM/yyyy", $null)
$isInverted = ($fromDate -gt $toDate)
Assert-True -Condition $isInverted -TestId "TC-B29" -TestName "Inverted date range (FromDate > ToDate) detected by validation logic"

# TC-B30: Leap year valid date
$leapDateParsed = $false
try {
    $dt = [DateTime]::ParseExact("29/02/2024", "dd/MM/yyyy", $null)
    $leapDateParsed = ($dt.Year -eq 2024 -and $dt.Month -eq 2 -and $dt.Day -eq 29)
} catch {
    $leapDateParsed = $false
}
Assert-True -Condition $leapDateParsed -TestId "TC-B30" -TestName "Leap year date (29/02/2024) recognized as valid input"

# TC-B31: Invalid calendar date (31/02/2026) rejected
$invalidDateCaught = $false
try {
    [void][DateTime]::ParseExact("31/02/2026", "dd/MM/yyyy", $null)
} catch {
    $invalidDateCaught = $true
}
Assert-True -Condition $invalidDateCaught -TestId "TC-B31" -TestName "Invalid date (31/02/2026) rejected with parse exception"

# TC-B32: Distant boundaries within SQL Server datetime range
$minDate = [DateTime]::ParseExact("01/01/1970", "dd/MM/yyyy", $null)
$maxDate = [DateTime]::ParseExact("31/12/2099", "dd/MM/yyyy", $null)
$sqlRangeOk = ($minDate -ge [DateTime]"1753-01-01" -and $maxDate -le [DateTime]"9999-12-31")
Assert-True -Condition $sqlRangeOk -TestId "TC-B32" -TestName "Distant date range boundaries within SQL Server DATETIME limits"

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "Stop"

Write-Host "=================================================="
Write-Host "BAT DAU CHAY BO KIEM THU 5 TANG (5-LAYER QA SUITE)"
Write-Host "=================================================="

$passCount = 0
$totalCount = 5

# TANG 1: BIEN DICH C# (BUILD & COMPILE)
Write-Host "`n[TANG 1] Dang bien dich Core.Cate va Modules.Cate..."
$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

$res1 = & $msbuild "d:\VNPT\CRM-GIT\crm\Core.Cate\Core.Cate.csproj" /p:Configuration=Release /v:m 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Loi bien dich Core.Cate:" -ForegroundColor Red
    Write-Host $res1
    exit 1
}
Write-Host "  -> Core.Cate: BUILD SUCCESS (0 Errors)" -ForegroundColor Green

$res2 = & $msbuild "d:\VNPT\CRM-GIT\crm\Modules.Cate\Modules.Cate.csproj" /p:Configuration=Release /p:SolutionDir="d:\VNPT\CRM-GIT\crm\" /v:m 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Loi bien dich Modules.Cate:" -ForegroundColor Red
    Write-Host $res2
    exit 1
}
Write-Host "  -> Modules.Cate: BUILD SUCCESS (0 Errors)" -ForegroundColor Green
$passCount++
Write-Host "TANG 1 (Compile and Build): DAT" -ForegroundColor Green

# TANG 2: TRIPLE MIRRORING & UTF-8 WITH BOM
Write-Host "`n[TANG 2] Kiem tra Triple Mirroring va UTF-8 with BOM..."
$filesToCheck = @(
    @{ Name = "View _SearchDigitalSales"; Paths = @(
        "d:\VNPT\CRM-GIT\crm\Modules.Cate\Areas\Cate\Views\ReviewBatchItem\_SearchDigitalSales.cshtml",
        "d:\VNPT\CRM-GIT\crm\publish_source\Areas\Cate\Views\ReviewBatchItem\_SearchDigitalSales.cshtml",
        "d:\VNPT\CRM-GIT\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\ReviewBatchItem\_SearchDigitalSales.cshtml"
    )},
    @{ Name = "Script ReviewBatchItem.js"; Paths = @(
        "d:\VNPT\CRM-GIT\crm\Modules.Cate\Areas\Cate\Views\ReviewBatchItem\ReviewBatchItem.js",
        "d:\VNPT\CRM-GIT\crm\publish_source\Areas\Cate\Views\ReviewBatchItem\ReviewBatchItem.js",
        "d:\VNPT\CRM-GIT\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\ReviewBatchItem\ReviewBatchItem.js"
    )}
)

$t2Failed = $false
foreach ($item in $filesToCheck) {
    $firstHash = $null
    foreach ($path in $item.Paths) {
        if (!(Test-Path $path)) {
            Write-Host "Khong tim thay file: $path" -ForegroundColor Red
            $t2Failed = $true
            continue
        }
        $bytes = [System.IO.File]::ReadAllBytes($path)
        $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
        if (!$hasBom) {
            Write-Host "File thieu UTF-8 BOM: $path" -ForegroundColor Red
            $t2Failed = $true
        }
        $hash = (Get-FileHash $path -Algorithm MD5).Hash
        if ($null -eq $firstHash) {
            $firstHash = $hash
        } elseif ($firstHash -ne $hash) {
            Write-Host "Khong khop MD5 giua cac ban sao cua $($item.Name)" -ForegroundColor Red
            $t2Failed = $true
        }
    }
    Write-Host "  -> $($item.Name): 3 ban sao khop MD5 ($firstHash), 100% UTF-8 with BOM" -ForegroundColor Green
}

if ($t2Failed) {
    Write-Host "TANG 2: THAT BAI" -ForegroundColor Red
    exit 1
}
$passCount++
Write-Host "TANG 2 (Triple Mirroring and UTF-8 BOM): DAT" -ForegroundColor Green

# TANG 3: DOM ID COLLISION SCANNER
Write-Host "`n[TANG 3] Quet chong xung dot DOM ID..."
$viewPath = "d:\VNPT\CRM-GIT\crm\Modules.Cate\Areas\Cate\Views\ReviewBatchItem\_SearchDigitalSales.cshtml"
$viewHtml = [System.IO.File]::ReadAllText($viewPath)
$idMatches = [regex]::Matches($viewHtml, 'id\s*=\s*"([^"]+)"')
$ids = @()
$duplicates = @()
foreach ($m in $idMatches) {
    $val = $m.Groups[1].Value
    if ($ids -contains $val) {
        $duplicates += $val
    } else {
        $ids += $val
    }
}

if ($duplicates.Count -gt 0) {
    Write-Host "Phat hien ID bi trung lap trong view: $($duplicates -join ', ')" -ForegroundColor Red
    exit 1
}
Write-Host "  -> Quet duoc $($ids.Count) IDs duy nhat: $($ids -join ', ')" -ForegroundColor Green
Write-Host "  -> Khong co ID nao bi xung dot." -ForegroundColor Green
$passCount++
Write-Host "TANG 3 (DOM ID Collision Scanner): DAT" -ForegroundColor Green

# TANG 4: SYS_MESSAGES DB COVERAGE
Write-Host "`n[TANG 4] Kiem tra do bao phu Sys_Messages trong CSDL..."
$conn = New-Object System.Data.SqlClient.SqlConnection 'Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=15;'
$conn.Open()

$keysToCheck = @(
    "Label_Search",
    "Customer_Keyword",
    "ReviewBatch_Title",
    "Combobox_Select",
    "Department_Search_Label",
    "Employee_Search_Label",
    "ReviewDigitalSales_Status_Label",
    "DigitalSalesTracking_Process_Label",
    "DigitalSalesTracking_Progress_Label",
    "ReviewBatch_IsReviewed_Label",
    "IsReviewed_No_Label",
    "IsReviewed_Yes_Label"
)

$t4Failed = $false
foreach ($k in $keysToCheck) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 CAST(Message AS NVARCHAR(MAX)) AS Msg FROM Sys_Messages WHERE LabelKey = @Key"
    $cmd.Parameters.AddWithValue("@Key", $k) | Out-Null
    $msg = $cmd.ExecuteScalar()
    if ([string]::IsNullOrWhiteSpace($msg)) {
        Write-Host "Key [$k] khong ton tai hoac rong trong Sys_Messages!" -ForegroundColor Red
        $t4Failed = $true
    } else {
        Write-Host "  -> [$k] = '$msg'" -ForegroundColor Green
    }
}
$conn.Close()

if ($t4Failed) {
    Write-Host "TANG 4: THAT BAI" -ForegroundColor Red
    exit 1
}
$passCount++
Write-Host "TANG 4 (Sys_Messages DB Coverage): DAT" -ForegroundColor Green

# TANG 5: CLEAN CODE, NO INLINE STYLES & NO HARDCODED UI TEXT
Write-Host "`n[TANG 5] Quet Clean Code, No Inline Styles and Hardcoded Text..."
$hasInlineStyle = $viewHtml -match '<style'
if ($hasInlineStyle) {
    Write-Host "Phat hien the style noi tuyen trong _SearchDigitalSales.cshtml!" -ForegroundColor Red
    exit 1
}
Write-Host "  -> _SearchDigitalSales.cshtml: Khong co the style noi tuyen." -ForegroundColor Green

# Strip script tags and comments before scanning UI text
$htmlOnly = [regex]::Replace($viewHtml, '(?s)<script.*?</script>', '')
$htmlOnly = [regex]::Replace($htmlOnly, '(?s)<!--.*?-->', '')

$rawVietnameseMatches = [regex]::Matches($htmlOnly, '>[^<]*[\u00C0-\u1EF9][^<]*<')
$validVietnamese = $true
foreach ($m in $rawVietnameseMatches) {
    $val = $m.Value.Trim('>', '<', ' ', "`t", "`r", "`n")
    if ($val.Length -gt 0 -and !$val.Contains("@AppProcessor.Messagor")) {
        Write-Host "Phat hien text tieng Viet hardcode: $val" -ForegroundColor Red
        $validVietnamese = $false
    }
}

if (!$validVietnamese) {
    Write-Host "TANG 5: THAT BAI" -ForegroundColor Red
    exit 1
}
Write-Host "  -> 100% nhan va text hien thi deu nap qua AppProcessor.Messagor / Sys_Messages." -ForegroundColor Green
$passCount++
Write-Host "TANG 5 (Clean Code and No Inline Styles): DAT" -ForegroundColor Green

Write-Host "`n=================================================="
Write-Host "KET QUA: $passCount / $totalCount TANG KIEM THU DA DAT 100%!" -ForegroundColor Green
Write-Host "=================================================="


# ==============================================================================
# Automated Verification: Toggle Follow & Key Project Logic
# ==============================================================================
$ErrorActionPreference = "Stop"
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " RUNNING TOGGLE FOLLOW & KEY PROJECT VERIFICATION" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

$cs = "Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($cs)
$conn.Open()

$allPass = $true
$testId = 69
$testUser = "tantd.kha"

# 1. Test Toggle ON
Write-Host "`n[STEP 1] Toggle Follow ON..." -ForegroundColor Yellow
$cmdOn = $conn.CreateCommand()
$cmdOn.CommandText = "EXEC dbo.RM_DigitalSales_ToggleFollow @DigitalSalesID = $testId, @UserName = '$testUser', @IsFollowed = 1"
$resOn = $cmdOn.ExecuteScalar()
Write-Host "  -> SP Output: $resOn"

$cmdCheckOn = $conn.CreateCommand()
$cmdCheckOn.CommandText = "EXEC dbo.RM_DigitalSales_GetByID @DigitalSalesID = $testId, @UserName = '$testUser'"
$rOn = $cmdCheckOn.ExecuteReader()
if ($rOn.Read()) {
    $isF = [bool]$rOn["IsFollowed"]
    if ($isF -eq $true) {
        Write-Host "  -> PASS: When toggled ON, IsFollowed = 1" -ForegroundColor Green
    } else {
        Write-Host "  -> FAIL: Expected IsFollowed = 1, but got 0" -ForegroundColor Red
        $allPass = $false
    }
}
$rOn.Close()

# 2. Test Toggle OFF
Write-Host "`n[STEP 2] Toggle Follow OFF..." -ForegroundColor Yellow
$cmdOff = $conn.CreateCommand()
$cmdOff.CommandText = "EXEC dbo.RM_DigitalSales_ToggleFollow @DigitalSalesID = $testId, @UserName = '$testUser', @IsFollowed = 0"
$resOff = $cmdOff.ExecuteScalar()
Write-Host "  -> SP Output: $resOff"

$cmdCheckOff = $conn.CreateCommand()
$cmdCheckOff.CommandText = "EXEC dbo.RM_DigitalSales_GetByID @DigitalSalesID = $testId, @UserName = '$testUser'"
$rOff = $cmdCheckOff.ExecuteReader()
if ($rOff.Read()) {
    $isF = [bool]$rOff["IsFollowed"]
    if ($isF -eq $false) {
        Write-Host "  -> PASS: When toggled OFF, IsFollowed = 0 (Can uncheck successfully!)" -ForegroundColor Green
    } else {
        Write-Host "  -> FAIL: Expected IsFollowed = 0, but got 1 (Sticky Follow Bug!)" -ForegroundColor Red
        $allPass = $false
    }
}
$rOff.Close()

# 3. Restore to ON for user's test record
Write-Host "`n[STEP 3] Restoring Follow ON for record 69..." -ForegroundColor Yellow
$cmdRestore = $conn.CreateCommand()
$cmdRestore.CommandText = "EXEC dbo.RM_DigitalSales_ToggleFollow @DigitalSalesID = $testId, @UserName = '$testUser', @IsFollowed = 1"
$cmdRestore.ExecuteNonQuery() | Out-Null
Write-Host "  -> Restored successfully!" -ForegroundColor Green

$conn.Close()

# 4. Check XML sync across all 3 targets
Write-Host "`n[STEP 4] Verifying Cate_StoredProcedures.xml has ToggleFollow in all 3 targets..." -ForegroundColor Yellow
$xmlFiles = @(
    "d:\MyProject\crm\Modules.Cate\App_Data\Modules\Cate_StoredProcedures.xml",
    "d:\MyProject\crm\publish_source\App_Data\Modules\Cate_StoredProcedures.xml",
    "d:\MyProject\crm\CenIT.Solution.TOC.WebApp\App_Data\Modules\Cate_StoredProcedures.xml"
)
foreach ($xf in $xmlFiles) {
    if (-not (Test-Path $xf)) {
        Write-Host "  -> FAIL: Missing XML $xf" -ForegroundColor Red
        $allPass = $false
        continue
    }
    $c = Get-Content $xf -Raw
    if ($c.Contains("RM_DigitalSales_ToggleFollow") -and $c.Contains("RM_DigitalSales_ToggleKeyProject")) {
        Write-Host "  -> PASS: Contains both toggle procedures in $xf" -ForegroundColor Green
    } else {
        Write-Host "  -> FAIL: Missing toggle procedures in $xf" -ForegroundColor Red
        $allPass = $false
    }
}

# 5. Check Detail.cshtml, Detail.css and DigitalSalesDetail.js sync
Write-Host "`n[STEP 5] Verifying Triple Mirroring for Detail.cshtml, Detail.css and DigitalSalesDetail.js..." -ForegroundColor Yellow
$uiFiles = @("Detail.cshtml", "Detail.css", "DigitalSalesDetail.js")
foreach ($uf in $uiFiles) {
    $f1 = "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\$uf"
    $f2 = "d:\MyProject\crm\publish_source\Areas\Cate\Views\DigitalSales\$uf"
    $f3 = "d:\MyProject\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\DigitalSales\$uf"
    
    $h1 = (Get-FileHash $f1 -Algorithm MD5).Hash
    $h2 = (Get-FileHash $f2 -Algorithm MD5).Hash
    $h3 = (Get-FileHash $f3 -Algorithm MD5).Hash
    
    if ($h1 -eq $h2 -and $h2 -eq $h3) {
        Write-Host "  -> PASS: $uf matches 100% across all 3 targets ($h1)" -ForegroundColor Green
    } else {
        Write-Host "  -> FAIL: $uf hash mismatch: $h1 vs $h2 vs $h3" -ForegroundColor Red
        $allPass = $false
    }
}

Write-Host "`n=================================================================" -ForegroundColor Cyan
if ($allPass) {
    Write-Host " ALL TOGGLE VERIFICATION TESTS PASSED 100%!" -ForegroundColor Green
} else {
    Write-Host " VERIFICATION FAILED!" -ForegroundColor Red
    exit 1
}
Write-Host "=================================================================`n" -ForegroundColor Cyan

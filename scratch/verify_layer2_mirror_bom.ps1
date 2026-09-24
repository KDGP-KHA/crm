$files = @(
    "Areas\Cate\Views\DigitalSales\_ChangeStatusModal.cshtml",
    "Areas\Cate\Views\DigitalSales\_ChangeStatusForm.cshtml",
    "Areas\Cate\Views\DigitalSales\_ChangeStatusModal.css",
    "Areas\Cate\Views\DigitalSales\_DetailDiscussions.cshtml",
    "Areas\Cate\Views\DigitalSales\_DetailTracking.cshtml",
    "Areas\Cate\Views\DigitalSales\_DetailTracking.css",
    "Areas\Cate\Views\DigitalSales\DigitalSales.js",
    "Areas\Cate\Views\DigitalSales\DigitalSalesDetail.js",
    "Areas\Cate\Views\ContactPersons\ContactPersons.css",
    "Areas\Cate\Views\ContactPersons\ContactPersons.js",
    "App_Data\Modules\Cate_StoredProcedures.xml"
)

$srcDir = "d:\MyProject\crm\Modules.Cate"
$webappDir = "d:\MyProject\crm\CenIT.Solution.TOC.WebApp"
$pubDir = "d:\MyProject\crm\publish_source"

$utf8WithBom = New-Object System.Text.UTF8Encoding($true)
$allPass = $true

Write-Host "=== TẦNG 2: TRIPLE MIRRORING & UTF-8 BOM VERIFICATION ==="

# Step 1: Ensure BOM on all Razor files in source, then mirror
foreach ($f in $files) {
    $srcPath = Join-Path $srcDir $f
    if (-not (Test-Path $srcPath)) {
        Write-Host "ERROR: Missing $srcPath" -ForegroundColor Red
        $allPass = $false
        continue
    }

    if ($f.EndsWith(".cshtml")) {
        $bytes = [System.IO.File]::ReadAllBytes($srcPath)
        $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
        if (-not $hasBom) {
            $text = [System.IO.File]::ReadAllText($srcPath, [System.Text.Encoding]::UTF8)
            [System.IO.File]::WriteAllText($srcPath, $text, $utf8WithBom)
            Write-Host "Added BOM to $srcPath" -ForegroundColor Yellow
        }
    }

    $srcBytes = [System.IO.File]::ReadAllBytes($srcPath)

    # Mirror to WebApp
    $webPath = Join-Path $webappDir $f
    $webParent = Split-Path $webPath -Parent
    if (-not (Test-Path $webParent)) { New-Item -ItemType Directory -Path $webParent -Force | Out-Null }
    [System.IO.File]::WriteAllBytes($webPath, $srcBytes)

    # Mirror to publish_source
    $pubPath = Join-Path $pubDir $f
    $pubParent = Split-Path $pubPath -Parent
    if (-not (Test-Path $pubParent)) { New-Item -ItemType Directory -Path $pubParent -Force | Out-Null }
    [System.IO.File]::WriteAllBytes($pubPath, $srcBytes)
}

# Step 2: Verify MD5 and BOM across all 3
$md5Alg = [System.Security.Cryptography.MD5]::Create()
foreach ($f in $files) {
    $p1 = Join-Path $srcDir $f
    $p2 = Join-Path $webappDir $f
    $p3 = Join-Path $pubDir $f

    $b1 = [System.IO.File]::ReadAllBytes($p1)
    $b2 = [System.IO.File]::ReadAllBytes($p2)
    $b3 = [System.IO.File]::ReadAllBytes($p3)

    $h1 = [System.BitConverter]::ToString($md5Alg.ComputeHash($b1)).Replace("-", "")
    $h2 = [System.BitConverter]::ToString($md5Alg.ComputeHash($b2)).Replace("-", "")
    $h3 = [System.BitConverter]::ToString($md5Alg.ComputeHash($b3)).Replace("-", "")

    $match = ($h1 -eq $h2) -and ($h2 -eq $h3)

    $bomStatus = "N/A"
    if ($f.EndsWith(".cshtml")) {
        $bom1 = ($b1.Length -ge 3 -and $b1[0] -eq 0xEF -and $b1[1] -eq 0xBB -and $b1[2] -eq 0xBF)
        $bom2 = ($b2.Length -ge 3 -and $b2[0] -eq 0xEF -and $b2[1] -eq 0xBB -and $b2[2] -eq 0xBF)
        $bom3 = ($b3.Length -ge 3 -and $b3[0] -eq 0xEF -and $b3[1] -eq 0xBB -and $b3[2] -eq 0xBF)
        $allBom = $bom1 -and $bom2 -and $bom3
        $bomStatus = "BOM: $allBom"
        if (-not $allBom) { $allPass = $false }
    }

    if ($match) {
        Write-Host "[PASS] $f | MD5: $h1 | $bomStatus" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] $f | Mismatch MD5: $h1 vs $h2 vs $h3" -ForegroundColor Red
        $allPass = $false
    }
}

if ($allPass) {
    Write-Host "`n>>> TẦNG 2 PASSED 100%! All files mirrored and UTF-8 BOM confirmed. <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n>>> TẦNG 2 FAILED! <<<" -ForegroundColor Red
    exit 1
}

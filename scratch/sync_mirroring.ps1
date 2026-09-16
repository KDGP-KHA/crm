$utf8WithBom = New-Object System.Text.UTF8Encoding($true)

function SaveWithBom($sourcePath, $targetPaths) {
    $text = [System.IO.File]::ReadAllText($sourcePath)
    [System.IO.File]::WriteAllText($sourcePath, $text, $utf8WithBom)
    
    foreach ($target in $targetPaths) {
        $dir = [System.IO.Path]::GetDirectoryName($target)
        if (!(Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
        [System.IO.File]::WriteAllText($target, $text, $utf8WithBom)
        Write-Host "Synced -> $target"
    }
}

$viewSource = "d:\VNPT\CRM-GIT\crm\Modules.Cate\Areas\Cate\Views\ReviewBatchItem\_SearchDigitalSales.cshtml"
$viewTargets = @(
    "d:\VNPT\CRM-GIT\crm\publish_source\Areas\Cate\Views\ReviewBatchItem\_SearchDigitalSales.cshtml",
    "d:\VNPT\CRM-GIT\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\ReviewBatchItem\_SearchDigitalSales.cshtml"
)
SaveWithBom $viewSource $viewTargets

$jsSource = "d:\VNPT\CRM-GIT\crm\Modules.Cate\Areas\Cate\Views\ReviewBatchItem\ReviewBatchItem.js"
$jsTargets = @(
    "d:\VNPT\CRM-GIT\crm\publish_source\Areas\Cate\Views\ReviewBatchItem\ReviewBatchItem.js",
    "d:\VNPT\CRM-GIT\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\ReviewBatchItem\ReviewBatchItem.js"
)
SaveWithBom $jsSource $jsTargets

# Check MD5 hashes
Write-Host "`n--- MD5 Verification ---"
$allFiles = @($viewSource) + $viewTargets + @($jsSource) + $jsTargets
foreach ($f in $allFiles) {
    $hash = (Get-FileHash $f -Algorithm MD5).Hash
    $bytes = [System.IO.File]::ReadAllBytes($f)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    Write-Host "$f`n  MD5: $hash | UTF8-BOM: $hasBom"
}


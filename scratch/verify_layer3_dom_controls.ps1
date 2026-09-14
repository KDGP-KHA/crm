$files = @(
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\Index.cshtml",
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_Search.cshtml",
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\Detail.cshtml",
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_DetailDiscussions.cshtml",
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_ChangeStatusModal.cshtml",
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_ChangeStatusForm.cshtml"
)

$controlRegex = '<(?:input|select|textarea)[^>]*?\bid\s*=\s*["'']([^"''>\s]+)["'']'
$ids = @()

foreach ($filePath in $files) {
    if (-not (Test-Path $filePath)) { continue }
    $content = [System.IO.File]::ReadAllText($filePath)
    $fileName = Split-Path $filePath -Leaf
    $matches = [regex]::Matches($content, $controlRegex, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    foreach ($m in $matches) {
        $id = $m.Groups[1].Value
        if ($id -match '@|\{') { continue }
        $ids += [PSCustomObject]@{ Id = $id; File = $fileName }
    }
}

$dups = $ids | Group-Object Id | Where-Object { $_.Count -gt 1 }
$controlDups = @()
foreach ($d in $dups) {
    $fileCount = ($d.Group | Select-Object -ExpandProperty File -Unique).Count
    if ($fileCount -gt 1) {
        $controlDups += $d
    }
}

Write-Host "=== TẦNG 3: DOM CONTROL ID COLLISION SCANNER ==="
if ($controlDups.Count -gt 0) {
    Write-Host "[FAIL] Found duplicate form control IDs across files:" -ForegroundColor Red
    $controlDups | ForEach-Object {
        $fileNames = ($_.Group | Select-Object -ExpandProperty File -Unique) -join ', '
        Write-Host "  Control ID: '$($_.Name)' in: $fileNames" -ForegroundColor Red
    }
    exit 1
} else {
    Write-Host "[PASS] 100% Form control IDs (input/select/textarea) are completely unique across all views and modals!" -ForegroundColor Green
    exit 0
}

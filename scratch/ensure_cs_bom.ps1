$utf8WithBom = New-Object System.Text.UTF8Encoding($true)

$csFiles = @(
    "d:\VNPT\CRM-GIT\crm\Core.Cate\Models\RM_ReviewBatchItemModel.cs",
    "d:\VNPT\CRM-GIT\crm\Core.Cate\Biz\RM_ReviewBatchItemBiz.cs",
    "d:\VNPT\CRM-GIT\crm\Modules.Cate\Areas\Cate\Controllers\ReviewBatchItemController.cs",
    "d:\VNPT\CRM-GIT\crm\Database\DigitalSalesReview.sql"
)

foreach ($f in $csFiles) {
    $text = [System.IO.File]::ReadAllText($f)
    [System.IO.File]::WriteAllText($f, $text, $utf8WithBom)
    $bytes = [System.IO.File]::ReadAllBytes($f)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    Write-Host "$f BOM: $hasBom"
}


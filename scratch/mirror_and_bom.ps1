$filesToMirror = @(
    "Areas\Cate\Views\DigitalSales\_ChangeStatusModal.cshtml",
    "Areas\Cate\Views\DigitalSales\_ChangeStatusForm.cshtml",
    "Areas\Cate\Views\DigitalSales\_DetailDiscussions.cshtml"
)

$srcDir = "d:\MyProject\crm\Modules.Cate"
$targetDirs = @(
    "d:\MyProject\crm\CenIT.Solution.TOC.WebApp",
    "d:\MyProject\crm\publish_source"
)

$utf8WithBom = New-Object System.Text.UTF8Encoding($true)

foreach ($rel in $filesToMirror) {
    $srcPath = Join-Path $srcDir $rel
    if (Test-Path $srcPath) {
        # Đọc nội dung và lưu lại với UTF-8 BOM
        $content = [System.IO.File]::ReadAllText($srcPath, [System.Text.Encoding]::UTF8)
        [System.IO.File]::WriteAllText($srcPath, $content, $utf8WithBom)

        foreach ($td in $targetDirs) {
            $dstPath = Join-Path $td $rel
            $dstParent = Split-Path $dstPath -Parent
            if (-not (Test-Path $dstParent)) {
                New-Item -ItemType Directory -Path $dstParent -Force | Out-Null
            }
            [System.IO.File]::WriteAllText($dstPath, $content, $utf8WithBom)
            Write-Host "Mirrored $rel -> $dstPath"
        }
    } else {
        Write-Warning "Source file not found: $srcPath"
    }
}

# Kiểm tra lại MD5 hash và UTF-8 BOM
Write-Host "`n=== VERIFYING MD5 AND UTF-8 BOM ==="
foreach ($rel in $filesToMirror) {
    $paths = @(
        (Join-Path $srcDir $rel),
        (Join-Path $targetDirs[0] $rel),
        (Join-Path $targetDirs[1] $rel)
    )

    Write-Host "`nFile: $rel"
    foreach ($p in $paths) {
        $bytes = [System.IO.File]::ReadAllBytes($p)
        $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
        $md5 = [System.BitConverter]::ToString([System.Security.Cryptography.MD5]::Create().ComputeHash($bytes)).Replace("-", "")
        Write-Host "  $p | HasBOM: $hasBom | MD5: $md5"
    }
}

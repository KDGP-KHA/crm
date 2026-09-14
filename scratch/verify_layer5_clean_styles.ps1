$views = @(
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_ChangeStatusModal.cshtml",
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_ChangeStatusForm.cshtml",
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_DetailDiscussions.cshtml"
)

Write-Host "=== TẦNG 5: CLEAN CODE & NO INLINE STYLES VERIFICATION ==="
$allPass = $true

foreach ($v in $views) {
    $text = [System.IO.File]::ReadAllText($v)
    $fileName = Split-Path $v -Leaf
    
    # Check for <style> tag
    if ($text -match '<style[\s>]') {
        Write-Host "[FAIL] $fileName contains inline <style> tag!" -ForegroundColor Red
        $allPass = $false
    } else {
        Write-Host "[PASS] $fileName has NO inline <style> tag." -ForegroundColor Green
    }
}

# Check if _ChangeStatusModal.css is linked with timestamp in _ChangeStatusModal.cshtml
$modalContent = [System.IO.File]::ReadAllText("d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_ChangeStatusModal.cshtml")
if ($modalContent -match '_ChangeStatusModal\.css\?v=@DateTime\.Now\.Ticks') {
    Write-Host "[PASS] _ChangeStatusModal.cshtml correctly links _ChangeStatusModal.css with ?v=@DateTime.Now.Ticks cache-busting timestamp." -ForegroundColor Green
} else {
    Write-Host "[FAIL] Missing cache-busting timestamp on _ChangeStatusModal.css in _ChangeStatusModal.cshtml!" -ForegroundColor Red
    $allPass = $false
}

if ($allPass) {
    Write-Host "`n>>> TẦNG 5 PASSED 100%! <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n>>> TẦNG 5 FAILED! <<<" -ForegroundColor Red
    exit 1
}

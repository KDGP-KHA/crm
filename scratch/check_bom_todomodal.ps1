$paths = @(
    'd:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_TodoModal.cshtml',
    'd:\SVN\crm\publish_source\Areas\Cate\Views\DigitalSales\_TodoModal.cshtml',
    'd:\SVN\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\DigitalSales\_TodoModal.cshtml'
)
foreach ($p in $paths) {
    $bytes = [System.IO.File]::ReadAllBytes($p)
    $hasBom = ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $hash = (Get-FileHash $p -Algorithm MD5).Hash
    Write-Host ("{0} | HasBOM: {1} | MD5: {2}" -f $p, $hasBom, $hash)
}

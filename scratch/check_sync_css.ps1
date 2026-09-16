$paths = @(
    'd:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_DetailTracking.css',
    'd:\SVN\crm\publish_source\Areas\Cate\Views\DigitalSales\_DetailTracking.css',
    'd:\SVN\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\DigitalSales\_DetailTracking.css'
)
foreach ($p in $paths) {
    Copy-Item 'd:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_DetailTracking.css' $p -Force
    $hash = (Get-FileHash $p -Algorithm MD5).Hash
    Write-Host ("{0} | MD5: {1}" -f $p, $hash)
}

$paths = @(
    'd:\SVN\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_TodoModal.cshtml',
    'd:\SVN\crm\publish_source\Areas\Cate\Views\DigitalSales\_TodoModal.cshtml',
    'd:\SVN\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\DigitalSales\_TodoModal.cshtml'
)
$utf8Bom = New-Object System.Text.UTF8Encoding($true)
foreach ($p in $paths) {
    $content = [System.IO.File]::ReadAllText($p, [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllText($p, $content, $utf8Bom)
    Write-Host "Added BOM to $p"
}

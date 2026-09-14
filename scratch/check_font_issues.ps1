$dirs = @(
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales",
    "d:\MyProject\crm\publish_source\Areas\Cate\Views\DigitalSales",
    "d:\MyProject\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\DigitalSales"
)

$results = @()

foreach ($d in $dirs) {
    if (Test-Path $d) {
        $files = Get-ChildItem -Path $d -Include "*.cshtml", "*.js", "*.css" -Recurse
        foreach ($f in $files) {
            $bytes = [System.IO.File]::ReadAllBytes($f.FullName)
            $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
            $rawText = [System.Text.Encoding]::UTF8.GetString($bytes)
            
            # Check for mojibake patterns (UTF-8 double-encoded as Windows-1252/Latin-1)
            $hasMoji = $rawText -match '[ÃÂ][\x80-\xFF]|á»|áº|Ä‘|Æ°|Ã´|Ãª'
            
            if (-not $hasBom -or $hasMoji) {
                $results += [PSCustomObject]@{
                    Path = $f.FullName.Replace("d:\MyProject\crm\", "")
                    HasBOM = $hasBom
                    HasMoji = $hasMoji
                    Size = $bytes.Length
                }
            }
        }
    }
}

$results | Format-Table -AutoSize

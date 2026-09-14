$connStr = "Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=30;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$targetFiles = @(
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_ChangeStatusModal.cshtml",
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_ChangeStatusForm.cshtml",
    "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\DigitalSales\_DetailDiscussions.cshtml",
    "d:\MyProject\crm\Core.Cate\Models\RM_DigitalSalesModel.cs"
)

$keys = [System.Collections.Generic.HashSet[string]]::new()

foreach ($f in $targetFiles) {
    if (-not (Test-Path $f)) { continue }
    $text = [System.IO.File]::ReadAllText($f)
    
    # Match GetMessage("KEY") or Messagor.GetMessage("KEY")
    $m1 = [regex]::Matches($text, 'GetMessage\(\s*["'']([^"'']+)["'']\s*\)')
    foreach ($m in $m1) { [void]$keys.Add($m.Groups[1].Value) }

    # Match CustomDisplayName("KEY")
    $m2 = [regex]::Matches($text, 'CustomDisplayName\(\s*["'']([^"'']+)["'']\s*\)')
    foreach ($m in $m2) { [void]$keys.Add($m.Groups[1].Value) }
}

Write-Host "=== TẦNG 4: SYS_MESSAGES DB COVERAGE VERIFICATION ==="
Write-Host "Scanning $($keys.Count) unique message keys in change status components..."

$missing = @()
foreach ($k in $keys) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT Message FROM Sys_Messages WHERE LabelKey = @key"
    $cmd.Parameters.AddWithValue("@key", $k) | Out-Null
    $val = $cmd.ExecuteScalar()
    if ($val -ne $null -and -not [string]::IsNullOrWhiteSpace($val.ToString())) {
        Write-Host "  [OK] $k => '$val'" -ForegroundColor Green
    } else {
        Write-Host "  [MISSING] $k" -ForegroundColor Red
        $missing += $k
    }
}
$conn.Close()

if ($missing.Count -eq 0) {
    Write-Host "`n>>> TẦNG 4 PASSED 100%! All keys exist with valid Vietnamese messages in Sys_Messages. <<<" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n>>> TẦNG 4 FAILED! Missing $($missing.Count) keys. <<<" -ForegroundColor Red
    exit 1
}

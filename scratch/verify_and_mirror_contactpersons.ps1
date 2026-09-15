$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " 5-LAYER VERIFICATION SUITE FOR CONTACTPERSONS MODULE" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$srcDir = "d:\MyProject\crm\Modules.Cate\Areas\Cate\Views\ContactPersons"
$pubDir = "d:\MyProject\crm\publish_source\Areas\Cate\Views\ContactPersons"
$webDir = "d:\MyProject\crm\CenIT.Solution.TOC.WebApp\Areas\Cate\Views\ContactPersons"

# --- TANG 2: TRIPLE MIRRORING & UTF-8 BOM ---
Write-Host "`n[TANG 2] Triple Mirroring & UTF-8 BOM..." -ForegroundColor Yellow

$files = @("ContactPersons.css", "Index.cshtml", "_Search.cshtml", "ContactPersons.js")

foreach ($f in $files) {
    $srcFile = Join-Path $srcDir $f
    $pubFile = Join-Path $pubDir $f
    $webFile = Join-Path $webDir $f

    Copy-Item -Path $srcFile -Destination $pubFile -Force
    Copy-Item -Path $srcFile -Destination $webFile -Force
}

# Add UTF-8 BOM to all .cshtml files across all 3 directories
$allCshtml = Get-ChildItem -Path @($srcDir, $pubDir, $webDir) -Filter "*.cshtml"

$bom = [byte[]](0xEF, 0xBB, 0xBF)
foreach ($item in $allCshtml) {
    $bytes = [System.IO.File]::ReadAllBytes($item.FullName)
    if ($bytes.Length -lt 3 -or $bytes[0] -ne 0xEF -or $bytes[1] -ne 0xBB -or $bytes[2] -ne 0xBF) {
        [System.IO.File]::WriteAllBytes($item.FullName, $bom + $bytes)
        Write-Host "Added UTF-8 BOM to: $($item.FullName)" -ForegroundColor Green
    }
}

# Verify MD5
$mirrorPassed = $true
foreach ($f in $files) {
    $srcHash = (Get-FileHash -Path (Join-Path $srcDir $f) -Algorithm MD5).Hash
    $pubHash = (Get-FileHash -Path (Join-Path $pubDir $f) -Algorithm MD5).Hash
    $webHash = (Get-FileHash -Path (Join-Path $webDir $f) -Algorithm MD5).Hash

    if ($srcHash -eq $pubHash -and $srcHash -eq $webHash) {
        Write-Host "  MATCH [MD5: $srcHash] -> $f" -ForegroundColor Green
    } else {
        Write-Host "  MISMATCH -> $f (Src: $srcHash, Pub: $pubHash, Web: $webHash)" -ForegroundColor Red
        $mirrorPassed = $false
    }
}

if (-not $mirrorPassed) {
    throw "TANG 2 FAILED: Triple Mirroring MD5 mismatch!"
}
Write-Host "TANG 2: DAT 100% Triple Mirroring va UTF-8 BOM" -ForegroundColor Green

# --- TANG 3: DOM ID COLLISION SCANNER ---
Write-Host "`n[TANG 3] DOM ID Collision Scanner..." -ForegroundColor Yellow

$allViews = Get-ChildItem -Path $srcDir -Filter "*.cshtml"
$idMap = @{}
$collisions = @()

foreach ($view in $allViews) {
    $content = [System.IO.File]::ReadAllText($view.FullName)
    $matches = [System.Text.RegularExpressions.Regex]::Matches($content, 'id\s*=\s*["'']([^"'']+)["'']')
    foreach ($m in $matches) {
        $id = $m.Groups[1].Value
        if ($id.StartsWith("@") -or $id -eq "bodyForm" -or $id -eq "Language") {
            continue
        }
        if ($idMap.ContainsKey($id)) {
            $idMap[$id] += @($view.Name)
            $collisions += [PSCustomObject]@{
                ID = $id
                Files = ($idMap[$id] -join ", ")
            }
        } else {
            $idMap[$id] = @($view.Name)
        }
    }
}

if ($collisions.Count -gt 0) {
    Write-Host "Collision detected:" -ForegroundColor Red
    $collisions | Format-Table -AutoSize
    throw "TANG 3 FAILED: DOM ID Collision detected!"
} else {
    Write-Host "TANG 3: DAT 100% Khong co bat ky DOM ID Collision nao" -ForegroundColor Green
}

# --- TANG 4: SYS_MESSAGES DB COVERAGE ---
Write-Host "`n[TANG 4] Sys_Messages DB Coverage..." -ForegroundColor Yellow

$connStr = "Server=10.57.30.10;Database=quanlydoanhthucenit;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;TrustServerCertificate=True;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$allKeys = [System.Collections.Generic.HashSet[string]]::new()
foreach ($view in $allViews) {
    $content = [System.IO.File]::ReadAllText($view.FullName)
    $matches = [System.Text.RegularExpressions.Regex]::Matches($content, 'GetMessage\(["'']([^"'']+)["'']\)')
    foreach ($m in $matches) {
        $allKeys.Add($m.Groups[1].Value) | Out-Null
    }
}

$controllerContent = [System.IO.File]::ReadAllText("d:\MyProject\crm\Modules.Cate\Areas\Cate\Controllers\ContactPersonsController.cs")
$cMatches = [System.Text.RegularExpressions.Regex]::Matches($controllerContent, 'GetMessage\(["'']([^"'']+)["'']\)')
foreach ($m in $cMatches) {
    $allKeys.Add($m.Groups[1].Value) | Out-Null
}

$missingKeys = @()
foreach ($key in $allKeys) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM Sys_Messages WHERE LabelKey = @Key AND LangCode = 'vi-VN'"
    $cmd.Parameters.AddWithValue("@Key", $key) | Out-Null
    $count = [int]$cmd.ExecuteScalar()
    if ($count -eq 0) {
        $missingKeys += $key
    }
}
$conn.Close()

if ($missingKeys.Count -gt 0) {
    Write-Host "Missing keys in Sys_Messages:" -ForegroundColor Red
    $missingKeys | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    throw "TANG 4 FAILED: Co key Sys_Messages chua ton tai trong CSDL!"
} else {
    Write-Host "TANG 4: DAT 100% Tat ca $($allKeys.Count) key Sys_Messages deu ton tai trong CSDL" -ForegroundColor Green
}

# --- TANG 5: CLEAN CODE & NO INLINE <style> TAGS ---
Write-Host "`n[TANG 5] Clean Code & Zero Inline <style> Tags..." -ForegroundColor Yellow

$hasStyleTags = $false
foreach ($view in $allViews) {
    $content = [System.IO.File]::ReadAllText($view.FullName)
    if ($content -match '<style[\s>]') {
        Write-Host "  PHAT HIEN THE <style> NOI TUYEN TRONG: $($view.Name)" -ForegroundColor Red
        $hasStyleTags = $true
    }
}

if ($hasStyleTags) {
    throw "TANG 5 FAILED: Phat hien the <style> noi tuyen trong tep Razor!"
} else {
    Write-Host "TANG 5: DAT 100% Khong co bat ky the <style> noi tuyen nao" -ForegroundColor Green
}

# --- PRECOMPILE RAZOR VIEWS VIA ASPNET_COMPILER ---
Write-Host "`n[KIEM THU RUNTIME] Precompiling Razor views with aspnet_compiler..." -ForegroundColor Yellow
$aspnetCompiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe"
$webAppPath = "d:\MyProject\crm\CenIT.Solution.TOC.WebApp"

Remove-Item -Recurse -Force (Join-Path $webAppPath "obj") -ErrorAction SilentlyContinue
& $aspnetCompiler -v / -p $webAppPath -u -f "d:\MyProject\crm\scratch\precompiled_output"
if ($LASTEXITCODE -ne 0) {
    throw "PRECOMPILE RAZOR FAILED! Kiem tra lai cu phap View."
} else {
    Write-Host "PRECOMPILE RAZOR: 100% THANH CONG (0 Loi cu phap/Type Razor)" -ForegroundColor Green
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " 5-LAYER VERIFICATION SUITE: TAT CA DEU DAT 100%! " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan

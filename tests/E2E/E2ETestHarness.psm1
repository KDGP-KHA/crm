# CenIT TOC CRM - E2E Test Harness Module
# Encoding: UTF-8 with BOM

if ($null -eq $script:TestResults) {
    $script:TestResults = [System.Collections.Generic.List[PSObject]]::new()
}
$script:CurrentTier = "Tier 1"
$script:CurrentFeature = "Feature 1"

$script:DefaultConnStr = "Data Source=10.57.30.10;Initial Catalog=quanlydoanhthucenit;Persist Security Info=True;User Id=quanlydoanhthucenit;Password=Kdhe@543HE2;Connect Timeout=15;"

function Reset-TestResults {
    $script:TestResults.Clear()
}

function Set-TestContext {
    param(
        [string]$Tier,
        [string]$Feature
    )
    $script:CurrentTier = $Tier
    $script:CurrentFeature = $Feature
}

function Record-TestResult {
    param(
        [string]$TestId,
        [string]$TestName,
        [string]$Status,
        [string]$ErrorMessage = "",
        [double]$DurationMs = 0,
        [string]$Tier = $script:CurrentTier,
        [string]$Feature = $script:CurrentFeature
    )
    $result = [PSCustomObject]@{
        TestId       = $TestId
        TestName     = $TestName
        Tier         = $Tier
        Feature      = $Feature
        Status       = $Status
        ErrorMessage = $ErrorMessage
        DurationMs   = [math]::Round($DurationMs, 2)
        Timestamp    = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
    }
    $script:TestResults.Add($result)

    $color = switch ($Status) {
        "PASS"    { "Green" }
        "FAIL"    { "Red" }
        "SKIP"    { "Yellow" }
        default   { "White" }
    }
    $statusText = "[$Status]"
    Write-Host "    $statusText $($TestId): $TestName" -ForegroundColor $color
    if ($Status -eq "FAIL" -and $ErrorMessage) {
        Write-Host "         ERROR: $ErrorMessage" -ForegroundColor DarkRed
    }
}

function Assert-True {
    param(
        [bool]$Condition,
        [string]$TestId,
        [string]$TestName,
        [string]$Message = "Expected true but got false"
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    if ($Condition) {
        $sw.Stop()
        Record-TestResult -TestId $TestId -TestName $TestName -Status "PASS" -DurationMs $sw.Elapsed.TotalMilliseconds
    } else {
        $sw.Stop()
        Record-TestResult -TestId $TestId -TestName $TestName -Status "FAIL" -ErrorMessage $Message -DurationMs $sw.Elapsed.TotalMilliseconds
    }
}

function Assert-False {
    param(
        [bool]$Condition,
        [string]$TestId,
        [string]$TestName,
        [string]$Message = "Expected false but got true"
    )
    Assert-True -Condition (-not $Condition) -TestId $TestId -TestName $TestName -Message $Message
}

function Assert-Equal {
    param(
        $Expected,
        $Actual,
        [string]$TestId,
        [string]$TestName,
        [string]$Message = ""
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $isEqual = ($Expected -eq $Actual)
    $sw.Stop()
    if ($isEqual) {
        Record-TestResult -TestId $TestId -TestName $TestName -Status "PASS" -DurationMs $sw.Elapsed.TotalMilliseconds
    } else {
        $errMsg = if ($Message) { "$Message. Expected: '$Expected', Actual: '$Actual'" } else { "Expected: '$Expected', Actual: '$Actual'" }
        Record-TestResult -TestId $TestId -TestName $TestName -Status "FAIL" -ErrorMessage $errMsg -DurationMs $sw.Elapsed.TotalMilliseconds
    }
}

function Assert-NotEqual {
    param(
        $Unexpected,
        $Actual,
        [string]$TestId,
        [string]$TestName,
        [string]$Message = ""
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $isNotEqual = ($Unexpected -ne $Actual)
    $sw.Stop()
    if ($isNotEqual) {
        Record-TestResult -TestId $TestId -TestName $TestName -Status "PASS" -DurationMs $sw.Elapsed.TotalMilliseconds
    } else {
        $errMsg = if ($Message) { "$Message. Unexpected value matched: '$Unexpected'" } else { "Value should not equal: '$Unexpected'" }
        Record-TestResult -TestId $TestId -TestName $TestName -Status "FAIL" -ErrorMessage $errMsg -DurationMs $sw.Elapsed.TotalMilliseconds
    }
}

function Assert-Contains {
    param(
        [string]$SubString,
        [string]$SourceString,
        [string]$TestId,
        [string]$TestName,
        [string]$Message = ""
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $contains = ($SourceString -and $SourceString.Contains($SubString))
    $sw.Stop()
    if ($contains) {
        Record-TestResult -TestId $TestId -TestName $TestName -Status "PASS" -DurationMs $sw.Elapsed.TotalMilliseconds
    } else {
        $errMsg = if ($Message) { "$Message. String did not contain substring: '$SubString'" } else { "String did not contain substring: '$SubString'" }
        Record-TestResult -TestId $TestId -TestName $TestName -Status "FAIL" -ErrorMessage $errMsg -DurationMs $sw.Elapsed.TotalMilliseconds
    }
}

function Assert-Matches {
    param(
        [string]$Pattern,
        [string]$SourceString,
        [string]$TestId,
        [string]$TestName,
        [string]$Message = ""
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $matches = ($SourceString -and ($SourceString -match $Pattern))
    $sw.Stop()
    if ($matches) {
        Record-TestResult -TestId $TestId -TestName $TestName -Status "PASS" -DurationMs $sw.Elapsed.TotalMilliseconds
    } else {
        $errMsg = if ($Message) { "$Message. String did not match regex pattern: '$Pattern'" } else { "String did not match pattern: '$Pattern'" }
        Record-TestResult -TestId $TestId -TestName $TestName -Status "FAIL" -ErrorMessage $errMsg -DurationMs $sw.Elapsed.TotalMilliseconds
    }
}

function Assert-Throws {
    param(
        [scriptblock]$Action,
        [string]$ExpectedExceptionType = "",
        [string]$TestId,
        [string]$TestName,
        [string]$Message = ""
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $threw = $false
    $exception = $null
    try {
        & $Action
    } catch {
        $threw = $true
        $exception = $_.Exception
    }
    $sw.Stop()

    if (-not $threw) {
        Record-TestResult -TestId $TestId -TestName $TestName -Status "FAIL" -ErrorMessage "Expected exception but action completed without error" -DurationMs $sw.Elapsed.TotalMilliseconds
        return
    }

    if ($ExpectedExceptionType) {
        $actualType = $exception.GetType().FullName
        if ($actualType -notmatch $ExpectedExceptionType -and $exception.GetType().Name -notmatch $ExpectedExceptionType) {
            Record-TestResult -TestId $TestId -TestName $TestName -Status "FAIL" -ErrorMessage "Expected exception of type '$ExpectedExceptionType' but got '$actualType'" -DurationMs $sw.Elapsed.TotalMilliseconds
            return
        }
    }

    Record-TestResult -TestId $TestId -TestName $TestName -Status "PASS" -DurationMs $sw.Elapsed.TotalMilliseconds
}

function Assert-DoesNotThrow {
    param(
        [scriptblock]$Action,
        [string]$TestId,
        [string]$TestName,
        [string]$Message = ""
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $Action
        $sw.Stop()
        Record-TestResult -TestId $TestId -TestName $TestName -Status "PASS" -DurationMs $sw.Elapsed.TotalMilliseconds
    } catch {
        $sw.Stop()
        $errMsg = if ($Message) { "$Message. Unexpected exception: $($_.Exception.Message)" } else { "Unexpected exception: $($_.Exception.Message)" }
        Record-TestResult -TestId $TestId -TestName $TestName -Status "FAIL" -ErrorMessage $errMsg -DurationMs $sw.Elapsed.TotalMilliseconds
    }
}

# ----------------- SECURITY & VALIDATION RULES ENGINE -----------------

$script:AllowedExtensions = @('.doc', '.docx', '.xls', '.xlsx', '.pdf', '.ppt', '.pptx', '.txt', '.zip', '.rar')
$script:BlacklistedExtensions = @('.exe', '.dll', '.bat', '.cmd', '.ps1', '.vbs', '.sh', '.com', '.msi', '.jar', '.vbe', '.jse', '.wsf', '.wsh', '.scr', '.pif', '.app', '.gadget', '.asp', '.aspx', '.php', '.jsp', '.cgi')
$script:MaxFileSizeBytes = 52428800 # 50 MB

function Test-FileExtensionAllowed {
    param([string]$FileName)
    if ([string]::IsNullOrWhiteSpace($FileName)) { return $false }
    $lowerName = $FileName.ToLower().Trim()
    
    foreach ($badExt in $script:BlacklistedExtensions) {
        if ($lowerName.EndsWith($badExt) -or $lowerName.Contains("$badExt.")) {
            return $false
        }
    }
    
    $lastExt = [System.IO.Path]::GetExtension($lowerName)
    if ([string]::IsNullOrEmpty($lastExt)) { return $false }
    return ($script:AllowedExtensions -contains $lastExt)
}

function Test-FileSizeAllowed {
    param([long]$FileSizeBytes)
    if ($FileSizeBytes -le 0) { return $false }
    if ($FileSizeBytes -gt $script:MaxFileSizeBytes) { return $false }
    return $true
}

function ConvertTo-UnSign {
    param([string]$InputText)
    if ([string]::IsNullOrEmpty($InputText)) { return "" }
    $normalized = $InputText.Normalize([System.Text.NormalizationForm]::FormD)
    $sb = [System.Text.StringBuilder]::new()
    foreach ($c in $normalized.ToCharArray()) {
        $cat = [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory($c)
        if ($cat -ne [System.Globalization.UnicodeCategory]::NonSpacingMark) {
            if ($c -eq [char]0x0111 -or $c -eq [char]0x0110) {
                [void]$sb.Append('d')
            } else {
                [void]$sb.Append($c)
            }
        }
    }
    return $sb.ToString().Normalize([System.Text.NormalizationForm]::FormC)
}

function Get-SanitizedFileName {
    param([string]$OriginalFileName)
    if ([string]::IsNullOrWhiteSpace($OriginalFileName)) { return "" }
    $ext = [System.IO.Path]::GetExtension($OriginalFileName).ToLower()
    $nameWithoutExt = [System.IO.Path]::GetFileNameWithoutExtension($OriginalFileName)
    
    $unsign = ConvertTo-UnSign -InputText $nameWithoutExt
    $sanitized = [System.Text.RegularExpressions.Regex]::Replace($unsign, "[^a-zA-Z0-9_-]", "_")
    if ($sanitized.Length -gt 50) {
        $sanitized = $sanitized.Substring(0, 50)
    }
    $timestamp = (Get-Date).ToString("yyyyMMddHHmmssfff")
    $guidSuffix = [System.Guid]::NewGuid().ToString("N").Substring(0, 6)
    return "${sanitized}_${timestamp}_${guidSuffix}${ext}"
}

function Test-PathTraversal {
    param([string]$FilePath)
    if ([string]::IsNullOrWhiteSpace($FilePath)) { return $true }
    $normalized = $FilePath.Trim().Replace("~", "").Replace("\", "/")
    if ($normalized.Contains("..") -or $normalized.Contains(":") -or $normalized.Contains("//")) {
        return $true
    }
    if (-not $normalized.StartsWith("/Contents/Uploads/SharedDocuments/") -and -not $normalized.StartsWith("/Contents/Uploads/Document/")) {
        return $true
    }
    return $false
}

function Test-TwoTierAuthorization {
    param(
        [string]$Username,
        [int]$RoleId,
        [string]$DocCreatedBy,
        [string]$Action # 'View', 'Download', 'Upload', 'Edit', 'Delete'
    )
    if ([string]::IsNullOrWhiteSpace($Username)) { return $false }
    
    if ($Action -in @('View', 'Download', 'Upload', 'List', 'Detail')) {
        return $true
    }

    $isQTHT = ($RoleId -eq 1 -or $Username.ToLower() -eq 'admin' -or $Username.ToLower() -eq 'quantri')
    $isOwner = ($Username.Equals($DocCreatedBy, [System.StringComparison]::OrdinalIgnoreCase))
    
    if ($Action -in @('Edit', 'Delete', 'ReplaceFile')) {
        return ($isQTHT -or $isOwner)
    }
    return $false
}

# ----------------- DATABASE VERIFICATION HELPERS -----------------

function Get-CrmConnection {
    param([string]$ConnStr = $script:DefaultConnStr)
    $conn = New-Object System.Data.SqlClient.SqlConnection($ConnStr)
    $conn.Open()
    return $conn
}

function Invoke-CrmQuery {
    param(
        [string]$Sql,
        [hashtable]$Parameters = @{},
        [string]$ConnStr = $script:DefaultConnStr
    )
    $conn = Get-CrmConnection -ConnStr $ConnStr
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Sql
        $cmd.CommandTimeout = 30
        foreach ($k in $Parameters.Keys) {
            $val = if ($null -eq $Parameters[$k]) { [System.DBNull]::Value } else { $Parameters[$k] }
            [void]$cmd.Parameters.AddWithValue("@$k", $val)
        }
        $da = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
        $dt = New-Object System.Data.DataTable
        [void]$da.Fill($dt)
        return $dt
    } finally {
        $conn.Close()
        $conn.Dispose()
    }
}

function Invoke-CrmScalar {
    param(
        [string]$Sql,
        [hashtable]$Parameters = @{},
        [string]$ConnStr = $script:DefaultConnStr
    )
    $conn = Get-CrmConnection -ConnStr $ConnStr
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Sql
        $cmd.CommandTimeout = 30
        foreach ($k in $Parameters.Keys) {
            $val = if ($null -eq $Parameters[$k]) { [System.DBNull]::Value } else { $Parameters[$k] }
            [void]$cmd.Parameters.AddWithValue("@$k", $val)
        }
        return $cmd.ExecuteScalar()
    } finally {
        $conn.Close()
        $conn.Dispose()
    }
}

function Test-CrmTableExists {
    param([string]$TableName, [string]$ConnStr = $script:DefaultConnStr)
    $sql = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName"
    $count = Invoke-CrmScalar -Sql $sql -Parameters @{ TableName = $TableName } -ConnStr $ConnStr
    return ([int]$count -gt 0)
}

function Get-CrmTableColumns {
    param([string]$TableName, [string]$ConnStr = $script:DefaultConnStr)
    $sql = "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName ORDER BY ORDINAL_POSITION"
    return Invoke-CrmQuery -Sql $sql -Parameters @{ TableName = $TableName } -ConnStr $ConnStr
}

function Test-CrmProcedureExists {
    param([string]$ProcedureName, [string]$ConnStr = $script:DefaultConnStr)
    $sql = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_NAME = @ProcedureName AND ROUTINE_TYPE = 'PROCEDURE'"
    $count = Invoke-CrmScalar -Sql $sql -Parameters @{ ProcedureName = $ProcedureName } -ConnStr $ConnStr
    return ([int]$count -gt 0)
}

function Test-CrmMenuExists {
    param([string]$Link, [int]$ParentId = 1, [string]$ConnStr = $script:DefaultConnStr)
    $sql = "SELECT COUNT(1) FROM dbo.Sys_Menus WHERE Link LIKE '%' + @Link + '%' AND (ParentId = @ParentId OR @ParentId = 0)"
    $count = Invoke-CrmScalar -Sql $sql -Parameters @{ Link = $Link; ParentId = $ParentId } -ConnStr $ConnStr
    return ([int]$count -gt 0)
}

function Get-CrmMessage {
    param([string]$LabelKey, [string]$LangCode = 'vi-VN', [string]$ConnStr = $script:DefaultConnStr)
    $sql = "SELECT TOP 1 Message FROM dbo.Sys_Messages WHERE LabelKey = @LabelKey AND LangCode = @LangCode"
    return Invoke-CrmScalar -Sql $sql -Parameters @{ LabelKey = $LabelKey; LangCode = $LangCode } -ConnStr $ConnStr
}

# ----------------- 5-LAYER CODE & LAYOUT SCANNERS -----------------

function Test-Utf8WithBom {
    param([string]$FilePath)
    if (-not (Test-Path $FilePath)) { return $false }
    $bytes = [System.IO.File]::ReadAllBytes($FilePath)
    if ($bytes.Length -lt 3) { return $false }
    return ($bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
}

function Get-FileMd5Hash {
    param([string]$FilePath)
    if (-not (Test-Path $FilePath)) { return "" }
    $md5 = [System.Security.Cryptography.MD5]::Create()
    $stream = [System.IO.File]::OpenRead($FilePath)
    try {
        $hashBytes = $md5.ComputeHash($stream)
        return [BitConverter]::ToString($hashBytes).Replace("-", "").ToLower()
    } finally {
        $stream.Close()
        $stream.Dispose()
        $md5.Dispose()
    }
}

function Test-TripleMirroring {
    param(
        [string]$ProjectRoot,
        [string]$RelativePath
    )
    $p1 = Join-Path $ProjectRoot (Join-Path "Modules.Sys" $RelativePath)
    $p2 = Join-Path $ProjectRoot (Join-Path "publish_source" $RelativePath)
    $p3 = Join-Path $ProjectRoot (Join-Path "CenIT.Solution.TOC.WebApp" $RelativePath)

    if (-not (Test-Path $p1) -or -not (Test-Path $p2) -or -not (Test-Path $p3)) {
        return @{
            IsMirrored = $false
            MissingPath = @($p1, $p2, $p3) | Where-Object { -not (Test-Path $_) }
            Hashes = @{}
        }
    }

    $h1 = Get-FileMd5Hash -FilePath $p1
    $h2 = Get-FileMd5Hash -FilePath $p2
    $h3 = Get-FileMd5Hash -FilePath $p3

    $isEqual = ($h1 -eq $h2 -and $h2 -eq $h3)
    return @{
        IsMirrored = $isEqual
        Hash1 = $h1
        Hash2 = $h2
        Hash3 = $h3
        Paths = @($p1, $p2, $p3)
    }
}

function Scan-DomIdCollisions {
    param([string[]]$FilePaths)
    $ids = @{}
    $collisions = @()
    foreach ($f in $FilePaths) {
        if (-not (Test-Path $f)) { continue }
        $content = [System.IO.File]::ReadAllText($f, [System.Text.Encoding]::UTF8)
        $matches = [System.Text.RegularExpressions.Regex]::Matches($content, 'id\s*=\s*["'']([^"''@\s>]+)["'']', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        foreach ($m in $matches) {
            $id = $m.Groups[1].Value.Trim()
            if ($id.StartsWith("modal_") -or $id -eq "bodyForm" -or $id -eq "modal-content") {
                continue
            }
            if ($ids.ContainsKey($id)) {
                $collisions += [PSCustomObject]@{
                    Id = $id
                    File1 = $ids[$id]
                    File2 = $f
                }
            } else {
                $ids[$id] = $f
            }
        }
    }
    return $collisions
}

function Scan-InlineStyles {
    param([string[]]$CshtmlFiles)
    $violations = @()
    foreach ($f in $CshtmlFiles) {
        if (-not (Test-Path $f)) { continue }
        $content = [System.IO.File]::ReadAllText($f, [System.Text.Encoding]::UTF8)
        if ($content -match '<style\b[^>]*>') {
            $violations += [PSCustomObject]@{ File = $f; Rule = "Tag <style> found" }
        }
        if ($content -match '#[0-9a-fA-F]{6}\b|#[0-9a-fA-F]{3}\b') {
            $violations += [PSCustomObject]@{ File = $f; Rule = "Hardcoded hex color found" }
        }
    }
    return $violations
}

# ----------------- REPORTING & SUMMARY -----------------

function Get-TestSummary {
    $total = $script:TestResults.Count
    $passed = ($script:TestResults | Where-Object { $_.Status -eq "PASS" }).Count
    $failed = ($script:TestResults | Where-Object { $_.Status -eq "FAIL" }).Count
    $skipped = ($script:TestResults | Where-Object { $_.Status -eq "SKIP" }).Count
    $passRate = if ($total -gt 0) { [math]::Round(($passed / $total) * 100, 1) } else { 0 }
    
    return [PSCustomObject]@{
        Total    = $total
        Passed   = $passed
        Failed   = $failed
        Skipped  = $skipped
        PassRate = $passRate
    }
}

function Export-TestResultsJson {
    param([string]$FilePath)
    $summary = Get-TestSummary
    $exportObj = [PSCustomObject]@{
        Summary     = $summary
        GeneratedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        Results     = $script:TestResults
    }
    $json = $exportObj | ConvertTo-Json -Depth 5
    $dir = [System.IO.Path]::GetDirectoryName($FilePath)
    if ($dir -and -not (Test-Path $dir)) {
        [System.IO.Directory]::CreateDirectory($dir) | Out-Null
    }
    [System.IO.File]::WriteAllText($FilePath, $json, [System.Text.Encoding]::UTF8)
}

Export-ModuleMember -Function @(
    'Reset-TestResults', 'Set-TestContext', 'Record-TestResult', 'Assert-True', 'Assert-False',
    'Assert-Equal', 'Assert-NotEqual', 'Assert-Contains', 'Assert-Matches',
    'Assert-Throws', 'Assert-DoesNotThrow',
    'Test-FileExtensionAllowed', 'Test-FileSizeAllowed', 'ConvertTo-UnSign',
    'Get-SanitizedFileName', 'Test-PathTraversal', 'Test-TwoTierAuthorization',
    'Get-CrmConnection', 'Invoke-CrmQuery', 'Invoke-CrmScalar',
    'Test-CrmTableExists', 'Get-CrmTableColumns', 'Test-CrmProcedureExists',
    'Test-CrmMenuExists', 'Get-CrmMessage',
    'Test-Utf8WithBom', 'Get-FileMd5Hash', 'Test-TripleMirroring',
    'Scan-DomIdCollisions', 'Scan-InlineStyles',
    'Get-TestSummary', 'Export-TestResultsJson'
)

param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,

    [string]$ScriptPath = (Join-Path $PSScriptRoot '..\Database\DigitalSalesProductDetail.sql'),

    [int]$CommandTimeoutSeconds = 120
)

$ErrorActionPreference = 'Stop'

$resolvedScriptPath = (Resolve-Path -LiteralPath $ScriptPath).Path
$utf8Strict = New-Object System.Text.UTF8Encoding($false, $true)
$scriptContent = [System.IO.File]::ReadAllText($resolvedScriptPath, $utf8Strict)

# SQL Server treats GO as a client-side batch separator. Split only when GO is
# the complete line so occurrences inside procedure bodies or string values remain intact.
$batches = [System.Text.RegularExpressions.Regex]::Split(
    $scriptContent,
    '(?im)^\s*GO\s*(?:--.*)?$'
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
try {
    $connection.Open()

    $batchNumber = 0
    foreach ($batch in $batches) {
        $batchNumber++
        $command = $connection.CreateCommand()
        try {
            $command.CommandTimeout = $CommandTimeoutSeconds
            $command.CommandText = $batch
            [void]$command.ExecuteNonQuery()
        }
        catch {
            throw "SQL batch $batchNumber failed in '$resolvedScriptPath': $($_.Exception.Message)"
        }
        finally {
            $command.Dispose()
        }
    }

    Write-Host "Applied $batchNumber UTF-8 SQL batches from '$resolvedScriptPath'." -ForegroundColor Green
}
finally {
    $connection.Dispose()
}

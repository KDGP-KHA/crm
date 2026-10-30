# Direct upload to Demo FTP from a workstation inside the VNPT network.
[CmdletBinding()]
param(
    [string]$Server = $(if ($env:FTP_SERVER_DEMO) { $env:FTP_SERVER_DEMO } else { "10.57.30.10" }),
    [int]$Port = 21,
    [string]$User = $(if ($env:FTP_USERNAME_DEMO) { $env:FTP_USERNAME_DEMO } else { "quanlydoanhthucenit" }),
    [string]$Password = $env:FTP_PASSWORD_DEMO,
    [string]$SourceDir = "publish_source",
    [string]$CredentialPath = ".secrets/ftp-demo.credential.xml",
    [switch]$Force,
    [switch]$InitializeManifest
)

$ErrorActionPreference = "Stop"

if ($Force -and $InitializeManifest) {
    throw "Force and InitializeManifest cannot be used together."
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$localPublishDir = Join-Path $rootDir $SourceDir
$localCredentialPath = Join-Path $rootDir $CredentialPath

if (-not (Test-Path $localPublishDir)) {
    throw "Directory $localPublishDir does not exist! Please run build_publish.ps1 first."
}

$mainDll = Join-Path $localPublishDir "bin/CenIT.Solution.TOC.WebApp.dll"
if (-not (Test-Path -LiteralPath $mainDll -PathType Leaf)) {
    throw "Published application DLL is missing: $mainDll"
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    if (-not (Test-Path -LiteralPath $localCredentialPath -PathType Leaf)) {
        throw "FTP credential is missing. Set FTP_PASSWORD_DEMO or create $localCredentialPath with Export-Clixml."
    }

    $storedCredential = Import-Clixml -LiteralPath $localCredentialPath
    $User = $storedCredential.UserName
    $Password = $storedCredential.GetNetworkCredential().Password
}

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   Direct Upload to Demo FTP ($Server)   " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

function Ensure-FtpDirectory {
    param([string]$remoteUri, [System.Net.NetworkCredential]$cred)
    try {
        $req = [System.Net.FtpWebRequest]::Create($remoteUri)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $req.Credentials = $cred
        $req.UsePassive = $true
        $req.GetResponse().Close()
    } catch {
        # Directory might already exist
    }
}

function Upload-FtpFile {
    param([string]$localFile, [string]$remoteUri, [System.Net.NetworkCredential]$cred)
    $req = [System.Net.FtpWebRequest]::Create($remoteUri)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.Credentials = $cred
    $req.UseBinary = $true
    $req.UsePassive = $true
    
    $fileBytes = [System.IO.File]::ReadAllBytes($localFile)
    $req.ContentLength = $fileBytes.Length
    $stream = $req.GetRequestStream()
    $stream.Write($fileBytes, 0, $fileBytes.Length)
    $stream.Close()
    $req.GetResponse().Close()
}

function Upload-FtpBytes {
    param([byte[]]$content, [string]$remoteUri, [System.Net.NetworkCredential]$cred)
    $req = [System.Net.FtpWebRequest]::Create($remoteUri)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.Credentials = $cred
    $req.UseBinary = $true
    $req.UsePassive = $true
    $req.ContentLength = $content.Length
    $stream = $req.GetRequestStream()
    try {
        $stream.Write($content, 0, $content.Length)
    } finally {
        $stream.Close()
    }
    $req.GetResponse().Close()
}

function Get-FtpText {
    param([string]$remoteUri, [System.Net.NetworkCredential]$cred)
    try {
        $req = [System.Net.FtpWebRequest]::Create($remoteUri)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
        $req.Credentials = $cred
        $req.UseBinary = $true
        $req.UsePassive = $true
        $response = $req.GetResponse()
        try {
            $reader = New-Object System.IO.StreamReader($response.GetResponseStream(), [System.Text.Encoding]::UTF8)
            try {
                return $reader.ReadToEnd()
            } finally {
                $reader.Close()
            }
        } finally {
            $response.Close()
        }
    } catch [System.Net.WebException] {
        $ftpResponse = $_.Exception.Response -as [System.Net.FtpWebResponse]
        if ($ftpResponse -and $ftpResponse.StatusCode -eq [System.Net.FtpStatusCode]::ActionNotTakenFileUnavailable) {
            $ftpResponse.Close()
            return $null
        }
        throw
    }
}

$cred = New-Object System.Net.NetworkCredential($User, $Password)
$baseUri = "ftp://$Server`:$Port/"
$manifestRelativePath = ".deploy-manifest.sha256.json"
$manifestUri = "$baseUri$manifestRelativePath"

function Get-RelativeFtpPath {
    param([string]$FullName)
    return $FullName.Substring($localPublishDir.Length).TrimStart('\', '/').Replace('\', '/')
}

$excludedRelativePaths = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase
)
[void]$excludedRelativePaths.Add("Web.config")
[void]$excludedRelativePaths.Add("Configs/AppSettings.config")
[void]$excludedRelativePaths.Add("Configs/ConnectionStrings.config")

$allFiles = @(Get-ChildItem -Path $localPublishDir -Recurse -File)
$files = @($allFiles | Where-Object {
    -not $excludedRelativePaths.Contains((Get-RelativeFtpPath -FullName $_.FullName))
})
$skippedFiles = @($allFiles | Where-Object {
    $excludedRelativePaths.Contains((Get-RelativeFtpPath -FullName $_.FullName))
})
$localEntries = @($files | ForEach-Object {
    [PSCustomObject]@{
        path = Get-RelativeFtpPath -FullName $_.FullName
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        length = $_.Length
        file = $_
    }
})

function New-ManifestBytes {
    param([object[]]$entries)
    $manifest = [ordered]@{
        schemaVersion = 1
        generatedAtUtc = [DateTime]::UtcNow.ToString("o")
        files = @($entries | ForEach-Object {
            [ordered]@{
                path = $_.path
                sha256 = $_.sha256
                length = $_.length
            }
        })
    }
    $json = $manifest | ConvertTo-Json -Depth 5
    return (New-Object System.Text.UTF8Encoding($false)).GetBytes($json)
}

Write-Host "Managed files: $($localEntries.Count); skipping $($skippedFiles.Count) environment config file(s)." -ForegroundColor Yellow
foreach ($skippedFile in $skippedFiles) {
    Write-Host "[SKIP] $(Get-RelativeFtpPath -FullName $skippedFile.FullName)" -ForegroundColor DarkYellow
}

if ($InitializeManifest) {
    Upload-FtpBytes -content (New-ManifestBytes -entries $localEntries) -remoteUri $manifestUri -cred $cred
    Write-Host "`n[SUCCESS] Initialized SHA-256 manifest for $($localEntries.Count) file(s); uploaded 0 application file(s); unchanged $($localEntries.Count); skipped $($skippedFiles.Count); failed 0." -ForegroundColor Green
    exit 0
}

$remoteHashes = @{}
if (-not $Force) {
    $manifestJson = Get-FtpText -remoteUri $manifestUri -cred $cred
    if ([string]::IsNullOrWhiteSpace($manifestJson)) {
        throw "Remote manifest is missing. Run once with -InitializeManifest after confirming FTP matches publish_source, or use -Force to upload all files."
    }

    try {
        $remoteManifest = $manifestJson | ConvertFrom-Json
        if ($remoteManifest.schemaVersion -ne 1) {
            throw "Unsupported schema version: $($remoteManifest.schemaVersion)"
        }
        foreach ($entry in @($remoteManifest.files)) {
            if (-not [string]::IsNullOrWhiteSpace($entry.path) -and -not [string]::IsNullOrWhiteSpace($entry.sha256)) {
                $remoteHashes[[string]$entry.path] = ([string]$entry.sha256).ToLowerInvariant()
            }
        }
    } catch {
        throw "Remote manifest is invalid: $($_.Exception.Message)"
    }
}

$filesToUpload = if ($Force) {
    @($localEntries)
} else {
    @($localEntries | Where-Object {
        -not $remoteHashes.ContainsKey($_.path) -or $remoteHashes[$_.path] -ne $_.sha256
    })
}

$total = $filesToUpload.Count
$current = 0
$uploaded = 0
$failed = [System.Collections.Generic.List[string]]::new()
$unchanged = $localEntries.Count - $total

Write-Host "Files selected for upload: $total; unchanged: $unchanged; mode: $(if ($Force) { 'force' } else { 'incremental' })." -ForegroundColor Yellow

# Ensure only directories needed by new or changed files, including parent directories.
$neededDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $filesToUpload) {
    $segments = $entry.path.Split('/')
    for ($i = 1; $i -lt $segments.Length; $i++) {
        [void]$neededDirectories.Add(($segments[0..($i - 1)] -join '/'))
    }
}
foreach ($rel in @($neededDirectories) | Sort-Object { ($_ -split '/').Count }) {
    Ensure-FtpDirectory -remoteUri "$baseUri$rel/" -cred $cred
}

foreach ($entry in $filesToUpload) {
    $current++
    $rel = $entry.path
    $targetUri = "$baseUri$rel"
    Write-Progress -Activity "Uploading to FTP Demo" -Status "$current/${total}: $rel" -PercentComplete (($current / $total) * 100)
    try {
        Upload-FtpFile -localFile $entry.file.FullName -remoteUri $targetUri -cred $cred
        $uploaded++
    } catch {
        Write-Warning "Failed to upload $rel : $($_.Exception.Message)"
        $failed.Add($rel)
    }
}

Write-Progress -Activity "Uploading to FTP Demo" -Completed
if ($failed.Count -gt 0) {
    Write-Error "FTP upload failed: uploaded $uploaded/$total selected file(s); unchanged $unchanged; skipped $($skippedFiles.Count); failed $($failed.Count). Manifest was not updated."
    exit 1
}

try {
    Upload-FtpBytes -content (New-ManifestBytes -entries $localEntries) -remoteUri $manifestUri -cred $cred
} catch {
    Write-Error "Application files uploaded, but manifest update failed: $($_.Exception.Message)"
    exit 1
}

Write-Host "`n[SUCCESS] Uploaded $uploaded/$total selected file(s); unchanged $unchanged; skipped $($skippedFiles.Count); failed 0. SHA-256 manifest updated." -ForegroundColor Green
exit 0

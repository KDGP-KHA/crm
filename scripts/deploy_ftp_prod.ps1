# ==============================================================================
# Script: deploy_ftp_prod.ps1
# Muc dich: Trien khai truc tiep ma nguon tu publish_source len FTP Chinh thuc (Prod)
#           Server: 10.57.47.3:21
#           User: crm
#           BAO VE: TUYET DOI KHONG GHI DE Web.config va Configs/AppSettings.config
# ==============================================================================

[CmdletBinding()]
param(
    [string]$Server = "10.57.47.3",
    [int]$Port = 21,
    [string]$User = "crm",
    [string]$Password = "Kh@2026",
    [string]$SourceDir = "publish_source",
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

if (-not (Test-Path $localPublishDir)) {
    throw "Directory $localPublishDir does not exist! Please run build_publish.ps1 first."
}

$mainDll = Join-Path $localPublishDir "bin/CenIT.Solution.TOC.WebApp.dll"
if (-not (Test-Path -LiteralPath $mainDll -PathType Leaf)) {
    throw "Published application DLL is missing: $mainDll"
}

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "   DEPLOY TO PRODUCTION FTP ($($Server):$($Port))      " -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "• Server    : $($Server):$($Port)" -ForegroundColor Gray
Write-Host "• User      : $User" -ForegroundColor Gray
Write-Host "• Source    : $localPublishDir" -ForegroundColor Gray
Write-Host "• Mode      : $(if ($Force) { 'FORCE ALL' } elseif ($InitializeManifest) { 'INIT MANIFEST' } else { 'INCREMENTAL (SHA-256)' })" -ForegroundColor Gray
Write-Host ""

function Ensure-FtpDirectory {
    param([string]$remoteUri, [System.Net.NetworkCredential]$cred)
    try {
        $req = [System.Net.FtpWebRequest]::Create($remoteUri)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $req.Credentials = $cred
        $req.UsePassive = $true
        $req.KeepAlive = $false
        $req.Timeout = 15000
        $resp = $req.GetResponse()
        $resp.Close()
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
    $req.KeepAlive = $false
    $req.Timeout = 30000
    $req.ReadWriteTimeout = 60000
    
    $fileBytes = [System.IO.File]::ReadAllBytes($localFile)
    $req.ContentLength = $fileBytes.Length
    $stream = $req.GetRequestStream()
    try {
        $stream.Write($fileBytes, 0, $fileBytes.Length)
    } finally {
        $stream.Close()
    }
    $resp = $req.GetResponse()
    $resp.Close()
}

function Upload-FtpBytes {
    param([byte[]]$content, [string]$remoteUri, [System.Net.NetworkCredential]$cred)
    $req = [System.Net.FtpWebRequest]::Create($remoteUri)
    $req.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $req.Credentials = $cred
    $req.UseBinary = $true
    $req.UsePassive = $true
    $req.KeepAlive = $false
    $req.Timeout = 30000
    $req.ReadWriteTimeout = 60000
    $req.ContentLength = $content.Length
    $stream = $req.GetRequestStream()
    try {
        $stream.Write($content, 0, $content.Length)
    } finally {
        $stream.Close()
    }
    $resp = $req.GetResponse()
    $resp.Close()
}

function Get-FtpText {
    param([string]$remoteUri, [System.Net.NetworkCredential]$cred)
    try {
        $req = [System.Net.FtpWebRequest]::Create($remoteUri)
        $req.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
        $req.Credentials = $cred
        $req.UseBinary = $true
        $req.UsePassive = $true
        $req.KeepAlive = $false
        $req.Timeout = 15000
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
        if ($ftpResponse -and ($ftpResponse.StatusCode -eq [System.Net.FtpStatusCode]::ActionNotTakenFileUnavailable -or [int]$ftpResponse.StatusCode -eq 550)) {
            $ftpResponse.Close()
            return $null
        }
        return $null
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

# CRITICAL EXCLUSIONS FOR PRODUCTION
$excludedRelativePaths = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase
)
[void]$excludedRelativePaths.Add("Web.config")
[void]$excludedRelativePaths.Add("Configs/AppSettings.config")

$allFiles = @(Get-ChildItem -Path $localPublishDir -Recurse -File)
$files = @($allFiles | Where-Object {
    -not $excludedRelativePaths.Contains((Get-RelativeFtpPath -FullName $_.FullName))
})
$skippedFiles = @($allFiles | Where-Object {
    $excludedRelativePaths.Contains((Get-RelativeFtpPath -FullName $_.FullName))
})

Write-Host "Scanning local files and calculating SHA-256 hashes..." -ForegroundColor Yellow
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

Write-Host "Managed files: $($localEntries.Count); Strictly skipping $($skippedFiles.Count) environment config file(s):" -ForegroundColor Yellow
foreach ($skippedFile in $skippedFiles) {
    Write-Host "  [SKIP/PROTECTED] $(Get-RelativeFtpPath -FullName $skippedFile.FullName)" -ForegroundColor DarkYellow
}

if ($InitializeManifest) {
    Write-Host "`nInitializing manifest on remote FTP..." -ForegroundColor Yellow
    Upload-FtpBytes -content (New-ManifestBytes -entries $localEntries) -remoteUri $manifestUri -cred $cred
    Write-Host "[SUCCESS] Initialized SHA-256 manifest for $($localEntries.Count) file(s) on $Server." -ForegroundColor Green
    exit 0
}

$remoteHashes = @{}
$manifestFound = $false

if (-not $Force) {
    Write-Host "`nChecking for remote SHA-256 manifest on $Server..." -ForegroundColor Yellow
    $manifestJson = Get-FtpText -remoteUri $manifestUri -cred $cred
    if (-not [string]::IsNullOrWhiteSpace($manifestJson)) {
        try {
            $remoteManifest = $manifestJson | ConvertFrom-Json
            if ($remoteManifest.schemaVersion -eq 1) {
                foreach ($entry in @($remoteManifest.files)) {
                    if (-not [string]::IsNullOrWhiteSpace($entry.path) -and -not [string]::IsNullOrWhiteSpace($entry.sha256)) {
                        $remoteHashes[[string]$entry.path] = ([string]$entry.sha256).ToLowerInvariant()
                    }
                }
                $manifestFound = $true
                Write-Host "Remote manifest found! Contains $($remoteHashes.Count) tracked files." -ForegroundColor Green
            }
        } catch {
            Write-Warning "Remote manifest could not be parsed: $($_.Exception.Message)"
        }
    } else {
        Write-Host "No remote manifest found on $Server. Performing full initial sync." -ForegroundColor Cyan
    }
}

$filesToUpload = if ($Force -or -not $manifestFound) {
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

Write-Host "`nDeploy Summary:" -ForegroundColor Cyan
Write-Host "• Total managed files : $($localEntries.Count)"
Write-Host "• Files to upload     : $total" -ForegroundColor $(if ($total -gt 0) { 'Yellow' } else { 'Green' })
Write-Host "• Unchanged files     : $unchanged" -ForegroundColor Gray
Write-Host "• Skipped configs     : $($skippedFiles.Count)" -ForegroundColor DarkYellow
Write-Host ""

if ($total -eq 0) {
    Write-Host "[SUCCESS] All files are identical to remote manifest. Nothing to upload!" -ForegroundColor Green
    exit 0
}

# 1. Ensure all required remote directories exist
Write-Host "Verifying remote directory structure..." -ForegroundColor Yellow
$neededDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $filesToUpload) {
    $segments = $entry.path.Split('/')
    for ($i = 1; $i -lt $segments.Length; $i++) {
        [void]$neededDirectories.Add(($segments[0..($i - 1)] -join '/'))
    }
}
$sortedDirs = @($neededDirectories) | Sort-Object { ($_ -split '/').Count }
$dirCount = 0
foreach ($rel in $sortedDirs) {
    Ensure-FtpDirectory -remoteUri "$baseUri$rel/" -cred $cred
    $dirCount++
}
Write-Host "Verified $dirCount directories." -ForegroundColor Green

# 2. Upload files
Write-Host "`nUploading $total file(s) to $Server..." -ForegroundColor Yellow
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

foreach ($entry in $filesToUpload) {
    $current++
    $rel = $entry.path
    $targetUri = "$baseUri$rel"
    
    $retry = 0
    $success = $false
    while (-not $success -and $retry -lt 3) {
        try {
            Upload-FtpFile -localFile $entry.file.FullName -remoteUri $targetUri -cred $cred
            $uploaded++
            $success = $true
        } catch {
            $retry++
            if ($retry -ge 3) {
                Write-Host " [FAIL] $rel : $($_.Exception.Message)" -ForegroundColor Red
                $failed.Add($rel)
            } else {
                Start-Sleep -Milliseconds 200
            }
        }
    }
    
    if ($current % 100 -eq 0 -or $current -eq $total) {
        $elapsedSec = [math]::Max(1, [int]$stopwatch.Elapsed.TotalSeconds)
        $speed = [math]::Round($current / $elapsedSec, 1)
        Write-Host "Progress: $current / $total files ($([math]::Round(($current / $total) * 100))%) - Speed: $speed files/s" -ForegroundColor Cyan
    }
}

$stopwatch.Stop()

if ($failed.Count -gt 0) {
    Write-Error "FTP upload failed: uploaded $uploaded/$total file(s); failed $($failed.Count). Remote manifest was NOT updated."
    exit 1
}

# 3. Save remote manifest
Write-Host "`nUpdating remote SHA-256 manifest on $Server..." -ForegroundColor Yellow
try {
    Upload-FtpBytes -content (New-ManifestBytes -entries $localEntries) -remoteUri $manifestUri -cred $cred
    Write-Host "Remote manifest updated successfully." -ForegroundColor Green
} catch {
    Write-Error "Files uploaded, but manifest update failed: $($_.Exception.Message)"
    exit 1
}

Write-Host "`n=======================================================" -ForegroundColor Green
Write-Host "   PRODUCTION DEPLOY COMPLETED SUCCESSFULLY!           " -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green
Write-Host "• Files uploaded      : $uploaded" -ForegroundColor Green
Write-Host "• Files unchanged     : $unchanged" -ForegroundColor Gray
Write-Host "• Configs protected   : $($skippedFiles.Count) (Web.config & AppSettings.config)" -ForegroundColor Yellow
Write-Host "• Elapsed time        : $($stopwatch.Elapsed.ToString('mm\:ss'))" -ForegroundColor Cyan
Write-Host ""
exit 0

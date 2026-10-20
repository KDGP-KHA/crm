# ==============================================================================
# Script: rollback_prod.ps1
# Muc dich: Rollback he thong Production ve ban sao luu on dinh (Source_Prod hoac thu muc chi dinh)
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
    [string]$SourceDir = "Source_Prod",
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$deployScript = Join-Path $scriptDir "deploy_ftp_prod.ps1"

if (-not (Test-Path $deployScript)) {
    throw "Khong tim thay script deploy_ftp_prod.ps1 tai $deployScript"
}

Write-Host "=======================================================" -ForegroundColor Red
Write-Host "        ROLLBACK PRODUCTION VE BAN SAO LUU              " -ForegroundColor Red
Write-Host "=======================================================" -ForegroundColor Red
Write-Host "• Nguon rollback : $SourceDir" -ForegroundColor Yellow
Write-Host "• May chu        : $Server:$Port" -ForegroundColor Yellow
Write-Host "• Che do         : $(if ($Force) { 'FORCE ALL' } else { 'INCREMENTAL (SHA-256)' })" -ForegroundColor Yellow
Write-Host ""

$deployParams = @{
    Server = $Server
    Port = $Port
    User = $User
    Password = $Password
    SourceDir = $SourceDir
}
if ($Force) {
    $deployParams["Force"] = $true
}

& $deployScript @deployParams

if ($LASTEXITCODE -ne 0) {
    throw "Rollback that bai voi ma loi $LASTEXITCODE"
}

Write-Host ""
Write-Host "=======================================================" -ForegroundColor Green
Write-Host "      ROLLBACK PRODUCTION HOAN TAT THANH CONG!         " -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Green

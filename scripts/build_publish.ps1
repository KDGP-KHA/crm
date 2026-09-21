# Build and publish script for CenIT CRM Solution
[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   CRM Build & Publish to publish_source  " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Locate MSBuild
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = ""
if (Test-Path $vswhere) {
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
}

if (-not $msbuild -or -not (Test-Path $msbuild)) {
    $defaultPaths = @(
        "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )
    foreach ($p in $defaultPaths) {
        if (Test-Path $p) {
            $msbuild = $p
            break
        }
    }
}

if (-not $msbuild) {
    throw "MSBuild.exe not found! Please check Visual Studio installation."
}

Write-Host "Using MSBuild: $msbuild" -ForegroundColor Green

# 2. Build Solution
Write-Host "`n[1/3] Building Solution ($Configuration)..." -ForegroundColor Yellow
$slnPath = Join-Path $rootDir "CenIT.Solution.TOC.sln"
& $msbuild $slnPath /p:Configuration=$Configuration /v:m
if ($LASTEXITCODE -ne 0) {
    throw "Failed to build solution $slnPath (Exit code: $LASTEXITCODE)"
}

# 3. Create Web Package
Write-Host "`n[2/3] Packaging CenIT.Solution.TOC.WebApp..." -ForegroundColor Yellow
$webAppCsproj = Join-Path $rootDir "CenIT.Solution.TOC.WebApp\CenIT.Solution.TOC.WebApp.csproj"
& $msbuild $webAppCsproj /target:PipelinePreDeployCopyAllFilesToOneFolder /p:Configuration=$Configuration /p:SolutionDir="$rootDir\" /v:m
if ($LASTEXITCODE -ne 0) {
    throw "Failed to package WebApp (Exit code: $LASTEXITCODE)"
}

# 4. Copy to publish_source
Write-Host "`n[3/3] Synchronizing package to publish_source..." -ForegroundColor Yellow
$packageTmpDir = Join-Path $rootDir "CenIT.Solution.TOC.WebApp\obj\$Configuration\Package\PackageTmp"
$publishSourceDir = Join-Path $rootDir "publish_source"

if (-not (Test-Path $publishSourceDir)) {
    New-Item -ItemType Directory -Path $publishSourceDir -Force | Out-Null
}

$excludeDirs = @("uploads", "Logs", "JobLogs", "ImportTemp", "EForm", "NewRegistrations", "avatars", "news", "Controllers", "Data", "Enums", "banners", "comment", "log", "task")
$excludeFiles = @("*.log", "*.cs", "ListUC.xlsx")

robocopy $packageTmpDir $publishSourceDir /E /XD $excludeDirs /XF $excludeFiles /NJH /NJS /NDL /NC /NS
$roboExit = $LASTEXITCODE
if ($roboExit -ge 8) {
    throw "Robocopy failed with exit code: $roboExit"
}

# Ensure full Triple Mirroring: sync all Area Views, Views, Contents from Modules and WebApp to publish_source (ONLY views and static assets, NO C# source files/controllers)
Write-Host "Synchronizing Views and Contents to publish_source..." -ForegroundColor Yellow
$moduleList = @("Cate", "Dashboard", "Sys")
$webAppDir = Join-Path $rootDir "CenIT.Solution.TOC.WebApp"

foreach ($areaName in $moduleList) {
    $modViews = Join-Path $rootDir "Modules.$areaName\Areas\$areaName\Views"
    $webAppViews = Join-Path $webAppDir "Areas\$areaName\Views"
    $pubViews = Join-Path $publishSourceDir "Areas\$areaName\Views"

    if (Test-Path $modViews) {
        robocopy $modViews $webAppViews /E /NJH /NJS /NDL /NC /NS
        robocopy $modViews $pubViews /E /NJH /NJS /NDL /NC /NS
    }
}

robocopy (Join-Path $webAppDir "Views") (Join-Path $publishSourceDir "Views") /E /NJH /NJS /NDL /NC /NS
robocopy (Join-Path $webAppDir "Scripts") (Join-Path $publishSourceDir "Scripts") /E /NJH /NJS /NDL /NC /NS
robocopy (Join-Path $webAppDir "Contents") (Join-Path $publishSourceDir "Contents") /E /XD $excludeDirs /XF $excludeFiles /NJH /NJS /NDL /NC /NS

# Không xóa dữ liệu có sẵn trong publish_source khi đóng gói.
$cleanupEnabled = $false
if ($cleanupEnabled) {
Write-Host "Purging any stray logs and attachment directories from publish_source..." -ForegroundColor Yellow
$cleanupDirs = @(
    "Contents\JobLogs",
    "Contents\Logs",
    "Logs",
    "Contents\uploads",
    "Contents\Files\EForm",
    "Contents\Files\NewRegistrations",
    "Contents\ImportTemp",
    "Contents\imgs\avatars",
    "Contents\imgs\news",
    "Contents\imgs\comment",
    "Contents\imgs\log",
    "Contents\imgs\task",
    "Contents\imgs\banners"
)
foreach ($cd in $cleanupDirs) {
    $targetDir = Join-Path $publishSourceDir $cd
    if (Test-Path $targetDir) {
        Remove-Item -Path $targetDir -Recurse -Force
    }
}
$cleanupFiles = @(
    "Contents\File\ListUC.xlsx"
)
foreach ($cf in $cleanupFiles) {
    $targetFile = Join-Path $publishSourceDir $cf
    if (Test-Path $targetFile) {
        Remove-Item -Path $targetFile -Force
    }
}
# Purge stray document/spreadsheet/pdf attachments from Contents\imgs
$imgsDir = Join-Path $publishSourceDir "Contents\imgs"
if (Test-Path $imgsDir) {
    Get-ChildItem -Path $imgsDir -File | Where-Object { $_.Extension -in @(".docx", ".xlsx", ".pdf", ".zip", ".rar") } | Remove-Item -Force
}
# Purge CKFinder test upload attachments
$ckImagesDir = Join-Path $publishSourceDir "Contents\Modules\Major\ckfinder\core\connector\aspx\Images"
if (Test-Path $ckImagesDir) {
    Get-ChildItem -Path $ckImagesDir -Recurse -File | Remove-Item -Force
    Get-ChildItem -Path $ckImagesDir -Recurse -Directory | Remove-Item -Recurse -Force
}
Get-ChildItem -Path $publishSourceDir -Recurse -Filter "*.log" | Remove-Item -Force
}

$itemCount = (Get-ChildItem $publishSourceDir).Count
Write-Host "`n[SUCCESS] Successfully published $itemCount items to $publishSourceDir (no delete)!" -ForegroundColor Green

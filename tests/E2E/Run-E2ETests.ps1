# CenIT TOC CRM - Automated E2E Test Runner
# Module: Quản lý tài liệu chung (General Document Management)
# Encoding: UTF-8 with BOM

param(
    [int[]]$Tier = @(1, 2, 3, 4),
    [int]$Feature = 0,
    [string]$Milestone = "All",
    [switch]$VerifyHarness,
    [string]$JsonReportPath = (Join-Path $PSScriptRoot "TestResults.json"),
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
)

$ErrorActionPreference = "Continue"

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host ""
Write-Host "========================================================================" -ForegroundColor DarkCyan
Write-Host "         CENIT TOC CRM - AUTOMATED E2E TEST RUNNER                      " -ForegroundColor Cyan
Write-Host "     Module: Quản lý tài liệu chung (Shared Document Management)        " -ForegroundColor Cyan
Write-Host "========================================================================" -ForegroundColor DarkCyan
Write-Host "Execution Mode     : $(if ($VerifyHarness) { 'Harness Verification & Specification Check' } else { 'Live Environment & Integration E2E' })" -ForegroundColor Gray
Write-Host "Active Tiers       : $($Tier -join ', ')" -ForegroundColor Gray
Write-Host "Milestone Filter   : $Milestone" -ForegroundColor Gray
Write-Host "Feature Filter     : $(if ($Feature -gt 0) { "Feature $Feature" } else { "All Features (1-23)" })" -ForegroundColor Gray
Write-Host "Project Root       : $ProjectRoot" -ForegroundColor Gray
Write-Host "Report Path        : $JsonReportPath" -ForegroundColor Gray
Write-Host "Start Time         : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray
Write-Host "========================================================================" -ForegroundColor DarkCyan
Write-Host ""

$harnessPath = Join-Path $PSScriptRoot "E2ETestHarness.psm1"
if (-not (Test-Path $harnessPath)) {
    Write-Host "FATAL ERROR: E2ETestHarness.psm1 not found at $harnessPath" -ForegroundColor Red
    exit 2
}

Import-Module $harnessPath -Force
Reset-TestResults

# Execute Tier 1: Feature Coverage
if ($Tier -contains 1) {
    $t1Script = Join-Path $PSScriptRoot "Tier1_FeatureCoverage.Tests.ps1"
    if (Test-Path $t1Script) {
        & $t1Script -ProjectRoot $ProjectRoot -SpecificFeature $Feature -MilestoneFilter $Milestone -VerifyHarness $VerifyHarness.IsPresent
    } else {
        Write-Host "WARNING: Tier1 script not found: $t1Script" -ForegroundColor Yellow
    }
}

# Execute Tier 2: Boundary & Corner Cases
if ($Tier -contains 2 -and ($Feature -eq 0 -or $Feature -in @(9, 10, 14, 23))) {
    $t2Script = Join-Path $PSScriptRoot "Tier2_BoundaryCorner.Tests.ps1"
    if (Test-Path $t2Script) {
        & $t2Script -ProjectRoot $ProjectRoot -VerifyHarness $VerifyHarness.IsPresent
    } else {
        Write-Host "WARNING: Tier2 script not found: $t2Script" -ForegroundColor Yellow
    }
}

# Execute Tier 3: Cross-Feature Combinations
if ($Tier -contains 3 -and $Feature -eq 0) {
    $t3Script = Join-Path $PSScriptRoot "Tier3_CrossFeatureCombinations.Tests.ps1"
    if (Test-Path $t3Script) {
        & $t3Script -ProjectRoot $ProjectRoot -VerifyHarness $VerifyHarness.IsPresent
    } else {
        Write-Host "WARNING: Tier3 script not found: $t3Script" -ForegroundColor Yellow
    }
}

# Execute Tier 4: Real-World Persona Scenarios
if ($Tier -contains 4 -and $Feature -eq 0) {
    $t4Script = Join-Path $PSScriptRoot "Tier4_RealWorldScenarios.Tests.ps1"
    if (Test-Path $t4Script) {
        & $t4Script -ProjectRoot $ProjectRoot -VerifyHarness $VerifyHarness.IsPresent
    } else {
        Write-Host "WARNING: Tier4 script not found: $t4Script" -ForegroundColor Yellow
    }
}

# Execute Tier 5: Adversarial Coverage Hardening
if ($Tier -contains 5 -and $Feature -eq 0) {
    $t5Script = Join-Path $PSScriptRoot "Tier5_AdversarialHardening.Tests.ps1"
    if (Test-Path $t5Script) {
        & $t5Script -ProjectRoot $ProjectRoot -VerifyHarness $VerifyHarness.IsPresent
    } else {
        Write-Host "WARNING: Tier5 script not found: $t5Script" -ForegroundColor Yellow
    }
}

$stopwatch.Stop()
$elapsedSec = [math]::Round($stopwatch.Elapsed.TotalSeconds, 2)

# Summarize results
$summary = Get-TestSummary

Write-Host ""
Write-Host "========================================================================" -ForegroundColor DarkCyan
Write-Host "                         TEST EXECUTION SUMMARY                         " -ForegroundColor Cyan
Write-Host "========================================================================" -ForegroundColor DarkCyan
Write-Host "  Total Test Cases Run   : $($summary.Total)" -ForegroundColor White
Write-Host "  Passed                 : $($summary.Passed)" -ForegroundColor Green
Write-Host "  Failed                 : $($summary.Failed)" -ForegroundColor $(if ($summary.Failed -gt 0) { "Red" } else { "Green" })
Write-Host "  Skipped / Pending      : $($summary.Skipped)" -ForegroundColor Yellow
Write-Host "  Pass Rate              : $($summary.PassRate) %" -ForegroundColor $(if ($summary.PassRate -eq 100) { "Green" } else { "Yellow" })
Write-Host "  Execution Duration     : $elapsedSec seconds" -ForegroundColor Gray
Write-Host "========================================================================" -ForegroundColor DarkCyan

# Export results to JSON
try {
    Export-TestResultsJson -FilePath $JsonReportPath
    Write-Host "Detailed report exported to: $JsonReportPath" -ForegroundColor DarkGray
} catch {
    Write-Host "Failed to export JSON report: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
if ($summary.Failed -eq 0 -and $summary.Total -gt 0) {
    Write-Host "RESULT: ALL TESTS PASSED SUCCESSFULLY! (Exit Code: 0)" -ForegroundColor Green
    exit 0
} else {
    Write-Host "RESULT: $($summary.Failed) TEST(S) FAILED! (Exit Code: 1)" -ForegroundColor Red
    exit 1
}

# Master Challenger Test Suite Runner for Milestone 1 Database Stress & Concurrency
# Tests Suites 1, 2, and 3 sequentially and aggregates empirical results.

$ErrorActionPreference = "Stop"
$swTotal = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host "==========================================================================" -ForegroundColor Magenta
Write-Host " CENIT TOC CRM — DATABASE STRESS & CONCURRENCY CHALLENGER TEST HARNESS" -ForegroundColor Magenta
Write-Host " Database: quanlydoanhthucenit @ 10.57.30.10" -ForegroundColor Magenta
Write-Host " Date/Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Magenta
Write-Host "==========================================================================" -ForegroundColor Magenta

$baseDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Run Suite 1
Write-Host "`n>>> RUNNING SUITE 1: Concurrency & Atomic Tracking..." -ForegroundColor Cyan
& (Join-Path $baseDir "test_concurrency_trackdownload.ps1")
$suite1Exit = $LASTEXITCODE

# Run Suite 2
Write-Host "`n>>> RUNNING SUITE 2: Complex Filtering & Date Boundaries..." -ForegroundColor Cyan
& (Join-Path $baseDir "test_filtering_getlist.ps1")
$suite2Exit = $LASTEXITCODE

# Run Suite 3
Write-Host "`n>>> RUNNING SUITE 3: Extreme Inputs & Boundary Values..." -ForegroundColor Cyan
& (Join-Path $baseDir "test_extreme_inputs.ps1")
$suite3Exit = $LASTEXITCODE

$swTotal.Stop()

Write-Host "`n==========================================================================" -ForegroundColor Magenta
Write-Host " MASTER CHALLENGER VERIFICATION SUMMARY" -ForegroundColor Magenta
Write-Host "==========================================================================" -ForegroundColor Magenta
Write-Host " Suite 1 (Concurrency Stress - 4 Test Blocks / 200 Executions): $(if ($suite1Exit -eq 0) { 'PASS' } else { 'FAIL' })" -ForegroundColor $(if ($suite1Exit -eq 0) { 'Green' } else { 'Red' })
Write-Host " Suite 2 (Complex Filtering - 23 Test Cases):                  $(if ($suite2Exit -eq 0) { 'PASS' } else { 'FAIL' })" -ForegroundColor $(if ($suite2Exit -eq 0) { 'Green' } else { 'Red' })
Write-Host " Suite 3 (Extreme Inputs & Boundaries - 23 Test Cases):        $(if ($suite3Exit -eq 0) { 'PASS' } else { 'FAIL' })" -ForegroundColor $(if ($suite3Exit -eq 0) { 'Green' } else { 'Red' })
Write-Host " Total Elapsed Time: $($swTotal.ElapsedMilliseconds) ms" -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Magenta

if ($suite1Exit -eq 0 -and $suite2Exit -eq 0 -and $suite3Exit -eq 0) {
    Write-Host "ALL 50 ADVERSARIAL STRESS TESTS PASSED SUCCESSFULLY!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "ONE OR MORE TEST SUITES FAILED!" -ForegroundColor Red
    exit 1
}

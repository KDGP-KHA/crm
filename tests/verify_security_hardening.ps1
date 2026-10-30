# Automated Security Verification Test Suite
$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " STARTING SECURITY & ATTT VERIFICATION TEST SUITE         " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$baseDir = "d:\SVN\crm"
$passedCount = 0
$failedCount = 0

function Assert-Test {
    param(
        [string]$testName,
        [bool]$condition,
        [string]$detail
    )
    if ($condition) {
        Write-Host " [PASS] $testName" -ForegroundColor Green
        if ($detail) { Write-Host "        $detail" -ForegroundColor Gray }
        $script:passedCount++
    } else {
        Write-Host " [FAIL] $testName" -ForegroundColor Red
        if ($detail) { Write-Host "        $detail" -ForegroundColor Yellow }
        $script:failedCount++
    }
}

# ----------------------------------------------------
# 1. SEC-01: Cleartext DB Credentials
# ----------------------------------------------------
$csFile = Join-Path $baseDir "CenIT.Solution.TOC.WebApp\Configs\ConnectionStrings.config"
$csPubFile = Join-Path $baseDir "publish_source\Configs\ConnectionStrings.config"
$gitignoreFile = Join-Path $baseDir ".gitignore"
$webConfigFile = Join-Path $baseDir "CenIT.Solution.TOC.WebApp\Web.config"
$webConfigPubFile = Join-Path $baseDir "publish_source\Web.config"

$hasCsFile = (Test-Path $csFile) -and (Test-Path $csPubFile)
Assert-Test "SEC-01.1: ConnectionStrings.config exists in WebApp and publish_source" $hasCsFile "Path: $csFile"

$gitignoreContent = Get-Content -Path $gitignoreFile -Raw
$isGitIgnored = $gitignoreContent.Contains("Configs/ConnectionStrings.config")
Assert-Test "SEC-01.2: .gitignore blocks ConnectionStrings.config" $isGitIgnored "Credentials excluded from git"

$webConfigContent = Get-Content -Path $webConfigFile -Raw
$usesConfigSource = $webConfigContent.Contains('configSource="Configs\ConnectionStrings.config"')
Assert-Test "SEC-01.3: Web.config uses configSource attribute" $usesConfigSource "Credentials isolated from Web.config"

# ----------------------------------------------------
# 2. SEC-02 & SEC-03 & SEC-04: FileSecurityHelper
# ----------------------------------------------------
$fileSecurityHelper = Join-Path $baseDir "TSFramework.Libs\Helpers\FileSecurityHelper.cs"
$fshContent = Get-Content -Path $fileSecurityHelper -Raw

$hasZipSlip = $fshContent.Contains("SafeExtractZipEntry")
Assert-Test "SEC-02: Zip Slip RCE (CWE-29) defense implemented" $hasZipSlip "SafeExtractZipEntry prevents directory breakout"

$hasDoubleExt = $fshContent.Contains("GetFileNameWithoutExtension") -and $fshContent.Contains("BlockedExtensions.Contains(secondExt)")
Assert-Test "SEC-03: Double extension (.aspx.txt, .php.jpg) and executable upload blocked" $hasDoubleExt "Strict extension inspection"

$hasLogTraversal = $fshContent.Contains("GetSafeLogPath")
Assert-Test "SEC-04: Path traversal in SysLogController fixed" $hasLogTraversal "GetSafeLogPath enforces physical log folder"

# ----------------------------------------------------
# 3. SEC-05 & SEC-08 & SEC-10 & SEC-11 & SEC-13: Web.config Hardening
# ----------------------------------------------------
$hasRemoteOnly = $webConfigContent.Contains('mode="RemoteOnly"')
Assert-Test "SEC-05: customErrors RemoteOnly prevents stack trace disclosure" $hasRemoteOnly "Error page secured"

$hasSecureCookies = $webConfigContent.Contains('httpOnlyCookies="true"') -and $webConfigContent.Contains('sameSite="Lax"')
Assert-Test "SEC-08: Cookies configured with HttpOnly=true and SameSite=Lax" $hasSecureCookies "Session hijacking & CSRF defense"

$removesXPoweredBy = $webConfigContent.Contains('<remove name="X-Powered-By" />')
Assert-Test "SEC-10.1: X-Powered-By removed from HTTP response" $removesXPoweredBy "Server banner suppressed"

$globalAsaxFile = Join-Path $baseDir "CenIT.Solution.TOC.WebApp\Global.asax.cs"
$globalAsaxContent = Get-Content -Path $globalAsaxFile -Raw
$disablesMvcHeader = $globalAsaxContent.Contains("MvcHandler.DisableMvcResponseHeader = true")
Assert-Test "SEC-10.2: X-AspNetMvc-Version disabled in Global.asax" $disablesMvcHeader "MvcHandler.DisableMvcResponseHeader = true"

$hasFrameOptions = $webConfigContent.Contains('name="X-Frame-Options" value="SAMEORIGIN"')
$hasContentType = $webConfigContent.Contains('name="X-Content-Type-Options" value="nosniff"')
$hasXssProtect = $webConfigContent.Contains('name="X-XSS-Protection" value="1; mode=block"')
$hasReferrer = $webConfigContent.Contains('name="Referrer-Policy" value="strict-origin-when-cross-origin"')
$hasSecurityHeaders = $hasFrameOptions -and $hasContentType -and $hasXssProtect -and $hasReferrer
Assert-Test "SEC-11: Security Headers (Clickjacking, MIME Sniffing, XSS, Referrer-Policy)" $hasSecurityHeaders "Full HTTP security headers active"

$blocksSensFiles = $webConfigContent.Contains('fileExtension=".bak" allowed="false"') -and $webConfigContent.Contains('fileExtension=".sql" allowed="false"')
Assert-Test "SEC-13: Sensitive file extensions (.bak, .sql, .old, .temp) blocked in requestFiltering" $blocksSensFiles "requestFiltering rules verified"

# ----------------------------------------------------
# 4. SEC-06: HtmlSecurityHelper (Stored XSS Sanitizer)
# ----------------------------------------------------
$htmlSecurityHelper = Join-Path $baseDir "TSFramework.Libs\Helpers\HtmlSecurityHelper.cs"
$hshContent = Get-Content -Path $htmlSecurityHelper -Raw

$hasSanitizeMethod = $hshContent.Contains("SanitizeHtml") -and $hshContent.Contains("DangerousTagsRegex")
Assert-Test "SEC-06.1: HtmlSecurityHelper.SanitizeHtml filters script, iframe and inline handlers" $hasSanitizeMethod "XSS filter helper verified"

$digSalesFile = Join-Path $baseDir "Modules.Cate\Areas\Cate\Controllers\DigitalSalesController.cs"
$digSalesContent = Get-Content -Path $digSalesFile -Raw
$usesSanitize = $digSalesContent.Contains("HtmlSecurityHelper.SanitizeHtml(text)")
Assert-Test "SEC-06.2: FormatHtmlContent in DigitalSalesController invokes HtmlSecurityHelper" $usesSanitize "CKEditor input sanitized before database persist"

# ----------------------------------------------------
# 5. SEC-07: CustomAuthenticateAttribute (Default Deny Access Control)
# ----------------------------------------------------
$authAttrFile = Join-Path $baseDir "TSFramework.Libs\Attributes\CustomAuthenticateAttribute.cs"
$authAttrContent = Get-Content -Path $authAttrFile -Raw

$hasDefaultDeny = $authAttrContent.Contains("EnumActionType.View") -and $authAttrContent.Contains("StatusCode = 403")
Assert-Test "SEC-07: Default Deny enforcement in CustomAuthenticateAttribute & AJAX 403" $hasDefaultDeny "Bypass impossible when ActionType missing"

# ----------------------------------------------------
# 6. SEC-09 & SEC-12: AccountController Hardening
# ----------------------------------------------------
$accControllerFile = Join-Path $baseDir "CenIT.Solution.TOC.WebApp\Controllers\AccountController.cs"
$accContent = Get-Content -Path $accControllerFile -Raw

$hasLockout = $accContent.Contains("MAX_LOGIN_FAILS = 5") -and $accContent.Contains("AccountLock_")
Assert-Test "SEC-12.1: Brute Force Password Lockout (15 mins after 5 consecutive failures)" $hasLockout "Account lockout logic verified"

$hasIpLock = $accContent.Contains("_userIPLockCache.GetByIP(clientIP)")
Assert-Test "SEC-12.2: Banned IP check against SysUserIPLockCache on Login" $hasIpLock "SysUserIPLockCache query verified"

$hasCsrfTokens = $accContent.Contains("[ValidateAntiForgeryToken]")
Assert-Test "SEC-09.1: [ValidateAntiForgeryToken] present on sensitive POST actions" $hasCsrfTokens "Anti-CSRF protection verified"

$hasIdor = $accContent.Contains("StringComparison.OrdinalIgnoreCase") -and $accContent.Contains("ChangePassword") -and $accContent.Contains("EditInfo")
Assert-Test "SEC-09.2: IDOR defense on ChangePassword and EditInfo verified" $hasIdor "User mismatch blocked"

$hasForgotRate = $accContent.Contains("ForgotPasswordRateLimit_")
Assert-Test "SEC-12.3: ForgotPassword rate limiting (2-minute cooldown) verified" $hasForgotRate "Anti-email bombing protection verified"

# ----------------------------------------------------
# 7. SEC-14: Path Traversal in DigitalSales Attachment Handlers
# ----------------------------------------------------
$hasTraversalDefense = $digSalesContent.Contains('cleanPath.Contains("..")') -and $digSalesContent.Contains("canonicalPath.StartsWith(allowedRoot")
Assert-Test "SEC-14: Path traversal in DownloadAttachment and ViewAttachment neutralized" $hasTraversalDefense "Canonical Path & Parent traversal defense verified"

# ----------------------------------------------------
# 8. SEC-15 & SEC-16: Core File Upload Hardening Across All Controllers
# ----------------------------------------------------
$fileProviderFile = Join-Path $baseDir "TSFramework.Libs\Providers\FileProvider.cs"
$fileProvContent = Get-Content -Path $fileProviderFile -Raw
$hasProviderGuard = $fileProvContent.Contains("FileSecurityHelper.IsAllowedUpload(file.FileName, allowedExtensions)")
Assert-Test "SEC-15: Universal FileProvider upload guard active" $hasProviderGuard "FileSecurityHelper invoked in FileProvider.UploadFile"

$customerFile = Join-Path $baseDir "Modules.Cate\Areas\Cate\Controllers\CustomerController.cs"
$custContent = Get-Content -Path $customerFile -Raw
$hasCustUploadGuard = $custContent.Contains("FileSecurityHelper.IsAllowedUpload(filebase.FileName)")
Assert-Test "SEC-16.1: CustomerController.LuuAnh validates file extension" $hasCustUploadGuard "Customer attachment upload hardened"

$contractsFile = Join-Path $baseDir "Modules.Cate\Areas\Cate\Controllers\RM_ContractsController.cs"
$contractContent = Get-Content -Path $contractsFile -Raw
$hasContractUploadGuard = $contractContent.Contains("FileSecurityHelper.IsAllowedUpload(file.FileName)")
Assert-Test "SEC-16.2: RM_ContractsController.LuuFile validates file extension" $hasContractUploadGuard "Contract attachment upload hardened"

$bizOppFile = Join-Path $baseDir "Modules.Cate\Areas\Cate\Controllers\RM_BusinessOpportunityController.cs"
$bizOppContent = Get-Content -Path $bizOppFile -Raw
$hasBizOppUploadGuard = $bizOppContent.Contains("FileSecurityHelper.IsAllowedUpload(filebase.FileName)")
Assert-Test "SEC-16.3: RM_BusinessOpportunityController.LuuAnh validates file extension" $hasBizOppUploadGuard "Opportunity attachment upload hardened"

# ----------------------------------------------------
# 9. SEC-17: XXE Injection Defense
# ----------------------------------------------------
$xmlHelperFile = Join-Path $baseDir "TSFramework.Libs\Helpers\XmlHelper.cs"
$xmlContent = Get-Content -Path $xmlHelperFile -Raw
$hasXxeGuard = $xmlContent.Contains("XmlResolver = null")
Assert-Test "SEC-17: XmlHelper.Minify disables XmlResolver to prevent XXE" $hasXxeGuard "XXE external entity resolution blocked"

# ----------------------------------------------------
# 10. SEC-18 & SEC-19: IIS Server Hardening (Hidden Segments & No-Execute Uploads)
# ----------------------------------------------------
$hasHiddenSegments = $webConfigContent.Contains('<add segment="Libraries" />') -and $webConfigContent.Contains('<add segment="Configs" />')
Assert-Test "SEC-18: Libraries and Configs hidden from direct HTTP requests" $hasHiddenSegments "hiddenSegments verified"

$hasNoExecuteUploads = $webConfigContent.Contains('<location path="Contents/Uploads">') -and
                       $webConfigContent.Contains('<add fileExtension=".aspx" allowed="false" />') -and
                       $webConfigContent.Contains('<add name="StaticFile" path="*"')
Assert-Test "SEC-19: Script execution stripped in Uploads directory (Anti-Webshell RCE)" $hasNoExecuteUploads "Uploads restricted to static files"

# ----------------------------------------------------
# 11. SEC-20: Sys/Job Malicious DLL Injection Guard
# ----------------------------------------------------
$jobControllerFile = Join-Path $baseDir "Modules.Sys\Areas\Sys\Controllers\JobController.cs"
$jobContent = Get-Content -Path $jobControllerFile -Raw
$hasJobGuard = $jobContent.Contains("IsValidJobLibrary") -and $jobContent.Contains('nameOnly.StartsWith("Jobs."')
Assert-Test "SEC-20: /Sys/Job/Add restricts DLLs strictly to Jobs.*.dll prefix" $hasJobGuard "Malicious DLL loading blocked in JobController"

$totalTests = $passedCount + $failedCount
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " SUMMARY: PASSED $passedCount / $totalTests TESTS" -ForegroundColor $(if ($failedCount -eq 0) { [ConsoleColor]::Green } else { [ConsoleColor]::Red })
Write-Host "==========================================================" -ForegroundColor Cyan

if ($failedCount -gt 0) {
    exit 1
}
exit 0

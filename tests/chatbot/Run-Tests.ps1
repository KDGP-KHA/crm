$ErrorActionPreference = 'Stop'
$crmRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$crmBin = Join-Path $crmRoot 'Modules.Cate\bin'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$csc = & $vswhere -latest -products '*' -find 'MSBuild\Current\Bin\Roslyn\csc.exe' | Select-Object -First 1
if (-not $csc -or -not (Test-Path $csc)) {
    $csc = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe"
}
if (-not $csc -or -not (Test-Path $csc)) { throw 'Visual Studio Roslyn compiler was not found.' }
$testDir = Join-Path ([IO.Path]::GetTempPath()) ('crm-chatbot-tests-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($testDir) | Out-Null
$testExe = Join-Path $testDir 'ChatbotTests.exe'
$refs = @('Modules.Cate.dll', 'Core.Cate.dll', 'TSFramework.Libs.dll', 'Newtonsoft.Json.dll', 'System.Web.Http.dll') |
    ForEach-Object { '/reference:' + (Join-Path $crmBin $_) }
& $csc /nologo /target:exe "/out:$testExe" /reference:System.Web.dll /reference:System.Net.Http.dll /reference:System.Configuration.dll $refs (Join-Path $PSScriptRoot 'ChatbotTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& $testExe $crmBin
if ($LASTEXITCODE -ne 0) { throw 'Chatbot checks failed.' }

$catalog = Get-Content (Join-Path $crmRoot 'docs\chatbot\external-tools.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$expected = @('get_digitalsales_summary', 'get_digitalsales_detail', 'get_project_summary', 'get_project_detail', 'get_opportunity_summary', 'get_opportunity_detail')
if ($catalog.tools.Count -ne 6 -or (Compare-Object $expected @($catalog.tools.name))) { throw 'Catalog tools do not match the contract.' }
Write-Output 'PASS: catalog contains the six CRM tools.'

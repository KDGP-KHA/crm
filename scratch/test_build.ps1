$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
& $msbuild "d:\VNPT\CRM-GIT\crm\Modules.Cate\Modules.Cate.csproj" /p:Configuration=Release /p:SolutionDir="d:\VNPT\CRM-GIT\crm\" /v:m
Write-Host "Modules.Cate exit code: $LASTEXITCODE"


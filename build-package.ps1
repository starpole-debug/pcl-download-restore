param([string]$Dotnet='dotnet')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
& $Dotnet build 'helper\PclRestore.csproj' -c Release --verbosity minimal
if($LASTEXITCODE -ne 0){throw 'Helper build failed.'}
Get-ChildItem -LiteralPath 'helper\bin\Release\net48' -File | Where-Object {$_.Extension -in @('.exe','.dll','.config')} | Copy-Item -Destination 'package' -Force
Write-Host 'Helper rebuilt in package/. The reference method payload and its source are included.'

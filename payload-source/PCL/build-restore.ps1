$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
$localDotnet = Join-Path $PSScriptRoot '.tools\dotnet\dotnet.exe'
$dotnetCmd = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { (Get-Command dotnet -ErrorAction Stop).Source }
$sdkVersion = (& $dotnetCmd --version | Select-Object -Last 1).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Install the .NET 9 SDK or newer first.' }
$sdkLine = & $dotnetCmd --list-sdks | Where-Object { $_.StartsWith($sdkVersion + ' ') } | Select-Object -First 1
if ($sdkLine -notmatch '\[(.+)\]') { throw 'Cannot locate .NET SDK directory.' }
$sdkFolder = Join-Path $Matches[1] $sdkVersion
$wpfTargets = Join-Path $sdkFolder 'Sdks\Microsoft.NET.Sdk.WindowsDesktop\targets\Microsoft.WinFX.targets'
if (!(Test-Path -LiteralPath $wpfTargets)) { throw 'Windows Desktop SDK targets are missing.' }
& $dotnetCmd restore 'PCLCS\PCLCS.csproj' --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed.' }
$assets = Get-Content -LiteralPath 'PCLCS\obj\project.assets.json' -Raw | ConvertFrom-Json
$refPackage = $assets.libraries.PSObject.Properties.Name | Where-Object { $_ -like 'Microsoft.NETFramework.ReferenceAssemblies.net48/*' } | Select-Object -First 1
if (!$refPackage) { throw 'net48 reference assemblies were not restored.' }
$refRoot = $null
foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
    $candidate = Join-Path $folder ($refPackage.ToLowerInvariant() + '/build')
    if (Test-Path -LiteralPath $candidate) { $refRoot = $candidate; break }
}
if (!$refRoot) { throw 'Cannot locate net48 reference assemblies.' }
& $dotnetCmd msbuild 'Plain Craft Launcher 2\Plain Craft Launcher 2.vbproj' '-p:Configuration=Release' "-p:TargetFrameworkRootPath=$refRoot" "-p:CustomAfterMicrosoftCommonTargets=$PSScriptRoot\portable-wpf.targets" "-p:PclWpfTargetsPath=$wpfTargets" '-p:IncludePackageReferencesDuringMarkupCompilation=false' '-verbosity:minimal' '-nologo'
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Write-Host 'Build succeeded: Plain Craft Launcher 2\bin\Plain Craft Launcher 2.exe'

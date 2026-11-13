param(
    [Parameter(Mandatory=$true)][string]$App,
    [Parameter(Mandatory=$true)][string]$Output
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$appPath = (Resolve-Path $App).Path
$icon = Join-Path $root 'assets/photoloja/photoloja.ico'
$logo = Join-Path $root 'assets/photoloja/logo-256.png'
$setup = [IO.Path]::GetFullPath($Output)
$msi = Join-Path (Split-Path $setup) 'PhotoLoja.msi'
wix build "$PSScriptRoot/Product.wxs" -arch x64 -d "App=$appPath" -d "Icon=$icon" -d "SourceRoot=$root" -o $msi
if ($LASTEXITCODE -ne 0) { throw "WiX failed to build the package." }
wix msi validate $msi -ice ICE50
if ($LASTEXITCODE -ne 0) { throw "WiX failed to validate the shortcut." }
$csc = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
& $csc /nologo /target:winexe /platform:x64 /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/win32icon:$icon" "/resource:$msi,PhotoLoja.msi" "/resource:$logo,PhotoLoja.logo" "/out:$setup" (Join-Path $PSScriptRoot "Setup.cs")
if ($LASTEXITCODE -ne 0) { throw "Could not compile the setup window." }
$embedded = [Reflection.Assembly]::LoadFile($setup).GetManifestResourceStream('PhotoLoja.msi')
try { $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($embedded)) } finally { $embedded.Dispose() }
if ($hash -ne (Get-FileHash $msi).Hash) { throw "Embedded MSI differs from the validated package." }
Get-FileHash $setup

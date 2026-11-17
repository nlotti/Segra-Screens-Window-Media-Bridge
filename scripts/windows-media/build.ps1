param([string]$OutputPath = (Join-Path $PSScriptRoot 'SigmaMedia.exe'))
$ErrorActionPreference = 'Stop'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$metadataPath = Join-Path $env:WINDIR 'System32/WinMetadata'
$gacPath = Join-Path $env:WINDIR 'Microsoft.NET/assembly/GAC_MSIL'
function Find-FrameworkAssembly([string]$Name) {
    $assemblyPath = Get-ChildItem -LiteralPath (Join-Path $gacPath $Name) -Recurse -Filter ($Name + '.dll') |
        Select-Object -First 1 -ExpandProperty FullName
    if (-not $assemblyPath) { throw "Missing .NET Framework assembly: $Name" }
    return $assemblyPath
}
$references = @('System.Windows.Forms.dll', 'System.Drawing.dll', 'System.Web.Extensions.dll',
    (Find-FrameworkAssembly 'System.Runtime'),
    (Find-FrameworkAssembly 'System.Runtime.InteropServices.WindowsRuntime'),
    (Join-Path $metadataPath 'Windows.Media.winmd'),
    (Join-Path $metadataPath 'Windows.Foundation.winmd'),
    (Join-Path $metadataPath 'Windows.Storage.winmd'))
$sources = @((Join-Path $PSScriptRoot 'MediaBridge.cs'), (Join-Path $PSScriptRoot 'ArtworkReader.cs')) +
    @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'bridge') -Filter '*.cs' | Sort-Object Name | Select-Object -ExpandProperty FullName)
$compilerArgs = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+', ('/out:' + $OutputPath)) +
    @($references | ForEach-Object { '/reference:' + $_ }) + $sources
& $compilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw "Media companion compilation failed ($LASTEXITCODE)." }
Get-FileHash -LiteralPath $OutputPath -Algorithm SHA256

# Regenerates lib/: reference assemblies of the game DLLs the mod compiles
# against, so CI can build without the game installed.
#
# Reference assemblies keep only the public API — types and member signatures,
# no method bodies — the same kind of file the ValheimGameLibs NuGet package
# ships. Run this after a Valheim update, then commit lib/.
#
# Needs JetBrains Refasmer:  dotnet tool install -g JetBrains.Refasmer.CliTool
param([string]$ValheimDir)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (!$ValheimDir) { $ValheimDir = Join-Path $projectRoot '..\..' }
$managed = Join-Path $ValheimDir 'valheim_Data\Managed'
if (!(Test-Path -LiteralPath $managed)) { throw "Game assemblies not found in $managed. Pass -ValheimDir." }
if (!(Get-Command refasmer -ErrorAction SilentlyContinue)) {
    throw 'refasmer is not installed: dotnet tool install -g JetBrains.Refasmer.CliTool'
}

$names = 'assembly_valheim.dll', 'assembly_utils.dll', 'assembly_guiutils.dll',
         'UnityEngine.UI.dll', 'Unity.TextMeshPro.dll'
$lib = Join-Path $projectRoot 'lib'
New-Item -ItemType Directory -Path $lib -Force | Out-Null

# Refasmer targets .NET 6; newer runtimes are fine.
$env:DOTNET_ROLL_FORWARD = 'Major'
$sources = $names | ForEach-Object { Join-Path $managed $_ }
& refasmer -c --omit-non-api-members=true -O $lib @sources
if ($LASTEXITCODE -ne 0) { throw 'refasmer failed.' }
foreach ($name in $names) {
    if (!(Test-Path -LiteralPath (Join-Path $lib $name))) { throw "refasmer did not produce $name." }
}

# Record which game build the references came from.
$version = 'unknown'
$log = Join-Path $ValheimDir 'BepInEx\LogOutput.log'
if (Test-Path -LiteralPath $log) {
    $m = Select-String -LiteralPath $log -Pattern 'Valheim version: ([^ ]+)' | Select-Object -Last 1
    if ($m) { $version = $m.Matches[0].Groups[1].Value }
}
Set-Content -LiteralPath (Join-Path $lib 'GAME_VERSION.txt') -Value $version -Encoding ASCII
Write-Host "lib/ regenerated from Valheim $version."

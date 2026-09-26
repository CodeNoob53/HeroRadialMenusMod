# -ValheimDir    build against a game install elsewhere (local builds).
# -ReferencePath build against a flat folder of reference DLLs instead (CI):
#                lib/ plus the Unity/BepInEx/Harmony NuGet packages. Profile
#                tests need the real Unity runtime and are skipped then.
param([string]$ValheimDir, [string]$ReferencePath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    $pathArgs = @()
    if ($ValheimDir) { $pathArgs += "-p:ValheimDir=$ValheimDir" }
    if ($ReferencePath) { $pathArgs += "-p:ReferencePath=$ReferencePath" }
    & dotnet build HeroRadialMenusMod.csproj -c Release @pathArgs
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    if ($ReferencePath) {
        Write-Host 'Profile tests skipped: they need the real Unity runtime, not reference assemblies.'
    } else {
        & dotnet run --project tools/ProfileTests/ProfileTests.csproj -c Release @pathArgs
        if ($LASTEXITCODE -ne 0) { throw 'Profile tests failed.' }
    }
    & (Join-Path $PSScriptRoot 'Check-Release.ps1')
    [xml]$project = Get-Content -LiteralPath 'HeroRadialMenusMod.csproj' -Raw
    $version = $project.SelectSingleNode('//Version').InnerText
    $candidateId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6)
    $candidate = Join-Path $projectRoot "artifacts/$candidateId"
    $stage = Join-Path $candidate 'package'
    $pluginDir = Join-Path $stage 'BepInEx/plugins'
    New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
    $docNames = 'configuration.md', 'radial-surfaces.md', 'release-readiness.md'
    foreach ($lang in 'en', 'uk') {
        New-Item -ItemType Directory -Path (Join-Path $stage "docs/$lang") -Force | Out-Null
    }
    Copy-Item -LiteralPath 'bin/Release/net472/HeroRadialMenusMod.dll' -Destination $pluginDir
    foreach ($name in 'README.md', 'README.uk.md', 'LICENSE', 'CHANGELOG.md', 'CONTRIBUTING.md') {
        Copy-Item -LiteralPath $name -Destination $stage
    }
    foreach ($lang in 'en', 'uk') {
        foreach ($name in $docNames) {
            Copy-Item -LiteralPath "docs/$lang/$name" -Destination (Join-Path $stage "docs/$lang")
        }
    }
    $zip = Join-Path $candidate "HeroRadialMenus-$version-manual.zip"
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $entries = @($archive.Entries | Where-Object { $_.Name } | ForEach-Object { $_.FullName.Replace('\', '/') })
        $expected = @(
            'BepInEx/plugins/HeroRadialMenusMod.dll',
            'README.md', 'README.uk.md', 'LICENSE', 'CHANGELOG.md', 'CONTRIBUTING.md'
        )
        foreach ($lang in 'en', 'uk') {
            foreach ($name in $docNames) { $expected += "docs/$lang/$name" }
        }
        if (@(Compare-Object $expected $entries).Count -ne 0) { throw 'Unexpected archive contents.' }
    } finally { $archive.Dispose() }
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([IO.Path]::GetFileName($zip))" | Set-Content -LiteralPath "$zip.sha256" -Encoding ASCII
    Write-Host "Candidate: $zip"
    # GitHub Actions: hand the ZIP path to the following steps.
    if ($env:GITHUB_OUTPUT) { Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "zip=$zip" -Encoding utf8 }
    Write-Host "SHA256: $hash"
    Write-Host 'Archive contents verified. In-game release checks remain manual.'
} finally { Pop-Location }

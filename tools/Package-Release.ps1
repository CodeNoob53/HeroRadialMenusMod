param([string]$ValheimDir)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location $projectRoot
try {
    $pathArgs = @()
    if ($ValheimDir) { $pathArgs += "-p:ValheimDir=$ValheimDir" }
    & dotnet build HeroRadialMenusMod.csproj -c Release @pathArgs
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
    & dotnet run --project tools/ProfileTests/ProfileTests.csproj -c Release @pathArgs
    if ($LASTEXITCODE -ne 0) { throw 'Profile tests failed.' }
    & (Join-Path $PSScriptRoot 'Check-Release.ps1')
    [xml]$project = Get-Content -LiteralPath 'HeroRadialMenusMod.csproj' -Raw
    $version = $project.SelectSingleNode('//Version').InnerText
    $candidateId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 6)
    $candidate = Join-Path $projectRoot "artifacts/$candidateId"
    $stage = Join-Path $candidate 'package'
    $pluginDir = Join-Path $stage 'BepInEx/plugins/HeroRadialMenus'
    New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $stage 'docs') -Force | Out-Null
    Copy-Item -LiteralPath 'bin/Release/net472/HeroRadialMenusMod.dll' -Destination $pluginDir
    foreach ($name in 'README.md', 'LICENSE', 'CHANGELOG.md', 'CONTRIBUTING.md') {
        Copy-Item -LiteralPath $name -Destination $stage
    }
    foreach ($name in 'configuration.md', 'radial-surfaces.md', 'release-readiness.md') {
        Copy-Item -LiteralPath (Join-Path 'docs' $name) -Destination (Join-Path $stage 'docs')
    }
    $zip = Join-Path $candidate "HeroRadialMenus-$version-manual.zip"
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $entries = @($archive.Entries | Where-Object { $_.Name } | ForEach-Object { $_.FullName.Replace('\', '/') })
        $expected = @(
            'BepInEx/plugins/HeroRadialMenus/HeroRadialMenusMod.dll',
            'README.md', 'LICENSE', 'CHANGELOG.md', 'CONTRIBUTING.md',
            'docs/configuration.md', 'docs/radial-surfaces.md', 'docs/release-readiness.md'
        )
        if (@(Compare-Object $expected $entries).Count -ne 0) { throw 'Unexpected archive contents.' }
    } finally { $archive.Dispose() }
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $([IO.Path]::GetFileName($zip))" | Set-Content -LiteralPath "$zip.sha256" -Encoding ASCII
    Write-Host "Candidate: $zip"
    Write-Host "SHA256: $hash"
    Write-Host 'Archive contents verified. In-game release checks remain manual.'
} finally { Pop-Location }

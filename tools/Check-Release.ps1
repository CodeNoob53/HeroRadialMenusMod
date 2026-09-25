$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$plugin = Get-Content -LiteralPath (Join-Path $projectRoot 'Plugin.cs') -Raw -Encoding UTF8
$guide = Get-Content -LiteralPath (Join-Path $projectRoot 'docs/configuration.md') -Raw -Encoding UTF8
[xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot 'HeroRadialMenusMod.csproj') -Raw
$versionMatch = [regex]::Match($plugin, 'PluginVersion\s*=\s*"([^"]+)"')
if (!$versionMatch.Success) { throw 'PluginVersion was not found.' }
$version = $versionMatch.Groups[1].Value
foreach ($property in 'Version', 'AssemblyVersion', 'FileVersion') {
    $node = $project.SelectSingleNode("//$property")
    if (!$node -or $node.InnerText -ne $version) { throw "$property does not match PluginVersion $version." }
}
$bindings = [regex]::Matches($plugin, 'Config\.Bind\("([^"]+)",\s*"([^"]+)",\s*([^,\r\n]+),')
if ($bindings.Count -eq 0) { throw 'No configuration bindings found.' }
$rows = @{}
$section = ''
foreach ($line in ($guide -split '\r?\n')) {
    if ($line -match '^## \[([^\]]+)\]') { $section = $Matches[1] }
    if ($line -match '^\| \x60([^\x60]+)\x60 \| \x60([^\x60]+)\x60 \|') {
        $id = "$section/$($Matches[1])"
        if ($rows.ContainsKey($id)) { throw "Duplicate configuration row: $id" }
        $rows[$id] = $Matches[2]
    }
}
foreach ($binding in $bindings) {
    $id = $binding.Groups[1].Value + '/' + $binding.Groups[2].Value
    $expected = $binding.Groups[3].Value.Trim() -replace '^KeyCode\.', '' -replace 'f$', ''
    if (!$rows.ContainsKey($id)) { throw "Missing configuration documentation: $id" }
    if ($rows[$id] -ne $expected) { throw "Wrong documented default for $id : expected $expected, found $($rows[$id])" }
}
if ($rows.Count -ne $bindings.Count) { throw 'Documentation contains stale configuration entries.' }
$readme = Get-Content -LiteralPath (Join-Path $projectRoot 'README.md') -Raw -Encoding UTF8
if (!$readme.Contains("all $($bindings.Count) settings")) { throw 'README setting count is stale.' }
$markdown = @(
    (Join-Path $projectRoot 'README.md'),
    (Join-Path $projectRoot 'CONTRIBUTING.md'),
    (Join-Path $projectRoot 'CHANGELOG.md')
) + @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'docs') -Filter '*.md' | ForEach-Object { $_.FullName })
foreach ($path in $markdown) {
    $body = Get-Content -LiteralPath $path -Raw -Encoding UTF8
    foreach ($link in [regex]::Matches($body, '\[[^\]]+\]\(([^)]+)\)')) {
        $target = $link.Groups[1].Value
        if ($target -match '^(https?://|#)') { continue }
        $target = ($target -split '#')[0]
        if (!(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $path) $target))) {
            throw "Broken link in $path : $target"
        }
    }
}
if (!(Test-Path -LiteralPath (Join-Path $projectRoot 'LICENSE'))) { throw 'LICENSE is missing.' }
Write-Host "PASS: $($bindings.Count) CFG entries/defaults, version $version, documentation links and license."

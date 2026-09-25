$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$plugin = Get-Content -LiteralPath (Join-Path $projectRoot 'Plugin.cs') -Raw -Encoding UTF8
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
# Both language versions of the settings reference must list every key with
# its real default, and nothing else.
foreach ($lang in 'en', 'uk') {
    $guide = Get-Content -LiteralPath (Join-Path $projectRoot "docs/$lang/configuration.md") -Raw -Encoding UTF8
    $rows = @{}
    $section = ''
    foreach ($line in ($guide -split '\r?\n')) {
        if ($line -match '^## \[([^\]]+)\]') { $section = $Matches[1] }
        if ($line -match '^\| `([^`]+)` \| `([^`]+)` \|') {
            $id = "$section/$($Matches[1])"
            if ($rows.ContainsKey($id)) { throw "Duplicate configuration row in $lang : $id" }
            $rows[$id] = $Matches[2]
        }
    }
    foreach ($binding in $bindings) {
        $id = $binding.Groups[1].Value + '/' + $binding.Groups[2].Value
        $expected = $binding.Groups[3].Value.Trim() -replace '^KeyCode\.', '' -replace 'f$', ''
        if (!$rows.ContainsKey($id)) { throw "Missing configuration documentation ($lang): $id" }
        if ($rows[$id] -ne $expected) { throw "Wrong documented default ($lang) for $id : expected $expected, found $($rows[$id])" }
    }
    if ($rows.Count -ne $bindings.Count) { throw "docs/$lang/configuration.md contains stale configuration entries." }
}
# Both READMEs: setting count and the version badge.
$count = $bindings.Count
$readmes = @{
    'README.md'    = "all $count settings"
    'README.uk.md' = "всі $count параметрів"
}
foreach ($name in $readmes.Keys) {
    $readme = Get-Content -LiteralPath (Join-Path $projectRoot $name) -Raw -Encoding UTF8
    if (!$readme.Contains($readmes[$name])) { throw "$name setting count is stale." }
    if (!$readme.Contains("badge/version-$version-")) { throw "$name version badge is stale." }
}
$markdown = @(
    (Join-Path $projectRoot 'README.md'),
    (Join-Path $projectRoot 'README.uk.md'),
    (Join-Path $projectRoot 'CONTRIBUTING.md'),
    (Join-Path $projectRoot 'CHANGELOG.md')
) + @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'docs') -Filter '*.md' -Recurse | ForEach-Object { $_.FullName })
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

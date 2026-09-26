# Developer guide

## Prerequisites

.NET Framework 4.7.2 target (net472), C# 9, BepInEx 5 and local Valheim
assemblies. Developed on Windows with .NET SDK 8.0. Install the .NET
Framework 4.7.2 targeting pack if the SDK reports missing reference assemblies.
Do not redistribute game/BepInEx DLLs with the plugin.

Default layout: <Valheim>/ValheimMods/HeroRadialMenusMod. Else copy
Environment.props.example to Environment.props and set ValheimDir.
The main project also accepts a flat DLL directory through ReferencePath;
the profile test project uses ValheimDir, not flat-reference mode.

## Build and test

Run from the project directory:

```powershell
dotnet build -c Release
dotnet run --project tools/ProfileTests/ProfileTests.csproj -c Release
powershell -NoProfile -File tools/Check-Release.ps1
```

Both projects import Environment.props. Alternatively pass
`-p:ValheimDir="D:\Games\Valheim"` to each dotnet command.

A normal `dotnet build -c Release` automatically installs to the local game,
backing up its previous root-level DLL as .dll.bak. Keep automatic deployment
enabled for local builds, including release packaging, so the game is ready
for immediate testing. Manual developer install after building:

```powershell
dotnet msbuild -t:InstallToBepInEx -p:Configuration=Release
```

Close the game first. Automatic deployment reports a locked destination as a
warning: build success is not proof of installation. Local builds and the
release ZIP both place the DLL directly in `BepInEx/plugins`, so they replace
each other rather than leaving duplicates.

ProfileTests runs production JSON parsing, polling/resolution and dynamic
visibility/tint merge. It cannot construct Unity objects. Existing source
wiring assertions supplement behavioral tests; they do not verify HUD
rendering, Harmony hooks, input or actual item actions in-game.

## Code map

| File | Responsibility |
|---|---|
| Plugin.cs | BepInEx lifecycle, CFG bindings, live CFG reload, input helpers and hotkey guards |
| Patches.cs | Harmony HUD updates, input suppression, eat-animation suppression, death/scene cleanup |
| RadialMenus.cs | Sprite lookup, procedural mesh/slots, click feedback, shared wheel and both state machines |
| ItemInfoPanel.cs | Item stats window, cloned from the game's radial InventoryInfo |
| GameText.cs | Wheel labels (own EN/UK text, game localization keys for other languages); detection of keys bound in the game's controls (arrow HoldDelay) |
| LayerProfile.cs | Per-surface file cache and property resolution/application |
| NodeBaseline.cs | Capture/restore of static UI properties |
| DynamicOverride.cs | Combining live visibility/colour with profiles |
| RadialSelection.cs | Wheel-local mouse hit testing; rejects centre and empty sectors |
| MiniJson.cs | Embedded lightweight JSON reader without external dependencies |

Unity lifecycle methods and Harmony Prefix/Postfix hooks are engine/reflection
entry points, not dead code. Profile members exercised by tests are also live.
Public implementation classes are not a versioned third-party API.

Item lists are rebuilt on opening, deduplicated by shared name and capped at
16. The visible game mouse position is converted into wheel-local coordinates for
selection; releasing the opening key applies/cancels. With ClickToUse, ClickKey
uses the hovered consumable immediately and the release then only closes; the
list is rebuilt when a stack runs out. Items go through the game's own
Humanoid.UseItem, so its restrictions apply; with ConsumeAnimation off, only
the "eat" trigger and the in-hand visual are suppressed for that one call.

The CFG file is watched with a FileSystemWatcher; the reload itself runs on
the main thread from the HUD update, with SaveOnConfigSet disabled so it cannot
trigger itself. Wheel offsets are read only when the wheel is created.
Arrow selection tracks equipped/hidden bows and remembers a bow for 1.5
seconds to tolerate vanilla R. PickDelay is visual confirmation after returning
control to the player.

Profile order: base layout, CFG, JSON. Static nodes restore baseline before
new overrides. Dynamic nodes merge values while rendering; restoring moving
transforms would break aiming. See [profile format](docs/en/radial-surfaces.md).

## Release workflow

Update docs/en/configuration.md, docs/uk/configuration.md and the setting
count in README.md / README.uk.md whenever Config.Bind
entries change. Keep PluginVersion, Version, AssemblyVersion and FileVersion
consistent. Do not install a sample CFG over user-owned settings.

```powershell
powershell -NoProfile -File tools/Package-Release.ps1
```

Packaging builds and installs the DLL locally, runs tests and contract checks, then
creates a new candidate directory in artifacts/ with only the DLL and selected
documentation. Previous candidates are preserved. The ZIP targets manual/Nexus
distribution, not Thunderstore. Complete the [release checklist](docs/en/release-readiness.md).

## CI and GitHub releases

[.github/workflows/build.yml](.github/workflows/build.yml) builds the mod on
GitHub without the game installed. Game assemblies come from `lib/`: reference
assemblies made from the local game with JetBrains Refasmer. They keep only the
public API (types and member signatures, no method bodies), like the
ValheimGameLibs NuGet package, which is too old for this mod (no `ZCursor`,
no `ZInput.pointerPosition`). Unity modules, BepInEx and Harmony come from
NuGet. `lib/GAME_VERSION.txt` records the game build they were made from.

After a Valheim update, regenerate and commit `lib/`:

```powershell
dotnet tool install -g JetBrains.Refasmer.CliTool   # once
powershell -NoProfile -File tools/Update-GameRefs.ps1
```

Every push and pull request builds with
`Package-Release.ps1 -ReferencePath refs`, runs the documentation/version
checks and uploads the ZIP as an artifact. Profile tests need the real Unity
runtime, so they run only in local packaging.

To publish a release:

1. Bump the version (PluginVersion, Version, AssemblyVersion, FileVersion and
   the README badges); `Check-Release.ps1` checks that they agree.
2. In CHANGELOG.md replace `— unpublished` in that version's heading with the
   release date. The workflow refuses to release a section still marked
   unpublished.
3. Commit, then tag and push:

   ```powershell
   git tag v1.3.1
   git push origin v1.3.1
   ```

The tag must equal `v` + PluginVersion. The workflow then creates a GitHub
Release with the ZIP, its SHA-256 and that CHANGELOG section as the notes.

MIT covers project code/documentation. Never package game DLLs, extracted
game assets, private configuration, logs or credentials. `lib/` holds only
reference assemblies, used for compiling and never packaged.

# Release readiness — 2026-09-23

**Status: release candidate; in-game release testing remains pending.**
Version 1.0.0 has not been published by this task.

## Audit findings and corrections

- Compared local vanilla RadialBase, RadialConfigHelper, GameCamera and ZCursor:
  the game shows its cursor through ZCursor and aims from ZInput.pointerPosition.
  Added a scoped mouse-capture prefix for our open wheels and wheel-local
  absolute mouse aiming (including wheel offset/scale).
- Removed the extra one-second consumable lockout and cooldown dimming.
- Empty sectors now reject hover/selection; selection regression tests added.

- Removed incorrect HeroEquipmentMod requirement from README.
- Added all 30 CFG keys/defaults, examples and automated documentation checks.
- Consolidated debug logging under EnableDebugLogs; removed the separate LogInput setting.
- Connected previously inert SlotSize, HoverFadeSeconds, EmptySlotAlpha,
  HoverColorR/G/B, HoverAlpha, TintBackground and OrnamentOffset to rendering.
  Existing CFGs can now change appearance.
- Removed unused Plugin.Instance, wheel IsActive/IsValid/Segments,
  LayerProfile.Invalidate/TryGetFloat, NodeBaseline.Has and logger locals.
  Removed hidden legacy hover-colour substitution.
- Fixed ProfileTests compilation: LayerProfile now takes optional debug logging
  instead of depending directly on Plugin.
- Fixed cursor tip size reset after deleting its profile override.
- Corrected profile prefixes, geometry, reload description and limitations.
- Added developer guide, MIT license, changelog and allowlisted ZIP packaging.

Dead-code review used symbol/reference inspection across source and tests.
Unity lifecycle/Harmony reflection entry points were retained. This does not
prove that every runtime branch is reachable.

## Automated verification

Run tools/Package-Release.ps1 to reproduce the build, profile tests,
30-key/default documentation check, version/link/license validation and ZIP
checks. Candidates and SHA-256 sidecars are in artifacts/. The build also updates
the local BepInEx plugin DLL for immediate in-game testing.

Local references: .NET SDK 8.0.425, BepInEx 5.4.23.3 (pack 5.4.2333).
The existing game log reports Valheim 1.0.15. This identifies local build
references, not a tested compatibility range.

## In-game release gate — pending

- [ ] Clean BepInEx profile with only this DLL: no load/Harmony exceptions,
      new CFG generated, no HeroEquipmentMod/editor dependency.
- [ ] Empty, 1–2 and many items; centre cancellation, consumption/cooldowns,
      selected arrows and incompatible ammunition.
- [ ] R short tap/hold, secondary key, remapping/conflicts, bow restoration.
- [ ] Visible game cursor in both wheels; close restores capture, inventory/menu
      keeps its cursor; offsets/scales and centre cancellation match pointer.
- [ ] Immediately reopen after a potion: no blanket dimming or extra lockout;
      normal mead effects still enforced. Empty sectors never highlight.
- [ ] Inventory/chat/map/menu guards, death, teleport, world exit/rejoin;
      input/time restored after forced closure.
- [ ] All newly connected appearance options, both offsets, BlockMovement,
      1080p/1440p and UI scale.
- [ ] Profile edits/deletion while open/closed, cursor size reset, independent surfaces.
- [ ] Multiplayer client with SlowMotion disabled; single-player SlowMotion separately.
- [ ] Manual install/update/uninstall; mod-manager installation if advertised.

Known limits: mouse aiming only, 16 item names maximum, some Ukrainian UI text.
SlowMotion is client-local and restores timeScale to 1; compatibility with
other time mods is unverified. Profile-hidden slots remain selectable.
Do not advertise full controller/localization support or universal compatibility.

## Publication materials

- [x] User README, CFG reference, developer guide, changelog and MIT.
- [x] Reproducible manual-install candidate packaging.
- [ ] Screenshots/video from this candidate.
- [ ] Public source/support URL and author/team presentation.
- [ ] Record completed game tests and tested versions.
- [ ] For Thunderstore: separate manifest and 256×256 PNG icon, with
      manifest.json, icon.png and README.md at ZIP root.

The local Git repository had no commits and all sources were untracked at
audit start. Establish a source commit/tag before public release. No remote
repository was invented and nothing was published.

Platform references checked:
[Nexus author practices](https://help.nexusmods.com/article/136-best-practices-for-mod-authors),
[Nexus submissions](https://help.nexusmods.com/article/28-file-submission-guidelines),
[Thunderstore format](https://wiki.thunderstore.io/mods/creating-a-package),
[Thunderstore layout](https://wiki.thunderstore.io/mods/packaging-your-mods).

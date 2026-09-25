# Release checklist

**English** · [Українська](../uk/release-readiness.md)

Work through this list before publishing a version. It describes what to
verify, not what was verified for any past build — record results in the
release notes.

## Automated

Run `tools/Package-Release.ps1`. It builds (and installs locally for
testing), runs the profile tests, checks that every CFG key and default is
documented in both languages, that both READMEs show the right setting count
and version badge, validates version numbers, links and the license, and
creates a ZIP with a SHA-256 sidecar in `artifacts/`.

Keep `README.md` / `README.uk.md` and `docs/en` / `docs/uk` in step: every
change to features or settings goes into both languages.

Record the .NET SDK, BepInEx and Valheim versions the build used.

## In-game

- [ ] Clean BepInEx profile with only this DLL: no load/Harmony errors, CFG
      generated, works without any other Hero mod or the editor.
- [ ] Consumable wheel with 0, 1–2 and many items; centre cancel; release to
      use; click-to-use of several items; gold/red click feedback; stack
      running out mid-opening.
- [ ] ConsumeAnimation true/false; potion cooldown and full-stomach refusals.
- [ ] Item stats window on both wheels: position, size, text, weight updates.
- [ ] Arrow wheel: R short tap/hold, secondary key, remapping, incompatible
      ammunition, hidden bow restored.
- [ ] Game cursor visible in both wheels and restored after closing; aiming
      matches the pointer with offsets and scale.
- [ ] Inventory/chat/map/menu guards, death, teleport, world exit/rejoin;
      controls restored after a forced close.
- [ ] Live CFG edits apply without restart; offsets after a world reload.
- [ ] Appearance options, BlockMovement, 1080p/1440p and UI scale.
- [ ] Editor profiles: edit/delete while open and closed, both wheels
      independent.
- [ ] Multiplayer client with SlowMotion off; single-player SlowMotion.
- [ ] Manual install, update over a previous version, uninstall; mod manager
      if advertised.

Known limits to state on the release page: mouse aiming only, up to 16 items
per wheel, part of the in-game text is Ukrainian. SlowMotion is client-local
and not tested with other time mods. Profile-hidden slots can still be
selected.

## Publication materials

- [ ] Preview image at `docs/images/preview.png` (shown at the top of both
      READMEs), screenshots or a short video of the release build.
- [ ] Public source/support URL and author presentation.
- [ ] Source commit and tag for the released version.
- [ ] For Thunderstore: manifest.json, a 256×256 icon.png and README.md at the
      ZIP root (the current ZIP targets manual/Nexus installation).

Platform references:
[Nexus author practices](https://help.nexusmods.com/article/136-best-practices-for-mod-authors),
[Nexus submissions](https://help.nexusmods.com/article/28-file-submission-guidelines),
[Thunderstore format](https://wiki.thunderstore.io/mods/creating-a-package),
[Thunderstore layout](https://wiki.thunderstore.io/mods/packaging-your-mods).

# Hero Radial Menus

Two mouse-driven radial menus for Valheim: consumables and bow ammunition.
Version **1.1.0**, plugin ID `com.heromod.valheim.radialmenus`.

## Install

1. Install BepInEx 5 for Valheim. This candidate builds against BepInEx
   5.4.23.3 / BepInExPack Valheim 5.4.2333 and assemblies from an installation
   whose log reports Valheim 1.0.15. In-game testing of this candidate is pending.
2. Close Valheim. Extract the release ZIP into the Valheim directory to obtain
   `BepInEx/plugins/HeroRadialMenus/HeroRadialMenusMod.dll`.
3. Remove any old copy of `HeroRadialMenusMod.dll` from other plugin folders
   first, including the root of `BepInEx/plugins`. Keep only one DLL.
4. Start the game once to generate
   `BepInEx/config/com.heromod.valheim.radialmenus.cfg`.

For a mod manager, use its profile's BepInEx directory. The ZIP targets manual
installation; mod-manager installation still needs validation. Install on the
game client; dedicated-server operation is not a tested release target.

## Controls

| Action | Default |
|---|---|
| Consumable wheel | Hold Mouse3 (a mouse side button) |
| Arrow wheel | Hold R for 0.25 seconds with a bow equipped/recently hidden |
| Select | Move the mouse toward a sector, then release the opening key |
| Cancel | Return to the centre dead zone, then release the key |

The game mouse cursor is visible while selecting. The small arrow inside the
wheel indicates its direction. Empty sectors are inactive.

A short R press retains the game's hide-weapon action. The arrow wheel
restores the hidden bow on normal close. Controller-stick aiming is not
implemented. Rebind Mouse3 if your mouse has no side buttons.

Each wheel shows up to 16 distinct item names. Consumables include food and
meads, not only health potions. Normal game restrictions still apply.
The arrow wheel filters ammunition for the current bow. Camera look and
combat actions are blocked while a wheel is open; keyboard walking remains
available unless BlockMovement is enabled.

## Configuration and support

The [complete CFG reference](docs/configuration.md) lists **all 33 settings**,
defaults, examples and reset instructions. Close the game before editing the
CFG, then restart. Optional editor profiles can override CFG appearance.

If the mod does not load, check `BepInEx/LogOutput.log` for
`Hero Radial Menus 1.1.0` and errors. Check that BepInEx loads and there is
only one copy of the DLL. For a misplaced/invisible wheel, temporarily move
its JSON file out of `BepInEx/config/HeroModUiProfiles` and reset the CFG.
Issue reports should include game/BepInEx versions, reproduction steps,
relevant logs and the CFG. Debug logs are disabled by default.

To uninstall, close the game and remove its DLL. Remove its CFG/profiles
separately if desired. This mod defines no new items and writes no custom
world/player-save data.

## Development and release

- [Developer guide](CONTRIBUTING.md): setup, builds, tests and architecture.
- [Editor profile contract](docs/radial-surfaces.md).
- [Release audit/checklist](docs/release-readiness.md).
- [Changelog](CHANGELOG.md).

Licensed under [MIT](LICENSE). Game assemblies/assets are not included and
are not covered by this license.

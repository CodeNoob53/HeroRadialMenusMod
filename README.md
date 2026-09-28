# Hero Radial Menus

**English** · [Українська](README.uk.md)

![Version](https://img.shields.io/badge/version-1.4.0-blue)
![Valheim](https://img.shields.io/badge/Valheim-1.0.16-orange)
![BepInEx](https://img.shields.io/badge/BepInEx-5.4.23-green)
![Client-side](https://img.shields.io/badge/side-client--only-lightgrey)
![License](https://img.shields.io/badge/license-MIT-yellow)

<!-- Preview image goes here: docs/images/preview.png -->

Hero Radial Menus adds two circular selection wheels to Valheim — one for
potions and food, one for arrows — so you can heal or swap ammunition in the
middle of a fight without opening the inventory or juggling the hotbar.

The wheels are built on the look of Valheim's own radial menu: the same
ornament, pointer, fonts and item stats window, so they feel like part of the
game. Think of them as an improved, mouse-first version of the vanilla menu:
a fixed item order you can learn by feel, several items per opening, clear
feedback on every use, and a live config.

Plugin ID `com.heromod.valheim.radialmenus`.

## Contents

- [Main features](#main-features)
- [Controls](#controls)
- [Requirements](#requirements)
- [Installation](#installation)
- [Configuration](#configuration)
- [Compatibility and limits](#compatibility-and-limits)
- [Troubleshooting](#troubleshooting)
- [Development](#development)
- [Shout outs](#shout-outs)

## Main features

- **Consumable wheel with a fixed order.** Hold a key to see every mead,
  potion and food you carry (up to 16 different items). The order never
  shuffles, so muscle memory works in the heat of battle:
  1. Health meads (Major, Medium, Minor)
  2. Stamina meads
  3. Eitr meads
  4. Resistance meads (Poison, Frost, Fire)
  5. Other meads (Tasty mead, etc.)
  6. Food, best first (health + stamina)
- **Use several at once.** Click slots to drink a health and a stamina mead in
  one opening. A used slot dips like a button and flashes gold; if the game
  refuses (effect still active, too full to eat) it flashes red.
- **Colour-coded effects.** Each consumable glows in the colour of what it
  restores — red health, yellow stamina, blue eitr. Balanced food, such as
  Wolf Jerky, gets a glow split between its colours.
- **Item stats at a glance.** A window left of the wheel shows the hovered
  item's full tooltip plus your armour and carry weight — the same window the
  game's radial menu uses.
- **Arrow wheel.** Hold R with a bow to pick any ammunition that bow can fire.
  A short R press still hides your weapon as usual, and a bow hidden that way
  is re-equipped.
- **Live tracking.** Icons and stack counts always match your inventory.
- **Optional eat/drink animation.** Turn it off for items used from the wheel;
  sound and all game rules stay.
- **Follows the game language.** Item names and tooltips come from the game.
  The wheels' few own labels have their own English and Ukrainian wording and
  use the game's closest translations in every other language.
- **Highly configurable.** Hotkeys, dead zone, movement lock, colours, sizes
  and more. Changes apply as soon as you save the config — no restart.

## Controls

| Action | Default |
|---|---|
| Consumable wheel | Hold Mouse3 (a mouse side button); a second key can be set with `RadialKeySecondary` |
| Arrow wheel | Hold R with a bow equipped |
| Aim | Move the mouse toward a sector |
| Use the selected item | Release the key |
| Use several consumables | Left-click each slot; release the key to close |
| Cancel | Return to the centre, then release the key |

Camera look and combat pause while a wheel is open; walking stays available
unless `BlockMovement` is enabled. If your mouse has no side buttons, set
`RadialKey` to any other key. Each wheel has a main and a secondary key; the
wheel closes when the key that opened it is released.

On R the arrow wheel waits 0.25 seconds (`HoldDelay`) before opening, so a
short press still hides your weapon. The delay applies only to keys that also
do something in the game's controls: put the arrow wheel on a free key
(`Key` or `KeySecondary`, for example Mouse4) and it opens instantly.

## Requirements

- [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
  (BepInEx 5). Built and tested with BepInEx 5.4.23.3 / BepInExPack Valheim
  5.4.2333 on Valheim 1.0.16.

## Installation

1. Install BepInExPack Valheim: follow the installation instructions on
   [its page](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/).
2. Close Valheim. Extract the release ZIP into the Valheim folder, so the DLL
   ends up at `BepInEx/plugins/HeroRadialMenusMod.dll`.
3. Start the game once to create
   `BepInEx/config/com.heromod.valheim.radialmenus.cfg`.

With a mod manager, use its profile's BepInEx folder.

## Configuration

The [complete settings reference](docs/en/configuration.md) lists
**all 39 settings**, their defaults and examples. Changes apply as soon as the
file is saved, even in-game; the wheel positions (`HealOffsetX/Y`,
`[ArrowRadial] OffsetX/Y`) apply on the next world load.

## Compatibility and limits

- Client-side only: works on any server, and the server does not need it.
- Mouse only; controller sticks cannot aim the wheels.
- Up to 16 different items per wheel.
- Normal game rules apply: potion cooldowns, full stomach, ammunition that
  fits the bow.
- The experimental `SlowMotion` option is for single player only.
- The mod adds no items and writes nothing to worlds or characters.

## Troubleshooting

- **The mod does not load.** Look in `BepInEx/LogOutput.log` for
  `Hero Radial Menus 1.4.0` and any errors, and make sure there is only one copy
  of the DLL.
- **A wheel is misplaced or looks wrong.** Reset the config: close the game,
  rename the file and let the game create a new one with defaults.
- **Reporting a bug.** Include the game and BepInEx versions, steps to
  reproduce, the log and the config; set `EnableDebugLogs = true` for detailed
  logging.
- **Uninstalling.** Close the game and delete the DLL; remove the config too
  if you like.

## Development

- [Developer guide](CONTRIBUTING.md): setup, builds, tests and code map.
- [Editor profile format](docs/en/radial-surfaces.md): JSON profiles made with
  Valheim UI Editor, the author's visual UI editor.
- [Release checklist](docs/en/release-readiness.md).
- [Changelog](CHANGELOG.md).

## Shout outs

Thanks to Iron Gate Studio for Valheim, the BepInEx and Harmony teams, and the
Valheim modding community for their tools and support.

Licensed under [MIT](LICENSE). Game assemblies and assets are not included and
are not covered by this license.

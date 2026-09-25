# Hero Radial Menus configuration

**English** · [Українська](../uk/configuration.md)

File: `BepInEx/config/com.heromod.valheim.radialmenus.cfg` in the game folder
or in your mod manager's active profile. BepInEx creates it on the first
launch. You can edit it while the game is running: the mod re-reads it a moment
after you save (the log shows "CFG перечитано з диска."). The exception is the
wheel positions (`HealOffsetX/Y`, `[ArrowRadial] OffsetX/Y`): they apply when a
world loads, so rejoin the world or restart the game after changing them.

The tables list all 37 settings. Do not translate section or key names. Write
decimals with a dot: 0.25, not 0,25. Booleans are true / false. RGB colours
and opacity are 0..1; alpha 0 is transparent, 1 is opaque. Sizes are HUD
units; the game's UI scale can change their physical size on screen.

## [Radial]

Consumable wheel controls and the shared look of both wheels.

| Key | Default | Effect |
|---|---|---|
| `RadialKey` | `Mouse3` | Hold to open the consumable wheel. A Unity KeyCode name such as G, Mouse3, Mouse4; None disables the key. |
| `IncludeAllConsumables` | `false` | false: food with a health value and consumables with a status effect (potions, meads); true: every consumable. It is not a "healing only" filter. |
| `Radius` | `320` | Base radial distance. Values below 200 use 320. Inner edge: Radius − 86, outer edge: Radius + 94, icon centres: Radius + 4. |
| `SlotSize` | `68` | Icon width and height, minimum 1. Does not change the sectors or the stack count text. |
| `DeadZone` | `100` | Radius of the centre cancel zone. Use a positive number smaller than Radius; too large a value makes selection impossible. |
| `HoverFadeSeconds` | `0.12` | Fade-in/out time of the highlight and ornament. 0 or less is instant. |
| `EmptySlotAlpha` | `0.35` | Opacity of empty sectors, clamped to 0..1. |
| `HoverColorR` | `0.23` | Red part of the hovered sector's background; needs TintBackground = true. |
| `HoverColorG` | `0.14` | Green part of the hovered sector's background; needs TintBackground = true. |
| `HoverColorB` | `0.03` | Blue part of the hovered sector's background; needs TintBackground = true. |
| `HoverAlpha` | `0.45` | Opacity of the hovered sector's background; needs TintBackground = true. |
| `FrameColorR` | `1.0` | Red part of the frame, ornament and direction arrow. |
| `FrameColorG` | `0.72` | Green part of the frame, ornament and direction arrow. |
| `FrameColorB` | `0.36` | Blue part of the frame, ornament and direction arrow. |
| `FrameAlpha` | `1.0` | Opacity of the frame, ornament and direction arrow. |
| `FrameWidth` | `4` | Frame thickness. 1..8 recommended, minimum 1. |
| `OrnamentOffset` | `115` | Ornament distance from Radius. At 100 the gap to the outer edge is 6, at 109 it is 15. The hover animation adds its own offset. |
| `SlowMotion` | `false` | Experimental: local time runs at 0.25x while the consumable wheel is open. Single player only, not synchronised with a server. Time returns to normal when the wheel closes; do not combine with other mods that change game speed. |
| `ShowFrame` | `true` | Frame around the hovered sector. Does not turn off the ornament or the direction arrow. |
| `TintBackground` | `false` | Use HoverColorR/G/B and HoverAlpha; false keeps the dark background. |
| `BlockMovement` | `false` | true blocks WASD. false lets you walk; combat, running, jumping and camera look are blocked either way. Gamepad movement is always blocked while a wheel is open. |
| `ClickToUse` | `true` | Clicking a slot with ClickKey uses the item at once and keeps the wheel open, so several items can be used in one opening. When a stack runs out, the sectors are rebuilt. After at least one click, releasing RadialKey only closes the wheel. false: items are used only on releasing RadialKey. |
| `ClickKey` | `Mouse0` | Key for ClickToUse (Unity KeyCode). Must differ from RadialKey; None disables clicking. |
| `ConsumeAnimation` | `true` | The game's eat/drink animation when using an item from the wheel. false: the item is used without the animation; the consume sound and game rules (potion cooldown, full stomach) stay. |
| `ShowItemInfo` | `true` | Stats window for the hovered item, left of the wheel, as in the game's own radial menu: armour, carry weight, item name and tooltip. It looks exactly like the game's window; applies to both wheels. |
| `ItemInfoScale` | `1` | Scale of the stats window, clamped to 0.3..3. |
| `ItemInfoOffsetX` | `0` | Extra X offset from the default place (40 units left of the sectors' outer edge); negative moves it left. |
| `ItemInfoOffsetY` | `0` | Extra Y offset; positive moves it up. |

## [ArrowRadial]

| Key | Default | Effect |
|---|---|---|
| `Key` | `R` | Main key; None disables it. A short R press keeps the game's hide-weapon action. |
| `KeySecondary` | `None` | Alternative key, for example Mouse4. None disables it. |
| `HoldDelay` | `0.25` | Seconds to hold before the wheel opens; applies to both Key and KeySecondary. It exists so a short R press keeps hiding the weapon; if neither key has a game action (e.g. Mouse4), set 0 to open instantly. |
| `PickDelay` | `0.15` | Seconds the wheel stays on screen after a pick; 0 closes it on the next update. Controls already return to the player during this time. Use a non-negative number. |
| `OffsetX` | `0` | Offset from the HUD centre; positive is right, negative is left. |
| `OffsetY` | `0` | Offset: positive is up, negative is down. |

## [Position]

| Key | Default | Effect |
|---|---|---|
| `HealOffsetX` | `0` | Horizontal offset of the consumable wheel; positive is right. |
| `HealOffsetY` | `0` | Vertical offset of the consumable wheel; positive is up. |

## [Debug]

| Key | Default | Effect |
|---|---|---|
| `EnableDebugLogs` | `false` | Detailed UI, profile and input diagnostics in BepInEx/LogOutput.log. Repeated input messages are logged once per session. Startup messages, warnings and errors are logged regardless. |

## Example

Change values in the existing sections; do not add duplicate sections:

```ini
[Radial]
RadialKey = G
TintBackground = true
HoverColorR = 0.23
HoverColorG = 0.14
HoverColorB = 0.03
HoverAlpha = 0.45
BlockMovement = true

[ArrowRadial]
Key = R
KeySecondary = Mouse4
HoldDelay = 0.25
```

Do not bind the same key to both wheels. Keep the game's and other mods'
bindings in mind: the mod does not cancel every action another key may have.

## Reset

Close the game and rename the CFG (for example, add .bak) to get a fresh one
with defaults.

# Radial editor profile format

**English** · [Українська](../uk/radial-surfaces.md)

A profile is an optional JSON file that fine-tunes one wheel on top of the
CFG: position and scale, colours, visibility and size of individual parts.
Profiles are normally created with **Valheim UI Editor**, the mod author's
separate visual editor for these mods' UI; it is not shipped with the mod and
not needed to play. This page describes the file format, so a profile can also
be written or checked by hand.

Plugin ID: com.heromod.valheim.radialmenus.
Each wheel owns a separate profile in BepInEx/config/HeroModUiProfiles:

- com.heromod.valheim.radialmenus.heal-wheel.json
- com.heromod.valheim.radialmenus.arrow-wheel.json

No editor/profile is required for normal use. If a wheel looks wrong, move
its profile out of the folder to rule the profile out.

## Surface and node IDs

Use the full prefix **heal-wheel** or **arrow-wheel**, not heal or arrow.
Both wheels expose the same nodes.

| Suffix after surface ID | Object | Effective profile properties |
|---|---|---|
| .root | HeroQuickHealRadial / HeroArrowRadial | pos, scale; visibility owned by menu open/close |
| .dim | Dim | tint, alpha, enabled |
| .wheel | ProceduralGraphic | enabled; geometry comes from CFG |
| .highlighter | Highlighter | tint, alpha, enabled |
| .cursor | Cursor | enabled |
| .cursor.tip | CursorTip | pos, tint, alpha, size |
| .centerLabel | CenterLabel | text.size, text.color, tint, enabled; text.value game-owned |
| .slot[i] | ProceduralSlot_i | enabled; positions/content game-owned |

`.cursor` and `.cursor.tip` are the small direction arrow inside the wheel,
not the mouse cursor.

Other RectTransform properties are accepted, but this table is the supported
contract. The wheel draws its own sectors, so `.wheel` size/alpha do not
reliably resize or fade them: use CFG Radius/colours or `.root` scale.
Disabling `slot[i]` hides its icon, not its sector or its selection; it is not
an item filter.

The item stats window left of the wheel is not a profile node. Position and
size it with the CFG keys ShowItemInfo, ItemInfoScale and ItemInfoOffsetX/Y.

## Example

Save as com.heromod.valheim.radialmenus.heal-wheel.json:

```json
{
  "schemaVersion": 1,
  "overrides": {
    "heal-wheel.root": { "pos": [40, -25], "scale": [0.9, 0.9] },
    "heal-wheel.dim": { "alpha": 0.2 },
    "heal-wheel.cursor.tip": { "size": [95, 20], "tint": [1, 0.72, 0.36, 1] },
    "heal-wheel.centerLabel": { "text": { "size": 20 } }
  }
}
```

Use schemaVersion 1. Newer schemas and malformed JSON are ignored with a
warning. Removing an override (or the whole file) returns that property to its
base value; `cursor.tip` size returns to 95 × 20. Its `pos` is local to the
rotating arrow: [0, distance] sets the distance from the centre; without an
override it sits 10 units inside the sectors.

## Lifecycle

Order: base layout → CFG → profile. Each wheel checks its profile file while
the HUD updates. Changes made while the wheel is closed apply at once; changes
made while it is open apply the next time it opens. The CFG is also re-read
when saved, and the next opening uses the new values.

Moving parts (the ornament, the direction arrow and the slots) are animated by
the wheel every frame. For them a profile can hide, tint or fade, but not pin
a position: visibility is the game's visibility AND the profile's, and alpha is
tint alpha × animation fade × profile alpha.

Radius is used directly, falling back to 320 below 200. Inner edge = Radius − 86;
outer edge = Radius + 94; icon centres = Radius + 4. There are 3–16 sectors,
starting upward and proceeding clockwise. SlotSize controls icon dimensions.
See [all CFG settings](configuration.md).

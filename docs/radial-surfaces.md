# Radial editor profile contract

Plugin ID: com.heromod.valheim.radialmenus.
Each wheel owns a separate profile in BepInEx/config/HeroModUiProfiles:

- com.heromod.valheim.radialmenus.heal-wheel.json
- com.heromod.valheim.radialmenus.arrow-wheel.json

No editor/profile is required for normal use.

The legacy .cursor / .cursor.tip IDs refer to the chevron direction indicator,
not the actual mouse cursor. The latter is shown by ZCursor while interacting.
IDs are kept unchanged for existing editor profiles.

## Surface and node IDs

Use the full prefix **heal-wheel** or **arrow-wheel**, not heal or arrow.
Both instances use ProceduralRadialWheel.

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

The generic reader accepts additional RectTransform properties, but this
table is the supported wheel-facing contract. The mesh writes its own vertex
colours/coordinates: wheel size/alpha do not reliably resize/fade the visuals.
Use CFG Radius/colours or root scale. Disabling slot[i] hides its widget,
not its mesh sector or item-selection logic; it is not an item filter.

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
warning. Missing/removed overrides fall back to base properties. Removed
cursor.tip size explicitly resets to 95 × 20. Its pos is local to the rotating indicator: [0, distance] sets distance from the centre; without an override it follows innerRadius - 10.

## Lifecycle

Order: base layout → CFG → profile. Each wheel checks its file timestamp in
HUD Update. Changes while closed apply then; changes while open are cached
and applied on the next rebuild/open. Deleting a profile clears its overrides.
CFG itself is not reloaded from disk.

Static nodes use NodeBaseline capture/restore. Dynamic nodes (ornament,
cursor, slots) are excluded from transform restoration and use resolved values.
Visibility is game visibility AND profile permission. Dynamic alpha is
base/profile tint alpha × animation fade × profile alpha.

Radius is used directly, falling back to 320 below 200. Inner edge = Radius − 86;
outer edge = Radius + 94; icon centres = Radius + 4. There are 3–16 sectors,
starting upward and proceeding clockwise. SlotSize controls icon dimensions.
See [all CFG settings](configuration.md).

Tests cover parsing/resolution/merge, not Unity component application.
Verify reset, reopen and live editing in-game.

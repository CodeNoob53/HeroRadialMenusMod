# Changelog

## 1.0.0 — release candidate (unpublished)

- Show/unlock the actual game mouse cursor while a wheel is open, using the
  vanilla ZCursor API. Selection follows its position relative to the wheel.
- The chevron indicates mouse direction; empty sectors cannot be hovered or selected.
- Removed the extra one-second consumable lockout and blanket icon dimming.
  Vanilla item-use restrictions remain intact.

- Unified diagnostic logging under EnableDebugLogs (false by default).
  The old LogInput setting is ignored; startup messages, warnings and errors remain enabled.

- Standalone consumable and bow-ammunition radial menus; no HeroEquipmentMod dependency.
- Independent optional editor profiles for both wheels.
- Release audit connected SlotSize, HoverFadeSeconds, EmptySlotAlpha,
  HoverColorR/G/B, HoverAlpha, TintBackground and OrnamentOffset to rendering.
  Existing CFGs now affect appearance; see the configuration reference.
- Removed unused singleton, wheel accessors, profile helpers and logger locals.
  Removed the hidden legacy hover-colour replacement.
- Repaired standalone profile-test compilation with optional debug logging.
- Reset cursor tip size when its profile override is removed.
- Added user/developer documentation, MIT license and candidate packaging.

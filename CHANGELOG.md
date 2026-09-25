# Changelog

## 1.1.1 — unpublished

- Fixed: ConsumeAnimation = false now reliably skips the eat/drink animation. Only the
  "eat" trigger and the in-hand item are suppressed during the wheel's own UseItem
  call, so consumption, sound, vanilla checks, inventory and hotbar use stay vanilla.
- The CFG is re-read from disk a moment after it is saved, so settings apply without
  restarting the game (wheel offsets still need a world reload).
- With EnableDebugLogs, each use from the wheel logs the ConsumeAnimation value in effect.

## 1.1.0 — unpublished

- Consumable wheel: ClickToUse (default on) uses the clicked slot immediately and keeps
  the wheel open, so several items can be used in one opening. ClickKey sets the button.
  A click gives button-like feedback: the slot dips and its sector flashes gold, or red
  when the game refuses the item (potion cooldown, full stomach).
- ConsumeAnimation (default on) can turn off the vanilla eat/drink animation for items
  used from the wheel; the consume sound/effect and vanilla restrictions stay.

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

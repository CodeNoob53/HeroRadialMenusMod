# Changelog

## 1.3.1 — 26-09-2026

- Fixed: the mouse side buttons Mouse3 and Mouse4 could both register on a
  single press, so a wheel bound to Mouse4 opened the Mouse3 wheel instead.
- Fixed: a free mouse side button counted as bound in the game's controls, so
  the arrow wheel still waited HoldDelay on it.

## 1.3.0 — unpublished

- RadialKeySecondary: a second key for the consumable wheel, like the arrow
  wheel's KeySecondary.
- The wheels' own labels ("no items" messages, the "active" arrow tag) follow
  the game language: own English and Ukrainian wording, the game's closest
  translations for every other language.
- The arrow wheel's HoldDelay applies only to a key that also has a game action
  (like R); on a free key such as Mouse4 the wheel opens instantly.

## 1.2.0 — unpublished

- Item stats window left of both wheels, matching the game's own radial menu:
  armour and carry weight, then the hovered item's name and tooltip.
  New settings: ShowItemInfo, ItemInfoScale, ItemInfoOffsetX/Y.

## 1.1.1 — unpublished

- Fixed: with ConsumeAnimation = false the eat/drink animation could still play.
  Consumption, sound and game rules are unchanged; inventory and hotbar use keep
  the animation.
- CFG changes now apply while the game runs, a moment after the file is saved
  (wheel positions apply on the next world load).
- With EnableDebugLogs, each use from the wheel logs the ConsumeAnimation value.

## 1.1.0 — unpublished

- Click-to-use (ClickToUse, ClickKey): click slots to use several consumables in
  one opening; releasing the key then just closes the wheel.
- Click feedback: a used item's slot dips and flashes gold; a refused one
  (potion still active, too full to eat) flashes red.
- ConsumeAnimation: turn off the eat/drink animation for items used from the
  wheel; sound and game rules stay.

## 1.0.0 — unpublished

- Consumable wheel (hold Mouse3): food, meads and potions around the screen
  centre; point and release to use, or return to the centre to cancel.
- Arrow wheel (hold R with a bow): pick the ammunition to load; a short R press
  keeps the game's hide-weapon action and a hidden bow is re-equipped.
- The game mouse cursor is shown while a wheel is open; empty sectors cannot be
  selected. Camera and combat pause while a wheel is open; walking can be
  blocked with BlockMovement.
- Appearance settings: radius, slot size, dead zone, colours, frame, ornament,
  hover fade and wheel positions.
- Optional editor profiles for each wheel.
- Experimental slow motion for single player.
- Diagnostic logging behind EnableDebugLogs.

# Changelog

## 1.1.1

- Enabled alternative furnace fuels by default.
- Selected configured fuel items when Valheim supplies no coal item to the fuel interaction.

## 1.1.0

- Corrected Valheim 1.0.12 private-member access in placement, area repair, smelter insertion, and Reclaim UI patches.
- Restored unrestricted placement while `noPlacementCost` is enabled.
- Added runtime-member auditing to the release verification process.

## 1.0.0

- Added configurable area repair with per-piece stamina and durability costs.
- Added a native crafting-panel Reclaim tab and capacity-checked inventory recycling.
- Added a configurable in-game clock.
- Added processing-machine capacities, speed multipliers, quick insertion, alternative fuels, all-ore blast furnace support, fermenter, beehive, and windmill settings.
- Added unrestricted placement and build-station range support.
- Built against Valheim 1.0.12 method signatures.
- Routed private placement, smelter, area-repair, and crafting-UI members through Harmony injection or reflection for the Valheim 1.0.12 runtime assembly.

# Changelog

## 1.2.2

- Updated the required BepInExPack Valheim dependency to `5.4.2350` and rebuilt against BepInEx `5.4.23.5`.

## 1.2.1

- Cleared Valheim's drag-item state and drag icon immediately when the held item is removed through `Delete`.

## 1.2.0

- Made inventory `Delete` remove the full selected stack when no reclaim recipe exists.
- Made zero-return recipes operate as deletion transactions.
- Added base resources and other non-reclaimable items to the Reclaim tab as delete entries.
- Kept crafted-item reclamation atomic: material returns are validated before the source stack is removed.

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

# DadsBetterVal

DadsBetterVal is a single BepInEx plugin for Valheim 1.0.12 that combines five configurable systems:

- Radius-based structure repair with normal stamina and tool-durability use for every repaired piece.
- Inventory recycling and material reclamation. Open inventory, point at an unequipped item, and press `Delete` by default.
- An in-game day clock.
- Smelter, blast furnace, kiln, windmill, spinning wheel, eitr refinery, fermenter, and beehive capacity/speed controls, plus quick insertion and optional alternative fuels.
- Removal of placement and build-station range restrictions.

All settings are stored in `BepInEx/config/com.dadisbored.dadsbetterval.cfg`. The plugin has no mod dependency beyond BepInEx.

Recycling performs a full inventory-capacity preflight. The item is removed once, and only after the complete return transaction has been validated.

## Installation

Install the Thunderstore package with a mod manager or place `DadsBetterVal.dll` in `BepInEx/plugins/DadsBetterVal/`. Disable the five standalone feature mods when this combined plugin is enabled to prevent duplicate patches.

## Build

Run `./build.ps1 -Package`. The script builds the DLL and creates both the unpacked package and root-layout ZIP in `dist/`.


# Project Stats

## Counts

- Handwritten runtime scripts under `Assets/Scripts`: 60
- Scene files under `Assets/Scenes`: 5
- Total scene files under `Assets`: 25
- Prefabs under `Assets`: 1952

## Script Density By Area

- `Assets/Scripts/Manager`: 9
- `Assets/Scripts/New ActionRPG Ctrl/Character`: 8
- `Assets/Scripts/New ActionRPG Ctrl/Skills/Effects`: 7
- `Assets/Scripts/Inventory/UI`: 5

The repo has a modest amount of gameplay code relative to a very large imported art/prefab footprint.

## Third-Party Asset Packs

Top-level asset-pack folders detected under `Assets/asset pack`:

- `3D Items - Mega Pack`
- `Animation`
- `ATART`
- `BOXOPHOBIC`
- `ClassMaterials01`
- `DogKnight`
- `GUI_Parts`
- `Nephilite Studios`
- `Polyart`
- `RPG Monster Duo PBR Polyart`
- `Simple Fantasy GUI`
- `SimpleNaturePack`
- `Text`
- `Text Effects For Games 1`
- `UI`

## Third-Party Code Boundary

Detected `.asmdef` files under imported asset packs:

- `BOXOPHOBIC/Utils/Scripts/Boxophobic.Utils.Scripts.asmdef`
- `BOXOPHOBIC/Utils/Editor/Boxophobic.Utils.Editor.asmdef`
- `BOXOPHOBIC/Skybox Cubemap Extended/Core/Editor/Boxophobic.SkyboxCubemapExtended.Editor.asmdef`

This suggests most custom gameplay logic is still centralized in `Assets/Scripts` rather than distributed across package assemblies.

## Known Risks And Signals

- The repository root is not a Git working tree at this level, so normal Git status/history commands will not work here unless the real repo root lives elsewhere.
- Several files contain garbled Chinese comments when read as UTF-8, indicating mixed or legacy encoding.
- `Main Menu.unity` exists but is absent from current build settings.
- Save/load relies on `PlayerPrefs` keys and ScriptableObject overwrite order, which is fast for prototyping but fragile for larger systems.
- Some gameplay files include recent modification markers and commented-out legacy code, indicating active in-place refactors rather than settled architecture.

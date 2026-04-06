# Architecture

## Project Profile

- Unity version: `2020.3.49f1c1`
- Render pipeline: URP (`com.unity.render-pipelines.universal` `10.10.1`)
- Main packages in use: Input System, Cinemachine, Animation Rigging, Timeline, TMP, UGUI
- Workspace type: Unity project root without a `.git` directory at this level

## Top-Level Layout

- `Assets/Scenes`: gameplay scenes and menu scene
- `Assets/Scripts`: handwritten gameplay/runtime scripts
- `Assets/GameData`: ScriptableObject gameplay data for characters, attacks, inventory, food, and items
- `Assets/prefabs`: runtime prefabs, including manager prefabs and UI prefabs
- `Assets/Settings`: URP settings, player input actions, and animation/controller assets
- `Assets/asset pack`: third-party assets and editor/runtime code from imported packages

## Main Runtime Spine

The project runtime is organized around a small set of persistent managers:

- `GameManager`: registers the player, manages Cinemachine camera binding, and broadcasts end-game notifications
- `SceneController`: handles scene transitions, player instantiation, and save/load calls around transitions
- `SaveManager`: serializes runtime data into `PlayerPrefs`
- `InventoryManager`: instantiates inventory/equipment/action data from templates and synchronizes UI

Manager prefabs exist under `Assets/prefabs/Managers`.

## Two Gameplay Stacks Coexist

This repository currently contains two partially overlapping gameplay implementations.

### Legacy stack

- Input: `Assets/Scripts/PlayerBaseController/PlayerInput.cs`
- Character control: `Assets/Scripts/PlayerBaseController/ActorController.cs`
- Combat collision: `Assets/Scripts/Manager/BattleManager.cs`
- Actor wiring and stats: `Assets/Scripts/Manager/ActorManager.cs`, `Assets/Scripts/Manager/StateManager.cs`
- Inventory interaction still depends on this stack through `PlayerInput`

### Newer ActionRPG stack

- Input asset: `Assets/Settings/PlayerInputSetting/PlayerInputMap.inputactions`
- Character control: `Assets/Scripts/New ActionRPG Ctrl/Character/CharCtrl.cs`
- Target collection: `Assets/Scripts/New ActionRPG Ctrl/New Skills System/TargetingUtil.cs`
- Skill execution: `Assets/Scripts/New ActionRPG Ctrl/Character/CharSkillCtrl.cs`, `Assets/Scripts/New ActionRPG Ctrl/Skills/SkillData.cs`, `Assets/Scripts/New ActionRPG Ctrl/Skills/Effects/*.cs`

Do not assume one stack fully replaced the other. Verify which prefab and scene objects the task actually uses before refactoring.

## Data Model Pattern

Gameplay state is largely driven by ScriptableObjects:

- Character data: `Assets/GameData/Char Data/*.asset`
- Attack data: `Assets/GameData/Attack Data/*.asset`
- Inventory containers: `Assets/GameData/Inventory Data/*.asset`
- Item definitions: `Assets/GameData/Item Data/**/*.asset`

At runtime, several managers instantiate template ScriptableObjects and mutate the clones in memory, then overwrite them from `PlayerPrefs` during load.

## Vendor Boundary

Most imported content lives under `Assets/asset pack`. The only detected `.asmdef` files there are BOXOPHOBIC editor/runtime assemblies. Prefer to keep project-specific logic in `Assets/Scripts`, `Assets/GameData`, `Assets/prefabs`, and `Assets/Scenes`.

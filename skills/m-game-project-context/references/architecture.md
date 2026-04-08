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
- `skills`: repo-local Codex skills for project context and future task routing

## Main Runtime Spine

The project runtime is organized around a small set of persistent managers:

- `GameManager`: registers the player object, exposes `PlayerUnit` / `PlayerTransform` / `PlayerState` / `PlayerCharacterData`, manages Cinemachine camera binding, and broadcasts end-game notifications
- `SceneController`: handles scene transitions, player instantiation, and save/load calls around transitions
- `SaveManager`: serializes runtime data into `PlayerPrefs`
- `InventoryManager`: instantiates inventory/equipment/action data from templates and synchronizes UI

Manager prefabs exist under `Assets/prefabs/Managers`.

## Blackboard-First Runtime Model

The active character architecture is now blackboard-first.

- `CharBlackBoard` is the shared runtime data hub for one unit.
- Each slice stores one concern: identity, motion, action, resources, combat, status, skills, equipment, and targeting.
- Feature toggles on the blackboard allow units to disable whole modules such as health, combat, skills, or equipment.
- `CharBlackBoardInitializer` is responsible for bootstrapping blackboard data from scene state or legacy template data.
- Resolver helpers such as `CharRelationResolver`, `CharResourceResolver`, `CharRuntimeResolver`, `CharStatusResolver`, and `CharEquipmentResolver` are the intended read path for gameplay/UI code.

Current authority rules:

- Status authority: `CharStatusCtrl`
- Shared status read outlet: `CharBlackBoard.Status.snapshot`
- Team authority: `Team`
- Resources/combat read path: resolver helpers first, blackboard slices underneath
- `StateManager`: compatibility bridge for older save/UI/equipment/prefab flows

## Gameplay Stack Reality

This repository still contains both a legacy gameplay stack and a newer ActionRPG stack, but they are not equally important anymore.

For current character-control, combat, skill, status, animation, and VFX work, the newer ActionRPG stack is the active path. The legacy stack still exists and still matters for some inventory, pickup, and older scene/prefab dependencies.

### Legacy stack

- Input: `Assets/Scripts/PlayerBaseController/PlayerInput.cs`
- Character control: `Assets/Scripts/PlayerBaseController/ActorController.cs`
- Combat collision: `Assets/Scripts/Manager/BattleManager.cs`
- Actor wiring and stats: `Assets/Scripts/Manager/ActorManager.cs`, `Assets/Scripts/Manager/StateManager.cs`
- Inventory interaction still depends on this stack through `PlayerInput`

Use this stack when a task is explicitly about old prefabs, bag/pickup behavior, or compatibility with older runtime code. Do not start new character foundation work here by default.

### Active ActionRPG stack

- Input asset: `Assets/Settings/PlayerInputSetting/PlayerInputMap.inputactions`
- Input reader for both player and AI controlled characters: `Assets/Scripts/New ActionRPG Ctrl/Character/CharSignalReader.cs`
- Character movement/facing/runtime lock application: `Assets/Scripts/New ActionRPG Ctrl/Character/CharCtrl.cs`
- Action gate and lightweight action runtime: `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharActionCtrl.cs`
- Status authority and snapshot rebuild: `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharStatusCtrl.cs`
- Body animation writer: `Assets/Scripts/New ActionRPG Ctrl/Character/CharAnimCtrl.cs`
- Weapon logic and weapon animation coordination: `Assets/Scripts/New ActionRPG Ctrl/Character/CharWeaponCtrl.cs`
- Weapon-prefab animation player: `Assets/Scripts/New ActionRPG Ctrl/Character/WeaponAnimCtrl.cs`
- Status VFX runtime player: `Assets/Scripts/New ActionRPG Ctrl/Character/CharStatusVfxCtrl.cs`
- Target collection: `Assets/Scripts/New ActionRPG Ctrl/New Skills System/TargetingUtil.cs`
- Skill execution: `Assets/Scripts/New ActionRPG Ctrl/Character/CharSkillCtrl.cs`, `Assets/Scripts/New ActionRPG Ctrl/Skills/SkillData.cs`, `Assets/Scripts/New ActionRPG Ctrl/Skills/Effects/*.cs`

Important current facts:

- Active gameplay input now uses the new input system; the old input chain is not the default gameplay path.
- All characters can flow through `CharSignalReader`, including AI-controlled units.
- `CharBlackBoard` now sits under the active stack as the shared runtime data center.
- `StateManager` still coexists in the runtime, but is now mainly a compatibility bridge for some HP checks, older weapon slots, save/UI reads, and legacy status/application entry points.
- `CharStatusCtrl` maps legacy `EStatusType` values through its inspector-configured legacy map instead of hard-coding old status logic directly into character behavior.
- Team relationship checks are no longer supposed to depend on layer/tag. `CharRelationResolver` prefers `Team`, and blackboard team data mirrors that component.
- Older monster prefabs such as `SlimePBR` have started moving to resolver-based runtime reads instead of direct old-stack assumptions.

## Data Model Pattern

Gameplay state is largely driven by ScriptableObjects:

- Character data: `Assets/GameData/Char Data/*.asset`
- Attack data: `Assets/GameData/Attack Data/*.asset`
- Inventory containers: `Assets/GameData/Inventory Data/*.asset`
- Item definitions: `Assets/GameData/Item Data/**/*.asset`

At runtime, several managers instantiate template ScriptableObjects and mutate the clones in memory, then overwrite them from `PlayerPrefs` during load.

For the new character foundation, also treat these data assets as important:

- Character status definitions: `Assets/GameData/Char Status/*.asset`
- Character animator controllers and animation assets under `Assets/Settings/Character Settings/**`
- Skill definitions using category-based cast animation in `SkillData`

For current runtime ownership, also treat these runtime components as important:

- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharBlackBoard.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharBlackBoardInitializer.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Team.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharRelationResolver.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharResourceResolver.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharRuntimeResolver.cs`

## Vendor Boundary

Most imported content lives under `Assets/asset pack`. The only detected `.asmdef` files there are BOXOPHOBIC editor/runtime assemblies. Prefer to keep project-specific logic in `Assets/Scripts`, `Assets/GameData`, `Assets/prefabs`, and `Assets/Scenes`.

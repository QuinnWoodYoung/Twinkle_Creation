---
name: m-game-project-context
description: Project-specific context for the M Game Unity repository. Use when working in this repo and you need to understand the active ActionRPG character stack, the remaining legacy dependencies, scene entry points, cross-scene singletons, inventory/save flow, animation/status/VFX responsibilities, or safe modification constraints before editing Unity scenes, prefabs, ScriptableObjects, or gameplay scripts.
---

# M Game Project Context

## Quick Start

Read the smallest reference set that matches the task:

- Read `references/architecture.md` first for any non-trivial task in this repository.
- Read `references/gameplay-systems.md` when the task touches character input, movement, combat, skills, status, animation, VFX, weapons, inventory, pickups, or gameplay ScriptableObjects.
- Read `references/scene-and-runtime.md` when the task touches scene loading, manager prefabs, save/load, or startup flow.
- Read `references/project-stats.md` when you need project scale, asset-pack scope, or known repo-level risks.

## Working Rules

Apply these constraints before editing:

- Treat the active gameplay path as the newer ActionRPG chain unless scene objects clearly prove otherwise.
- Treat `CharBlackBoard` as the shared runtime data hub for one unit. New gameplay code should prefer reading blackboard slices through resolver helpers instead of creating another authority path.
- Start character-combat work from `CharSignalReader`, `CharCtrl`, `CharActionCtrl`, `CharStatusCtrl`, `CharAnimCtrl`, `CharWeaponCtrl`, `WeaponAnimCtrl`, `CharSkillCtrl`, and `SkillData`.
- Treat the legacy stack as compatibility surface, not the default place for new combat work. It still matters for some inventory, pickup, and older prefab flows, but `StateManager` is now mainly a bridge.
- Preserve the current body/weapon split: `CharAnimCtrl` writes body Animator state, while weapon-prefab animation belongs to `WeaponAnimCtrl` and is coordinated by `CharWeaponCtrl`.
- Preserve the current status ownership split: `CharStatusCtrl` owns gameplay state, `CharBlackBoard.Status.snapshot` is the shared read outlet, `CharStatusDef` owns data/config, and `CharStatusVfxCtrl` owns runtime status VFX playback.
- Preserve the current team ownership rule: `Team` is the primary runtime authority for friend-or-foe. Blackboard team fields should mirror `Team`, not disagree with it.
- Treat `Assets/asset pack/` as vendor content by default. Avoid editing third-party asset-pack code or prefabs unless the task explicitly requires it.
- Preserve the singleton lifecycle around `GameManager`, `SceneController`, `SaveManager`, and `InventoryManager`. These objects are intended to survive scene changes.
- Preserve save/load semantics. This project relies on `PlayerPrefs` plus runtime-instantiated `ScriptableObject` data, not a file-based save system.
- Verify scene names and build settings before introducing new scene transitions. Existing code loads scenes by string name.
- Expect mixed text encoding in comments. Avoid mass rewrites or formatters that could further corrupt Chinese comments.

## Task Routing

Use this routing to avoid reading unnecessary context:

- For character input, player/AI signal flow, movement, facing, attack/cast locking, status gating, animation, or status VFX: inspect the newer chain first in `CharBlackBoard`, the resolver helpers, `CharSignalReader`, `CharCtrl`, `CharActionCtrl`, `CharStatusCtrl`, `CharAnimCtrl`, `CharWeaponCtrl`, `WeaponAnimCtrl`, `CharSkillCtrl`, `SkillData`, and `CharStatusVfxCtrl`.
- For targeting, lock-on, projectile skills, area targeting, or skill effects: inspect `TargetingUtil`, `SkillData`, `SkillEffect` assets, and the relevant controllers above.
- For bag toggle, item pickup, equipment inventory data, or older actor flows: inspect `PlayerInput`, `ActorController`, `BattleManager`, `StateManager`, and `InventoryManager`, then confirm whether the task should be migrated or only patched.
- For runtime HP, attack, death, relation, or current-player lookups: inspect `CharResourceResolver`, `CharRuntimeResolver`, `CharRelationResolver`, and `GameManager.PlayerUnit` before touching old direct references.
- For inventory and equipment data issues: inspect `InventoryData_SO`, `ItemData_SO`, `UsableItemData_SO`, `InventoryManager`, and related UI scripts.
- For scene flow or persistence issues: inspect `SceneController`, `TransitionPoint`, `TransitionDestination`, `SaveManager`, and the manager prefabs under `Assets/prefabs/Managers`.

## References

- `references/architecture.md`
- `references/scene-and-runtime.md`
- `references/gameplay-systems.md`
- `references/project-stats.md`

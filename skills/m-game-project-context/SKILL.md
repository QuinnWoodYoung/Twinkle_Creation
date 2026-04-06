---
name: m-game-project-context
description: Project-specific context for the M Game Unity repository. Use when working in this repo and you need to understand scene entry points, cross-scene singletons, the legacy controller stack versus the newer ActionRPG stack, inventory/save flow, third-party asset boundaries, or safe modification constraints before editing Unity scenes, prefabs, ScriptableObjects, or gameplay scripts.
---

# M Game Project Context

## Quick Start

Read the smallest reference set that matches the task:

- Read `references/architecture.md` first for any non-trivial task in this repository.
- Read `references/scene-and-runtime.md` when the task touches scene loading, manager prefabs, save/load, or startup flow.
- Read `references/gameplay-systems.md` when the task touches player control, combat, skills, inventory, pickups, or ScriptableObject gameplay data.
- Read `references/project-stats.md` when you need project scale, asset-pack scope, or known repo-level risks.

## Working Rules

Apply these constraints before editing:

- Determine whether the task belongs to the legacy gameplay chain or the newer ActionRPG chain before changing input, combat, animation, or target selection logic.
- Treat `Assets/asset pack/` as vendor content by default. Avoid editing third-party asset-pack code or prefabs unless the task explicitly requires it.
- Preserve the singleton lifecycle around `GameManager`, `SceneController`, `SaveManager`, and `InventoryManager`. These objects are intended to survive scene changes.
- Preserve save/load semantics. This project relies on `PlayerPrefs` plus runtime-instantiated `ScriptableObject` data, not a file-based save system.
- Verify scene names and build settings before introducing new scene transitions. Existing code loads scenes by string name.
- Expect mixed text encoding in comments. Avoid mass rewrites or formatters that could further corrupt Chinese comments.

## Task Routing

Use this routing to avoid reading unnecessary context:

- For player movement, melee combat, camera follow, pickup, or bag toggle issues: inspect the legacy chain in `PlayerInput`, `ActorController`, `BattleManager`, `StateManager`, and `InventoryManager`.
- For lock-on, mouse aiming, projectile skills, area targeting, or status effects: inspect the newer chain in `PlayerInputMap`, `CharCtrl`, `CharSkillCtrl`, `TargetingUtil`, `SkillData`, and `SkillEffect` assets.
- For inventory and equipment data issues: inspect `InventoryData_SO`, `ItemData_SO`, `UsableItemData_SO`, `InventoryManager`, and related UI scripts.
- For scene flow or persistence issues: inspect `SceneController`, `TransitionPoint`, `TransitionDestination`, `SaveManager`, and the manager prefabs under `Assets/prefabs/Managers`.

## References

- `references/architecture.md`
- `references/scene-and-runtime.md`
- `references/gameplay-systems.md`
- `references/project-stats.md`

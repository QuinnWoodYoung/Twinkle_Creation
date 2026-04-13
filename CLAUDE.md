# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**M Game** is a top-down ARPG built in Unity (URP 10.10.1) with combat inspired by Hades (melee feel) and Dota 2 (skill design). The project uses C#, New Input System, Animation Rigging, Cinemachine, and Timeline.

## Language

The user is a Chinese speaker. Respond in Chinese unless asked otherwise.

## Architecture

### Character Controller (Component Composition)

Each character is assembled from multiple MonoBehaviour controllers on the same GameObject:

```
PlayerInput → CharSignalReader → CharParam (input container)
  → CharCtrl         (movement, facing)
  → CharWeaponCtrl   (basic attacks, combos, projectiles)
  → CharAnimCtrl     (body Animator)
  → CharSkillCtrl    (skill casting)
  → CharStatusCtrl   (buffs, crowd control)
  → CharBlackBoard   (unified runtime state)
```

Input is abstracted via `ICharCtrlSignal` with two implementations: `PlayerInputSource` (player) and `AIInputSource` (AI).

### Blackboard Pattern

`CharBlackBoard` is the unified runtime state container, organized into typed slices (Identity, Transform, Motion, Action, Resources, Combat, Status, Skills, Equipment, Targeting). Systems read/write only their slice. Changes fire `RuntimeChanged` with a `CharBlackBoardChangeMask` bitmask.

**Migration in progress**: `StateManager` is the legacy state holder being gradually replaced by `CharBlackBoard`. Both coexist — new code should prefer the blackboard path.

### Action Occupancy System

`CharActionCtrl` gates all character actions (attack, cast, dodge, etc.) through `TryStart(CharActionReq)`. Only one action can be active at a time. Actions have states (Windup, Release, Recover) and can be interruptible. Events: `ActionStart`, `ActionEnd`, `ActionIntd`.

### Combat / Attack System

**Critical rule**: `AttackData_SO.basicAttackMode` is the authority for attack behavior, NOT `WeaponType`. The enum `BasicAttackMode` has: MeleeCombo, MeleeRepeat, RangedStraight, RangedHoming, RangedChargeRelease.

Attack flow: `CharWeaponCtrl` reads input → requests action via `CharActionCtrl` → triggers animation → spawns projectile or triggers melee hit → `BattleManager` resolves damage via `CharResourceResolver`.

Team relations use `Team` component + `TeamSide` enum (not Unity Layers). Resolution goes through `CharRelationResolver`.

### Skill System

`SkillData` (ScriptableObject) defines skills with a targeting mode (`NoTarget`, `Point`, `Unit`, `Direction`, etc.) and an ordered list of `SkillEffect` subclasses (DamageEffect, HealEffect, LaunchProjectileEffect, ApplyBuffEffect, BlinkEffect, AoeEffect, etc.). `CharSkillCtrl` manages slots, cooldowns, and cast execution.

### Status / Crowd Control

`CharStatusCtrl` manages active `CharStatusRt` instances from `CharStatusDef` templates (ScriptableObjects). Effects fold into `CharStateSnap` which exposes capability bools (`canMove`, `canAtk`, `canCast`) and multipliers (`moveSpdMul`, `atkSpdMul`). State tags are bitmask flags: Stun, Silence, Root, Sleep, Disarm, Invis, Invul, etc.

### Static Resolver Pattern

Stateless utility classes read data from the component composition without holding state:
- `CharResourceResolver` — damage, HP, attack stats
- `CharRuntimeResolver` — speeds with status multipliers, capability checks
- `CharRelationResolver` — team relation (Self/Ally/Enemy/Neutral)
- `CharEquipmentResolver` — weapon root location

### Animation

Two parallel animators per character:
- `CharAnimCtrl` drives the **body** Animator (locomotion, attack triggers, CC states). Uses weapon-typed layers: "Light Sword", "Sword", "Bow", "ShieldLayer", "UnarmedLayer".
- `WeaponAnimCtrl` drives the **weapon prefab** Animator independently.

## Key Conventions

- **Private fields**: `_camelCase` with underscore prefix
- **ScriptableObject data classes**: suffix `_SO` (e.g. `AttackData_SO`, `CharacterData_SO`)
- **Events**: C# events/delegates for inter-system communication, not Unity SendMessage
- **Data-driven design**: gameplay tuning lives in ScriptableObjects, not hardcoded

## Known Risks / In-Progress Work

From `skills.md`:
- MeleeCombo timing is duration-driven, not animation-event-driven (planned migration)
- Input abstraction is mouse-first; gamepad needs a dedicated aim-source abstraction
- No automated playmode/unit tests exist for combat systems

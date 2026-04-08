# Gameplay Systems

## Active Character Runtime

For current gameplay work, read the newer ActionRPG chain first.

Primary files:

- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharBlackBoard.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharBlackBoardInitializer.cs`
- `Assets/Settings/PlayerInputSetting/PlayerInputMap.inputactions`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharSignalReader.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharActionCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharStatusCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharAnimCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharWeaponCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/WeaponAnimCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharSkillCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharStatusVfxCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Skills/SkillData.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Skills/Effects/*.cs`

## Blackboard And Resolver Pattern

Observed behavior:

- `CharBlackBoard` is now the shared runtime data center for one unit.
- Controllers are being pushed toward writing one part of the blackboard instead of owning isolated duplicate state.
- Resolver helpers are the preferred read API for gameplay and UI code.
- The feature-toggle slices on the blackboard are intended to support modular units that can disable health, combat, skills, equipment, or targeting.
- The migration is advanced but not fully complete; some legacy paths still mirror or bridge into blackboard-backed data.

Practical read rule:

- Read HP, attack, range, cooldown, and healing through `CharResourceResolver`.
- Read death, action lock, movement/cast availability, and other transient runtime flags through `CharRuntimeResolver`.
- Read ally/enemy/self through `CharRelationResolver`.
- Read status snapshot details through `CharStatusCtrl` or `CharBlackBoard.Status.snapshot` when the task is explicitly about status composition.

## Input And Signal Flow

Observed behavior:

- Active gameplay input uses the new Input System action asset, not the old string-based input path.
- `CharSignalReader` is the signal bridge for both player-controlled and AI-controlled units.
- `PlayerInputSource` writes locomotion, aim, attack state, dodge, lock, and skill-slot down events into `CharCtrl.Param`.
- `AIInputSource` implements the same interface and can drive the same runtime without special-case controller code.

If a future task touches control ownership or player/AI parity, inspect `CharSignalReader` before editing `CharCtrl`.

## Movement, Facing, And Action Locks

Observed behavior:

- `CharCtrl` owns movement application, gravity, free-look/lock-on facing, and short forced-facing helpers.
- `CharCtrl` reads both status restrictions and runtime action locks before moving or rotating.
- `CharActionCtrl` is the lightweight action gate for attack/cast/hit/dead style actions.
- `CharActionReq` carries runtime lock flags such as `lockMove`, `lockRotate`, `waitFace`, `faceDir`, and `animKey`.
- `CharActionCtrl` can delay the real action start until facing is ready, then emits `ActionStart`.
- Zero-duration cast/action requests are expected to end cleanly; the controller is already used as more than a read-only lock source.

This project is currently using the action system as a simplified ARPG runtime gate, not a fully split MOBA-style windup/release/recovery timeline yet.

## Status Authority And Snapshot Model

Primary files:

- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharStatusCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharStatusDef.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharRestrict.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharImmuneType.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharStateTag.cs`

Observed behavior:

- `CharStatusCtrl` is the gameplay authority for active runtime statuses.
- Runtime statuses are stored as `CharStatusRt` entries and folded into a snapshot (`CharStateSnap`).
- After blackboard binding, the runtime list and snapshot live directly in `CharBlackBoard.Status`.
- Status definitions are data-driven through `CharStatusDef` assets.
- The controller supports duration ticking, stacking, refresh, exclusive groups, priority, immunity checks, break-on-damage, and dispel.
- Snapshot output separates tags, restrictions, immunities, and stat modifiers.
- Convenience booleans such as `canMove`, `canAtk`, `canCast`, `canRotate`, `canSelect`, `canUnitTarget`, and `canAtkTarget` are rebuilt from the snapshot.
- `SetActionState(...)` appends action-derived tags like `Atking`, `Casting`, `ForcedMove`, `HitReact`, and `Dead`.
- Legacy `EStatusType` application still enters through the inspector-configured legacy map in `CharStatusCtrl`.

Important current limits:

- The data model already has tags/restricts/immunes/stat mods, but only part of the Dota-like state surface is authored into assets today.
- `StateManager` still exists, so some callers may still query old booleans or old HP/state data.
- The status authority is clearer than before, but save/equipment/growth style systems are not fully blackboard-native yet.

## Body Animation Runtime

Primary files:

- `Assets/Scripts/New ActionRPG Ctrl/Character/CharAnimCtrl.cs`
- `Assets/Settings/Character Settings/Mei/MeiAnimCtrl.controller`

Observed behavior:

- `CharAnimCtrl` is the main writer for the body Animator.
- It owns movement parameter writes, dead bool sync, attack/cast/hit triggers, weapon pose sync, and status-layer bool sync.
- It listens to `CharActionCtrl.ActionStart`, `CharWeaponCtrl.WeaponChanged`, and `CharStatusCtrl.SnapUpd`.
- Cast animation is category-based through `SkillData.GetCastAnimKey()` rather than per-skill animator state names.
- Current cast categories are `None`, `Cast`, `Shoot`, `Buff`, and `Dash`.
- Status animation currently keys off the dominant control tag from the status snapshot, while allowing softer states like root/silence to yield when action animation should remain visible.
- `MeiAnimCtrl.controller` is currently the best live reference for the intended layer structure, including weapon-specific skill layers and a status layer.

Current ownership rule:

- Body animation belongs in `CharAnimCtrl`.
- Weapon-prefab animation should not be authored into the body controller unless the task is specifically about body pose/upper-body layering.

## Weapon Logic And Weapon Animation

Primary files:

- `Assets/Scripts/New ActionRPG Ctrl/Character/CharWeaponCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/WeaponAnimCtrl.cs`

Observed behavior:

- `CharWeaponCtrl` owns current weapon type, attack input handling, weapon-specific attack movement rules, projectile spawn for bow, and binding to the active weapon root.
- Normal attacks are currently unified around the trigger name `Attack`; weapon differences are expressed mainly through body layer selection and whether attack allows movement.
- Weapon-prefab animation is delegated to `WeaponAnimCtrl`.
- `WeaponAnimCtrl` binds to an owner and weapon type at runtime, then plays trigger/state names on the weapon prefab's own Animator.
- The weapon system expects the actual weapon prefab to carry its own Animator and optional `WeaponAnimCtrl`.

Current gameplay rule already encoded:

- Axe attacks lock movement.
- Sword and bow attacks can move while attacking.

## Skill Runtime

Primary files:

- `Assets/Scripts/New ActionRPG Ctrl/Character/CharSkillCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/New Skills System/TargetingUtil.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Skills/SkillData.cs`

Observed behavior:

- `CharSkillCtrl` manages skill slots, cooldowns, input reads, targeting, and skill execution.
- `TargetingUtil.Collect(...)` resolves targets from lock-on, unit raycast, ground raycast, or fallback plane intersection.
- `CharSkillCtrl` can create a pending cast when a skill requires facing first.
- `CharActionCtrl` is used to hold cast animation and cast-facing waits, using `CharActionReq` with `type = Cast`.
- `SkillData` validates target shape, range, team rules, optional range clamp, and optional caster facing on activation.
- `SkillData` no longer needs unique animator keys per skill for standard body cast presentation; it maps to a small category set.
- Skill friend-or-foe validation should now be understood as resolver/team based, not layer/tag based.

## Status VFX Runtime

Primary files:

- `Assets/Scripts/New ActionRPG Ctrl/Character/CharStatusVfxCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharStatusDef.cs`
- `Assets/GameData/Char Status/*.asset`

Observed behavior:

- Status VFX is data-driven from `CharStatusDef`, not hard-coded into each skill/effect script.
- `CharStatusVfxCtrl` listens to status add/update/remove events and plays enter/loop/exit VFX.
- Each status definition can configure:
  - whether status VFX is enabled
  - enter VFX
  - looping VFX
  - remove VFX
  - follow behavior
  - refresh behavior
  - mount point
  - local offset/rotation
- Mount points are resolved at runtime as `Root`, `Body`, `Head`, or `Feet`.
- If explicit mount transforms are not assigned, `CharStatusVfxCtrl` falls back to the body Animator and tries to use humanoid chest/head bones, then falls back to the character root.

This is the current answer to "where should state VFX live": gameplay state stays in `CharStatusCtrl`, while runtime visual playback lives in `CharStatusVfxCtrl` and is configured through `CharStatusDef`.

## Legacy Dependencies Still In Play

Primary files:

- `Assets/Scripts/PlayerBaseController/PlayerInput.cs`
- `Assets/Scripts/PlayerBaseController/ActorController.cs`
- `Assets/Scripts/Manager/BattleManager.cs`
- `Assets/Scripts/Manager/StateManager.cs`
- `Assets/Scripts/Inventory/Logic/MonoBehaviour/InventoryManager.cs`
- `Assets/Scripts/Inventory/Item/MonoBehaviour/ItemPickUp.cs`

Observed behavior:

- The old input/controller stack still exists and still drives some bag-toggle, pickup, and older actor behavior.
- `InventoryManager.Update()` and `ItemPickUp` still contain legacy input assumptions.
- `StateManager` is still present beside the new character stack and may still provide HP, weapon-slot, or compatibility data to newer scripts.
- Some older prefabs, UI, and save/load flows still expect `characterData` / `attackData` mirroring to exist.

Do not remove legacy code blindly. First determine whether the task is:

- a compatibility patch for old flow
- a migration to the newer ActionRPG chain
- or a cleanup after all references have already been moved

## Inventory And Pickup Flow

Primary files:

- `Assets/Scripts/Inventory/Logic/MonoBehaviour/InventoryManager.cs`
- `Assets/Scripts/Inventory/Logic/scriptableObject/InventoryData_SO.cs`
- `Assets/Scripts/Inventory/Item/ScriptableObject/ItemData_SO.cs`
- `Assets/Scripts/Inventory/Item/MonoBehaviour/ItemPickUp.cs`
- `Assets/Scripts/Inventory/UI/*.cs`

Observed behavior:

- `InventoryManager` clones three template data objects: bag, action bar, and equipment
- `InventoryManager.Update()` still checks `FindObjectOfType<PlayerInput>().OpenBag`
- `ItemPickUp` checks legacy `PlayerInput.pickupKeyPressed` before adding item data to inventory
- `InventoryData_SO.AddItem(...)` supports stacking and first-empty-slot insertion
- `StateManager.EquipWeapon(...)` instantiates weapon prefabs and applies weapon attack data

If you move the active player fully to the newer ActionRPG stack, inventory and pickup input paths will need explicit migration.

## Gameplay Data Assets

Current gameplay data folders indicate the intended feature set:

- Character templates: player and slime
- Attack data: player base attack, slime attack, weapon attack data
- Inventory data: bag, action, equipment
- Item data: usable item and weapon item definitions
- Food data: chicken leg

The small amount of authored gameplay data compared with the large number of imported prefabs suggests the repo is still in an integration/prototyping phase rather than content-complete production.

## Editing Guidance

- Default to the ActionRPG chain for new character gameplay work.
- Change one gameplay stack at a time unless the task is explicitly a migration.
- When debugging combat, verify the effective unit relation first. Do not use layer/tag as the enemy-or-ally authority.
- When debugging animation, check whether the issue belongs to body animation (`CharAnimCtrl` + character Animator) or weapon-prefab animation (`WeaponAnimCtrl` + weapon Animator).
- When debugging status presentation, check both the gameplay snapshot (`CharStatusCtrl.Snap` or `CharBlackBoard.Status.snapshot`) and the configured `CharStatusDef` asset.
- When debugging player UI, start from `GameManager.PlayerUnit` and the resolver helpers before wiring to `StateManager.UpdateHP`.
- When debugging UI or item flow, check scene objects and prefabs for references to manager singletons before assuming pure-code fixes are sufficient.

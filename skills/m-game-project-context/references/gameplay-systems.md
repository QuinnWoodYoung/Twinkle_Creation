# Gameplay Systems

## Legacy Player And Combat Flow

Primary files:

- `Assets/Scripts/PlayerBaseController/PlayerInput.cs`
- `Assets/Scripts/PlayerBaseController/ActorController.cs`
- `Assets/Scripts/Manager/ActorManager.cs`
- `Assets/Scripts/Manager/BattleManager.cs`
- `Assets/Scripts/Manager/StateManager.cs`

Observed behavior:

- `PlayerInput` polls classic Unity input strings such as `w`, `a`, `s`, `d`, mouse buttons, `e`, and `b`
- `ActorController` drives movement, jump, attack triggers, and death animation from `PlayerInput`
- `GameManager.RigisterPlayer(...)` is called from `ActorController.OnEnable()` and binds the Cinemachine follow camera
- `BattleManager` applies damage when weapon or projectile colliders hit an actor sensor
- `StateManager` owns HP, attack data, weapon equip logic, and recent status-effect additions such as stun/root/silence tracking

This stack still matters for bag toggling, item pickup, and likely the currently playable character prefab.

## Newer ActionRPG Skill Flow

Primary files:

- `Assets/Settings/PlayerInputSetting/PlayerInputMap.inputactions`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharSkillCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/New Skills System/TargetingUtil.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Skills/SkillData.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Skills/Effects/*.cs`

Observed behavior:

- Uses the Input System action asset with movement, attack, dodge, lock, and four skill buttons
- `CharCtrl` handles movement, facing, dodge, lock-on facing, gravity, and death animation
- `TargetingUtil.Collect(...)` resolves a target from locked target, unit raycast, ground raycast, or fallback plane intersection
- `CharSkillCtrl` initializes skill input slots, manages cooldowns, gathers targeting context, and activates `SkillData`
- `SkillData.Activate(...)` validates range and target requirements, then executes attached `SkillEffect` objects

This subsystem appears under active refactor. The file contains large commented sections plus newer active logic.

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

- Change one gameplay stack at a time unless the task is explicitly a migration.
- When debugging combat, verify which colliders/tags are active: `weapon`, `Projectile`, and `Player` matter in current code.
- When debugging UI or item flow, check scene objects and prefabs for references to manager singletons before assuming pure-code fixes are sufficient.

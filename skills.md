# M Game Basic Attack Status

## Verdict
- Current normal attack system is ready to use as the gameplay baseline for active development.
- It is not yet "final production complete". It still needs targeted follow-up on animation-event authority, input abstraction, and validation.

## Current Runtime Path
- Input:
  `CharSignalReader -> CharParam.AttackState -> CharWeaponCtrl`
- Execution:
  `CharWeaponCtrl -> CharActionCtrl -> targeting/projectile -> BattleManager/Bullet -> CharResourceResolver -> StateManager/CharBlackBoard`
- Presentation:
  `CharAnimCtrl` handles body animator state
  `WeaponAnimCtrl` handles weapon prefab animator state

## Supported Basic Attack Modes
- `MeleeRepeat`
- `MeleeCombo`
- `RangedStraight`
- `RangedHoming`
- `RangedChargeRelease`

## What Is Stable Now
- `MeleeRepeat`
  - cadence is attack-speed driven
  - body repeat speed can drive Animator `meleeSpeed`
  - can keep moving during allowed windows
  - moving repeat can suppress locomotion animation and keep pure displacement
  - facing is maintained by the melee-aim state instead of per-hit force-face spam
  - long hold and rapid taps are unified into one repeat intent
- `MeleeCombo`
  - combo trigger path is separated from repeat
  - combo can now share the same melee aim ownership model
  - moving combo can also suppress locomotion animation and keep pure displacement
  - combo chain can preserve a short mouse-facing grace window between taps
- `Ranged`
  - melee weapons no longer incorrectly spawn ranged projectiles in the intended path
  - projectile aim point uses target center instead of feet/root
  - homing projectile close-range orbit behavior was tightened
- Data / bridge layer
  - `AttackData_SO` now owns the main basic attack mode selection
  - `StateManager` now restores runtime attack data more safely when equipping weapons
  - `CharResourceResolver` can read attack data through the current runtime owner path
  - weapon ownership is bound onto runtime weapon instances for hit resolution

## Important Behavior Rules
- `WeaponType` is no longer the main authority for attack behavior. `AttackData_SO.basicAttackMode` is.
- `MeleeRepeat` should be treated as sustained basic attack logic.
- `MeleeCombo` should be treated as tap-chain logic.
- `MeleeRepeat` facing and `MeleeCombo` facing are currently mouse-driven in the PC path.
- Repeat and combo movement can intentionally ignore locomotion animation while still allowing displacement.

## Why It Is Usable For Development
- The main gameplay loop is coherent now:
  - input
  - state gate
  - action occupancy
  - facing
  - animation trigger
  - hit / projectile
  - damage application
- The biggest previously visible bugs in normal attack feel have already been addressed:
  - melee/ranged mode confusion
  - projectile miss-at-feet
  - homing orbiting
  - repeat attack speed not affecting body playback
  - repeat/combo facing ownership instability
  - movement + attack animation fighting each other

## Remaining Risks Before "Production Complete"
- `MeleeCombo` timing is still duration/request-driven, not animation-event-driven.
  - File:
    `Assets/Scripts/New ActionRPG Ctrl/Character/CharWeaponCtrl.cs`
  - Current code still starts combo actions with `_lightAtkDur`, not per-segment authored animation timing.
- Input abstraction is still mouse-first.
  - The current melee aim logic is correct for the current mouse-controlled build.
  - Gamepad support still needs a dedicated aim-source abstraction.
- No automated verification exists yet.
  - There are no playmode/unit tests covering attack mode switching, equip swaps, combo chain resets, or projectile targeting.
- Full scene validation is still required.
  - This has not been verified across all player prefabs, enemies, and weapon assets in play mode.

## Recommended Next Steps
- Keep using the current system for gameplay iteration now.
- Next, finish these in order:
  - move `MeleeCombo` timing authority toward animation events
  - abstract melee aim source so mouse and future gamepad paths do not fork the combat logic
  - run focused play tests on weapon swap, attack data swap, enemy targeting, and hit confirmation

## Files To Watch
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharWeaponCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharAnimCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharBasicAttackTargeting.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharResourceResolver.cs`
- `Assets/Scripts/Combat States/AttackData_SO.cs`
- `Assets/Scripts/Manager/StateManager.cs`
- `Assets/Scripts/Manager/BattleManager.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Bullet.cs`

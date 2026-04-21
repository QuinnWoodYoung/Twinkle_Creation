# M Game Combat Systems

更新日期: `2026-04-20`

本文档用于给后续开发和 AI 修改提供统一入口。当前包含两部分:
- 普攻系统现状
- 技能系统现状与补充清单

---

## Basic Attack System

### Verdict
- Current normal attack system is ready to use as the gameplay baseline for active development.
- It is not yet "final production complete". It still needs targeted follow-up on animation-event authority, input abstraction, and validation.

### Current Runtime Path
- Input:
  `CharSignalReader -> CharParam.AttackState -> CharWeaponCtrl`
- Execution:
  `CharWeaponCtrl -> CharActionCtrl -> targeting/projectile -> BattleManager/Bullet -> CharResourceResolver -> StateManager/CharBlackBoard`
- Presentation:
  `CharAnimCtrl` handles body animator state
  `WeaponAnimCtrl` handles weapon prefab animator state

### Supported Basic Attack Modes
- `MeleeRepeat`
- `MeleeCombo`
- `RangedStraight`
- `RangedHoming`
- `RangedChargeRelease`

### What Is Stable Now
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

### Important Behavior Rules
- `WeaponType` is no longer the main authority for attack behavior. `AttackData_SO.basicAttackMode` is.
- `MeleeRepeat` should be treated as sustained basic attack logic.
- `MeleeCombo` should be treated as tap-chain logic.
- `MeleeRepeat` facing and `MeleeCombo` facing are currently mouse-driven in the PC path.
- Repeat and combo movement can intentionally ignore locomotion animation while still allowing displacement.

### Why It Is Usable For Development
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

### Remaining Risks Before "Production Complete"
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

### Recommended Next Steps
- Keep using the current system for gameplay iteration now.
- Next, finish these in order:
  - move `MeleeCombo` timing authority toward animation events
  - abstract melee aim source so mouse and future gamepad paths do not fork the combat logic
  - run focused play tests on weapon swap, attack data swap, enemy targeting, and hit confirmation

### Files To Watch
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharWeaponCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharAnimCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharBasicAttackTargeting.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharResourceResolver.cs`
- `Assets/Scripts/Combat States/AttackData_SO.cs`
- `Assets/Scripts/Manager/StateManager.cs`
- `Assets/Scripts/Manager/BattleManager.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Bullet.cs`

---

## Skill System

### Verdict
- 当前技能系统已经能支撑常见 ARPG / Dota 风格技能迭代。
- 它不是“很完善”的最终版，但已经从“立即放技能”升级成“有施法阶段、释放点、引导态、黑板同步、动画桥接”的可扩展版本。

### Current Runtime Path
- Input:
  `CharSignalReader / CharParam.SkillInputDown -> CharSkillCtrl`
- Targeting:
  `TargetingUtil.Collect -> CastContext`
- Validation:
  `SkillData.CanActivate`
- Execution:
  `CharSkillCtrl -> SkillData.Activate -> SkillEffectUtility.ExecuteEffects`
- Action / animation bridge:
  `CharSkillCtrl -> CharActionCtrl -> CharAnimCtrl`
- Runtime sync:
  `CharSkillCtrl -> CharBlackBoard.Skills`

### Supported Skill Authoring Modes
- `Instant`
  - 技能通过校验后立即生效
  - 可选播放一次施法动画
- `CastPointRelease`
  - 先进入前摇
  - 前摇结束后才真正执行 `effects`
- `Channel`
  - 先进入前摇
  - 前摇结束时执行一次主技能效果
  - 之后进入引导态
  - 引导期间周期执行 `channelTickEffects`
  - 引导结束或被打断时执行 `channelEndEffects`

### Supported Skill Data Fields
- `castAnimType`
  - `None / Cast / Shoot / Buff / Dash`
- `castAnimDur`
  - 施法前摇时长
  - 受 `castSpeed` 影响
- `castFlowType`
  - `Instant / CastPointRelease / Channel`
- `channelDuration`
  - 引导总时长
- `channelTickInterval`
  - 引导周期触发间隔
- `triggerChannelTickImmediately`
  - 进入引导时是否立刻触发一次 tick
- `lockMoveOnCastAnim`
- `lockRotateOnCastAnim`
- `lockMoveDuringChannel`
- `lockRotateDuringChannel`
- `requireFacingBeforeCast`
- `castFacingAngleTolerance`
- `effects`
- `channelTickEffects`
- `channelEndEffects`

### What Was Added In This Round
- `SkillData`
  - 新增 `SkillCastFlowType`
  - 新增引导相关配置字段
  - 支持区分即时施法、前摇后释放、前摇后引导
- `CharSkillCtrl`
  - 增加“先转向再施法”的挂起阶段
  - 增加 cast point release 阶段
  - 增加 channel 生命周期管理
  - 增加周期 tick 与结束效果
  - 增加动作结束 / 打断钩子
- `CharAnimCtrl`
  - 增加 `Channeling` Bool 支持
  - 修复 `Cast` 触发器为空时不播动画的问题
  - 当 Inspector 里的 `_castTrig / _shootCastTrig / _buffCastTrig / _dashCastTrig / _atkTrig` 留空时，会回退到默认名字
- `CharBlackBoard`
  - `Skills` 切片新增:
    - `pendingCast`
    - `pendingSlot`
    - `channeling`
    - `channelSlot`
    - `channelRemain`

### Animation Rules
- 普通技能推荐:
  - `Cast` 触发一次
  - 技能在 cast point 或即时点释放
  - 不要用重复触发 `Cast` 的方式模拟持续施法
- 引导技能推荐:
  - 进入技能时触发一次 `Cast`
  - 引导期间由 Animator 的 `Channeling` Bool 驱动循环动画
  - 引导结束后退出循环
- 当前实现是“时间驱动释放”，不是“动画事件驱动释放”。
  - `castAnimDur` 是当前释放时机主权
  - 如果以后要做到更精细的镜头点、抬手点、发射点，建议追加 animation event 或状态机回调

### Cast Animation Issue Already Fixed
- 问题现象:
  - `Mei` / `DP` / `Test Room` 中有技能使用 `Cast`
  - 但角色不播 `Cast` 动画
- 根因:
  - `CharAnimCtrl` 的 `_castTrig` 在场景 / prefab 上是空字符串
  - 但 Animator Controller 内实际存在 `Cast` Trigger
- 当前处理:
  - 代码已增加默认回退
  - 即使 Inspector 不填，也会尝试触发 `Cast`
- 结论:
  - 现在这类角色可以直接使用 `castAnimType = Cast`
  - 但更稳妥的做法仍然是后续把 prefab / scene 上的 trigger 名补齐

### What Skills The Current System Can Already Make
- 单体指向伤害
- 点地范围伤害
- 自身 Buff
- Dash / 位移后接效果
- 前摇后落地生效技能
- 持续引导区域技能
- 引导期间持续伤害 / 减速 / VFX
- 引导结束爆发
- 一次起手 VFX + 持续区域效果 + 结束爆发 VFX

### What It Still Cannot Express Cleanly Yet
- 通用吸附 / 拉扯 / 黑洞
- 更复杂的强制位移链条
- 条件分支型技能效果
- 公式化伤害缩放节点
- 独立 Aura Actor 或持续技能实体
- 基于动画事件的精准发射点释放
- 更完善的编辑器校验与可视化 authoring

### Sample Skill Added: 剧变
- 这是一个“类剧变”的引导技能样例
- 目录:
  - `Assets/Scripts/New ActionRPG Ctrl/Skills/Effects/Skills/剧变/`
- 关键资源:
  - `剧变.asset`
  - `剧变范围.asset`
  - `剧变减速.asset`
  - `剧变起始VFX.asset`
  - `剧变结束VFX.asset`
- 当前结构:
  - 施法开始后进入前摇
  - 前摇结束时释放范围效果
  - 进入引导
  - 引导期间执行 tick
  - 引导结束时播放结束 VFX

### How To Author A New Channel Skill
- 在 `SkillData` 上设置:
  - `castFlowType = Channel`
  - `castAnimType = Cast` 或其他对应动画类型
  - `castAnimDur = 前摇时长`
  - `channelDuration = 引导总时长`
  - `channelTickInterval = tick 间隔`
- 把“起手时就生效”的效果放进 `effects`
- 把“引导期间周期执行”的效果放进 `channelTickEffects`
- 把“结束或被打断时执行”的效果放进 `channelEndEffects`
- Animator 侧建议至少准备:
  - `Cast` Trigger
  - `Channeling` Bool
  - `Cast_Start -> Channel_Loop -> Exit` 的状态切换

### Skill System Completion Checklist
- 已完成
  - `SkillData` 支持 `Instant / CastPointRelease / Channel`
  - 技能释放支持先转向再释放
  - `castAnimDur` 受施法速度影响
  - 引导技能支持独立 tick 与 end 效果
  - 引导状态同步到黑板
  - 动画支持 `Channeling` Bool
  - `Cast` Trigger 空配置回退修复
  - 已落一个 `剧变` 示例并挂上 VFX
- 建议下一步
  - 增加通用 Pull / Knockback / ForcedMove 效果节点
  - 增加 animation event 版释放时机
  - 增加公式化数值节点
  - 增加技能实体 / 持续区域 Actor
  - 增加技能 authoring 校验，避免空配和错误组合
  - 做至少一轮 Unity 场景实测，覆盖打断、死亡、中断后 VFX 清理

### Files To Watch
- `Assets/Scripts/New ActionRPG Ctrl/Skills/SkillData.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharSkillCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharAnimCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/Core/CharBlackBoard.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Skills/Effects/AoeEffect.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Skills/Effects/ApplyBuffEffect.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Skills/Effects/PlayVfxEffect.cs`

### Notes For Future AI

### Aim Preview
- Purpose:
  - Show authored skill area before release for normal-cast skills.
  - Keep the preview attached to ground height by sampling terrain/collider height.
  - Only player-controlled casting should show this preview. AI should not show it.
- Main fields:
  - `useAimPreview`
    - `false`: quick cast. Keep old behavior and cast immediately.
    - `true`: normal cast. Press skill key to enter preview, left click to confirm, right click to cancel.
  - `previewShape`
    - `Auto / Circle / Rectangle / Sector`
    - `Auto` can infer from common effect chains, but manual shape is safer for production assets.
  - `previewAnchor`
    - `Auto / TargetPoint / Caster`
    - `TargetPoint`: preview is centered or built around the final target point.
    - `Caster`: preview starts from or is centered on the caster side, useful for sectors and some forward rectangles.
  - `previewRadius`
    - Circle radius.
    - Sector radius.
  - `previewLength`
    - Rectangle forward length.
  - `previewWidth`
    - Rectangle width.
  - `previewAngle`
    - Sector angle in degrees.
  - `showCastRangeIndicator`
    - Show the max cast range ring around the caster.
  - `previewValidColor`
    - Color used when the target point is valid.
  - `previewInvalidColor`
    - Color used when the target point is invalid or out of cast range.
- Related cast rule fields:
  - `maxCastRange`
    - Maximum allowed cast distance.
  - `targetingMode`
    - Decides whether the skill is effectively self, unit, point, or directional in the cast pipeline.
  - `clampToMaxRange`
    - `true`: target point is clamped to the max range edge.
    - `false`: preview can move beyond range and turn invalid color.

### Skill Authoring Rules
- Quick cast skill:
  - Set `useAimPreview = false`.
  - Runtime stays close to the old instant/quick workflow.
- Normal circle skill:
  - Set `useAimPreview = true`.
  - Set `previewShape = Circle`.
  - Fill `previewRadius`.
  - Usually use `previewAnchor = TargetPoint`.
- Normal sector skill:
  - Set `useAimPreview = true`.
  - Set `previewShape = Sector`.
  - Fill `previewRadius` and `previewAngle`.
  - Usually use `previewAnchor = Caster`.
- Normal rectangle skill:
  - Set `useAimPreview = true`.
  - Set `previewShape = Rectangle`.
  - Fill `previewLength` and `previewWidth`.
  - `previewAnchor` depends on whether the box should be centered on target or projected from caster.
- Range behavior:
  - If the design should stop at max range, set `clampToMaxRange = true`.
  - If the design should allow over-aim and show invalid state, set `clampToMaxRange = false`.
- Production recommendation:
  - Prefer explicit preview values over `Auto` when the skill is important, special-cased, or likely to evolve.
  - Treat preview as authored combat UX data, not just as a derived debug helper.

### Input Constraint
- Current aim preview interaction is mouse-first.
  - Aim position currently follows the existing screen-space targeting path.
  - Confirm is currently left click.
  - Cancel is currently right click.
- `InputMap` exists, but preview confirm/cancel and aim semantics are not yet fully abstracted into an input-agnostic layer.
- Future gamepad support is mandatory.
  - Do not design future combat input features around permanent mouse assumptions.
  - The exact gamepad aiming / confirm / cancel scheme will be defined later by the user.
  - Any future preview/input refactor should preserve current skill runtime behavior while replacing only the input source and preview interaction layer.

---

## 2026-04-20 Gamepad Control Status

### Scope Of This Round
- Goal:
  keep the existing mouse path unchanged, and add a separate gamepad path for:
  - movement
  - normal attack
  - lock-on / lock switching
  - hold-preview-release skill casting
- Current mode selection is intentionally simple:
  - `PlayerInputManager.useGamepadInput`
  - no automatic device switching
  - no mixed mouse/gamepad arbitration logic

### What Was Changed Today
- `PlayerInputManager`
  - added manual `useGamepadInput` mode selection
  - non-selected device input is ignored
  - when gamepad mode is enabled, the input asset is restricted to `Gamepad` devices so mouse position does not keep stealing the shared `Aim` action
  - in gamepad mode, `playerInputAimValue` and `GamepadAimStick` now represent raw right-stick input, not a virtual cursor position
- `CharBasicAttackTargeting`
  - in gamepad mode, normal attack direction falls back to character facing or locked target
  - this matches the intended rule: normal attack direction is not driven by a virtual cursor
- `CharAimCtrl`
  - gamepad can use right-stick press to lock
  - while locked, pushing right stick toward another direction can switch to another nearby target
- `CharSkillCtrl`
  - gamepad skill flow is hold-to-preview / release-to-cast
  - release skill button while still holding modifier: cast
  - release modifier first: cancel
- `TargetingUtil`
  - in gamepad mode, skill targeting no longer uses mouse screen ray
  - it resolves target direction from right stick and tries:
    directional unit -> ground point -> forward fallback point

### Current Verdict
- The gamepad path is now partially usable.
- It is not production ready yet.
- Movement and basic attack are in a workable state.
- Lock-on and skill casting exist, but the overall feel is still rough and inconsistent compared with the mouse path.

### Known Problems
- There is still no true gamepad cursor model.
  - Current gamepad aim is only a direction vector from the right stick.
  - This is enough for rough directional casting, but not enough for precise cursor-style skills.
- Cursor-oriented skills are still approximate.
  - `TargetingUtil` currently probes a unit or ground point along a direction.
  - This is not the same as a real screen/world cursor, so placement precision and player expectation can drift.
- Gamepad preview currently has direction but weak distance control.
  - The player can adjust direction with the right stick.
  - The current path does not expose a strong "push farther / pull nearer" cursor-distance interaction.
- Lock switching is functional but still heuristic.
  - The current switch rule is based on nearby candidates, world direction, angle threshold, and range threshold.
  - It may still feel jumpy or choose a target that is technically valid but not the one the player expects.
- The system now has two aim semantics.
  - Mouse mode: `AimTarget` is screen position.
  - Gamepad mode: `AimTarget` is effectively raw stick input, while real skill targeting is redirected through the gamepad path.
  - This works, but any older code that assumes `AimTarget` is always screen-space should be treated carefully.
- Mode selection is static, not a polished runtime feature.
  - `useGamepadInput` is a manual boolean.
  - This is correct for the current debugging phase, but it is not a final shipping UX.
- Runtime validation is still missing.
  - No final Unity playtest pass has confirmed the whole chain across all skills, player prefabs, enemy cases, and scene setups.

### Why It Still Feels "Barely Usable"
- The mouse path is cursor-first and naturally precise.
- The current gamepad path is direction-first and still relies on fallback heuristics.
- For an ARPG with cursor-oriented skills, this gap matters a lot.
- So the gamepad path is no longer blocked at the input level, but it still lacks the final layer of aiming UX polish.

### Recommended Next Steps
- Decide whether the long-term design should use:
  - directional casting only
  - or a real virtual cursor driven by right stick
- If the game should support many cursor-sensitive skills, a real virtual cursor is the cleaner long-term answer.
- If the game should feel closer to console ARPG lock-and-direction combat, keep the directional model but add:
  - better target scoring
  - better stick deadzone / response tuning
  - explicit distance rules for ground-target skills
- Before any broader refactor, run a focused Unity playtest on:
  - unlocked normal attack
  - locked normal attack
  - right-stick target switching
  - hold-preview-release skills on all four skill slots
  - cancel by releasing modifier
- 目前技能释放时机主权在 `CharSkillCtrl + castAnimDur`，不是动画事件。
- 引导技能不要重复打 `Cast` Trigger，应使用 `Channeling` Bool 驱动循环动画。
- `Mei / DP / Test Room` 的 `Cast` 动画问题，当前已用代码回退修复，但场景配置层仍建议补齐。
- 项目里存在一个遗留临时文件:
  - `Assets/Scripts/New ActionRPG Ctrl/Character/CharSkillCtrl.cs.codex_new`
  - Unity 通常会忽略它，但后续应择机清理。
- 当前未完成 Unity 运行态验证，以上结论主要基于代码和资源静态检查。
---

## 2026-04-21 Dodge System Status

### Scope Of This Round
- Goal:
  finish the dodge interaction layer before invulnerability-frame work.
- Implemented now:
  - directional dodge displacement
  - forward-only dodge animation trigger
  - dodge VFX playback
  - dodge attack input buffering
  - post-dodge attack hook window
  - bow charge-release on dodge
  - fallback body animation layer / locomotion bool when weapon presentation config is missing

### Current Dodge Rules
- Input:
  `CharSignalReader -> CharParam.Dodge -> CharCtrl`
- Direction:
  - if locomotion input exists, dodge along that direction
  - if locomotion input is empty, dodge along current facing
- Runtime:
  - dodge is no longer only a temporary move-speed multiplier
  - `CharCtrl` now drives a short locked displacement using `_dodgeDistance` and `_dodgeDuration`
- Animation:
  - only forward dodge plays body dodge animation
  - dodge uses a dedicated trigger path and no longer reuses the skill `Dash` trigger
  - default dodge trigger name is `DodgeAction`
- VFX:
  - all dodge directions can spawn dodge VFX
  - VFX can be attached to the character and rotated to the actual dodge direction

### Files Touched For Dodge
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharAnimCtrl.cs`
- `Assets/Scripts/New ActionRPG Ctrl/Character/CharWeaponCtrl.cs`
- `Assets/Scripts/Combat States/AttackData_SO.cs`

### Dodge Attack Interaction
- Current built-in basic attack flow now buffers attack input during self dodge.
- When dodge ends, buffered attack is retried immediately.
- After dodge ends, a short `dodgeAttackWindow` can mark the next basic attack as a dodge follow-up attack.
- This currently covers the built-in basic attack flow:
  - `MeleeCombo`
  - `RangedStraight`
  - `RangedHoming`
  - `RangedChargeRelease`
- This is not a blanket guarantee for all future custom weapon logic or skill logic.

### Bow Special Rule
- For `RangedChargeRelease` bows:
  - if the bow is charging and dodge is pressed
  - and `AttackData_SO.releaseChargeAttackOnDodge == true`
  - the current charged shot is released first, then dodge starts

### Post-Dodge Attack Hook
- `AttackData_SO` now exposes:
  - `dodgeAttackWindow`
  - `dodgeAttackDamageMultiplier`
  - `dodgeAttackDamageBonus`
- Current intent:
  - these are extension hooks only
  - current runtime stores them in `CharWeaponCtrl.DodgeAttackContext`
  - current runtime does not force a generic damage formula from them
- Runtime bridge:
  - `CharWeaponCtrl.LastAttackContext`
  - `CharWeaponCtrl.DodgeAttackStarted`

### Weapon Presentation Fallback
- `CharAnimCtrl` now supports inspector fallback fields:
  - `_defaultWeaponLayerName`
  - `_defaultLocomotionBoolName`
- Fallback is used when:
  - no weapon is equipped
  - no binding exists for the current weapon type
  - configured layer does not exist in the animator controller
  - configured locomotion bool does not exist in the animator controller

### Ranged Attack Speed Notes
- Practical cadence priority for ranged basic attack is:
  1. `AttackData_SO.coolDown`
  2. `AttackData_SO.rangedAttackSpeed`
  3. fallback to attack duration
- Runtime path:
  - `CharWeaponCtrl.BeginAttackCooldown()`
  - `CharResourceResolver.GetAttackCooldown()`
  - `CharResourceResolver.GetRangedAttackSpeed()`
- Authoring rule:
  - if `coolDown > 0`, it overrides ranged cadence directly
  - if `coolDown == 0`, cadence falls back to `1 / rangedAttackSpeed`
- Override path:
  - `CharBlackBoardInitializer` can override runtime ranged speed and cooldown through:
    - `_rangedAttackSpeed`
    - `_attackCooldown`

### Validation Still Needed
- No Unity playtest validation has been run yet for:
  - dodge into melee combo
  - dodge into ranged repeat
  - dodge into homing ranged attack
  - bow charge then dodge release
  - dodge follow-up attack timing feel

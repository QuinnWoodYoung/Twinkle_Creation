# 新角色控制器中文总览

## 先看整体

这套“新角色控制器”的核心思想不是把所有逻辑塞进一个大脚本里，而是拆成两层：

1. `CharBlackBoard`
   这是单个角色的运行时共享数据中心。
   所有系统最后都会把自己的结果写回这里，其他系统优先从这里读。

2. 一组职责明确的控制器
   它们各自只管一块事情，比如输入、移动、动作、状态、动画、武器、技能。

最关键的链路可以按下面顺序理解：

`CharSignalReader -> CharCtrl -> CharActionCtrl / CharStatusCtrl / CharWeaponCtrl / CharSkillCtrl -> CharAnimCtrl`

## 推荐阅读顺序

如果你要从头看懂，建议按这个顺序读：

1. `Core/CharBlackBoard.cs`
   先理解黑板有哪些数据切片。

2. `Core/CharBlackBoardInitializer.cs`
   看角色出生时这些数据是怎么初始化进去的。

3. `CharSignalReader.cs`
   看玩家输入/AI 输入怎样统一写入 `CharParam`。

4. `CharCtrl.cs`
   看角色怎么移动、怎么转向、什么时候不能动。

5. `Core/CharActionReq.cs` 和 `Core/CharActionCtrl.cs`
   看攻击/施法这种“动作请求”怎样被统一管理。

6. `Core/CharStatusCtrl.cs` 和 `Core/CharStateSnap.cs`
   看状态系统怎样把 buff/debuff 折叠成一份统一快照。

7. `CharAnimCtrl.cs`
   看身体 Animator 怎么根据动作和状态切表现。

8. `CharWeaponCtrl.cs`
   看普通攻击、武器动画、投射物、攻击特效。

9. `CharSkillCtrl.cs`
   看技能输入、冷却、目标收集、等待转向、真正施法。

10. `Core/CharBasicAttackTargeting.cs`
    看普攻到底是如何选目标、算方向的。

## 黑板切片在干什么

`CharBlackBoard` 里最重要的是这些切片：

- `Identity`
  角色身份信息，比如 `runtimeId`、`unitId`、队伍、是否玩家控制。

- `TransformState`
  当前世界位置和朝向。

- `Motion`
  输入方向、移动向量、速度、基础移速、基础转速、是否能移动/旋转。

- `Action`
  当前动作状态，比如是否攻击中、施法中、死亡、是否等待转向。

- `Resources`
  HP / Energy 这类资源。

- `Combat`
  攻击模板、伤害、攻速、射程、冷却、施法速度等战斗参数。

- `Status`
  运行时状态列表，以及折叠后的状态快照。

- `Skills`
  技能栏、技能冷却、是否处于 pending cast。

- `Equipment`
  当前武器类型、武器根节点。

- `Targeting`
  锁定目标、当前攻击目标、瞄准点。

## 每个控制器的职责

### `CharSignalReader`

输入桥接层。

- 它不直接做移动或攻击。
- 它只负责把“玩家输入”或者“AI 输入”整理成统一格式。
- 最终写入 `CharCtrl.Param`。

可以把它理解成“输入翻译器”。

### `CharCtrl`

角色本体控制器。

- 负责读取 `CharParam` 里的输入。
- 计算移动方向、重力、闪避。
- 检查当前是否允许移动或转向。
- 负责普通转向、强制转向、技能转向。
- 最后把运动信息同步给 `CharAnimCtrl` 和 `CharBlackBoard`。

这份脚本是“角色身体怎么动”的核心。

### `CharActionCtrl`

动作闸门。

- 攻击、施法、受击这类动作都先经过它。
- 它会判断动作是否允许开始。
- 它可以在动作期间锁移动、锁转向。
- 支持 `waitFace`，也就是先转过去再正式开始动作。
- 动作状态会同步给状态系统和黑板。

它不是复杂动作树，更像一个统一的“动作占用和锁定系统”。

### `CharStatusCtrl`

状态系统权威入口。

- 保存当前所有运行时状态。
- 处理免疫、互斥组、叠层、刷新持续时间、驱散、受伤打断。
- 把原始状态列表折叠成 `CharStateSnap`。

其他系统一般不直接遍历状态列表，而是直接读取 `Snap`。

### `CharStateSnap`

这是状态系统给外部看的最终结果。

里面会直接给出：

- `canMove`
- `canAtk`
- `canCast`
- `canRotate`
- `moveSpdMul`
- `atkSpdMul`
- `castSpdMul`
- `dmgTakenMul`
- `tags`
- `restricts`

也就是说，外部系统不需要知道“到底有哪几个 buff”，只需要知道“最终结论是什么”。

### `CharAnimCtrl`

身体动画控制器。

- 它只管角色身体 Animator。
- 负责移动参数、死亡布尔、攻击/施法/受击触发器。
- 会根据武器类型切身体动画层。
- 会根据状态快照切 Stun / Sleep / Root / Silence 这类表现层布尔。

注意：它不负责武器 prefab 上自己的动画，那部分交给 `WeaponAnimCtrl`。

### `CharWeaponCtrl`

武器与普通攻击控制器。

- 管当前武器类型。
- 处理普攻输入。
- 解析普攻模式：近战连段、远程直射、远程追踪、蓄力释放。
- 调目标选择工具决定这一下打谁。
- 播放武器动画。
- 生成攻击特效。
- 如果是远程，就发投射物。

这份脚本是“普通攻击系统”的核心。

### `CharSkillCtrl`

技能控制器。

- 管技能栏和冷却。
- 读技能输入。
- 调目标系统收集目标。
- 检查能量、冷却、状态限制。
- 对需要先转向的技能，先进入 pending cast。
- 最后调用 `SkillData.Activate(context)` 真正施法。

可以理解为“技能释放流程调度器”。

## 普攻目标怎么选

普攻目标不是直接拿最近敌人，而是走 `CharBasicAttackTargeting`：

1. 先看当前有没有锁定目标
2. 再看玩家当前输入想朝哪打
3. 在这个方向附近做有限辅助瞄准
4. 输出：
   - `targetUnit`
   - `attackPoint`
   - `attackDirection`

所以这套逻辑更接近：

- 既保留玩家意图
- 又允许一定程度的软锁敌辅助

## 这套代码为什么看起来“分得很散”

因为它在刻意做“职责拆分”：

- `CharCtrl` 不直接决定状态
- `CharStatusCtrl` 不直接播放动画
- `CharAnimCtrl` 不直接处理输入
- `CharWeaponCtrl` 不直接保存统一角色数据

它们都通过黑板协作。

这样做的好处是：

- 每个系统职责更单一
- 调试时更容易定位是哪一层出了问题
- 以后做 AI、联网、UI 读取时，统一从黑板取数据会更方便

## 你后续读代码时可以抓的主线

如果你想从“发生了一次普攻”来理解：

1. `CharSignalReader` 把攻击输入写进 `CharParam`
2. `CharWeaponCtrl` 读取攻击输入
3. `CharWeaponCtrl` 发起 `CharActionReq`
4. `CharActionCtrl` 判断能不能开始攻击动作
5. 攻击动作开始后，`CharAnimCtrl` 和 `WeaponAnimCtrl` 播放动画
6. `CharWeaponCtrl` 调目标解析，生成 VFX / 投射物
7. 结果同步回 `CharBlackBoard`

如果你想从“施放一次技能”来理解：

1. `CharSignalReader` 把技能按键写进 `CharParam`
2. `CharSkillCtrl` 读取对应技能输入
3. 检查冷却、蓝量、目标是否合法
4. 如果要求先转向，就先进入 pending cast
5. `CharActionCtrl` / `CharCtrl` 完成转向
6. `CharSkillCtrl` 调 `SkillData.Activate`
7. 扣蓝、进冷却、同步黑板

## 最后给你的一个简单判断法

以后你遇到问题时，可以先判断它属于哪层：

- 角色动不了/转不过去：先看 `CharCtrl` 和 `CharRuntimeResolver`
- 攻击/施法起不来：先看 `CharActionCtrl`
- 被沉默/眩晕后行为不对：先看 `CharStatusCtrl`
- 身体动画不对：看 `CharAnimCtrl`
- 武器不打、子弹不出：看 `CharWeaponCtrl`
- 技能按了没反应：看 `CharSkillCtrl`
- 数据到底现在是什么：看 `CharBlackBoard`

如果你愿意，我下一步可以继续做两件事中的一种：

1. 我把 `CharCtrl`、`CharWeaponCtrl`、`CharSkillCtrl` 三份脚本再继续细化成“逐段中文讲解版”
2. 我直接按“普攻流程”或“技能流程”给你画一份更细的调用链说明

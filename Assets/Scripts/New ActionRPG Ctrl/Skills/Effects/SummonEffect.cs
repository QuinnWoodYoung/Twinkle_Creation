// using UnityEngine;
//
// /// <summary>
// /// 召唤效果：在命中点周围生成单位。
// /// </summary>
// [CreateAssetMenu(fileName = "New Summon Effect", menuName = "SkillSystem/Effects/Summon")]
// public class SummonEffect : SkillEffect
// {
//     [Header("召唤参数")]
//     public GameObject summonPrefab;
//
//     [Tooltip("召唤单位的数量")]
//     [Min(1)]
//     public int summonCount = 1;
//
//     [Tooltip("围绕中心点生成的半径")]
//     public float spawnRadius = 2f;
//
//     public override void Apply(CastContext context)
//     {
//         if (summonPrefab == null)
//         {
//             Debug.LogError($"{this.name}: summonPrefab 未设置！");
//             return;
//         }
//
//         // --- 逻辑升级：使用 hitPoint 作为中心 ---
//         // 如果是无目标技能，hitPoint 是施法者位置。
//         // 如果是点地技能，hitPoint 就是玩家点的那个坑。
//         Vector3 center = context.hitPoint;
//
//         for (int i = 0; i < summonCount; i++)
//         {
//             Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
//             Vector3 spawnPosition = center + new Vector3(randomCircle.x, 0, randomCircle.y);
//             
//             // 实例化召唤物
//             GameObject summoned = Instantiate(summonPrefab, spawnPosition, Quaternion.identity);
//             
//             // DotA2 进阶：通常需要让召唤物知道谁是它的主人
//             // if (summoned.TryGetComponent<BaseUnit>(out var unit)) unit.SetOwner(context.caster);
//         }
//
//         Debug.Log($"在 {center} 附近召唤了 {summonCount} 个单位。");
//     }
// }
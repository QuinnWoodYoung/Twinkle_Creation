using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能效果运行时宿主。
///
/// ScriptableObject 自己不能直接启动协程，
/// 所以 DelayEffect、RepeatEffect 这类异步效果，需要把协程挂到某个 MonoBehaviour 上运行。
/// 这个组件就是专门干这个的。
/// </summary>
public sealed class SkillEffectRuntime : MonoBehaviour
{
    // 以 GameObject 为 key 做缓存，保证同一个角色只维护一个运行时组件。
    private static readonly Dictionary<GameObject, SkillEffectRuntime> Cache = new Dictionary<GameObject, SkillEffectRuntime>();

    /// <summary>
    /// 获取某个对象对应的 SkillEffectRuntime。
    /// 如果没有，就自动挂一个上去。
    /// </summary>
    public static SkillEffectRuntime Get(GameObject owner)
    {
        if (owner == null)
        {
            return null;
        }

        if (Cache.TryGetValue(owner, out SkillEffectRuntime runtime) && runtime != null)
        {
            return runtime;
        }

        runtime = owner.GetComponent<SkillEffectRuntime>();
        if (runtime == null)
        {
            runtime = owner.AddComponent<SkillEffectRuntime>();
        }

        Cache[owner] = runtime;
        return runtime;
    }

    /// <summary>
    /// 运行一个协程。
    /// </summary>
    public void Run(IEnumerator routine)
    {
        if (routine != null)
        {
            StartCoroutine(routine);
        }
    }

    private void OnDestroy()
    {
        Cache.Remove(gameObject);
    }
}

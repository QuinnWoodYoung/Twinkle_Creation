using UnityEngine;

/// <summary>
/// 控制目标指示器的显示和位置，使其跟随指定的目标。
/// </summary>
public class TargetIndicator : MonoBehaviour
{
    [Tooltip("指示器跟随的目标Transform。")]
    public Transform target;

    [Tooltip("指示器相对于目标中心的Y轴偏移，通常是目标的高度一半以下。")]
    public float yOffsetFromTarget = 0.05f; // Small offset to avoid Z-fighting with ground

    [Tooltip("如果设置为true，指示器会跟随目标的旋转。")]
    public bool followTargetRotation = false;

    // ========== MODIFICATION START | 2026年2月6日 ==========
    // 缓存子对象上的Renderer组件
    private Renderer _targetRenderer;
    // ========== MODIFICATION END | 2026年2月6日 ==========

    private void Awake()
    {
        // ========== MODIFICATION START | 2026年2月6日 ==========
        // 在Awake中获取子对象上的Renderer
        _targetRenderer = GetComponentInChildren<Renderer>();
        if (_targetRenderer == null)
        {
            //Debug.LogError("TargetIndicator: 无法在子对象上找到 Renderer 组件！请确保 TargetIndicatorVisual 预制体内部包含了实际的渲染网格。", this);
        }
        // ========== MODIFICATION END | 2026年2月6日 ==========
    }

    private void LateUpdate()
    {
        // 只有当有目标时才更新位置
        if (target != null)
        {
            // 将指示器放置在目标的位置，但Y轴使用预设偏移
            Vector3 newPosition = target.position;
            newPosition.y += yOffsetFromTarget;
            transform.position = newPosition;

                        // 根据设置决定是否跟随旋转

                        if (followTargetRotation)

                        {

                            transform.rotation = target.rotation;

                        }

                        // else: transform.rotation remains its original rotation or world identity

                        // ========== MODIFICATION START | 2026年2月6日 ==========

                        // Debugging: Confirm visibility and material state

                        if (_targetRenderer != null && _targetRenderer.enabled && _targetRenderer.sharedMaterial != null)

                        {

                            Color matColor = _targetRenderer.sharedMaterial.color;

                            if (gameObject.activeInHierarchy)

                            {

                                //Debug.Log($"Target Indicator active. Pos: {transform.position}, Mat Alpha: {matColor.a}, Mat Name: {_targetRenderer.sharedMaterial.name}, Renderer Enabled: {_targetRenderer.enabled}");

                            }

                            else

                            {

                                //Debug.LogWarning($"Target Indicator is not active in hierarchy despite having a target. Current state: {gameObject.activeSelf}");

                            }

                        }

                        else if (_targetRenderer == null)

                        {

                            // This case should be caught by Awake, but kept for robustness

                            //Debug.LogWarning("Target Indicator: _targetRenderer is null in LateUpdate!");

                        }

                        else if (!_targetRenderer.enabled)

                        {

                            //Debug.LogWarning("Target Indicator: Renderer component is disabled!");

                        }

                        else if (_targetRenderer.sharedMaterial == null)

                        {

                            //Debug.LogWarning("Target Indicator: Shared Material is null!");

                        }

                        // ========== MODIFICATION END | 2026年2月6日 ==========

                    }

                    else

                    {

                        // 如果没有目标，确保指示器是隐藏的

                        gameObject.SetActive(false);

                    }

                }

    /// <summary>
    /// 设置目标并显示指示器。
    /// </summary>
    /// <param name="newTarget">新的目标Transform。</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            gameObject.SetActive(true);
            // ========== MODIFICATION START | 2026年2月6日 (激活子对象) ==========
            if (_targetRenderer != null) 
            {
                _targetRenderer.enabled = true;
                _targetRenderer.gameObject.SetActive(true); // Explicitly activate child GameObject
            }
            // ========== MODIFICATION END | 2026年2月6日 (激活子对象) ==========
        }
        else
        {
            // ========== MODIFICATION START | 2026年2月6日 (禁用子对象) ==========
            if (_targetRenderer != null) 
            {
                _targetRenderer.enabled = false;
                _targetRenderer.gameObject.SetActive(false); // Explicitly deactivate child GameObject
            }
            // ========== MODIFICATION END | 2026年2月6日 (禁用子对象) ==========
            gameObject.SetActive(false); // Deactivate parent
        }
    }

    /// <summary>
    /// 隐藏指示器并清除目标。
    /// </summary>
    public void ClearTarget()
    {
        target = null;
        gameObject.SetActive(false); // Deactivate parent
        // ========== MODIFICATION START | 2026年2月6日 (禁用子对象) ==========
        if (_targetRenderer != null) 
        {
            _targetRenderer.enabled = false;
            _targetRenderer.gameObject.SetActive(false); // Explicitly deactivate child GameObject
        }
        // ========== MODIFICATION END | 2026年2月6日 (禁用子对象) ==========
    }
}
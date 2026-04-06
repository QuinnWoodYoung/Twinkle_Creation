using UnityEngine;
using System.Collections;

/// <summary>
/// 一个简单的VFX工具脚本，用于播放“放大/缩小并淡出”的动画。
/// 非常适合用作临时的占位符特效。
/// </summary>
public class SimpleVfx : MonoBehaviour
{
    [Header("缩放动画")]
    [Tooltip("是否启用缩放动画")]
    public bool enableScaling = true;
    [Tooltip("动画持续时间")]
    public float scaleDuration = 0.5f;
    [Tooltip("起始大小")]
    public Vector3 startScale = Vector3.zero;
    [Tooltip("结束大小")]
    public Vector3 endScale = Vector3.one;

    [Header("淡出动画")]
    [Tooltip("是否启用淡出动画（需要材质支持透明度）")]
    public bool enableFadeOut = true;
    [Tooltip("开始淡出前的延迟时间")]
    public float fadeDelay = 0.25f;
    [Tooltip("淡出过程的持续时间")]
    public float fadeDuration = 0.25f;
    
    [Header("生命周期")]
    [Tooltip("特效的总生命周期时长，到时后会自动销毁")]
    public float totalLifetime = 1.0f;

    private Renderer _renderer;
    private Material _materialInstance;
    private Color _originalColor;

    void Start()
    {
        // 确保总生命周期足够长以播放动画
        if (totalLifetime < scaleDuration || totalLifetime < fadeDelay + fadeDuration)
        {
            totalLifetime = Mathf.Max(scaleDuration, fadeDelay + fadeDuration);
        }

        // 获取材质实例以便修改颜色而不影响原始材质资源
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _materialInstance = _renderer.material;
            _originalColor = _materialInstance.color;
        }

        // 启动动画和销毁计时器
        StartCoroutine(AnimateVfx());
        Destroy(gameObject, totalLifetime);
    }

    private IEnumerator AnimateVfx()
    {
        // 缩放动画
        if (enableScaling)
        {
            float timer = 0;
            while (timer < scaleDuration)
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, timer / scaleDuration);
                timer += Time.deltaTime;
                yield return null;
            }
            transform.localScale = endScale;
        }

        // 淡出动画
        if (enableFadeOut && _materialInstance != null)
        {
            yield return new WaitForSeconds(fadeDelay);

            float timer = 0;
            while (timer < fadeDuration)
            {
                // Lerp a new color with modified alpha
                Color newColor = _originalColor;
                newColor.a = Mathf.Lerp(_originalColor.a, 0f, timer / fadeDuration);
                _materialInstance.color = newColor;

                timer += Time.deltaTime;
                yield return null;
            }
             // Ensure it's fully transparent at the end
            Color finalColor = _originalColor;
            finalColor.a = 0f;
            _materialInstance.color = finalColor;
        }
    }
    
    // 在编辑器中绘制一个预览范围，方便对齐
    void OnDrawGizmosSelected()
    {
        if(enableScaling)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireMesh(GetComponent<MeshFilter>()?.sharedMesh, -1, transform.position, transform.rotation, endScale);
        }
    }
}

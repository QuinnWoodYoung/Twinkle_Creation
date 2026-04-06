using UnityEngine;
#if UNITY_EDITOR // Only include Editor namespaces in the editor
using UnityEditor;
#endif

public class TargetIndicatorCreator : MonoBehaviour
{
    [Tooltip("光圈的材质，建议使用透明且自发光的材质。")]
    public Material indicatorMaterial;

    [Tooltip("光圈的颜色。")]
    public Color indicatorColor = Color.yellow;

    [Tooltip("光圈的半径。")]
    public float radius = 1.0f;

    [Tooltip("光圈的高度（厚度）。")]
    public float height = 0.05f;

    [Tooltip("光圈在Y轴上的偏移，使其位于角色底部。")]
    public float yOffset = 0.01f;

    // Use HideInInspector to prevent it from showing in the Inspector if we manage it internally
    [HideInInspector]
    [SerializeField] private GameObject _indicatorInstance; 
    private const string INDICATOR_NAME = "TargetIndicatorRing"; // Constant name for easier lookup

    /// <summary>
    /// 创建或更新目标指示器。
    /// </summary>
    private void CreateOrUpdateIndicator()
    {
        // Check if an existing valid instance exists
        bool instanceExistsAndIsValid = (_indicatorInstance != null && _indicatorInstance.transform.parent == this.transform && _indicatorInstance.name == INDICATOR_NAME);

        if (!instanceExistsAndIsValid)
        {
            // If there's an old, invalid instance or no instance, clean up any previous child with the same name
            Transform existingChild = transform.Find(INDICATOR_NAME);
            if (existingChild != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingChild.gameObject);
                }
                else
                {
                    DestroyImmediate(existingChild.gameObject); // This is where the warning happens, but often acceptable for editor cleanup
                }
            }

            // Create new cylinder
            _indicatorInstance = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _indicatorInstance.name = INDICATOR_NAME;
            _indicatorInstance.transform.SetParent(this.transform); // Set parent
            // Hide from hierarchy, don't save with scene (prefab takes care)
            _indicatorInstance.hideFlags = HideFlags.DontSaveInEditor; 
        }

        // --- Always update properties of the instance ---
        _indicatorInstance.transform.localPosition = new Vector3(0, yOffset, 0);
        _indicatorInstance.transform.localRotation = Quaternion.identity;
        _indicatorInstance.transform.localScale = new Vector3(radius * 2, height, radius * 2);

        Renderer renderer = _indicatorInstance.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (indicatorMaterial != null)
            {
                // Use sharedMaterial for assignment to avoid leaking materials
                renderer.sharedMaterial = indicatorMaterial;
                
                // Modify properties of the shared material (this will affect the material asset)
                renderer.sharedMaterial.color = indicatorColor;

                if (renderer.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    renderer.sharedMaterial.EnableKeyword("_EMISSION");
                    renderer.sharedMaterial.SetColor("_EmissionColor", indicatorColor * 0.5f);
                }
            }
            else
            {
                Debug.LogWarning("TargetIndicatorCreator: 未指定指示器材质，将使用默认材质。", this);
            }
        }
        else
        {
            Debug.LogError("TargetIndicatorCreator: 无法获取Renderer组件来应用材质。", this);
        }

        // Disable collider
        Collider collider = _indicatorInstance.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        #if UNITY_EDITOR
        // Mark the object as dirty so changes are saved (for the prefab)
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(gameObject);
            EditorUtility.SetDirty(_indicatorInstance);
        }
        #endif
    }

    private void OnValidate()
    {
        // Only run if the component is active and enabled, and we are in the editor
        // This helps prevent issues when script is being deserialized or during compilation
        #if UNITY_EDITOR
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            CreateOrUpdateIndicator();
        }
        #endif
    }

    private void OnEnable()
    {
        // Ensure the indicator is created when the component is enabled in editor or runtime
        CreateOrUpdateIndicator();
    }

    private void OnDisable()
    {
        // Hide the indicator when the component is disabled
        if (_indicatorInstance != null)
        {
            _indicatorInstance.SetActive(false);
        }
    }


    private void OnDestroy()
    {
        if (_indicatorInstance != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_indicatorInstance);
            }
            else
            {
                // In editor, if the parent (this GameObject) is destroyed,
                // its children (_indicatorInstance) will also be destroyed.
                // Only destroy explicitly if _indicatorInstance is not a child (shouldn't happen here)
                // or if we are cleaning up detached objects.
                // For simplicity, if we are not playing, and _indicatorInstance is still valid,
                // we can safely destroy it immediately here.
                DestroyImmediate(_indicatorInstance);
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class SkillPreviewController : MonoBehaviour
{
    [Header("Render")]
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private float surfaceOffset = 0.05f;
    [SerializeField] private int circleSegments = 48;
    [SerializeField] private int sectorSegments = 28;

    [Header("Ground")]
    [SerializeField] private float groundProbeHeight = 6f;
    [SerializeField] private float groundProbeDistance = 18f;

    private CharCtrl _charCtrl;
    private GameObject _root;
    private LineRenderer _shapeLine;
    private LineRenderer _rangeLine;
    private Material _lineMaterial;
    private LayerMask _groundLayer;

    private SkillData _activeSkill;
    private int _activeSkillIndex = -1;
    private CastContext _currentContext;
    private SkillPreviewResolvedSpec _resolvedSpec;
    private bool _isCurrentContextValid;
    private int _blockAttackUntilFrame = -1;

    public bool IsPreviewing => _activeSkill != null;
    public int ActiveSkillIndex => _activeSkillIndex;
    public SkillData ActiveSkill => _activeSkill;
    public CastContext CurrentContext => _currentContext;
    public bool IsCurrentContextValid => _isCurrentContextValid;
    public bool ShouldBlockAttackThisFrame => IsPreviewing || _blockAttackUntilFrame == Time.frameCount;

    private void Awake()
    {
        _charCtrl = GetComponent<CharCtrl>();
    }

    private void Update()
    {
        if (!IsPreviewing)
        {
            return;
        }

        RefreshPreview();
    }

    private void OnDestroy()
    {
        if (_lineMaterial != null)
        {
            Destroy(_lineMaterial);
        }
    }

    public void BeginPreview(int skillIndex, SkillData skill, LayerMask groundLayer)
    {
        if (skill == null)
        {
            return;
        }

        _activeSkillIndex = skillIndex;
        _activeSkill = skill;
        _groundLayer = groundLayer;
        EnsureRenderers();
        SetVisible(true);
        RefreshPreview();
    }

    public void CancelPreview(bool suppressAttackThisFrame = false)
    {
        _activeSkillIndex = -1;
        _activeSkill = null;
        _currentContext = null;
        _isCurrentContextValid = false;
        if (suppressAttackThisFrame)
        {
            _blockAttackUntilFrame = Time.frameCount;
        }
        SetVisible(false);
    }

    public void RefreshPreview()
    {
        if (_activeSkill == null || _charCtrl == null)
        {
            CancelPreview();
            return;
        }

        TargetInfo info = TargetingUtil.Collect(_charCtrl, _groundLayer);
        CastContext context = new CastContext(gameObject, info);
        bool isValid = _activeSkill.CanActivate(context);

        _currentContext = context;
        _resolvedSpec = SkillPreviewResolver.Resolve(_activeSkill, context);
        _isCurrentContextValid = isValid;

        DrawSkillShape(_resolvedSpec, isValid);
        DrawCastRange(isValid);
    }

    private void EnsureRenderers()
    {
        if (_root == null)
        {
            _root = new GameObject("SkillPreviewRuntime");
            _root.transform.SetParent(transform, false);
        }

        if (_shapeLine == null)
        {
            _shapeLine = CreateLineRenderer("Shape");
        }

        if (_rangeLine == null)
        {
            _rangeLine = CreateLineRenderer("CastRange");
        }
    }

    private LineRenderer CreateLineRenderer(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(_root.transform, false);

        LineRenderer line = child.AddComponent<LineRenderer>();
        line.sharedMaterial = GetOrCreateLineMaterial();
        line.useWorldSpace = true;
        line.loop = false;
        line.widthMultiplier = lineWidth;
        line.positionCount = 0;
        line.numCornerVertices = 2;
        line.numCapVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.textureMode = LineTextureMode.Stretch;
        return line;
    }

    private Material GetOrCreateLineMaterial()
    {
        if (_lineMaterial != null)
        {
            return _lineMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        _lineMaterial = new Material(shader);
        _lineMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        return _lineMaterial;
    }

    private void DrawSkillShape(SkillPreviewResolvedSpec spec, bool isValid)
    {
        if (_shapeLine == null || spec.displayContext == null || _activeSkill == null)
        {
            return;
        }

        ApplyLineStyle(_shapeLine, isValid ? _activeSkill.previewValidColor : _activeSkill.previewInvalidColor);

        switch (spec.shape)
        {
            case SkillPreviewShape.Rectangle:
                DrawRectangle(spec);
                break;
            case SkillPreviewShape.Sector:
                DrawSector(spec);
                break;
            default:
                DrawCircle(spec);
                break;
        }
    }

    private void DrawCastRange(bool isValid)
    {
        if (_rangeLine == null || _activeSkill == null)
        {
            return;
        }

        if (!_activeSkill.showCastRangeIndicator || _activeSkill.maxCastRange <= 0f)
        {
            _rangeLine.positionCount = 0;
            return;
        }

        Color color = isValid ? _activeSkill.previewValidColor : _activeSkill.previewInvalidColor;
        color.a *= 0.45f;
        ApplyLineStyle(_rangeLine, color);
        DrawCircleInternal(_rangeLine, transform.position, _activeSkill.maxCastRange, circleSegments);
    }

    private void DrawCircle(SkillPreviewResolvedSpec spec)
    {
        Vector3 center = ResolveShapeCenter(spec.displayContext, spec.anchor);
        DrawCircleInternal(_shapeLine, center, spec.radius, circleSegments);
    }

    private void DrawCircleInternal(LineRenderer line, Vector3 center, float radius, int segments)
    {
        int count = Mathf.Max(12, segments);
        Vector3[] points = new Vector3[count + 1];

        for (int i = 0; i <= count; i++)
        {
            float angle = (Mathf.PI * 2f * i) / count;
            Vector3 basePoint = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            points[i] = ProjectToGround(basePoint);
        }

        line.positionCount = points.Length;
        line.SetPositions(points);
    }

    private void DrawRectangle(SkillPreviewResolvedSpec spec)
    {
        Vector3 forward = ResolvePreviewDirection(spec.displayContext);
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        float width = spec.width;
        float length = spec.length;
        SkillPreviewAnchor anchor = spec.anchor;

        Vector3 p0;
        Vector3 p1;
        Vector3 p2;
        Vector3 p3;

        if (anchor == SkillPreviewAnchor.Caster)
        {
            Vector3 origin = ResolveShapeCenter(spec.displayContext, anchor);
            p0 = origin - right * (width * 0.5f);
            p1 = origin + right * (width * 0.5f);
            p2 = p1 + forward * length;
            p3 = p0 + forward * length;
        }
        else
        {
            Vector3 center = ResolveShapeCenter(spec.displayContext, anchor);
            Vector3 halfForward = forward * (length * 0.5f);
            Vector3 halfRight = right * (width * 0.5f);
            p0 = center - halfForward - halfRight;
            p1 = center - halfForward + halfRight;
            p2 = center + halfForward + halfRight;
            p3 = center + halfForward - halfRight;
        }

        Vector3[] points =
        {
            ProjectToGround(p0),
            ProjectToGround(p1),
            ProjectToGround(p2),
            ProjectToGround(p3),
            ProjectToGround(p0),
        };

        _shapeLine.positionCount = points.Length;
        _shapeLine.SetPositions(points);
    }

    private void DrawSector(SkillPreviewResolvedSpec spec)
    {
        Vector3 center = ResolveShapeCenter(spec.displayContext, spec.anchor);
        Vector3 forward = ResolvePreviewDirection(spec.displayContext);
        float radius = spec.radius;
        float angle = spec.angle;
        int segments = Mathf.Max(6, sectorSegments);
        float halfAngle = angle * 0.5f;

        List<Vector3> points = new List<Vector3>(segments + 3)
        {
            ProjectToGround(center)
        };

        for (int i = 0; i <= segments; i++)
        {
            float t = segments > 0 ? i / (float)segments : 0f;
            float yaw = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 dir = Quaternion.AngleAxis(yaw, Vector3.up) * forward;
            points.Add(ProjectToGround(center + dir * radius));
        }

        points.Add(ProjectToGround(center));
        _shapeLine.positionCount = points.Count;
        _shapeLine.SetPositions(points.ToArray());
    }

    private void ApplyLineStyle(LineRenderer line, Color color)
    {
        line.startColor = color;
        line.endColor = color;
        line.enabled = true;
    }

    private void SetVisible(bool visible)
    {
        if (_root != null)
        {
            _root.SetActive(visible);
        }
    }

    private Vector3 ResolveShapeCenter(CastContext context, SkillPreviewAnchor anchor)
    {
        if (anchor == SkillPreviewAnchor.Caster)
        {
            return transform.position;
        }

        return context != null ? context.rawTarget.position : transform.position;
    }

    private Vector3 ResolvePreviewDirection(CastContext context)
    {
        if (context != null)
        {
            Vector3 direction = context.rawTarget.direction;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                return direction.normalized;
            }

            direction = context.rawTarget.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                return direction.normalized;
            }
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
    }

    private Vector3 ProjectToGround(Vector3 worldPoint)
    {
        Vector3 rayOrigin = new Vector3(
            worldPoint.x,
            worldPoint.y + Mathf.Max(groundProbeHeight, 0.1f),
            worldPoint.z);

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            groundProbeHeight + Mathf.Max(groundProbeDistance, 0.1f),
            _groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * surfaceOffset;
        }

        worldPoint.y = transform.position.y + surfaceOffset;
        return worldPoint;
    }
}

using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public GameObject healthUIPrefab;
    public Transform barPoint;
    public bool alwaysVisible;
    public float visibleTime;

    private float _timeLeft;
    private Image _healthSlider;
    private Transform _energyContainer;
    private Image _energySlider;
    private Transform _uiBar;
    private Transform _cam;
    private float _lastHp = -1f;
    private float _lastMaxHp = -1f;
    private float _lastEnergy = -1f;
    private float _lastMaxEnergy = -1f;

    private void OnEnable()
    {
        Camera mainCamera = Camera.main;
        _cam = mainCamera != null ? mainCamera.transform : null;

        foreach (Canvas canvas in FindObjectsOfType<Canvas>())
        {
            if (canvas.renderMode != RenderMode.WorldSpace)
            {
                continue;
            }

            _uiBar = Instantiate(healthUIPrefab, canvas.transform).transform;
            _healthSlider = _uiBar.GetChild(0).GetComponent<Image>();
            if (_uiBar.childCount > 1)
            {
                _energyContainer = _uiBar.GetChild(1);
                if (_energyContainer != null && _energyContainer.childCount > 0)
                {
                    _energySlider = _energyContainer.GetChild(0).GetComponent<Image>();
                }
            }

            if (_energyContainer != null)
            {
                _energyContainer.gameObject.SetActive(false);
            }

            _uiBar.gameObject.SetActive(alwaysVisible);
            break;
        }

        RefreshFromRuntime(true);
    }

    private void OnDisable()
    {
        if (_uiBar != null)
        {
            Destroy(_uiBar.gameObject);
            _uiBar = null;
        }
    }

    public void UpdateHealthBar(float hitPoint, float maxHitPoint)
    {
        ApplyHealthBar(hitPoint, maxHitPoint, true);
    }

    private void LateUpdate()
    {
        RefreshFromRuntime();

        if (_uiBar == null)
        {
            return;
        }

        if (_cam == null && Camera.main != null)
        {
            _cam = Camera.main.transform;
        }

        if (barPoint != null)
        {
            _uiBar.position = barPoint.position;
        }

        if (_cam != null)
        {
            _uiBar.forward = -_cam.forward;
        }

        if (_timeLeft <= 0f && !alwaysVisible)
        {
            _uiBar.gameObject.SetActive(false);
            if (_energyContainer != null)
            {
                _energyContainer.gameObject.SetActive(false);
            }
        }
        else
        {
            _timeLeft -= Time.deltaTime;
        }
    }

    private void RefreshFromRuntime(bool force = false)
    {
        if (_uiBar == null)
        {
            return;
        }

        if (CharRuntimeResolver.IsDead(gameObject))
        {
            _lastHp = -1f;
            _lastMaxHp = -1f;
            _lastEnergy = -1f;
            _lastMaxEnergy = -1f;
            _timeLeft = 0f;
            _uiBar.gameObject.SetActive(false);
            if (_energyContainer != null)
            {
                _energyContainer.gameObject.SetActive(false);
            }
            return;
        }

        if (!CharResourceResolver.HasHealth(gameObject))
        {
            _lastHp = 0f;
            _lastMaxHp = 0f;
            _uiBar.gameObject.SetActive(false);
            if (_energyContainer != null)
            {
                _energyContainer.gameObject.SetActive(false);
            }
            return;
        }

        float hitPoint = CharResourceResolver.GetHitPoint(gameObject);
        float maxHitPoint = CharResourceResolver.GetMaxHitPoint(gameObject);
        float energy = CharResourceResolver.GetEnergy(gameObject);
        float maxEnergy = CharResourceResolver.GetMaxEnergy(gameObject);

        if (!force &&
            Mathf.Approximately(hitPoint, _lastHp) &&
            Mathf.Approximately(maxHitPoint, _lastMaxHp) &&
            Mathf.Approximately(energy, _lastEnergy) &&
            Mathf.Approximately(maxEnergy, _lastMaxEnergy))
        {
            return;
        }

        ApplyBars(hitPoint, maxHitPoint, energy, maxEnergy, false);
    }

    private void ApplyHealthBar(float hitPoint, float maxHitPoint, bool refreshVisibility)
    {
        ApplyBars(hitPoint, maxHitPoint, 0f, 0f, refreshVisibility);
    }

    private void ApplyBars(float hitPoint, float maxHitPoint, float energy, float maxEnergy, bool refreshVisibility)
    {
        if (_uiBar == null || _healthSlider == null)
        {
            return;
        }

        _lastHp = hitPoint;
        _lastMaxHp = maxHitPoint;
        _lastEnergy = energy;
        _lastMaxEnergy = maxEnergy;

        if (maxHitPoint <= 0f)
        {
            _healthSlider.fillAmount = 0f;
            _uiBar.gameObject.SetActive(false);
            if (_energyContainer != null)
            {
                _energyContainer.gameObject.SetActive(false);
            }
            return;
        }

        _healthSlider.fillAmount = Mathf.Clamp01(hitPoint / maxHitPoint);
        bool hasEnergy = _energyContainer != null && _energySlider != null && maxEnergy > 0f;
        if (hasEnergy)
        {
            _energySlider.fillAmount = Mathf.Clamp01(energy / maxEnergy);
        }

        if (alwaysVisible || refreshVisibility || !Mathf.Approximately(hitPoint, maxHitPoint) || hitPoint <= 0f)
        {
            _uiBar.gameObject.SetActive(true);
            if (_energyContainer != null)
            {
                _energyContainer.gameObject.SetActive(hasEnergy);
            }
            _timeLeft = visibleTime;
            return;
        }

        if (_energyContainer != null)
        {
            _energyContainer.gameObject.SetActive(alwaysVisible && hasEnergy);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public GameObject healthUIPrefab;

    public Transform barPoint;

    public bool alwaysVisible;

    public float visibleTime;

    private float timeLeft;

    Image healthSlider;

    Transform UIbar;

    Transform cam;

    StateManager currentStats;

    SceneController sceneController;
    void Awake()
    {
        currentStats = GetComponent<StateManager>();

        currentStats.UpdateHP += UpdateHealthBar;

    }

    // ========== MODIFICATION START | 2026年2月3日 ==========
    // 新增：OnDestroy 方法，用于管理脚本生命周期结束时的清理工作。
    private void OnDestroy()
    {
        // 安全检查：如果 currentStats 存在，则取消订阅 UpdateHP 事件。
        // 这是为了防止在 HealthBarUI 对象被销毁后，仍然尝试调用 UpdateHealthBar 方法。
        if (currentStats != null)
        {
            currentStats.UpdateHP -= UpdateHealthBar;
        }

        // 安全检查：如果 UIbar 仍然存在（例如父对象被直接销毁，而不是血量归零），
        // 确保它也被销毁，避免场景中残留不必要的UI对象。
        if (UIbar != null)
        {
            Destroy(UIbar.gameObject);
        }
    }
    // ========== MODIFICATION END | 2026年2月3日 ==========

    void OnEnable()
    {
        cam = Camera.main.transform;
        foreach(Canvas canvas in FindObjectsOfType<Canvas>())
        {
            if(canvas.renderMode == RenderMode.WorldSpace)
            {
                UIbar = Instantiate(healthUIPrefab,canvas.transform).transform;
                healthSlider = UIbar.GetChild(0).GetComponent<Image>();
                UIbar.gameObject.SetActive(alwaysVisible);
            }
        }
    }
    public void UpdateHealthBar(float HitPoint, float MaxHitPoint)
    {
        // ========== MODIFICATION START | 2026年2月3日 ==========
        // 修复：重构了整个方法的逻辑顺序，使其更加健壮。
        
        // 1. 安全检查：如果 UIbar 已经被销毁，则不执行任何操作，直接返回。
        if (UIbar == null)
        {
            return;
        }

        // 2. 计算血量百分比
        float sliderPercent = HitPoint / MaxHitPoint;
        healthSlider.fillAmount = sliderPercent;

        // 3. 根据血量决定是否销毁UI
        if (HitPoint <= 0)
        {
            Destroy(UIbar.gameObject);
            // 关键修复：销毁后立刻返回，不再执行后续代码。
            return;
        }

        // 4. 如果没被销毁，则确保UI是可见的
        UIbar.gameObject.SetActive(true);
        //timeLeft = visibleTime; // 您原来的代码，暂时注释
        // ========== MODIFICATION END | 2026年2月3日 ==========
    }



    void LateUpdate()
    {
        // ========== MODIFICATION START | 2026年2月3日 ==========
        // 修复：在操作 UIbar 之前，先进行空值检查，防止在UI被销毁后仍然尝试访问。
        if (UIbar != null)
        // ========== MODIFICATION END | 2026年2月3日 ==========
        {
            UIbar.position = barPoint.position;
            UIbar.forward = -cam.forward;

            if(timeLeft <= 0 && !alwaysVisible)
              UIbar.gameObject.SetActive(false);
            else
              timeLeft -= Time.deltaTime;
        }
    }

}

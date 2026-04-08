// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
//
// public class WeaponVisualCtrl : MonoBehaviour
// {
//     [SerializeField] private Transform[] weaponTransforms;
//     
//     [SerializeField] private Transform Sword;
//     [SerializeField] private Transform Axe;
//     [SerializeField] private Transform Bow;
//     [SerializeField] private Transform Shield;
//
//
//     private void Start()
//     {
//         SwitchOffWeapons();
//     }
//
//     private void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.Alpha1))
//         {
//             SwitchOffWeapons();
//             Sword.gameObject.SetActive(true);
//         }
//         else if (Input.GetKeyDown(KeyCode.Alpha2))
//         {
//             SwitchOffWeapons();
//             Axe.gameObject.SetActive(true);
//             Shield.gameObject.SetActive(true);
//         }
//         else if (Input.GetKeyDown(KeyCode.Alpha3))
//         {
//             SwitchOffWeapons();
//             Bow.gameObject.SetActive(true);
//         }
//     }
//     
//     private void SwitchOnWeapons(Transform weaponTransform)
//     {
//         SwitchOffWeapons();
//         weaponTransform.gameObject.SetActive(true);
//     }
//     private void SwitchOffWeapons()
//     {
//         foreach (var item in weaponTransforms)
//         {
//             Debug.Log(item);
//             item.gameObject.SetActive(false);
//         }
//     }
// }
#if false
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponVisualCtrl : MonoBehaviour
{
    [Header("核心组件")]
    [SerializeField] private Animator animator; // 记得在Inspector里把Animator拖进来！

    [Header("武器模型列表")]
    [SerializeField] private Transform[] weaponTransforms;
    
    [SerializeField] private Transform Sword;
    [SerializeField] private Transform Axe;
    [SerializeField] private Transform Bow;
    [SerializeField] private Transform Shield;
    
    
    [Header("我真得控制你了")]
    [SerializeField] private CharWeaponCtrl charWeaponCtrl;

    

    private void Start()
    {
        SwitchOffWeapons();
        // 如果需要，可以在这里重置动画层，例如：
        // ResetAllLayers();
        animator = GetComponentInChildren<Animator>();

        charWeaponCtrl = GetComponent<CharWeaponCtrl>();
    }

    private void Update()
    {
        // 按 1：换剑
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchOffWeapons();
            Sword.gameObject.SetActive(true);
            Shield.gameObject.SetActive(true);


            // ⚠️ 注意：把 "Layer_Sword" 改成你 Animator 里剑的那个 Layer 名字
            SwitchLayer("Light Sword"); 
            animator.SetBool("isSword",true);
            animator.SetBool("isArcher",false);

            charWeaponCtrl._currentWeapon = WeaponType.Sword;
        }
        // 按 2：换斧头+盾
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchOffWeapons();
            Axe.gameObject.SetActive(true);
            Shield.gameObject.SetActive(true);

            // ⚠️ 注意：把 "Layer_Axe" 改成你 Animator 里斧头的那个 Layer 名字
            SwitchLayer("Sword");
            animator.SetBool("isSword",true);
            animator.SetBool("isArcher",false);
            
            charWeaponCtrl._currentWeapon = WeaponType.Axe;
            
        }
        // 按 3：换弓
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchOffWeapons();
            Bow.gameObject.SetActive(true);

            // ⚠️ 注意：把 "Layer_Bow" 改成你 Animator 里弓的那个 Layer 名字
            SwitchLayer("Bow");
            animator.SetBool("isArcher",true);
            animator.SetBool("isSword",false);
            
            charWeaponCtrl._currentWeapon = WeaponType.Bow;
        }
    }
    
    // --- 核心：通过名字切换动画层 ---
    private void SwitchLayer(string layerName)
    {
        // 1. 找 ID
        int targetIndex = animator.GetLayerIndex(layerName);

        if (targetIndex == -1)
        {
            Debug.LogError($"找不到名字叫 '{layerName}' 的动画层，请检查代码里的拼写！");
            return;
        }

        // 2. 暴力归零：把除了 Base Layer (0) 以外的所有层权重设为 0
        for (int i = 1; i < animator.layerCount; i++)
        {
            animator.SetLayerWeight(i, 0f);
        }

        // 3. 独宠一人：把目标层权重设为 1
        animator.SetLayerWeight(targetIndex, 1f);
    }

    // --- 原始逻辑保持不变 ---
    private void SwitchOffWeapons()
    {
        foreach (var item in weaponTransforms)
        {
            // Debug.Log(item); // 嫌吵可以注释掉
            if(item != null) 
                item.gameObject.SetActive(false);
        }
    }
}
#endif

using UnityEngine;

public class WeaponVisualCtrl : MonoBehaviour
{
    [Header("武器模型")]
    [Tooltip("需要统一关闭的所有武器模型。")]
    [SerializeField] private Transform[] weaponTransforms;
    [Tooltip("剑模型。")]
    [SerializeField] private Transform Sword;
    [Tooltip("斧头模型。")]
    [SerializeField] private Transform Axe;
    [Tooltip("弓模型。")]
    [SerializeField] private Transform Bow;
    [Tooltip("盾模型。")]
    [SerializeField] private Transform Shield;

    [Header("引用")]
    [Tooltip("逻辑武器控制器。WeaponVisualCtrl 只监听它，不再自己切武器逻辑。")]
    [SerializeField] private CharWeaponCtrl charWeaponCtrl;

    private void Awake()
    {
        if (charWeaponCtrl == null)
        {
            charWeaponCtrl = GetComponent<CharWeaponCtrl>();
        }
    }

    private void OnEnable()
    {
        if (charWeaponCtrl != null)
        {
            charWeaponCtrl.WeaponChanged += ApplyVisual;
            ApplyVisual(charWeaponCtrl.CurWeapon);
        }
        else
        {
            SwitchOffWeapons();
        }
    }

    private void OnDisable()
    {
        if (charWeaponCtrl != null)
        {
            charWeaponCtrl.WeaponChanged -= ApplyVisual;
        }
    }

    public void ApplyVisual(WeaponType weaponType)
    {
        SwitchOffWeapons();

        switch (weaponType)
        {
            case WeaponType.Sword:
                SetActive(Sword, true);
                SetActive(Shield, true);
                break;

            case WeaponType.Axe:
                SetActive(Axe, true);
                SetActive(Shield, true);
                break;

            case WeaponType.Bow:
                SetActive(Bow, true);
                break;

            case WeaponType.Shield:
                SetActive(Shield, true);
                break;
        }

        if (charWeaponCtrl != null)
        {
            charWeaponCtrl.RefreshWeaponAnim();
        }
    }

    public Transform GetWeaponRoot(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                return Sword;

            case WeaponType.Axe:
                return Axe;

            case WeaponType.Bow:
                return Bow;

            case WeaponType.Shield:
                return Shield;
        }

        return null;
    }

    private void SwitchOffWeapons()
    {
        if (weaponTransforms == null)
        {
            return;
        }

        for (int i = 0; i < weaponTransforms.Length; i++)
        {
            SetActive(weaponTransforms[i], false);
        }
    }

    private void SetActive(Transform target, bool active)
    {
        if (target != null)
        {
            target.gameObject.SetActive(active);
        }
    }
}

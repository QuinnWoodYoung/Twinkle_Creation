using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
   // TextMeshProUGUI 
    Text levelText;

    Image healthSlider;

    Image expSlider;


    void Awake()
    {
        levelText = transform.GetChild(2).GetComponent<Text>();
        healthSlider = transform.GetChild(0).GetChild(0).GetComponent<Image>();
        expSlider = transform.GetChild(1).GetChild(0).GetComponent<Image>();
    }

    void Update()
    {
        // Character level/exp still depends on legacy character data and is not
        // yet mirrored into the blackboard.
        UpdateHealth();
        //UpdateExp();
    }

    void UpdateHealth()
    {
        GameObject playerObject = ResolvePlayerObject();
        if (playerObject == null || !CharResourceResolver.HasHealth(playerObject))
        {
            healthSlider.fillAmount = 0f;
            return;
        }

        float maxHitPoint = CharResourceResolver.GetMaxHitPoint(playerObject);
        if (maxHitPoint <= 0f)
        {
            healthSlider.fillAmount = 0f;
            return;
        }

        float hitPoint = CharResourceResolver.GetHitPoint(playerObject);
        healthSlider.fillAmount = Mathf.Clamp01(hitPoint / maxHitPoint);
    }

    GameObject ResolvePlayerObject()
    {
        if (GameManager.Instance != null && GameManager.Instance.PlayerUnit != null)
        {
            return GameManager.Instance.PlayerUnit;
        }

        foreach (CharBlackBoard board in CharBlackBoard.ActiveBoards)
        {
            if (board != null && board.Identity.isPlayerControlled)
            {
                return board.gameObject;
            }
        }

        return GameObject.FindGameObjectWithTag("Player");
    }
    /*void UpdateExp()
    {
        float sliderPercent = (float)GameManager.Instance.PlayerCharacterData.currentExp / GameManager.Instance.PlayerCharacterData.baseExp;
        expSlider.fillAmount = sliderPercent;
    }*/
}

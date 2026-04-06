using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public Collider weaponCol;


    public void WeaponEnable()
    {
        weaponCol.enabled = true;
        //Debug.Log(weaponCol);
    }

    public void WeaponDisable()
    {
        weaponCol.enabled = false;
    }
}

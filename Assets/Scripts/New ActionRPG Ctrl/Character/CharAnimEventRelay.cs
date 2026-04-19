using UnityEngine;

[DisallowMultipleComponent]
public class CharAnimEventRelay : MonoBehaviour
{
    [SerializeField] private CharMeleeSlashVfxCtrl _meleeSlashVfxCtrl;

    public void Bind(CharMeleeSlashVfxCtrl meleeSlashVfxCtrl)
    {
        _meleeSlashVfxCtrl = meleeSlashVfxCtrl;
    }

    public void ShowMeleeSlash()
    {
        ResolveTarget()?.ShowMeleeSlash();
    }

    public void ShowMeleeSlashStage(int stageNumber)
    {
        ResolveTarget()?.ShowMeleeSlashStage(stageNumber);
    }

    public void HideMeleeSlash()
    {
        ResolveTarget()?.HideMeleeSlash();
    }

    private CharMeleeSlashVfxCtrl ResolveTarget()
    {
        if (_meleeSlashVfxCtrl == null)
        {
            _meleeSlashVfxCtrl = GetComponentInParent<CharMeleeSlashVfxCtrl>();
        }

        return _meleeSlashVfxCtrl;
    }
}

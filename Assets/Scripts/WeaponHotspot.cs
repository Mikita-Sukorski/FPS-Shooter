using UnityEngine;

public class WeaponHotspot : MonoBehaviour
{
    public string slotType; // "Muzzle", "Scope", etc.
    public Transform visualContainer; // —сылка на дочерний VisualContainer
    public Transform uiPivot;

    public void SetIndicatorVisible(bool visible)
    {
        //if (uiPivot != null) uiPivot.SetActive(visible);
    }
}
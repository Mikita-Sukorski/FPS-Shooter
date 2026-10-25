using UnityEngine;

[CreateAssetMenu(fileName = "New Attachment", menuName = "Weapon/Attachment")]
public class AttachmentData : ScriptableObject
{
    public string attachmentName; // Название (глушитель, прицел, рукоятка и т.д.)
    public GameObject prefab;     // 3D модель обвеса
    public string slotType;       // СТРОГО: "Muzzle", "Scope", "Grip" (или другие типы)

    [Header("Scope Settings")]
    public bool isScope;
    public float zoomFOV = 30f; // FOV при прицеливании через этот прицел
    public Vector3 aimOffset;

    [Header("Muzzle Settings")]
    public bool isMuzzle;
    [Tooltip("Полностью скрывает вспышку дульного огня")]
    public bool hideMuzzleFlash;
    [Range(0f, 1f)]
    [Tooltip("Множитель масштаба вспышки для локального игрока (для пламегасителя)")]
    public float localFlashScaleMultiplier = 1f;
    [Tooltip("Множитель скорости полета пули")]
    public float bulletSpeedMultiplier = 1f;
    [Tooltip("Множитель наносимого урона")]
    public float damageMultiplier = 1f;
    [Range(0.5f, 2.0f)]
    [Tooltip("Высота тона звука выстрела (выше 1.0 — более писклявый)")]
    public float soundPitch = 1f;
    [Range(0f, 1f)]
    [Tooltip("Множитель громкости звука выстрела")]
    public float soundVolumeMultiplier = 1f;

    [Header("Grip & Bipod Settings")]
    public bool isGrip;
    [Range(0.1f, 1.5f)]
    [Tooltip("Множитель горизонтального разброса (меньше 1.0 — зона сужается по ширине)")]
    public float horizontalSpreadMultiplier = 1.0f;
    [Range(0.1f, 1.5f)]
    [Tooltip("Множитель вертикального разброса (меньше 1.0 — зона сужается по высоте)")]
    public float verticalSpreadMultiplier = 1.0f;
}
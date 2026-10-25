using UnityEngine;

[CreateAssetMenu(menuName = "FPS/Gun Info")]
public class GunInfo : ItemInfo
{
    public float minDamage;
    public float maxDamage;
    public float fireRate;
    public float reloadTime;
    public int maxAmmo;
    public bool isAutomatic; // Автомат или одиночные?
    public bool reloadShellByShell; // Если true - заряжаем по одному патрону
    public float timePerShell = 0.5f; // Время на вставку одного патрона

    [Header("Spread Settings (Degrees)")]
    public float spreadMin = 1f;          // Минимальный угол (в покое)
    public float spreadMax = 10f;         // Максимальный угол (при зажиме)
    public float spreadPerShot = 1.5f;    // Прирост угла за выстрел
    public float spreadRecovery = 10f;    // Скорость возврата угла в секунду
    public float recoveryShooting = 5f;

    [Header("UI Visuals")]
    public float crosshairSizeMultiplier = 1f; // Тонкая настройка "чувствительности" визуала
}
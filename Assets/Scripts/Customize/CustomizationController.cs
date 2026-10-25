using UnityEngine;

public class CustomizationController : MonoBehaviour
{
    [Header("Data (Из папки Resources или перетащить)")]
    public CustomItem[] hats;
    public CustomItem[] masks;

    [Header("Mannequin References")]
    public Transform hatAttachPoint;
    public Transform maskAttachPoint;

    private int currentHatIndex = 0;
    private int currentMaskIndex = 0;

    private GameObject spawnedHat;
    private GameObject spawnedMask;

    private void Awake()
    {
        LoadSelections();
        UpdateVisuals();
    }

    // --- ЛОГИКА ШАПОК ---
    public void NextHat()
    {
        currentHatIndex = (currentHatIndex + 1) % hats.Length;
        SaveAndApply();
    }

    public void PreviousHat()
    {
        currentHatIndex--;
        if (currentHatIndex < 0) currentHatIndex = hats.Length - 1;
        SaveAndApply();
    }

    // --- ЛОГИКА МАСОК ---
    public void NextMask()
    {
        currentMaskIndex = (currentMaskIndex + 1) % masks.Length;
        SaveAndApply();
    }

    public void PreviousMask()
    {
        currentMaskIndex--;
        if (currentMaskIndex < 0) currentMaskIndex = masks.Length - 1;
        SaveAndApply();
    }

    // --- ПРИМЕНЕНИЕ И СОХРАНЕНИЕ ---
    private void SaveAndApply()
    {
        PlayerPrefs.SetInt("SavedHat", currentHatIndex);
        PlayerPrefs.SetInt("SavedMask", currentMaskIndex);
        PlayerPrefs.Save();
        UpdateVisuals();
    }

    private void LoadSelections()
    {
        currentHatIndex = PlayerPrefs.GetInt("SavedHat", 0);
        currentMaskIndex = PlayerPrefs.GetInt("SavedMask", 0);
    }

    private void UpdateVisuals()
    {
        // Удаляем старые предметы
        if (spawnedHat != null) Destroy(spawnedHat);
        if (spawnedMask != null) Destroy(spawnedMask);

        // Спавним новые, если префаб не null
        if (hats[currentHatIndex].prefab != null)
        {
            spawnedHat = Instantiate(hats[currentHatIndex].prefab, hatAttachPoint);
        }

        if (masks[currentMaskIndex].prefab != null)
        {
            spawnedMask = Instantiate(masks[currentMaskIndex].prefab, maskAttachPoint);
        }
    }
}
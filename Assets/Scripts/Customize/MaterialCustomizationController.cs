using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaterialCustomizationController : MonoBehaviour
{
    [Header("UI References")]
    public Transform materialsPanel;
    public GameObject materialButtonPrefab;
    public GameObject colorPickerPanel;
    public FlexibleColorPicker fcp;

    [Header("Target Model")]
    public MeshRenderer mannequinMeshRenderer;

    [Header("Material Data")]
    public Material baseRGBMaterial;
    public Material[] customMaterials;

    [Header("Icons")]
    public Sprite rainbowSprite;
    [Tooltip("Плоские иконки/текстуры для кнопок (размер массива должен совпадать с customMaterials)")]
    public Sprite[] customMaterialIcons;

    private List<GameObject> spawnedButtons = new List<GameObject>();
    private int selectedIndex = -1; // -1 это режим RGB палитры, 0+ это индексы массива
    private Color currentRGBColor = Color.white;

    private void Start()
    {
        LoadSelection();
        GenerateButtons();
        ApplySavedSettings();
        UpdateHighlight();

        // Подписываемся на смену меню
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OnMenuStateChanged += HandleMenuChange;
        }
    }

    private void OnDestroy()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OnMenuStateChanged -= HandleMenuChange;
        }
    }

    private void HandleMenuChange(string menuName, bool isOpen)
    {
        // Закрываем палитру и сохраняем цвет при уходе из меню кастомизации
        if (menuName == "customization" && !isOpen)
        {
            if (colorPickerPanel != null) colorPickerPanel.SetActive(false);
            SaveSelection();
        }

        // Обновляем цвет манекена при входе в меню
        if (isOpen && (menuName == "title" || menuName == "customization"))
        {
            ApplySavedSettings();
        }
    }

    private void GenerateButtons()
    {
        CreateButton(Color.white, null, -1, "RGB_Button");

        for (int i = 0; i < customMaterials.Length; i++)
        {
            Color buttonColor = customMaterials[i].HasProperty("_Color") ? customMaterials[i].color : Color.white;
            CreateButton(buttonColor, customMaterials[i], i, customMaterials[i].name);
        }
    }

    private void CreateButton(Color color, Material mat, int index, string btnName)
    {
        GameObject btnObj = Instantiate(materialButtonPrefab, materialsPanel);
        btnObj.name = "Btn_" + btnName;
        spawnedButtons.Add(btnObj);

        Image btnImage = btnObj.GetComponent<Image>();
        Button btn = btnObj.GetComponent<Button>();

        if (index == -1) // Кнопка палитры RGB
        {
            if (rainbowSprite != null)
            {
                btnImage.sprite = rainbowSprite;
                btnImage.color = Color.white;
            }
            else
            {
                btnImage.color = Color.magenta;
            }
        }
        else if (mat != null)
        {
            Sprite iconToUse = null;

            // 1. Пытаемся взять подготовленный спрайт из массива в инспекторе
            if (customMaterialIcons != null && index >= 0 && index < customMaterialIcons.Length && customMaterialIcons[index] != null)
            {
                iconToUse = customMaterialIcons[index];
            }

            // 2. Если спрайта нет, пытаемся вытащить плоскую текстуру из самого материала
            if (iconToUse == null)
            {
                Texture2D mainTex = null;

                if (mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null)
                {
                    mainTex = mat.GetTexture("_MainTex") as Texture2D;
                }
                else if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") != null) // Для URP-шейдеров
                {
                    mainTex = mat.GetTexture("_BaseMap") as Texture2D;
                }

                if (mainTex != null)
                {
                    // Создаем спрайт из текстуры во время выполнения
                    iconToUse = Sprite.Create(mainTex, new Rect(0, 0, mainTex.width, mainTex.height), new Vector2(0.5f, 0.5f));
                }
            }

            // Применяем иконку и цвет
            if (iconToUse != null)
            {
                btnImage.sprite = iconToUse;
                // Задаем цвет материала (обычно белый для текстурных кнопок, чтобы они не перекрашивались)
                btnImage.color = mat.HasProperty("_Color") ? mat.color : Color.white;
            }
            else
            {
                // Если текстур и картинок вообще нет, просто красим в сплошной цвет
                btnImage.sprite = null;
                btnImage.color = mat.HasProperty("_Color") ? mat.color : Color.white;
            }
        }

        btn.onClick.AddListener(() => OnButtonClicked(index));
    }

    private void OnButtonClicked(int index)
    {
        selectedIndex = index;

        if (index == -1)
        {
            // СОХРАНЯЕМ ПРАВИЛЬНЫЙ ЦВЕТ ПЕРЕД ОТКРЫТИЕМ ПАЛИТРЫ
            Color correctColor = currentRGBColor;

            // Включаем панель (в этот момент багованный FCP попытается сбросить цвет)
            colorPickerPanel.SetActive(true);

            // ПРИНУДИТЕЛЬНО возвращаем палитре правильный цвет!
            if (fcp != null) fcp.color = correctColor;
        }
        else
        {
            colorPickerPanel.SetActive(false);
        }

        ApplySavedSettings();
        SaveSelection();
        UpdateHighlight();
    }

    // Вызывается из FlexibleColorPicker при вождении по палитре
    public void UpdateLiveColor(Color color)
    {
        currentRGBColor = color;
        if (selectedIndex == -1)
        {
            ApplyRGBColor(currentRGBColor);
        }
    }

    // Вызывается кнопкой OK на ColorPickerPanel
    public void ConfirmRGBColor()
    {
        colorPickerPanel.SetActive(false);
        SaveSelection();
    }

    private void UpdateHighlight()
    {
        if (spawnedButtons.Count == 0) return;

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            bool isSelected = (i - 1 == selectedIndex);

            Outline outline = spawnedButtons[i].GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = isSelected;
                if (isSelected)
                {
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(3, -3);
                }
            }

            spawnedButtons[i].transform.localScale = isSelected ? Vector3.one * 1.15f : Vector3.one;
        }
    }

    private void SaveSelection()
    {
        PlayerPrefs.SetInt("SavedMaterialIndex", selectedIndex);
        if (selectedIndex == -1)
        {
            PlayerPrefs.SetString("SavedRGBColor", "#" + ColorUtility.ToHtmlStringRGBA(currentRGBColor));
        }
        PlayerPrefs.Save();
    }

    private void LoadSelection()
    {
        selectedIndex = PlayerPrefs.GetInt("SavedMaterialIndex", -1);

        string htmlColor = PlayerPrefs.GetString("SavedRGBColor", "#FFFFFFFF");
        if (ColorUtility.TryParseHtmlString(htmlColor, out Color loadedColor))
        {
            currentRGBColor = loadedColor;
        }

        // Передаем стартовый цвет в палитру, чтобы она не сходила с ума
        if (fcp != null) fcp.color = currentRGBColor;
    }

    private void ApplySavedSettings()
    {
        if (selectedIndex == -1)
        {
            mannequinMeshRenderer.material = baseRGBMaterial;
            ApplyRGBColor(currentRGBColor);
        }
        else if (selectedIndex >= 0 && selectedIndex < customMaterials.Length)
        {
            mannequinMeshRenderer.material = customMaterials[selectedIndex];
        }
    }

    // Универсальный метод покраски (спасает от багов материалов)
    private void ApplyRGBColor(Color col)
    {
        if (mannequinMeshRenderer == null || mannequinMeshRenderer.material == null) return;

        mannequinMeshRenderer.material.color = col;

        if (mannequinMeshRenderer.material.HasProperty("_Color"))
            mannequinMeshRenderer.material.SetColor("_Color", col);

        if (mannequinMeshRenderer.material.HasProperty("_BaseColor"))
            mannequinMeshRenderer.material.SetColor("_BaseColor", col);
    }
}
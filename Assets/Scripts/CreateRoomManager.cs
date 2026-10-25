using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class MapData
{
    public string mapDisplayName;
    public string prefabName;
    public Sprite mapIcon;
}

[System.Serializable]
public class GameModeData
{
    public string modeName;
    public string modeCode;
    public List<MapData> availableMaps;
}

public class CreateRoomManager : MonoBehaviour
{
    [Header("Данные режимов")]
    public List<GameModeData> gameModes;

    [Header("UI Элементы Левой Панели")]
    public TMP_Dropdown modeDropdown;
    public Transform mapsContentPanel;
    public GameObject mapIconPrefab;

    [Header("UI Элементы Правой Панели")]
    public GameObject rightPanelContentInfo; // Тот самый объект ContentInfo
    public Image previewImage;
    public TMP_Text previewMapNameText;

    [Header("Текущий выбор")]
    public string selectedModeCode;
    public string selectedMapPrefabName;

    private List<MapSelectionItem> spawnedMapButtons = new List<MapSelectionItem>();

    public static CreateRoomManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public MapData GetMapDataByPrefabName(string prefabName)
    {
        foreach (var mode in gameModes)
        {
            foreach (var map in mode.availableMaps)
            {
                if (map.prefabName == prefabName) return map;
            }
        }
        return null;
    }

    void Start()
    {
        // Скрываем правую панель при старте
        rightPanelContentInfo.SetActive(false);
        SetupModeDropdown();
    }

    private void SetupModeDropdown()
    {
        modeDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (var mode in gameModes)
        {
            options.Add(mode.modeName);
        }

        modeDropdown.AddOptions(options);
        modeDropdown.onValueChanged.AddListener(OnModeChanged);

        OnModeChanged(0);
    }

    public void OnModeChanged(int index)
    {
        selectedModeCode = gameModes[index].modeCode;
        GenerateMapList(gameModes[index].availableMaps);
    }

    private void GenerateMapList(List<MapData> maps)
    {
        foreach (var btn in spawnedMapButtons) Destroy(btn.gameObject);
        spawnedMapButtons.Clear();

        // Когда меняем режим, скрываем инфу справа, так как выбор сбросился
        rightPanelContentInfo.SetActive(false);
        selectedMapPrefabName = "";

        foreach (var map in maps)
        {
            GameObject go = Instantiate(mapIconPrefab, mapsContentPanel);
            MapSelectionItem item = go.GetComponent<MapSelectionItem>();
            item.Setup(map.mapDisplayName, map.prefabName, map.mapIcon, this);
            spawnedMapButtons.Add(item);
        }
    }

    // Обновили метод SelectMap, добавили передачу картинки и названия
    public void SelectMap(string prefabName, MapSelectionItem clickedItem, Sprite icon, string displayName)
    {
        selectedMapPrefabName = prefabName;

        // Обновляем визуал кнопок
        foreach (var item in spawnedMapButtons)
        {
            item.SetSelected(item == clickedItem);
        }

        // Показываем правую панель и заполняем данные
        rightPanelContentInfo.SetActive(true);
        if (previewImage != null) previewImage.sprite = icon;
        if (previewMapNameText != null) previewMapNameText.text = displayName;
    }
}
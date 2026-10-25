using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapSelectionItem : MonoBehaviour
{
    // Теперь сама кнопка является и картинкой
    [SerializeField] private Image mapIconImage;
    [SerializeField] private TMP_Text mapNameText;

    // Используем встроенный компонент Outline Unity
    [SerializeField] private Outline outline;
    [SerializeField] private Button button;

    private string prefabName;
    private CreateRoomManager manager;

    public void Setup(string displayName, string prefabName, Sprite icon, CreateRoomManager manager)
    {
        this.prefabName = prefabName;
        this.manager = manager;
        mapNameText.text = displayName;

        if (mapIconImage != null && icon != null)
        {
            mapIconImage.sprite = icon;
            mapIconImage.color = Color.white; // Убеждаемся, что цвет белый (не прозрачный)
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        // При спавне обводка выключена
        SetSelected(false);
    }

    private void OnClick()
    {
        manager.SelectMap(prefabName, this, mapIconImage.sprite, mapNameText.text);
    }

    public void SetSelected(bool isSelected)
    {
        if (outline != null)
        {
            // Просто включаем или выключаем компонент Outline
            outline.enabled = isSelected;
        }
    }
}
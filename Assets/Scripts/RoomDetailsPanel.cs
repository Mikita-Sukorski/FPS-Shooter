using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Realtime;

public class RoomDetailsPanel : MonoBehaviour
{
    public static RoomDetailsPanel Instance;

    [SerializeField] private GameObject contentContainer;
    [SerializeField] private Image mapPreviewImage;

    // ТЕКСТ НАЗВАНИЯ КАРТЫ НА КАРТИНКЕ (сюда перетащить тот самый объект, где написано "New Text")
    [SerializeField] private TMP_Text mapNameText;

    [SerializeField] private TMP_Text roomNameText;
    [SerializeField] private Button joinButton;

    private RoomInfo selectedRoom;

    private void Awake()
    {
        Instance = this;
        contentContainer.SetActive(false);
    }

    // ТУТ ТЕПЕРЬ 3 ПАРАМЕТРА (RoomInfo, Sprite, string)
    public void UpdateDetails(RoomInfo info, Sprite mapSprite, string mapDisplayName)
    {
        selectedRoom = info;
        roomNameText.text = info.Name;
        mapNameText.text = mapDisplayName;

        if (mapSprite != null)
        {
            mapPreviewImage.sprite = mapSprite;
            mapPreviewImage.color = Color.white;
        }
        else
        {
            mapPreviewImage.color = Color.gray; // Или картинка-заглушка
        }

        contentContainer.SetActive(true);
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => JoinSelectedRoom());
    }

    private void JoinSelectedRoom()
    {
        if (selectedRoom != null)
        {
            Launcher.Instance.JoinRoom(selectedRoom);
        }
    }
}
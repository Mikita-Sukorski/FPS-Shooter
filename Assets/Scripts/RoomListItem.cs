using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] private TMP_Text roomNameText; // Переименовал для понятности
    [SerializeField] private TMP_Text playerCountText; // Добавили текст игроков
    [SerializeField] private Image mapIcon;

    public RoomInfo info;

    private MapData savedMapData; 

    public void SetUp(RoomInfo _info)
    {
        info = _info;
        roomNameText.text = _info.Name;
        playerCountText.text = $"{_info.PlayerCount}/{_info.MaxPlayers}";

        if (info.CustomProperties.ContainsKey(GameSettings.MAP_KEY))
        {
            string mapPrefabName = (string)info.CustomProperties[GameSettings.MAP_KEY];
            savedMapData = CreateRoomManager.Instance.GetMapDataByPrefabName(mapPrefabName);

            if (savedMapData != null && mapIcon != null)
            {
                mapIcon.sprite = savedMapData.mapIcon;
            }
        }
    }

    public void OnClick()
    {
        if (savedMapData != null)
        {
            RoomDetailsPanel.Instance.UpdateDetails(info, savedMapData.mapIcon, savedMapData.mapDisplayName);
        }
        else
        {
            RoomDetailsPanel.Instance.UpdateDetails(info, null, "Unknown Map");
        }
    }
}
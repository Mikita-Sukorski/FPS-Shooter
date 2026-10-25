using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using Photon.Realtime;
using System.Linq;
using System.IO;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher Instance;

    [SerializeField] TMP_InputField roomNameInputField;
    [SerializeField] TMP_Text errorText;
    [SerializeField] TMP_Text roomNameText;
    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject PlayerListItemPrefab;
    [SerializeField] GameObject startGameButton;

    private const int MaxPlayersPerRoom = 10;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MenuManager.Instance.OpenMenu("title");

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        MenuManager.Instance.OpenMenu("title");
        Debug.Log("Joined Lobby");
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(roomNameInputField.text)) return;

        CreateRoomManager createManager = FindObjectOfType<CreateRoomManager>();

        if (createManager == null || string.IsNullOrEmpty(createManager.selectedMapPrefabName))
        {
            Debug.LogError("Не выбрана карта или режим!");
            return;
        }

        ExitGames.Client.Photon.Hashtable customRoomProperties = new ExitGames.Client.Photon.Hashtable();

        customRoomProperties[GameSettings.GAME_MODE_KEY] = createManager.selectedModeCode;
        customRoomProperties[GameSettings.MAP_KEY] = createManager.selectedMapPrefabName;

        customRoomProperties[GameSettings.RED_SCORE_KEY] = 0;
        customRoomProperties[GameSettings.BLUE_SCORE_KEY] = 0;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = (byte)MaxPlayersPerRoom,
            CustomRoomProperties = customRoomProperties,
            CustomRoomPropertiesForLobby = new string[] { GameSettings.GAME_MODE_KEY, GameSettings.MAP_KEY }
        };

        string targetRoomName = roomNameInputField.text;
        roomNameInputField.text = "";

        PhotonNetwork.CreateRoom(targetRoomName, options);
        MenuManager.Instance.OpenMenu("loading");
    }

    public override void OnJoinedRoom()
    {
        // Очищаем старые свойства игрока при входе в новую комнату,
        // чтобы данные о команде и счёте из предыдущей игры не переносились.
        ResetLocalPlayerProperties();

        MenuManager.Instance.OpenMenu("room");
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        Player[] players = PhotonNetwork.PlayerList;

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
        }

        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    private void ResetLocalPlayerProperties()
    {
        ExitGames.Client.Photon.Hashtable propertiesToClear = new ExitGames.Client.Photon.Hashtable
        {
            { GameSettings.TEAM_KEY, null },
            { GameSettings.KILLS_KEY, null },
            { GameSettings.DEATHS_KEY, null }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(propertiesToClear);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        errorText.text = "Room Creation Failed: " + message;
        Debug.LogError("Room Creation Failed: " + message);
        MenuManager.Instance.OpenMenu("error");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("Disconnected from Photon: " + cause.ToString());
        MenuManager.Instance.OpenMenu("error");
    }

    public void StartGame()
    {
        string mapSceneToLoad = "";
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSettings.MAP_KEY, out object mapName))
        {
            mapSceneToLoad = (string)mapName;
        }
        else
        {
            mapSceneToLoad = "Map_TDM_City";
        }

        PhotonNetwork.LoadLevel(mapSceneToLoad);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.AutomaticallySyncScene = false;

        PhotonNetwork.LeaveRoom();
        MenuManager.Instance.OpenMenu("loading");
    }

    public void JoinRoom(RoomInfo info)
    {
        if (info.PlayerCount >= info.MaxPlayers)
        {
            Debug.Log("Room is full!");
            return;
        }

        PhotonNetwork.JoinRoom(info.Name);
        MenuManager.Instance.OpenMenu("loading");
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform trans in roomListContent)
        {
            Destroy(trans.gameObject);
        }

        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].RemovedFromList)
                continue;

            GameObject roomListItem = Instantiate(roomListItemPrefab, roomListContent);
            RoomListItem listItem = roomListItem.GetComponent<RoomListItem>();
            listItem.SetUp(roomList[i]);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(PlayerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
    }
}

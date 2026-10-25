using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TeamSelectionUI : MonoBehaviourPunCallbacks
{
    public GameObject teamSelectionPanel;

    [Header("Списки игроков")]
    public TMP_Text redTeamListText;
    public TMP_Text blueTeamListText;

    void Start()
    {
        // Проверяем режим игры. Если это мясорубка - скрываем окно и спавнимся как обычно (Команда 0)
        string gameMode = (string)PhotonNetwork.CurrentRoom.CustomProperties[GameSettings.GAME_MODE_KEY];

        if (gameMode != "TDM")
        {
            teamSelectionPanel.SetActive(false);
            JoinTeam(0); // 0 - Без команды (FFA)
            return;
        }

        teamSelectionPanel.SetActive(true);
        UpdateTeamLists();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Обновляем списки, если кто-то зашел в команду
        if (changedProps.ContainsKey(GameSettings.TEAM_KEY))
        {
            UpdateTeamLists();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateTeamLists(); // Обновляем, если кто-то вышел
    }

    private void UpdateTeamLists()
    {
        redTeamListText.text = "";
        blueTeamListText.text = "";

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue(GameSettings.TEAM_KEY, out object teamIdObj))
            {
                int teamId = (int)teamIdObj;
                if (teamId == 1) redTeamListText.text += p.NickName + "\n";
                else if (teamId == 2) blueTeamListText.text += p.NickName + "\n";
            }
        }
    }

    public void OnRedTeamButtonClicked()
    {
        Debug.Log("Red Button Clicked");
        if (CanJoinTeam(1)) JoinTeam(1);
    }

    public void OnBlueTeamButtonClicked()
    {
        Debug.Log("Blue Button Clicked");
        if (CanJoinTeam(2)) JoinTeam(2);
    }

    private bool CanJoinTeam(int teamId)
    {
        // Считаем, сколько людей уже в этой команде
        int playersInTeam = PhotonNetwork.PlayerList.Count(p =>
            p.CustomProperties.ContainsKey(GameSettings.TEAM_KEY) &&
            (int)p.CustomProperties[GameSettings.TEAM_KEY] == teamId);

        int maxPlayersPerTeam = PhotonNetwork.CurrentRoom.MaxPlayers / 2;

        if (playersInTeam >= maxPlayersPerTeam)
        {
            Debug.Log("Команда переполнена!");
            return false;
        }

        return true;
    }

    private void JoinTeam(int teamId)
    {
        Hashtable hash = new Hashtable();
        hash.Add(GameSettings.TEAM_KEY, teamId);
        PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

        teamSelectionPanel.SetActive(false);

        Invoke(nameof(CallSpawn), 0.5f);
    }

    private void CallSpawn()
    {
        PlayerManager.Find(PhotonNetwork.LocalPlayer)?.SpawnPlayerCharacter();
    }
}
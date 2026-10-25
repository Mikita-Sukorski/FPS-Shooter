using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scoreboard : MonoBehaviourPunCallbacks
{
    [Header("Главные окна режимов")]
    [SerializeField] GameObject tdmLayout; // Сюда перетащи объект TDM_Layout (в котором лежат Red и Blue Wrapper)
    [SerializeField] GameObject ffaWrapper; // Сюда перетащи объект FFAWrapper (целиком)

    [Header("Контейнеры списков (куда спавнятся игроки)")]
    [SerializeField] Transform ffaContainer;
    [SerializeField] Transform redTeamContainer;
    [SerializeField] Transform blueTeamContainer;

    [SerializeField] GameObject scoreboardItemPrefab;
    [SerializeField] CanvasGroup canvasGroup;

    Dictionary<Player, ScoreboardItem> scoreboardItems = new Dictionary<Player, ScoreboardItem>();

    void Start()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        string mode = "FFA";
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSettings.GAME_MODE_KEY, out object m))
            mode = (string)m;

        bool isTDM = (mode == "TDM");
        
        tdmLayout.SetActive(isTDM);
        ffaWrapper.SetActive(!isTDM);

        foreach (Player player in PhotonNetwork.PlayerList) AddScoreboardItem(player);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) => AddScoreboardItem(newPlayer);
    public override void OnPlayerLeftRoom(Player otherPlayer) => RemoveScoreboardItem(otherPlayer);

    void AddScoreboardItem(Player player)
    {
        Transform targetContainer = GetTargetContainer(player);

        ScoreboardItem item = Instantiate(scoreboardItemPrefab, targetContainer).GetComponent<ScoreboardItem>();
        item.Initialize(player);
        scoreboardItems[player] = item;

        SortTable();
    }

    Transform GetTargetContainer(Player player)
    {
        if (tdmLayout.activeSelf && player.CustomProperties.TryGetValue(GameSettings.TEAM_KEY, out object teamIdObj))
        {
            int teamId = (int)teamIdObj;
            return (teamId == 1) ? redTeamContainer : blueTeamContainer;
        }
        return ffaContainer;
    }

    void RemoveScoreboardItem(Player player)
    {
        if (scoreboardItems.ContainsKey(player))
        {
            ScoreboardItem item = scoreboardItems[player];
            scoreboardItems.Remove(player);

            item.transform.SetParent(null);
            Destroy(item.gameObject);

            SortTable();
        }
    }

    public void SortTable()
    {
        SortContainer(ffaContainer);
        SortContainer(redTeamContainer);
        SortContainer(blueTeamContainer);
    }

    void SortContainer(Transform container)
    {
        if (container.childCount == 0) return;

        // Берем все элементы, сортируем их по Kills
        var sorted = container.GetComponentsInChildren<ScoreboardItem>()
            .OrderByDescending(x => {
                x.player.CustomProperties.TryGetValue(GameSettings.KILLS_KEY, out object k);
                return (int)(k ?? 0);
            }).ToList();

        // Меняем их порядок в иерархии и обновляем №
        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].transform.SetSiblingIndex(i);
            sorted[i].UpdateIndex(i + 1); // Устанавливаем номер 1, 2, 3...
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer == null || !PhotonNetwork.CurrentRoom.Players.ContainsKey(targetPlayer.ActorNumber))
        {
            return;
        }

        if (changedProps.ContainsKey(GameSettings.TEAM_KEY))
        {
            if (scoreboardItems.ContainsKey(targetPlayer))
            {
                ScoreboardItem item = scoreboardItems[targetPlayer];
                scoreboardItems.Remove(targetPlayer);

                item.transform.SetParent(null);
                Destroy(item.gameObject);
            }
            AddScoreboardItem(targetPlayer);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) canvasGroup.alpha = 1;
        else if (Input.GetKeyUp(KeyCode.Tab)) canvasGroup.alpha = 0;
    }
}
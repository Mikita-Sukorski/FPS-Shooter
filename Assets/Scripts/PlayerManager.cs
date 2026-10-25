using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;
using System.IO;
using Hashtable = ExitGames.Client.Photon.Hashtable;
public class PlayerManager : MonoBehaviour
{
    public static Dictionary<Player, PlayerManager> AllManagers = new Dictionary<Player, PlayerManager>();
    PhotonView PV;
    GameObject controller;

    int kills;
    int deaths;

    void Awake()
    {
        PV = GetComponent<PhotonView>();
        AllManagers[PV.Owner] = this;
    }

    void OnDestroy()
    {
        if (PV.Owner != null) AllManagers.Remove(PV.Owner);
    }

    void Start()
    {
    }

    public void SpawnPlayerCharacter()
    {
        if (PV.IsMine)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (SpawnManager.Instance == null) yield return null;
        CreateController();
    }

    void CreateController()
    {
        int myTeam = 0;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(GameSettings.TEAM_KEY, out object teamObj))
        {
            myTeam = (int)teamObj;
        }

        Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint(myTeam);

        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"),
            spawnpoint.position, spawnpoint.rotation, 0, new object[] { PV.ViewID });
    }

    public void Die()
    {
        PhotonNetwork.Destroy(controller);
        deaths++;

        if (PV.IsMine)
        {
            Hashtable hash = new Hashtable { { GameSettings.DEATHS_KEY, deaths } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

            SpawnPlayerCharacter();
        }
    }

    public void GetKill(string victimName, int killerTeam, int victimTeam)
    {
        PV.RPC(nameof(RPC_GetKill), RpcTarget.All, victimName, killerTeam, victimTeam);
    }

    [PunRPC]
    void RPC_GetKill(string victimName, int killerTeam, int victimTeam)
    {
        if (PhotonNetwork.IsMasterClient && killerTeam != 0)
        {
            string scoreKey = killerTeam == 1 ? "RedScore" : "BlueScore";

            int currentTeamScore = 0;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(scoreKey, out object scoreValue))
            {
                currentTeamScore = (int)scoreValue;
            }

            ExitGames.Client.Photon.Hashtable scoreHash = new ExitGames.Client.Photon.Hashtable();
            scoreHash.Add(scoreKey, currentTeamScore + 1);
            PhotonNetwork.CurrentRoom.SetCustomProperties(scoreHash);

            Debug.Log($"Master updated {scoreKey} to {currentTeamScore + 1}");
        }

        if (PV.IsMine)
        {
            kills++;

            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
            hash.Add(GameSettings.KILLS_KEY, kills);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }

        string killerColor = killerTeam == 1 ? "#FF5555" : (killerTeam == 2 ? "#5555FF" : "#FFFFFF");
        string victimColor = victimTeam == 1 ? "#FF5555" : (victimTeam == 2 ? "#5555FF" : "#FFFFFF");

        string coloredKiller = $"<color={killerColor}>{PV.Owner.NickName}</color>";
        string coloredVictim = $"<color={victimColor}>{victimName}</color>";

        if (KillFeedManager.Instance != null)
        {
            KillFeedManager.Instance.AddKillfeedMessage(coloredKiller, coloredVictim);
        }
    }

    public static PlayerManager Find(Player player)
    {
        if (player != null && AllManagers.TryGetValue(player, out PlayerManager m))
            return m;
        return null;
    }
}
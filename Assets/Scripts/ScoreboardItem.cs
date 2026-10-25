using UnityEngine;
using TMPro;
using Photon.Realtime;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ScoreboardItem : MonoBehaviourPunCallbacks
{
    public TMP_Text indexText; // Новый текст для №
    public TMP_Text usernameText;
    public TMP_Text killsText;
    public TMP_Text deathsText;

    public Player player;

    public void Initialize(Player player)
    {
        this.player = player;
        usernameText.text = player.NickName;
        UpdateStats();
    }

    // Метод для обновления номера в таблице
    public void UpdateIndex(int newIndex)
    {
        if (indexText != null) indexText.text = newIndex.ToString();
    }

    void UpdateStats()
    {
        if (player.CustomProperties.TryGetValue(GameSettings.KILLS_KEY, out object kills))
            killsText.text = kills.ToString();
        else
            killsText.text = "0";

        if (player.CustomProperties.TryGetValue(GameSettings.DEATHS_KEY, out object deaths))
            deathsText.text = deaths.ToString();
        else
            deathsText.text = "0";
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer == player)
        {
            if (changedProps.ContainsKey(GameSettings.KILLS_KEY) || changedProps.ContainsKey(GameSettings.DEATHS_KEY))
            {
                UpdateStats();
                // Просим таблицу пересортироваться, так как киллы изменились
                SendMessageUpwards("SortTable", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
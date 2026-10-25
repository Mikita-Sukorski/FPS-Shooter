using Photon.Pun;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TeamScoreUI : MonoBehaviourPunCallbacks
{
    public GameObject redScoreObj;  // Ссылка на сам объект с текстом и фоном
    public GameObject blueScoreObj;
    public TMP_Text redScoreText;
    public TMP_Text blueScoreText;

    void Start()
    {
        string mode = (string)PhotonNetwork.CurrentRoom.CustomProperties[GameSettings.GAME_MODE_KEY];

        bool isTDM = (mode == "TDM");

        redScoreObj.SetActive(isTDM);
        blueScoreObj.SetActive(isTDM);

        UpdateScores();
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(GameSettings.RED_SCORE_KEY) || propertiesThatChanged.ContainsKey(GameSettings.BLUE_SCORE_KEY))
        {
            UpdateScores();
        }
    }

    private void UpdateScores()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSettings.RED_SCORE_KEY, out object rScore))
            redScoreText.text = rScore.ToString();

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSettings.BLUE_SCORE_KEY, out object bScore))
            blueScoreText.text = bScore.ToString();
    }
}
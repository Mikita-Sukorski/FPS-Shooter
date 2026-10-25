using UnityEngine;
using TMPro;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MatchTimer : MonoBehaviourPunCallbacks
{
    [Header("Settings")]
    [SerializeField] private float matchDurationInSeconds = 30f;
    [SerializeField] private TextMeshProUGUI timerText;

    private double startTime;
    private bool timerStarted = false;
    private bool isFinished = false; // Чтобы не вызывать выход несколько раз

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Если мы мастер, и время еще не задано в свойствах комнаты
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("StartTime"))
            {
                startTime = PhotonNetwork.Time;
                Hashtable props = new Hashtable
                {
                    { "StartTime", startTime }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!timerStarted && propertiesThatChanged.ContainsKey("StartTime"))
        {
            startTime = (double)propertiesThatChanged["StartTime"];
            timerStarted = true;
        }
    }

    private void Update()
    {
        // 1. Проверяем, в комнате ли мы и получено ли время
        if (!PhotonNetwork.InRoom || !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("StartTime"))
            return;

        // 2. Считаем оставшееся время
        double currentStartTime = (double)PhotonNetwork.CurrentRoom.CustomProperties["StartTime"];
        double timePassed = PhotonNetwork.Time - currentStartTime;
        double timeLeft = matchDurationInSeconds - timePassed;

        // 3. Если время вышло
        if (timeLeft <= 0)
        {
            timeLeft = 0;
            if (!isFinished)
            {
                FinishMatch();
            }
        }

        UpdateTimerDisplay(timeLeft);
    }

    private void UpdateTimerDisplay(double timeToDisplay)
    {
        int minutes = Mathf.FloorToInt((float)timeToDisplay / 60);
        int seconds = Mathf.FloorToInt((float)timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void FinishMatch()
    {
        isFinished = true;
        Debug.Log("Матч окончен! Выходим в меню...");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ищем твой MenuManager, который есть на сцене игры (MenuCanvas)
        MenuManager menuMgr = FindObjectOfType<MenuManager>();

        if (menuMgr != null)
        {
            // Используем твой готовый метод для правильного выхода
            menuMgr.ExitSession();
        }
        else
        {
            // Если вдруг MenuManager не найден, делаем "грубый" выход
            PhotonNetwork.LeaveRoom();
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}
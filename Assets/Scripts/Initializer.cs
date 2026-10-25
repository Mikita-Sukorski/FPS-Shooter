using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using UnityEngine.UI; // Обязательно для Image (полоски)
using TMPro; // Обязательно для текста TMP
public class Initializer : MonoBehaviourPunCallbacks
{
    [Header("UI Загрузки")]
    public Slider loadingBar;      // Сюда перетащишь LoadingBarFill
    public TextMeshProUGUI progressText; // Сюда перетащишь ProgressText
    public TextMeshProUGUI statusText;   // (По желанию) Сюда можешь перетащить свой текст "Loading..."

    void Start()
    {
        if (loadingBar != null) loadingBar.value = 0f;
        if (progressText != null) progressText.text = "0%";
        if (statusText != null) statusText.text = "Connecting to Server...";

        PhotonNetwork.AutomaticallySyncScene = true;

        // Если мы не подключены — подключаемся
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();

            PhotonNetwork.SendRate = 30; // По умолчанию 20
            PhotonNetwork.SerializationRate = 20; // По умолчанию 10
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            StartCoroutine(LoadMainMenuAsync());
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        StartCoroutine(LoadMainMenuAsync());
    }

    // Тот самый метод, который предотвратит зависание (фриз) игры
    private IEnumerator LoadMainMenuAsync()
    {
        if (statusText != null) statusText.text = "Loading Game...";

        // SceneManager.LoadSceneAsync загружает сцену в фоне.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(1);

        // Ждем, пока сцена полностью не загрузится
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (loadingBar != null)
            {
                loadingBar.value = progress;
            }

            if (progressText != null)
            {
                progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
            }

            yield return null;
        }
    }
}
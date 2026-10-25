using TMPro;
using Photon.Pun;
using UnityEngine;

public class KillFeedManager : MonoBehaviour
{
    public static KillFeedManager Instance;

    [SerializeField] private Transform killfeedPanel;
    [SerializeField] private GameObject killfeedMessagePrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Multiple instances of KillFeedManager detected!");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (GetComponent<PhotonView>() == null)
        {
            Debug.LogError("PhotonView is missing on KillFeedManager! Adding automatically.");
            gameObject.AddComponent<PhotonView>(); // Автоматическое добавление компонента
        }
    }


    public void AddKillfeedMessage(string killer, string victim)
    {
        // Создаем новый объект сообщения
        GameObject newMessage = Instantiate(killfeedMessagePrefab, killfeedPanel);

        // Настраиваем текст сообщения
        TextMeshProUGUI messageText = newMessage.GetComponent<TextMeshProUGUI>();
        if (messageText != null)
        {
            messageText.text = $"{killer} kill {victim}";
        }

        // Уничтожаем сообщение через 5 секунд
        Destroy(newMessage, 10f);
    }

    [PunRPC]
    public void RPC_AddKillfeedMessage(string killer, string victim)
    {
        AddKillfeedMessage(killer, victim);
    }

    public static void NotifyKill(string killer, string victim)
    {
        if (KillFeedManager.Instance == null)
        {
            Debug.LogError("KillFeedManager.Instance is null! Make sure the KillFeedManager exists in the scene.");
            return;
        }

        if (PhotonNetwork.IsConnected)
        {
            PhotonView photonView = PhotonView.Get(KillFeedManager.Instance);
            if (photonView == null)
            {
                Debug.LogError("PhotonView is missing on KillFeedManager!");
                return;
            }
            photonView.RPC("RPC_AddKillfeedMessage", RpcTarget.All, killer, victim);
        }
    }

    [PunRPC]
    public void RPC_AddEventMessage(string message)
    {
        AddEventMessage(message);
    }

    public void AddEventMessage(string message)
    {
        GameObject newMessage = Instantiate(killfeedMessagePrefab, killfeedPanel);

        TextMeshProUGUI messageText = newMessage.GetComponent<TextMeshProUGUI>();
        if (messageText != null)
        {
            messageText.text = message;
        }

        Destroy(newMessage, 10f);
    }

    public static void NotifyEvent(string message)
    {
        if (KillFeedManager.Instance == null)
        {
            Debug.LogError("KillFeedManager.Instance is null! Make sure the KillFeedManager exists in the scene.");
            return;
        }

        Debug.Log("Sending event message: " + message); // Проверка вызова

        if (PhotonNetwork.IsConnected)
        {
            PhotonView photonView = PhotonView.Get(KillFeedManager.Instance);
            if (photonView == null)
            {
                Debug.LogError("PhotonView is missing on KillFeedManager!");
                return;
            }
            photonView.RPC("RPC_AddEventMessage", RpcTarget.All, message);
        }
    }

}

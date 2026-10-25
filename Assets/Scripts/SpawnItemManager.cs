using Photon.Pun;  // Для использования Photon
using System.Collections;
using System.Linq;
using UnityEngine;

public class SpawnItemManager : MonoBehaviourPunCallbacks
{
    public static SpawnItemManager Instance;

    [SerializeField] private GameObject[] spawnPoints; // Ссылки на места для спавна
    [SerializeField] private GameObject[] items; // Ссылки на предметы
    [SerializeField] private float respawnTime = 30f; // Время для респавна

    private bool[] spawnPointAvailable; // Следим за доступностью каждой точки
    private string[] activeItemNames; // Имена заспавненных префабов на точках
    private GameObject[] spawnedObjects; // Ссылки на созданные объекты на сцене

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        spawnPoints = GameObject.FindGameObjectsWithTag("ItemSpawnPoint")
                                .OrderBy(go => go.name)
                                .ToArray();

        // Если точек спавна нет вообще, мы просто мягко отключаем скрипт,
        // так как это может быть режим командного боя.
        if (spawnPoints.Length == 0)
        {
            Debug.Log("SpawnItemManager: Точки спавна не найдены на этой карте. Скрипт отключен.");
            enabled = false;
            return;
        }

        if (items.Length == 0)
        {
            Debug.LogError("SpawnItemManager: Префабы предметов не назначены в инспекторе!");
            return;
        }

        DeactivateAllItems();

        int length = spawnPoints.Length;
        spawnPointAvailable = new bool[length];
        activeItemNames = new string[length];
        spawnedObjects = new GameObject[length];

        for (int i = 0; i < length; i++)
        {
            spawnPointAvailable[i] = true;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            for (int i = 0; i < length; i++)
            {
                SpawnRandomItemAtPoint(i);
            }
        }
        else
        {
            photonView.RPC("RPC_RequestCurrentItemsState", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    private void DeactivateAllItems()
    {
        foreach (GameObject item in items)
        {
            if (item != null)
            {
                item.SetActive(false);
            }
        }
    }

    private void SpawnRandomItemAtPoint(int spawnPointIndex)
    {
        if (spawnPointIndex < 0 || spawnPointIndex >= spawnPoints.Length || !spawnPointAvailable[spawnPointIndex])
        {
            return;
        }

        int randomItemIndex = Random.Range(0, items.Length);
        GameObject itemToSpawnPrefab = items[randomItemIndex];

        if (itemToSpawnPrefab != null)
        {
            activeItemNames[spawnPointIndex] = itemToSpawnPrefab.name;
            spawnPointAvailable[spawnPointIndex] = false;

            // Отправляем всем клиентам команду создать этот предмет
            photonView.RPC("SpawnItemAtPoint_RPC", RpcTarget.All, spawnPointIndex, itemToSpawnPrefab.name);
        }
    }

    public void CollectItem(int spawnPointIndex)
    {
        photonView.RPC("RPC_OnItemCollected", RpcTarget.All, spawnPointIndex);
    }

    [PunRPC]
    private void RPC_OnItemCollected(int spawnPointIndex)
    {
        if (spawnPointIndex < 0 || spawnPointIndex >= spawnPoints.Length) return;

        // Удаляем объект у всех игроков на сцене
        if (spawnedObjects[spawnPointIndex] != null)
        {
            Destroy(spawnedObjects[spawnPointIndex]);
            spawnedObjects[spawnPointIndex] = null;
        }

        activeItemNames[spawnPointIndex] = null;
        spawnPointAvailable[spawnPointIndex] = false; // Точка уходит на перезарядку

        // Только Мастер-клиент запускает таймер для последующего спавна
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(RespawnTimer(spawnPointIndex));
        }
    }

    public void StartRespawnTimer(int spawnPointIndex)
    {
        if (spawnPointIndex >= 0 && spawnPointIndex < spawnPoints.Length)
        {
            StartCoroutine(RespawnTimer(spawnPointIndex));
        }
    }

    private IEnumerator RespawnTimer(int spawnPointIndex)
    {
        yield return new WaitForSeconds(respawnTime);
        spawnPointAvailable[spawnPointIndex] = true; // Освобождаем точку

        // Запускаем спавн нового предмета только у MasterClient
        if (PhotonNetwork.IsMasterClient)
        {
            SpawnRandomItemAtPoint(spawnPointIndex); // Респавним новый предмет
        }
    }

    // RPC метод для спавна предмета на всех клиентах
    [PunRPC]
    private void SpawnItemAtPoint_RPC(int spawnPointIndex, string itemName)
    {
        GameObject itemToSpawnPrefab = System.Array.Find(items, item => item.name == itemName);

        if (itemToSpawnPrefab != null)
        {
            Vector3 spawnPosition = spawnPoints[spawnPointIndex].transform.position + Vector3.up * 1.5f;
            Quaternion spawnRotation = spawnPoints[spawnPointIndex].transform.rotation;

            // Создаем предмет
            GameObject spawnedItem = Instantiate(itemToSpawnPrefab, spawnPosition, spawnRotation);
            spawnedItem.SetActive(true);

            // Привязываем события для предметов, чтобы отслеживать, когда они собраны
            PickableItem pickableItem = spawnedItem.GetComponent<PickableItem>();
            if (pickableItem != null)
            {
                pickableItem.spawnPointIndex = spawnPointIndex;
            }
        }
        else
        {
            Debug.LogError($"Item with name {itemName} not found!");
        }
    }

    [PunRPC]
    private void RPC_RequestCurrentItemsState(int requestingActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Photon.Realtime.Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(requestingActorNumber);
        if (targetPlayer != null)
        {
            // Мастер высылает зашедшему игроку список всех активных на данный момент предметов
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (!spawnPointAvailable[i] && !string.IsNullOrEmpty(activeItemNames[i]))
                {
                    photonView.RPC("SpawnItemAtPoint_RPC", targetPlayer, i, activeItemNames[i]);
                }
            }
        }
    }
}

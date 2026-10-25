using UnityEngine;
using Photon.Pun;
using System.IO;

public class SceneInitializer : MonoBehaviour
{
    void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.InstantiateRoomObject(Path.Combine("PhotonPrefabs", "SpawnItemManager"), Vector3.zero, Quaternion.identity);
            Debug.Log("[SceneInitializer] SpawnItemManager успешно заспавнен MasterClient.");
        }

        // 2. Спавним менеджер локального игрока
        PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerManager"), Vector3.zero, Quaternion.identity);
    }
}
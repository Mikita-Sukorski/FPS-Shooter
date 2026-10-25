using UnityEngine;

public class PickableItem : MonoBehaviour
{
    public enum ItemType { Health, AmmoAR, AmmoSG, AmmoRifle }
    public ItemType itemType;

    public int spawnPointIndex; // Индекс точки спавна

    // Событие, которое будет вызываться, когда предмет собран
    public delegate void ItemCollectedHandler(int spawnPointIndex);
    public event ItemCollectedHandler OnItemCollected;

    private void Update()
    {
        // Вращение предмета вокруг оси Y
        transform.Rotate(0, 50 * Time.deltaTime, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null && player.photonView.IsMine)
            {
                ApplyEffect(player);

                // Сообщаем менеджеру по сети, что предмет собран
                if (SpawnItemManager.Instance != null)
                {
                    SpawnItemManager.Instance.CollectItem(spawnPointIndex);
                }

                // Локально выключаем сразу для мгновенного отклика
                gameObject.SetActive(false);
            }
        }
    }

    private void ApplyEffect(PlayerController player)
    {
        switch (itemType)
        {
            case ItemType.Health:
                player.Heal(25);
                break;
            case ItemType.AmmoAR:
                player.AddAmmo("AssaultRifle", 30);
                break;
            case ItemType.AmmoSG:
                player.AddAmmo("Shotgun", 8);
                break;
            case ItemType.AmmoRifle:
                player.AddAmmo("Rifle", 5);
                break;
        }
    }
}

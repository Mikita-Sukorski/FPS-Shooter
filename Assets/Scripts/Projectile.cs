// ЭТАЛОННЫЙ СКРИПТ ПУЛИ
using UnityEngine;
using Photon.Pun;

public class Projectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float damage;
    private PhotonView ownerPV;
    private GameObject bloodPrefab;
    private GameObject wallPrefab;

    private Vector3 lastPosition;

    public void Initialize(Vector3 dir, float spd, float dmg, PhotonView owner, GameObject blood, GameObject wall)
    {
        direction = dir.normalized;
        transform.forward = direction;
        speed = spd;
        damage = dmg;
        ownerPV = owner;
        bloodPrefab = blood;
        wallPrefab = wall;
        lastPosition = transform.position;
        Destroy(gameObject, 5f); // Самоликвидация
    }

    void Update()
    {
        Vector3 nextPosition = transform.position + direction * speed * Time.deltaTime;

        // РЕШЕНИЕ ПРОБЛЕМЫ ПРОЛЕТА СКВОЗЬ СТЕНЫ:
        // Используем Raycast от старой позиции к новой
        float distance = Vector3.Distance(lastPosition, nextPosition);
        RaycastHit hit;

        // Маска: стреляем во всё, кроме стрелка (слой LocalPlayer)
        int layerMask = ~(1 << LayerMask.NameToLayer("LocalPlayer"));

        if (Physics.Raycast(lastPosition, direction, out hit, distance, layerMask))
        {
            OnHit(hit);
        }

        lastPosition = transform.position;
        transform.position = nextPosition;
    }

    void OnHit(RaycastHit hit)
    {
        // Проверяем, есть ли на объекте (или его родителях) IDamageable
        IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
        PhotonView targetPV = hit.collider.GetComponentInParent<PhotonView>();

        // === НОВАЯ ПРОВЕРКА ===
        // Если мы попали в объект, который принадлежит НАМ ЖЕ (наш PhotonView),
        // мы просто выходим из метода и НЕ уничтожаем пулю. Она пролетит насквозь.
        if (targetPV != null && targetPV == ownerPV)
        {
            return;
        }
        // ======================

        bool isFlesh = (damageable != null);
        int targetID = (targetPV != null) ? targetPV.ViewID : -1;

        // Вызываем RPC через владельца пули, чтобы заспавнить эффекты у всех
        if (ownerPV.IsMine)
        {
            ownerPV.RPC("RPC_SpawnImpact", RpcTarget.All, hit.point, hit.normal, isFlesh, targetID);

            if (damageable != null)
            {
                damageable.TakeDamage(damage);

                PlayerController shooterPC = ownerPV.GetComponent<PlayerController>();
                if (shooterPC != null)
                {
                    shooterPC.SpawnLocalDamageNumber(hit.point, damage);
                }
            }
        }

        Destroy(gameObject);
    }
}
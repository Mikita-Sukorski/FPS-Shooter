using Photon.Pun;
using UnityEngine;

public class AssaultRifle : Gun
{
    [SerializeField] Camera cam;

    protected override void ExecuteShootLogic()
    {
        float finalSpread = (playerController != null && playerController.isAiming) ? 0f : currentSpread;

        // Генерируем точку ВНУТРИ круга
        Vector2 shotDeviation = Random.insideUnitCircle * finalSpread;

        // Применяем горизонтальный и вертикальный модификаторы рукоятки к отклонению
        if (equippedGrip != null && equippedGrip.isGrip)
        {
            shotDeviation.x *= equippedGrip.horizontalSpreadMultiplier; // Ось X (влево-вправо)
            shotDeviation.y *= equippedGrip.verticalSpreadMultiplier;   // Ось Y (вверх-вниз)
        }

        // Применяем отклонение по осям X и Y (shotDeviation.y отвечает за наклон вверх/вниз, shotDeviation.x за поворот влево/вправо)
        Quaternion spreadRotation = Quaternion.Euler(shotDeviation.y, shotDeviation.x, 0);

        // Направление из камеры
        Vector3 shootDirection = cam.transform.rotation * spreadRotation * Vector3.forward;

        Vector3 origin = muzzleFlashPoint.position;
        Ray ray = new Ray(cam.transform.position, shootDirection);

        Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 500f, ~LayerMask.GetMask("LocalPlayer"))
            ? hit.point : ray.GetPoint(500f);

        Vector3 direction = (targetPoint - origin).normalized;

        SpawnProjectile(origin, direction, true);
        PV.RPC("RPC_ShootProjectile", RpcTarget.Others, origin, direction);
    }
}
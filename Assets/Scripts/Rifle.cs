using Photon.Pun;
using UnityEngine;

public class Rifle : Gun
{
    [SerializeField] Camera cam;

    protected override void ExecuteShootLogic()
    {
        float finalSpread = (playerController != null && playerController.isAiming) ? 0f : currentSpread;

        Vector2 shotDeviation = Random.insideUnitCircle * finalSpread;

        // Применяем раздельные множители разброса
        if (equippedGrip != null && equippedGrip.isGrip)
        {
            shotDeviation.x *= equippedGrip.horizontalSpreadMultiplier;
            shotDeviation.y *= equippedGrip.verticalSpreadMultiplier;
        }

        Quaternion spreadRotation = Quaternion.Euler(shotDeviation.y, shotDeviation.x, 0);

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
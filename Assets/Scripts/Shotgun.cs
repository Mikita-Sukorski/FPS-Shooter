using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : Gun
{
    [SerializeField] Camera cam;
    public int pellets = 8;
    public float spread = 0.05f;

    public float adsSpreadMultiplier = 0.4f;
    protected override void ExecuteShootLogic()
    {
        Vector3 origin = muzzleFlashPoint.position;
        Vector3[] directions = new Vector3[pellets];

        float finalSpread = (playerController != null && playerController.isAiming)
            ? currentSpread * adsSpreadMultiplier
            : currentSpread;

        for (int i = 0; i < pellets; i++)
        {
            Vector2 circle = Random.insideUnitCircle * finalSpread;

            // Применяем модификаторы к дробинкам
            if (equippedGrip != null && equippedGrip.isGrip)
            {
                // circle.x отвечает за тангаж (X Rotation в Euler -> вертикаль)
                circle.x *= equippedGrip.verticalSpreadMultiplier;
                // circle.y отвечает за рыскание (Y Rotation в Euler -> горизонталь)
                circle.y *= equippedGrip.horizontalSpreadMultiplier;
            }

            Quaternion spreadRotation = Quaternion.Euler(circle.x, circle.y, 0);

            Vector3 shootDirection = cam.transform.rotation * spreadRotation * Vector3.forward;

            Ray ray = new Ray(cam.transform.position, shootDirection);
            Vector3 targetPoint = Physics.Raycast(ray, out RaycastHit hit, 500f, ~LayerMask.GetMask("LocalPlayer"))
                ? hit.point : ray.GetPoint(500f);

            directions[i] = (targetPoint - origin).normalized;
            SpawnProjectile(origin, directions[i], true);
        }

        PV.RPC("RPC_ShootShotgun", RpcTarget.Others, origin, directions);
    }
}
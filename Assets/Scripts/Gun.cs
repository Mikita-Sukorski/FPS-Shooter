using Photon.Pun;
using System.Collections;
using UnityEngine;

public abstract class Gun : Item
{
    [Header("Base Gun Settings")]
    public GunInfo gunInfo;
    public GameObject bulletImpactPrefab;
    public GameObject bloodImpactPrefab;
    public Transform muzzleFlashPoint;

    [Header("Muzzle Flash Settings")]
    public GameObject muzzleFlashFPSPrefab;
    public GameObject muzzleFlashThirdPersonPrefab;
    [Range(0.1f, 2f)] public float fpsMuzzleFlashScale = 0.5f;
    public Vector3 thirdPersonFlashRotationOffset = new Vector3(0, 180, 0);

    [Header("Ammo & UI")]
    public int currentAmmo;
    public int totalAmmo;
    public string ammoType;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    [Range(0f, 1f)][SerializeField] private float localShootVolume = 0.35f;

    [Header("Reload Sounds (Animation Events)")]
    public AudioClip magOutSound;
    public AudioClip magInSound;
    public AudioClip boltPullSound;
    public AudioClip boltReleaseSound;
    public AudioClip shellInsertSound;

    [Header("Shotgun Settings")]
    [SerializeField] protected float shotgunReloadEndDelay = 0.9f;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;
    public float bulletSpeed = 100f;

    [Header("Shells & Physics (Local)")]
    [SerializeField] private GameObject shellPrefab;
    [SerializeField] private Transform shellEjectPoint;
    [SerializeField] private GameObject magazinePrefab;
    public Transform animatedMagazineObject;

    [Header("Animations")]
    public string shootAnimationStateName = "Shoot";

    [Header("Active Attachments (Assigned by PlayerController)")]
    [HideInInspector] public AttachmentData equippedMuzzle;
    [HideInInspector] public AttachmentData equippedGrip;

    protected float currentSpread;
    protected PhotonView PV;
    protected bool isReloading;
    protected bool isShooting;

    protected PlayerController playerController;
    protected CrosshairController crosshair;
    protected bool wantToShootDuringReload;

    protected Animator animator;
    private Coroutine currentReloadRoutine;
    private float nextTimeCanShoot;
    private Camera cachedCamera;

    protected virtual void Awake()
    {
        PV = GetComponentInParent<PhotonView>();
        playerController = GetComponentInParent<PlayerController>();
        animator = GetComponentInChildren<Animator>();

        if (playerController != null)
        {
            cachedCamera = playerController.GetComponentInChildren<Camera>();
        }

        if (PV.IsMine)
        {
            crosshair = GetComponentInParent<PlayerController>().GetComponentInChildren<CrosshairController>();
        }

        if (gunInfo != null) currentAmmo = gunInfo.maxAmmo;
    }

    protected virtual void OnEnable()
    {
        isShooting = false;
        isReloading = false;
        wantToShootDuringReload = false;

        if (animator != null)
        {
            animator.ResetTrigger("Shoot");
            animator.ResetTrigger("Reload");
            animator.ResetTrigger("Hide");

            if (HasAnimatorParameter(animator, "StopReload"))
            {
                animator.ResetTrigger("StopReload");
            }
        }
    }

    private bool HasAnimatorParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    protected virtual void OnDisable()
    {
        isShooting = false;
        isReloading = false;

        if (currentReloadRoutine != null)
        {
            StopCoroutine(currentReloadRoutine);
            currentReloadRoutine = null;
        }

        if (animator != null)
        {
            animator.Rebind();
        }

        StopAllCoroutines();
    }

    public void SetCrosshair(CrosshairController controller)
    {
        crosshair = controller;
    }

    protected void UpdateSpreadUI()
    {
        if (PV.IsMine && crosshair != null && playerController != null && cachedCamera != null)
        {
            float screenHeight = Screen.height;
            float currentFOV = cachedCamera.fieldOfView;
            float pixelOffset = (currentSpread / currentFOV) * screenHeight * gunInfo.crosshairSizeMultiplier;
            crosshair.UpdateCrosshair(pixelOffset);
        }
    }

    protected virtual void Update()
    {
        if (PV.IsMine)
        {
            float currentRecovery = isShooting ? gunInfo.recoveryShooting : gunInfo.spreadRecovery;

            float minSpread = gunInfo.spreadMin;
            float maxSpread = gunInfo.spreadMax;

            // Для круглой UI-зоны берем средний коэффициент между вертикалью и горизонталью
            if (equippedGrip != null && equippedGrip.isGrip)
            {
                float averageGripSpreadMod = (equippedGrip.horizontalSpreadMultiplier + equippedGrip.verticalSpreadMultiplier) / 2f;
                minSpread *= averageGripSpreadMod;
                maxSpread *= averageGripSpreadMod;
            }

            if (currentSpread > minSpread)
            {
                currentSpread -= currentRecovery * Time.deltaTime;
            }

            currentSpread = Mathf.Clamp(currentSpread, minSpread, maxSpread);
            UpdateSpreadUI();

            if (animator != null && playerController != null)
            {
                int shootLayerIndex = animator.GetLayerIndex("Shoot Layer");
                if (shootLayerIndex != -1)
                {
                    float targetWeight = playerController.isAiming ? 0.15f : 1.0f;
                    float currentWeight = animator.GetLayerWeight(shootLayerIndex);
                    animator.SetLayerWeight(shootLayerIndex, Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * 10f));
                }
            }
        }
    }

    private void Shoot()
    {
        if (currentAmmo <= 0) return;
        currentAmmo--;

        ExecuteShootLogic();

        float spreadIncrease = gunInfo.spreadPerShot;
        if (equippedGrip != null && equippedGrip.isGrip)
        {
            float averageGripSpreadMod = (equippedGrip.horizontalSpreadMultiplier + equippedGrip.verticalSpreadMultiplier) / 2f;
            spreadIncrease *= averageGripSpreadMod;
        }
        currentSpread += spreadIncrease;

        if (PV.IsMine && playerController != null)
            playerController.UpdateAmmoHUD();

        PlayShootEffects();
        PV.RPC(nameof(PlayerController.RPC_PlayShootEffects), RpcTarget.Others);
    }

    public override void Use()
    {
        if (isReloading)
        {
            if (gunInfo.reloadShellByShell) wantToShootDuringReload = true;
            return;
        }

        if (currentAmmo > 0 && !isShooting)
        {
            StartCoroutine(FireRoutine());
        }
        else if (currentAmmo <= 0 && totalAmmo > 0)
        {
            Reload();
        }
    }

    private IEnumerator FireRoutine()
    {
        isShooting = true;

        while (currentAmmo > 0 && !isReloading)
        {
            if (Time.time >= nextTimeCanShoot)
            {
                nextTimeCanShoot = Time.time + gunInfo.fireRate;
                Shoot();
            }

            yield return null;

            if (!gunInfo.isAutomatic || !Input.GetMouseButton(0))
                break;
        }

        if (currentAmmo <= 0 && !isReloading && totalAmmo > 0)
        {
            Reload();
        }

        isShooting = false;
    }

    protected abstract void ExecuteShootLogic();

    protected void HandleHit(Vector3 origin, Vector3 direction)
    {
        int layerMask = ~LayerMask.GetMask("LocalPlayer");

        if (Physics.Raycast(origin, direction, out RaycastHit hit, 500f, layerMask))
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            PhotonView targetPV = hit.collider.GetComponentInParent<PhotonView>();
            int targetViewID = targetPV != null ? targetPV.ViewID : -1;

            if (damageable != null)
            {
                float baseDamage = Random.Range(gunInfo.minDamage, gunInfo.maxDamage);
                float finalDamage = baseDamage;

                if (equippedMuzzle != null && equippedMuzzle.isMuzzle)
                {
                    finalDamage *= equippedMuzzle.damageMultiplier;
                }

                damageable.TakeDamage(finalDamage);

                if (PV.IsMine && playerController != null)
                {
                    playerController.SpawnLocalDamageNumber(hit.point, finalDamage);
                }

                PV.RPC(nameof(PlayerController.RPC_SpawnImpact), RpcTarget.All, hit.point, hit.normal, true, targetViewID);
            }
            else
            {
                PV.RPC(nameof(PlayerController.RPC_SpawnImpact), RpcTarget.All, hit.point, hit.normal, false, targetViewID);
            }
        }
    }

    public void PlayShootEffects()
    {
        if (audioSource && shootSound)
        {
            float volume = PV.IsMine ? localShootVolume : 1f;
            float pitch = 1f;

            if (equippedMuzzle != null && equippedMuzzle.isMuzzle)
            {
                volume *= equippedMuzzle.soundVolumeMultiplier;
                pitch = equippedMuzzle.soundPitch;
            }

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(shootSound, volume);
        }

        bool shouldSpawnFlash = true;
        float scaleMultiplier = PV.IsMine ? fpsMuzzleFlashScale : 1f;

        if (equippedMuzzle != null && equippedMuzzle.isMuzzle)
        {
            if (equippedMuzzle.hideMuzzleFlash)
            {
                shouldSpawnFlash = false;
            }
            else if (PV.IsMine)
            {
                scaleMultiplier *= equippedMuzzle.localFlashScaleMultiplier;
            }
        }

        if (shouldSpawnFlash && muzzleFlashPoint != null)
        {
            GameObject flashPrefab = PV.IsMine ? muzzleFlashFPSPrefab : muzzleFlashThirdPersonPrefab;

            if (flashPrefab != null)
            {
                GameObject flash = Instantiate(flashPrefab, muzzleFlashPoint.position, muzzleFlashPoint.rotation);
                flash.transform.SetParent(muzzleFlashPoint);

                if (!PV.IsMine)
                {
                    flash.transform.localRotation = Quaternion.Euler(thirdPersonFlashRotationOffset);
                }
                else
                {
                    flash.transform.localRotation = Quaternion.identity;
                }

                flash.transform.localScale = Vector3.one * scaleMultiplier;

                StartCoroutine(DisableMuzzleLightDelayed(flash, 0.05f));
                Destroy(flash, 0.4f);
            }
        }

        if (animator != null && !string.IsNullOrEmpty(shootAnimationStateName))
        {
            int shootLayerIndex = animator.GetLayerIndex("Shoot Layer");
            if (shootLayerIndex == -1) shootLayerIndex = 0;
            animator.Play(shootAnimationStateName, shootLayerIndex, 0f);
        }
    }

    private IEnumerator DisableMuzzleLightDelayed(GameObject flashInstance, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (flashInstance != null)
        {
            Light flashLight = flashInstance.GetComponentInChildren<Light>();
            if (flashLight != null)
            {
                flashLight.enabled = false;
                Destroy(flashLight.gameObject);
            }
        }
    }

    public override void Reload()
    {
        if (!isReloading && currentAmmo < gunInfo.maxAmmo && totalAmmo > 0)
        {
            currentReloadRoutine = StartCoroutine(ReloadCoroutine());
        }
    }

    private IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        wantToShootDuringReload = false;

        if (animator != null)
        {
            animator.ResetTrigger("StopReload");
            animator.SetTrigger("Reload");
        }

        if (gunInfo.reloadShellByShell)
        {
            while (currentAmmo < gunInfo.maxAmmo && totalAmmo > 0)
            {
                if (wantToShootDuringReload) break;

                if (animator == null)
                {
                    if (audioSource && shellInsertSound)
                        audioSource.PlayOneShot(shellInsertSound, localShootVolume);
                    else if (audioSource && reloadSound)
                        audioSource.PlayOneShot(reloadSound, localShootVolume);
                }

                yield return new WaitForSeconds(gunInfo.timePerShell);

                currentAmmo++;
                totalAmmo--;

                if (PV.IsMine && playerController != null)
                    playerController.UpdateAmmoHUD();

                if (wantToShootDuringReload) break;
            }

            if (animator != null)
            {
                animator.SetTrigger("StopReload");
            }
            yield return new WaitForSeconds(shotgunReloadEndDelay);
        }
        else
        {
            if (audioSource && reloadSound && magOutSound == null)
                audioSource.PlayOneShot(reloadSound, localShootVolume);

            yield return new WaitForSeconds(gunInfo.reloadTime);

            int needed = gunInfo.maxAmmo - currentAmmo;
            int toAdd = Mathf.Min(needed, totalAmmo);
            currentAmmo += toAdd;
            totalAmmo -= toAdd;
        }

        isReloading = false;
        currentReloadRoutine = null;

        if (PV.IsMine && playerController != null)
            playerController.UpdateAmmoHUD();

        if (wantToShootDuringReload)
        {
            wantToShootDuringReload = false;
            if (currentAmmo > 0)
            {
                StartCoroutine(FireRoutine());
            }
        }
    }

    public void PlayHideAnimation()
    {
        if (animator != null) animator.SetTrigger("Hide");
    }

    #region Physics Spawners
    private void SetupPhysicsFailsafe(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col == null)
        {
            MeshRenderer meshRenderer = obj.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                BoxCollider box = obj.AddComponent<BoxCollider>();
                box.size = meshRenderer.bounds.size * 0.9f;
            }
            else
            {
                obj.AddComponent<SphereCollider>();
            }
        }

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();

        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void OnAnimationEjectShell()
    {
        if (PV.IsMine && shellPrefab != null && shellEjectPoint != null)
        {
            GameObject shell = Instantiate(shellPrefab, shellEjectPoint.position, shellEjectPoint.rotation);
            SetupPhysicsFailsafe(shell);

            if (playerController != null)
            {
                Collider playerCollider = playerController.GetComponent<Collider>();
                Collider shellCollider = shell.GetComponent<Collider>();
                if (playerCollider != null && shellCollider != null)
                {
                    Physics.IgnoreCollision(shellCollider, playerCollider);
                }
            }

            Rigidbody rb = shell.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 forceDirection = shellEjectPoint.right * Random.Range(1.5f, 3f) +
                                         shellEjectPoint.up * Random.Range(1f, 2f) +
                                         shellEjectPoint.forward * Random.Range(-0.5f, 0.5f);

                rb.AddForce(forceDirection, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * Random.Range(10f, 30f), ForceMode.Impulse);
            }
            Destroy(shell, 5f);
        }
    }

    public void OnAnimationDropMagazine()
    {
        if (PV.IsMine && magazinePrefab != null && animatedMagazineObject != null)
        {
            GameObject mag = Instantiate(magazinePrefab, animatedMagazineObject.position, animatedMagazineObject.rotation);
            SetupPhysicsFailsafe(mag);

            if (playerController != null)
            {
                Collider playerCollider = playerController.GetComponent<Collider>();
                Collider shellCollider = mag.GetComponent<Collider>();
                if (playerCollider != null && shellCollider != null)
                {
                    Physics.IgnoreCollision(shellCollider, playerCollider);
                }
            }

            Rigidbody rb = mag.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddRelativeTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
            Destroy(mag, 10f);
        }
    }
    #endregion

    public void InterruptReload()
    {
        if (isReloading)
        {
            if (currentReloadRoutine != null)
            {
                StopCoroutine(currentReloadRoutine);
                currentReloadRoutine = null;
            }

            isReloading = false;
            wantToShootDuringReload = false;

            if (PV.IsMine && playerController != null)
            {
                playerController.UpdateAmmoHUD();
            }
        }
    }

    public override void AddAmmo(int amount)
    {
        totalAmmo += amount;

        if (PV == null) PV = GetComponentInParent<PhotonView>();
        if (playerController == null) playerController = GetComponentInParent<PlayerController>();

        if (PV != null && PV.IsMine && playerController != null)
        {
            playerController.UpdateAmmoHUD();
        }
    }

    public void SpawnProjectile(Vector3 origin, Vector3 direction, bool isMine)
    {
        float speedMod = equippedMuzzle != null && equippedMuzzle.isMuzzle ? equippedMuzzle.bulletSpeedMultiplier : 1f;
        float finalSpeed = bulletSpeed * speedMod;

        float baseDamage = isMine ? Random.Range(gunInfo.minDamage, gunInfo.maxDamage) : 0;
        float damageMod = equippedMuzzle != null && equippedMuzzle.isMuzzle ? equippedMuzzle.damageMultiplier : 1f;
        float finalDamage = baseDamage * damageMod;

        GameObject bulletObj = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));
        Projectile proj = bulletObj.GetComponent<Projectile>();

        proj.Initialize(direction, finalSpeed, finalDamage, PV, bloodImpactPrefab, bulletImpactPrefab);
    }

    #region Animation Sound Events
    public void PlaySoundMagOut()
    {
        if (audioSource && magOutSound) audioSource.PlayOneShot(magOutSound, localShootVolume);
    }

    public void PlaySoundMagIn()
    {
        if (audioSource && magInSound) audioSource.PlayOneShot(magInSound, localShootVolume);
    }

    public void PlaySoundBoltPull()
    {
        if (audioSource && boltPullSound) audioSource.PlayOneShot(boltPullSound, localShootVolume);
    }

    public void PlaySoundBoltRelease()
    {
        if (audioSource && boltReleaseSound) audioSource.PlayOneShot(boltReleaseSound, localShootVolume);
    }

    public void PlaySoundShellInsert()
    {
        if (audioSource && shellInsertSound) audioSource.PlayOneShot(shellInsertSound, localShootVolume);
    }
    #endregion
}
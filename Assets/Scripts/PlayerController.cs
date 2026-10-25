using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PlayerController : MonoBehaviourPunCallbacks, IDamageable, IPunObservable
{
    [Header("Weapon & Customization Settings")]
    public GameObject[] weaponHolders;
    public WeaponData[] allWeapons;
    public AttachmentDatabase db;

    [Header("Movement Settings")]
    [SerializeField] float mouseSensitivity = 2f, sprintSpeed = 7f, walkSpeed = 4f, jumpForce = 300f, smoothTime = 0.1f;
    [SerializeField] float normalFOV = 60f, zoomSpeed = 5f;
    [SerializeField] float maxFallSpeed = 20f, fallGravityMultiplier = 2.5f;

    [Header("Player Audio Settings")]
    [SerializeField] private AudioSource playerAudioSource;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float baseStepInterval = 0.6f; // Интервал шагов при ходьбе (в секундах)
    [SerializeField] private float sprintStepInterval = 0.35f; // Интервал шагов при спринте

    [Space(10)]
    [SerializeField] private AudioClip hitDealtSound;      // Глухой звук попадания по врагу
    [SerializeField] private AudioClip killDealtSound;     // Звонкий звук убийства врага
    [SerializeField] private AudioClip hitReceivedSound;   // Звук попадания по мне (глухой)

    private float footstepTimer;

    [Header("UI References (Main HUD)")]
    [SerializeField] GameObject mainHUD; // Твой Screen Space Canvas
    [SerializeField] Image healthbarImage;
    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] TextMeshProUGUI ammoHUDText; // ТОТ САМЫЙ общий текст патронов

    [Header("Modern UI Settings")]
    [SerializeField] private RectTransform healthUIContainer; // Контейнер здоровья для анимации
    [SerializeField] private Color healthNormalColor = Color.white;
    [SerializeField] private Color healthDamageColor = Color.red;

    [Header("Weapon UI Animation")]
    [SerializeField] private Sprite[] weaponIcons; // Спрайты пушек (Автомат, Дробовик, Снайперка)
    [SerializeField] private RectTransform activeWeaponRect; // Трансформ основной иконки
    [SerializeField] private CanvasGroup activeWeaponCanvasGroup; // Прозрачность основной
    [SerializeField] private RectTransform transitionWeaponRect; // Трансформ уходящей иконки
    [SerializeField] private CanvasGroup transitionWeaponCanvasGroup; // Прозрачность уходящей
    [SerializeField] private Image activeWeaponImage;
    [SerializeField] private Image transitionWeaponImage;

    private Coroutine healthAnimCoroutine;
    private Coroutine weaponSwitchCoroutine;

    [Header("Core References")]
    [SerializeField] GameObject cameraHolder;
    [SerializeField] GameObject bodyVisual; // Ссылка на твою капсулу (слой RemotePlayer)

    [Header("Customization Sync")]
    [SerializeField] private MeshRenderer playerMeshRenderer; // Меш удаленного игрока (капсула/тело)
    [SerializeField] private Transform hatAttachPoint;      // Кость головы на префабе игрока
    [SerializeField] private Transform maskAttachPoint;     // Кость лица на префабе игрока

    [SerializeField] private CustomItem[] hats;
    [SerializeField] private CustomItem[] masks;
    [SerializeField] private Material baseRGBMaterial;
    [SerializeField] private Material[] customMaterials;

    [Header("Team Outline")]
    [SerializeField] private Material outlineMaterialPrefab; // Сюда закинешь TeamOutlineMaterial
    private Material outlineInstance;

    [Header("Death Settings")]
    [SerializeField] GameObject deathEffectPrefab;

    [Header("VFX Settings")]
    [SerializeField] GameObject debrisCubePrefab; // Сюда положим префаб нашего кубика
    [SerializeField] private GameObject damageNumberPrefab;

    [Header("Aiming Settings")]
    [SerializeField] Transform adsParent; // Объект, который мы будем двигать (ItemHolder)
    [SerializeField] float adsSpeed = 10f;
    private Vector3 weaponHolderOrigin;
    public bool isAiming { get; private set; }
    private AttachmentData currentScopeData;
    private Transform currentAimPoint; // Точка в прицеле, которую мы центрируем
    private Vector3 staticAimPointLocalOffset;

    private Vector3[] cachedAimOffsets;
    private string[] cachedScopeNames;
    private bool[] hasCachedOffset;

    public static Dictionary<Player, PlayerController> AllPlayers = new Dictionary<Player, PlayerController>();

    private GameObject currentCrosshair;

    private Camera playerCamera;
    private Camera activeScopeCam;
    private Rigidbody rb;
    private PhotonView PV;
    private PlayerManager playerManager;

    private float verticalLookRotation;
    private bool grounded;
    private Vector3 smoothMoveVelocity;
    private Vector3 moveAmount;

    private float currentHealth = 100f;
    private const float maxHealth = 100f;
    private bool isDead = false;
    private bool isMenuOpen = false;
    private int currentWeaponIndex = 0;

    private Transform killerTransform;

    private GameObject currentHatObj;
    private GameObject currentMaskObj;

    private bool isSwitchingWeapon = false;

    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private float lagTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        PV = GetComponent<PhotonView>();

        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();
    }

    void Start()
    {
        if (PV.Owner != null)
        {
            AllPlayers[PV.Owner] = this;
        }

        isDead = false;
        killerTransform = null;

        weaponHolderOrigin = weaponHolders[0].transform.parent.localPosition;

        if (PV.IsMine)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Делает движение плавным
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            cachedAimOffsets = new Vector3[weaponHolders.Length];
            cachedScopeNames = new string[weaponHolders.Length];
            hasCachedOffset = new bool[weaponHolders.Length];

            // Сбрасываем положение и поворот камеры к дефолтным
            cameraHolder.transform.localRotation = Quaternion.identity;
            verticalLookRotation = 0; // Обнуляем угол наклона

            gameObject.layer = LayerMask.NameToLayer("LocalPlayer");

            foreach (GameObject weapon in weaponHolders)
            {
                weapon.SetActive(false);
                SetLayerRecursive(weapon, LayerMask.NameToLayer("LocalPlayer"));
            }

            if (bodyVisual != null)
            {
                bodyVisual.SetActive(true);

                foreach (MeshRenderer mr in bodyVisual.GetComponentsInChildren<MeshRenderer>(true))
                {
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
                foreach (SkinnedMeshRenderer smr in bodyVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }

            playerCamera = GetComponentInChildren<Camera>();
            if (mainHUD) mainHUD.SetActive(true);

            UpdateHealthDisplay();
            DisableCursor();

            currentWeaponIndex = 0;
            ChangeWeaponLocal(currentWeaponIndex);

            ApplyCustomizationSync();
        }
        else
        {
            SetLayerRecursive(gameObject, LayerMask.NameToLayer("RemotePlayer"));

            if (bodyVisual != null) bodyVisual.SetActive(true);

            Camera remoteCam = GetComponentInChildren<Camera>();
            if (remoteCam != null)
            {
                Destroy(remoteCam.gameObject);
            }

            if (rb)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete; // Сначала режим
                rb.isKinematic = true; // Потом кинематика
            }
            if (mainHUD) mainHUD.SetActive(false);

            RefreshRemoteVisuals();

            ApplyRemoteCustomization();

            ApplyTeamOutline();
        }

        StripWeaponUIForGameplay();
    }

    void Update()
    {
        if (!PV.IsMine)
        {
            Vector3 targetPosition = networkPosition;

            // Плавно приводим позицию и вращение удаленного игрока к сетевым значениям
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Slerp(transform.rotation, networkRotation, Time.deltaTime * 15f);

            // Плавный поворот камеры/головы
            Quaternion targetRot = Quaternion.Euler(-verticalLookRotation, 0, 0);
            cameraHolder.transform.localRotation = Quaternion.Slerp(
                cameraHolder.transform.localRotation,
                targetRot,
                Time.deltaTime * 15f
            );
            return;
        }

        if (isDead)
        {
            HandleKillcam();
            return;
        }

        HandleMenuInput();
        if (isMenuOpen) return;

        Look();
        Move();
        Jump();
        HandleWeaponSwitching();
        HandleFootsteps();

        if (!isSwitchingWeapon)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Item currentItem = weaponHolders[currentWeaponIndex].GetComponent<Item>();
                if (currentItem != null)
                {
                    currentItem.Use();
                    UpdateAmmoHUD();
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Item currentItem = weaponHolders[currentWeaponIndex].GetComponent<Item>();
                if (currentItem != null)
                {
                    currentItem.Reload();
                }
            }
        }

        if (transform.position.y < -10f && !isDead)
        {
            TakeDamage(9999f);
        }

        if (Input.GetMouseButton(1) && !isSwitchingWeapon)
        {
            isAiming = true;
        }
        else
        {
            isAiming = false;
        }
    }

    void LateUpdate()
    {
        if (!PV.IsMine || isDead || isMenuOpen) return;

        HandleAiming();
    }

    void HandleAiming()
    {
        if (adsParent == null || playerCamera == null) return;

        Vector3 targetPos;
        float targetFOV;

        if (isAiming && currentAimPoint != null && currentScopeData != null)
        {
            Vector3 camLocalPos = adsParent.parent.InverseTransformPoint(playerCamera.transform.position);

            // Находим целевую позицию, вычитая статический оффсет точки прицеливания
            targetPos = camLocalPos - staticAimPointLocalOffset + currentScopeData.aimOffset;

            if (activeScopeCam != null)
            {
                // Для PIP-прицелов с линзой: основная камера лишь слегка фокусируется, а ScopeCam делает основное приближение
                targetFOV = normalFOV - 12f;
            }
            else
            {
                // Для мушки и коллиматоров без камеры: зумим напрямую основную камеру игрока
                targetFOV = currentScopeData.zoomFOV;
            }

            // ВЫКЛЮЧАЕМ КРЕСТИК ОТ БЕДРА
            if (currentCrosshair != null) currentCrosshair.SetActive(false);
        }
        else if (isAiming)
        {
            // Резервный режим (если у оружия нет прицела вообще)
            targetPos = new Vector3(0, weaponHolderOrigin.y, weaponHolderOrigin.z);
            targetFOV = normalFOV - 10f;

            if (currentCrosshair != null) currentCrosshair.SetActive(false);
        }
        else
        {
            targetPos = weaponHolderOrigin;
            targetFOV = normalFOV;

            // ВКЛЮЧАЕМ КРЕСТИК ОТ БЕДРА ОБРАТНО
            if (currentCrosshair != null) currentCrosshair.SetActive(true);
        }

        adsParent.localPosition = Vector3.Lerp(adsParent.localPosition, targetPos, Time.deltaTime * adsSpeed);
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * adsSpeed);
    }

    void HandleKillcam()
    {
        if (playerCamera == null) return;

        if (killerTransform == null)
        {
            Quaternion lookDown = Quaternion.Euler(60f, cameraHolder.transform.eulerAngles.y, 0);
            cameraHolder.transform.rotation = Quaternion.Slerp(cameraHolder.transform.rotation, lookDown, Time.deltaTime * 2f);
            return;
        }

        Vector3 deathPoint = transform.position + Vector3.up * 1.5f;
        Vector3 killerPoint = killerTransform.position + Vector3.up * 1.5f;

        Vector3 dirToKiller = killerPoint - deathPoint;
        float currentDist = dirToKiller.magnitude;

        float minSafeDist = 3.5f;

        Vector3 targetCameraPos = deathPoint;

        if (currentDist < minSafeDist)
        {
            Vector3 pushDir = -dirToKiller.normalized;
            targetCameraPos = deathPoint + (pushDir * (minSafeDist - currentDist)) + Vector3.up * 0.5f;
        }

        cameraHolder.transform.position = Vector3.Lerp(cameraHolder.transform.position, targetCameraPos, Time.deltaTime * 10f);

        Vector3 lookDir = killerPoint - cameraHolder.transform.position;

        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            cameraHolder.transform.rotation = Quaternion.Slerp(cameraHolder.transform.rotation, targetRot, Time.deltaTime * 15f);
        }


        float targetFOV = Mathf.Clamp(60f - (currentDist * 1.5f), 15f, 60f);
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * 10f);
    }

    #region Networking Logic

    void ApplyTeamOutline()
    {
        // Локальному игроку обводка не нужна (мы не видим свое тело от 1-го лица)
        if (PV.IsMine || outlineMaterialPrefab == null || playerMeshRenderer == null) return;

        string currentMode = "";
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameSettings.GAME_MODE_KEY, out object modeObj))
        {
            currentMode = (string)modeObj;
        }

        if (currentMode != "TDM")
        {
            return;
        }

        int team = 0;
        // Замени "Team" на GameSettings.TEAM_KEY, если у тебя нет прямого доступа к константе отсюда
        if (PV.Owner.CustomProperties.TryGetValue(GameSettings.TEAM_KEY, out object teamObj))
        {
            team = (int)teamObj;
        }

        if (team == 0) return; // Если игрок вне команды (например, FFA режим)

        // Создаем уникальный инстанс материала для этого игрока
        if (outlineInstance == null)
            outlineInstance = new Material(outlineMaterialPrefab);

        // Определяем цвет: 1 = Красные, 2 = Синие
        Color outlineColor = (team == 1) ? Color.red : (team == 2) ? Color.blue : Color.white;

        outlineInstance.SetColor("_OutlineColor", outlineColor);
        outlineInstance.SetFloat("_OutlineWidth", 0.04f); // Толщина обводки (настрой под себя)

        // Добавляем материал обводки ВТОРЫМ слоем в MeshRenderer
        List<Material> mats = playerMeshRenderer.materials.ToList();

        // Проверяем, чтобы не добавить обводку дважды
        if (!mats.Exists(m => m.name.Contains(outlineInstance.name)))
        {
            mats.Add(outlineInstance);
            playerMeshRenderer.materials = mats.ToArray();
        }
    }

    void ApplyRemoteCustomization()
    {
        var props = PV.Owner.CustomProperties;

        // --- 1. ПРИМЕНЯЕМ ШАПКУ ---
        if (props.TryGetValue("C_Hat", out object hatIndexObj))
        {
            int hIndex = (int)hatIndexObj;
            if (currentHatObj != null) Destroy(currentHatObj);

            if (hIndex >= 0 && hIndex < hats.Length && hats[hIndex].prefab != null)
            {
                currentHatObj = Instantiate(hats[hIndex].prefab, hatAttachPoint);

                int layerToSet = PV.IsMine ? LayerMask.NameToLayer("LocalPlayer") : LayerMask.NameToLayer("RemotePlayer");

                SetLayerRecursive(currentHatObj, layerToSet);

                if (PV.IsMine)
                {
                    foreach (MeshRenderer mr in currentHatObj.GetComponentsInChildren<MeshRenderer>(true))
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    foreach (SkinnedMeshRenderer smr in currentHatObj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        // --- 2. ПРИМЕНЯЕМ МАСКУ ---
        if (props.TryGetValue("C_Mask", out object maskIndexObj))
        {
            int mIndex = (int)maskIndexObj;
            if (currentMaskObj != null) Destroy(currentMaskObj);

            if (mIndex >= 0 && mIndex < masks.Length && masks[mIndex].prefab != null)
            {
                currentMaskObj = Instantiate(masks[mIndex].prefab, maskAttachPoint);

                int layerToSet = PV.IsMine ? LayerMask.NameToLayer("LocalPlayer") : LayerMask.NameToLayer("RemotePlayer");

                SetLayerRecursive(currentMaskObj, layerToSet);

                if (PV.IsMine)
                {
                    foreach (MeshRenderer mr in currentMaskObj.GetComponentsInChildren<MeshRenderer>(true))
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    foreach (SkinnedMeshRenderer smr in currentMaskObj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        // --- 3. ПРИМЕНЯЕМ МАТЕРИАЛ И ЦВЕТ ---
        if (playerMeshRenderer != null && props.TryGetValue("C_MatIdx", out object matIdxObj))
        {
            int matIndex = (int)matIdxObj;

            if (matIndex == -1) // Режим RGB цвета
            {
                playerMeshRenderer.material = baseRGBMaterial;

                if (props.TryGetValue("C_Color", out object colorObj))
                {
                    string hexColor = (string)colorObj;
                    if (ColorUtility.TryParseHtmlString(hexColor, out Color parsedColor))
                    {
                        playerMeshRenderer.material.color = parsedColor;
                        if (playerMeshRenderer.material.HasProperty("_Color"))
                            playerMeshRenderer.material.SetColor("_Color", parsedColor);
                        if (playerMeshRenderer.material.HasProperty("_BaseColor"))
                            playerMeshRenderer.material.SetColor("_BaseColor", parsedColor);
                    }
                }
            }
            else if (matIndex >= 0 && matIndex < customMaterials.Length)
            {
                playerMeshRenderer.material = customMaterials[matIndex];
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Отправляем данные локального игрока
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(verticalLookRotation);
        }
        else
        {
            // Получаем данные удаленного игрока
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            this.verticalLookRotation = (float)stream.ReceiveNext();

            // Рассчитываем примерную задержку пакета (пинг) для компенсации
            lagTime = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (targetPlayer == PV.Owner && !PV.IsMine)
        {
            if (changedProps.ContainsKey("W_Index") || changedProps.ContainsKey("Muzzle") ||
                changedProps.ContainsKey("Scope") || changedProps.ContainsKey("Grip"))
            {
                RefreshRemoteVisuals();
            }
        }

        if (changedProps.ContainsKey("C_Hat") || changedProps.ContainsKey("C_Mask") ||
            changedProps.ContainsKey("C_MatIdx") || changedProps.ContainsKey("C_Color"))
        {
            ApplyRemoteCustomization();
        }

        if (targetPlayer == PV.Owner && !PV.IsMine)
        {
            if (changedProps.ContainsKey(GameSettings.TEAM_KEY))
            {
                ApplyTeamOutline();
            }
        }
    }

    #endregion

    #region Weapon Logic

    void HandleWeaponSwitching()
    {
        if (isSwitchingWeapon) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeWeaponLocal(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeWeaponLocal(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeWeaponLocal(2);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int nextIndex = currentWeaponIndex + (scroll > 0f ? -1 : 1);
            if (nextIndex < 0) nextIndex = weaponHolders.Length - 1;
            if (nextIndex >= weaponHolders.Length) nextIndex = 0;
            ChangeWeaponLocal(nextIndex);
        }
    }

    void ChangeWeaponLocal(int index)
    {
        if (index < 0 || index >= weaponHolders.Length || isSwitchingWeapon) return;
        if (currentWeaponIndex == index && weaponHolders[index].activeSelf) return;

        isAiming = false;
        isSwitchingWeapon = true;
        StartCoroutine(SwitchWeaponRoutine(index));
    }

    private IEnumerator SwitchWeaponRoutine(int newIndex)
    {
        Gun currentGun = weaponHolders[currentWeaponIndex].GetComponent<Gun>();
        if (currentGun != null && weaponHolders[currentWeaponIndex].activeSelf)
        {
            Debug.Log($"<color=yellow>[SwitchWeaponRoutine]</color> Начинаем убирать оружие: {currentGun.gameObject.name} (Индекс: {currentWeaponIndex})");

            currentGun.InterruptReload();
            currentGun.PlayHideAnimation();
            yield return new WaitForSeconds(0.25f); // Время убирания оружия
        }

        Debug.Log($"<color=green>[SwitchWeaponRoutine]</color> Активируем новое оружие с индексом: {newIndex}");

        ExecuteWeaponSwitchLogic(newIndex, false);

        // КРИТИЧЕСКИЙ МОМЕНТ: Ждем, пока корутина установки обвесов ПОЛНОСТЬЮ завершится,
        // прежде чем разрешить игроку целиться, стрелять и двигаться.
        if (attachmentCoroutine != null)
        {
            yield return attachmentCoroutine;
        }

        isSwitchingWeapon = false;
    }

    private void ExecuteWeaponSwitchLogic(int index, bool isFirstSpawn)
    {
        currentWeaponIndex = index;

        for (int i = 0; i < weaponHolders.Length; i++)
        {
            weaponHolders[i].SetActive(i == index);
        }

        string weaponName = allWeapons[index].weaponName;
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
    {
        { "W_Index", index },
        { "Muzzle", PlayerPrefs.GetString(weaponName + "_Muzzle", "None") },
        { "Scope", PlayerPrefs.GetString(weaponName + "_Scope", "None") },
        { "Grip", PlayerPrefs.GetString(weaponName + "_Grip", "None") }
    };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        ApplyAttachmentsToActiveWeapon(true);

        if (currentCrosshair != null) Destroy(currentCrosshair);

        Item itemScript = weaponHolders[index].GetComponent<Item>();
        if (itemScript != null && itemScript.crosshairPrefab != null && mainHUD != null)
        {
            currentCrosshair = Instantiate(itemScript.crosshairPrefab, mainHUD.transform);
            Gun currentGun = itemScript as Gun;
            if (currentGun != null) currentGun.SetCrosshair(currentCrosshair.GetComponent<CrosshairController>());
        }

        UpdateAmmoHUD();

        if (PV.IsMine && weaponIcons != null && weaponIcons.Length > index)
        {
            if (isFirstSpawn)
            {
                // Если первый спавн - просто ставим иконку без анимаций
                activeWeaponImage.sprite = weaponIcons[index];
                activeWeaponCanvasGroup.alpha = 1f;
                if (transitionWeaponImage != null) transitionWeaponImage.gameObject.SetActive(false);
            }
            else
            {
                bool slideLeft = index > currentWeaponIndex;
                if (currentWeaponIndex == weaponHolders.Length - 1 && index == 0) slideLeft = true;
                if (currentWeaponIndex == 0 && index == weaponHolders.Length - 1) slideLeft = false;

                if (weaponSwitchCoroutine != null) StopCoroutine(weaponSwitchCoroutine);
                weaponSwitchCoroutine = StartCoroutine(AnimateWeaponSwitchUI(index, slideLeft));
            }
        }
    }

    private IEnumerator AnimateWeaponSwitchUI(int newWeaponIndex, bool slideLeft)
    {
        if (activeWeaponRect == null || transitionWeaponRect == null) yield break;

        float animDuration = 0.25f; // Длительность анимации свайпа
        float slideOffset = 150f;   // На сколько пикселей уезжает иконка

        // 1. Подготавливаем уходящую иконку
        transitionWeaponImage.sprite = activeWeaponImage.sprite;
        transitionWeaponImage.gameObject.SetActive(true);
        transitionWeaponRect.anchoredPosition = activeWeaponRect.anchoredPosition;
        transitionWeaponCanvasGroup.alpha = activeWeaponCanvasGroup.alpha;

        // 2. Подготавливаем новую иконку (ставим её сбоку)
        activeWeaponImage.sprite = weaponIcons[newWeaponIndex];
        Vector3 centerPos = Vector3.zero; // Предполагаем, что центр это 0,0

        float startX = slideLeft ? slideOffset : -slideOffset;
        float exitX = slideLeft ? -slideOffset : slideOffset;

        activeWeaponRect.anchoredPosition = new Vector2(startX, 0);
        activeWeaponCanvasGroup.alpha = 0f;

        float elapsed = 0f;

        // 3. Анимируем обе иконки одновременно
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            // Плавность (Ease Out)
            float smoothT = t * t * (3f - 2f * t);

            // Старая уезжает и растворяется
            transitionWeaponRect.anchoredPosition = Vector2.Lerp(Vector2.zero, new Vector2(exitX, 0), smoothT);
            transitionWeaponCanvasGroup.alpha = Mathf.Lerp(0.5f, 0f, smoothT); // 0.5f это полупрозрачность

            // Новая приезжает и появляется
            activeWeaponRect.anchoredPosition = Vector2.Lerp(new Vector2(startX, 0), Vector2.zero, smoothT);
            activeWeaponCanvasGroup.alpha = Mathf.Lerp(0f, 0.5f, smoothT);

            yield return null;
        }

        // 4. Финализируем
        activeWeaponRect.anchoredPosition = Vector2.zero;
        activeWeaponCanvasGroup.alpha = 0.5f; // Делаем её полупрозрачной (чтобы не бросалась в глаза)
        transitionWeaponImage.gameObject.SetActive(false);
    }

    void RefreshRemoteVisuals()
    {
        if (PV.Owner.CustomProperties.TryGetValue("W_Index", out object index))
        {
            int idx = (int)index;
            currentWeaponIndex = idx;

            for (int i = 0; i < weaponHolders.Length; i++)
            {
                weaponHolders[i].SetActive(i == idx);
            }

            // Запускаем применение обвесов для удаленного игрока
            ApplyAttachmentsToActiveWeapon(false);
        }
    }

    public void UpdateAmmoHUD()
    {
        if (ammoHUDText == null) return;

        Gun currentGun = weaponHolders[currentWeaponIndex].GetComponent<Gun>();
        if (currentGun != null)
        {
            // Формат: Большая белая цифра (в обойме) и маленькая серая (в запасе)
            ammoHUDText.text = $"<color=white>{currentGun.currentAmmo}</color> <size=60%><color=#A0A0A0>/ {currentGun.totalAmmo}</color></size>";
        }
    }

    #endregion

    #region Attachments

    private Coroutine attachmentCoroutine;

    void ApplyAttachmentsToActiveWeapon(bool isLocal)
    {
        if (attachmentCoroutine != null) StopCoroutine(attachmentCoroutine);

        attachmentCoroutine = StartCoroutine(ApplyAttachmentsDelayed(isLocal));
    }


    IEnumerator ApplyAttachmentsDelayed(bool isLocal)
    {
        if (!isLocal) yield return new WaitForEndOfFrame();
        if (db == null) yield break;

        Gun activeGun = weaponHolders[currentWeaponIndex].GetComponent<Gun>();

        // Очищаем старые данные прицеливания при смене оружия
        if (isLocal)
        {
            currentAimPoint = null;
            currentScopeData = null;
            activeScopeCam = null;
        }

        if (activeGun != null)
        {
            activeGun.equippedMuzzle = null;
            activeGun.equippedGrip = null;
        }

        string weaponName = allWeapons[currentWeaponIndex].weaponName;

        string[] slotTypes = new string[] { "Muzzle_Slot", "Scope_Slot", "Grip_Slot" };

        foreach (string slotType in slotTypes)
        {
            Transform slotTransform = weaponHolders[currentWeaponIndex].transform.FindRecursive(slotType);
            if (slotTransform == null) continue;

            // Ищем VisualContainer внутри этого слота
            Transform visualContainer = slotTransform.Find("VisualContainer");
            if (visualContainer == null) continue;

            string attName = "None";

            // В PlayerPrefs ты сохраняешь по имени типа? Если у тебя тип назывался "Muzzle", 
            // то убираем "_Slot" из строки для получения ключа сохранения:
            string saveKey = slotType.Replace("_Slot", "");

            if (isLocal) attName = PlayerPrefs.GetString(weaponName + "_" + saveKey, "None");
            else if (PV.Owner.CustomProperties.TryGetValue(saveKey, out object nameObj)) attName = (string)nameObj;

            // Очищаем старые визуалы
            foreach (Transform child in visualContainer) Destroy(child.gameObject);

            if (!string.IsNullOrEmpty(attName) && attName != "None")
            {
                var data = db.allAttachments.Find(a => a.attachmentName == attName);
                if (data != null && data.prefab != null)
                {
                    GameObject att = Instantiate(data.prefab, visualContainer);
                    att.transform.localPosition = Vector3.zero;
                    att.transform.localRotation = Quaternion.identity;

                    SetLayerRecursive(att, LayerMask.NameToLayer("Ignore Raycast"));

                    if (activeGun != null)
                    {
                        if (data.isMuzzle) activeGun.equippedMuzzle = data;
                        if (data.isGrip) activeGun.equippedGrip = data;
                    }

                    if (data.isScope)
                    {
                        if (isLocal) currentScopeData = data;

                        Camera scopeCam = att.GetComponentInChildren<Camera>();
                        if (scopeCam != null)
                        {
                            AudioListener al = scopeCam.GetComponent<AudioListener>();
                            if (al != null) Destroy(al);

                            if (isLocal)
                            {
                                scopeCam.fieldOfView = data.zoomFOV;
                                scopeCam.enabled = true;
                                activeScopeCam = scopeCam;
                            }
                            else
                            {
                                scopeCam.enabled = false;
                                scopeCam.gameObject.SetActive(false);
                            }

                        }

                        if (isLocal)
                        {
                            Transform[] allChildren = att.GetComponentsInChildren<Transform>();
                            currentAimPoint = allChildren.FirstOrDefault(t => t.name == "AimPoint");

                            if (currentAimPoint == null)
                            {
                                Debug.LogWarning($"В прицеле {data.attachmentName} забыли добавить пустой объект AimPoint!");
                            }
                            else
                            {
                                // ПРОВЕРКА КЭША: Если этот прицел для этого оружия уже рассчитывался ранее,
                                // просто берем сохраненное чистое значение и выходим из тяжелого расчета.
                                if (hasCachedOffset[currentWeaponIndex] && cachedScopeNames[currentWeaponIndex] == attName)
                                {
                                    staticAimPointLocalOffset = cachedAimOffsets[currentWeaponIndex];
                                    Debug.Log($"<color=green>[Aim Cache]</color> Используем сохраненный офсет для {data.attachmentName}: {staticAimPointLocalOffset}");
                                    continue;
                                }

                                Animator weaponAnimator = weaponHolders[currentWeaponIndex].GetComponentInChildren<Animator>();
                                RuntimeAnimatorController originalController = null;
                                bool wasAnimatorEnabled = weaponAnimator != null && weaponAnimator.enabled;

                                if (weaponAnimator != null)
                                {
                                    originalController = weaponAnimator.runtimeAnimatorController;
                                    weaponAnimator.runtimeAnimatorController = null;
                                    weaponAnimator.enabled = true;
                                    weaponAnimator.Rebind();
                                }

                                Vector3 originalAdsPosition = adsParent.localPosition;
                                adsParent.localPosition = weaponHolderOrigin;

                                // Вычисляем офсет
                                staticAimPointLocalOffset = adsParent.InverseTransformPoint(currentAimPoint.position);

                                // Сохраняем в кэш
                                cachedAimOffsets[currentWeaponIndex] = staticAimPointLocalOffset;
                                cachedScopeNames[currentWeaponIndex] = attName;
                                hasCachedOffset[currentWeaponIndex] = true;

                                Vector3 localPosInWeapon = weaponHolders[currentWeaponIndex].transform.InverseTransformPoint(currentAimPoint.position);

                                Debug.Log($"<color=cyan>[Aim Debug - Рассчитано впервые]</color> Офсет прицела {data.attachmentName} сохранен в кэш:\n" +
                                          $"- Локальная позиция в оружии: {localPosInWeapon}\n" +
                                          $"- Рассчитанный staticAimPointLocalOffset: {staticAimPointLocalOffset}");

                                adsParent.localPosition = originalAdsPosition;

                                if (weaponAnimator != null)
                                {
                                    weaponAnimator.runtimeAnimatorController = originalController;
                                    weaponAnimator.enabled = wasAnimatorEnabled;
                                    weaponAnimator.Rebind();
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void SetLayerRecursive(GameObject obj, int layer)
    {
        // Получаем индекс нашего слоя для интерфейса прицела
        int scopeUiLayer = LayerMask.NameToLayer("ScopeUI");

        // Если объект УЖЕ находится на слое ScopeUI, мы его пропускаем и НЕ меняем ему слой!
        if (obj.layer != scopeUiLayer)
        {
            obj.layer = layer;
        }

        // Идем дальше по детям
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    #endregion

    #region RPCs

    [PunRPC]
    public void RPC_SpawnImpact(Vector3 hitPos, Vector3 hitNormal, bool isFlesh, int targetViewID)
    {
        // Находим префаб через текущее оружие
        Gun currentGun = weaponHolders[currentWeaponIndex].GetComponentInChildren<Gun>();
        if (currentGun == null) return;

        GameObject prefab = isFlesh ? currentGun.bloodImpactPrefab : currentGun.bulletImpactPrefab;

        if (prefab != null)
        {
            // Спавним дырку/кровь
            GameObject impact = Instantiate(prefab, hitPos, Quaternion.LookRotation(-hitNormal));
            impact.transform.position += hitNormal * 0.01f;

            if (targetViewID != -1)
            {
                PhotonView targetPV = PhotonView.Find(targetViewID);
                if (targetPV != null)
                {
                    PlayerController targetPC = targetPV.GetComponent<PlayerController>();

                    if (targetPC != null && targetPC.bodyVisual != null)
                    {
                        impact.transform.SetParent(targetPC.bodyVisual.transform);
                    }
                    else
                    {
                        impact.transform.SetParent(targetPV.transform);
                    }
                }
            }
            Destroy(impact, 10f);
        }

        if (!isFlesh && debrisCubePrefab != null)
        {
            Material hitMaterial = null;
            RaycastHit hit;

            if (Physics.Raycast(hitPos + hitNormal * 0.1f, -hitNormal, out hit, 0.2f))
            {
                MeshRenderer mr = hit.collider.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    hitMaterial = mr.sharedMaterial;
                }
            }

            int debrisCount = Random.Range(7, 10);
            for (int i = 0; i < debrisCount; i++)
            {
                GameObject debris = Instantiate(debrisCubePrefab, hitPos + hitNormal * 0.05f, Quaternion.identity);
                debris.GetComponent<DebrisCube>().Initialize(hitNormal, hitMaterial);
            }
        }
    }

    [PunRPC]
    public void RPC_PlayShootEffects()
    {
        Gun currentGun = weaponHolders[currentWeaponIndex].GetComponent<Gun>();
        if (currentGun != null) currentGun.PlayShootEffects();
    }

    [PunRPC]
    public void RPC_ShootProjectile(Vector3 origin, Vector3 direction)
    {
        // Находим текущую пушку и спавним визуальную пулю
        Gun currentGun = weaponHolders[currentWeaponIndex].GetComponent<Gun>();
        if (currentGun != null) currentGun.SpawnProjectile(origin, direction, false);
    }

    [PunRPC]
    public void RPC_ShootShotgun(Vector3 origin, Vector3[] directions)
    {
        Gun currentGun = weaponHolders[currentWeaponIndex].GetComponent<Gun>();
        if (currentGun == null) return;

        foreach (Vector3 dir in directions)
        {
            currentGun.SpawnProjectile(origin, dir, false);
        }
    }

    #endregion

    #region Movement & Input

    void ApplyCustomizationSync()
    {
        if (!PV.IsMine) return;

        // Читаем ТОЧНО ТЕ ЖЕ ключи, что сохраняют CustomizationController и MaterialCustomizationController
        int hat = PlayerPrefs.GetInt("SavedHat", 0);
        int mask = PlayerPrefs.GetInt("SavedMask", 0);
        int matIndex = PlayerPrefs.GetInt("SavedMaterialIndex", -1);
        string color = PlayerPrefs.GetString("SavedRGBColor", "#FFFFFFFF");

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
{
    { "C_Hat", hat },
    { "C_Mask", mask },
    { "C_MatIdx", matIndex },
    { "C_Color", color }
};

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    void HandleMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen) CloseMenu();
            else OpenMenu();
        }
    }

    void OpenMenu()
    {
        MenuManager.Instance.OpenMenu("exitRoom");
        EnableCursor();
        DisablePlayerControl();
        isMenuOpen = true;
    }

    void CloseMenu()
    {
        MenuManager.Instance.CloseMenu(MenuManager.Instance.menus.FirstOrDefault(m => m.menuName == "exitRoom"));
        DisableCursor();
        EnablePlayerControl();
        isMenuOpen = false;
    }

    void Look()
    {
        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);
        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -70f, 70f);
        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
    }

    void Move()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * speed, ref smoothMoveVelocity, smoothTime);
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.AddForce(transform.up * jumpForce);

            if (playerAudioSource != null && jumpSound != null)
            {
                playerAudioSource.PlayOneShot(jumpSound);
            }
        }
    }

    void HandleFootsteps()
    {
        if (!grounded || footstepSounds.Length == 0 || playerAudioSource == null) return;

        // Проверяем, движется ли игрок (используем ввод движения)
        bool isMoving = Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;

        if (isMoving)
        {
            bool isSprinting = Input.GetKey(KeyCode.LeftShift);
            float currentInterval = isSprinting ? sprintStepInterval : baseStepInterval;

            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0)
            {
                // Выбираем случайный звук шага из массива, чтобы они не звучали монотонно
                int randomIndex = Random.Range(0, footstepSounds.Length);
                AudioClip clip = footstepSounds[randomIndex];

                // Слегка меняем высоту тона (pitch), чтобы шаги звучали более естественно
                playerAudioSource.pitch = Random.Range(0.9f, 1.1f);
                playerAudioSource.PlayOneShot(clip);

                footstepTimer = currentInterval;
            }
        }
        else
        {
            // Сбрасываем таймер, чтобы шаг воспроизводился мгновенно при начале движения
            footstepTimer = 0f;
        }
    }

    public void SetGroundedState(bool _grounded) => grounded = _grounded;

    void FixedUpdate()
    {
        if (!PV.IsMine) return;

        Vector3 targetVelocity = transform.TransformDirection(moveAmount);

        targetVelocity.y = rb.velocity.y;

        rb.velocity = targetVelocity;

        if (!grounded && rb.velocity.y < 0f)
            rb.AddForce(Physics.gravity * (fallGravityMultiplier - 1f), ForceMode.Acceleration);

        if (rb.velocity.y < -maxFallSpeed)
            rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
    }

    #endregion

    #region Audio Feedback Methods

    public void PlayHitFeedback(bool isKill)
    {
        if (playerAudioSource == null) return;

        AudioClip clipToPlay = isKill ? killDealtSound : hitDealtSound;
        if (clipToPlay != null)
        {
            // Явно передаем громкость 1f (100%), чтобы звук проигрывался максимально громко
            playerAudioSource.PlayOneShot(clipToPlay, 4f);
        }
    }

    private void PlayGotHitSound()
    {
        if (playerAudioSource != null && hitReceivedSound != null)
        {
            playerAudioSource.PlayOneShot(hitReceivedSound);
        }
    }

    #endregion

    #region Health & Damage

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        PV.RPC(nameof(RPC_TakeDamage), RpcTarget.All, damage);
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage, PhotonMessageInfo info)
    {
        if (currentHealth <= 0) return;

        bool iAmShooter = (info.Sender == PhotonNetwork.LocalPlayer);

        if (iAmShooter && info.Sender != PV.Owner) // Если стрелок - я, и я попал не в себя
        {
            bool isKill = (currentHealth - damage <= 0);

            // Находим контроллер нашего локального игрока и проигрываем звук обратной связи
            if (AllPlayers.TryGetValue(PhotonNetwork.LocalPlayer, out PlayerController localPC))
            {
                localPC.PlayHitFeedback(isKill);
            }
        }

        currentHealth -= damage;

        if (PV.IsMine)
        {
            UpdateHealthDisplay();

            if (healthAnimCoroutine != null) StopCoroutine(healthAnimCoroutine);
            healthAnimCoroutine = StartCoroutine(AnimateHealthDamage());

            PlayGotHitSound();
        }

        if (currentHealth <= 0)
        {
            Transform killerT = null;

            if (info.Sender != PV.Owner)
            {
                if (info.Sender != null && AllPlayers.TryGetValue(info.Sender, out PlayerController killerController))
                {
                    if (killerController != null)
                        killerT = killerController.transform;
                }

                if (killerT == null && info.Sender != null)
                {
                    PhotonView[] allViews = FindObjectsOfType<PhotonView>();
                    foreach (var view in allViews)
                    {
                        if (view.Owner == info.Sender && view.GetComponent<PlayerController>())
                        {
                            killerT = view.transform;
                            break;
                        }
                    }
                }
            }

            DieLocally(killerT);

            if (PV.IsMine)
            {
                if (info.Sender != PV.Owner)
                {
                    PlayerManager killerManager = PlayerManager.Find(info.Sender);
                    if (killerManager != null)
                    {
                        int victimTeam = 0;
                        if (PV.Owner.CustomProperties.TryGetValue(GameSettings.TEAM_KEY, out object vT))
                            victimTeam = (int)vT;

                        int killerTeam = 0;
                        if (info.Sender.CustomProperties.TryGetValue(GameSettings.TEAM_KEY, out object kT))
                            killerTeam = (int)kT;

                        killerManager.GetKill(PV.Owner.NickName, killerTeam, victimTeam);
                    }
                }
            }
        }
    }

    private IEnumerator AnimateHealthDamage()
    {
        if (healthUIContainer == null) yield break;

        float duration = 0.15f; // Скорость пульсации
        float elapsed = 0f;

        // Увеличиваем размер до 1.3 и красим в красный
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = new Vector3(1.4f, 1.4f, 1f);

        if (healthText != null) healthText.color = healthDamageColor;

        // Вздутие
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            healthUIContainer.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // Возврат обратно
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            healthUIContainer.localScale = Vector3.Lerp(targetScale, originalScale, t);
            if (healthText != null) healthText.color = Color.Lerp(healthDamageColor, healthNormalColor, t);
            yield return null;
        }

        healthUIContainer.localScale = originalScale;
        if (healthText != null) healthText.color = healthNormalColor;
    }

    void UpdateHealthDisplay()
    {
        if (healthText != null)
            healthText.text = $"{Mathf.Ceil(currentHealth)}";

        if (healthbarImage != null)
            healthbarImage.fillAmount = currentHealth / maxHealth;
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        PV.RPC(nameof(RPC_Heal), RpcTarget.All, amount);
    }

    [PunRPC]
    public void RPC_Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        if (PV.IsMine) UpdateHealthDisplay();
    }

    #endregion

    #region Death & Respawn

    void DieLocally(Transform killer = null)
    {
        if (isDead) return;
        isDead = true;
        killerTransform = killer;

        if (bodyVisual != null) bodyVisual.SetActive(false);

        Transform nickCanvas = transform.Find("NicknameCanvas");
        if (nickCanvas != null) nickCanvas.gameObject.SetActive(false);

        CapsuleCollider mainCollider = GetComponent<CapsuleCollider>();
        if (mainCollider != null) mainCollider.enabled = false;

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Transform groundCheck = transform.Find("GroundCheck");
        if (groundCheck != null) groundCheck.gameObject.SetActive(false);

        if (deathEffectPrefab != null)
        {
            int count = 25;
            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = transform.position + Vector3.up * Random.Range(0.5f, 1.5f);
                GameObject part = Instantiate(deathEffectPrefab, spawnPos, Random.rotation);

                Rigidbody partRb = part.GetComponent<Rigidbody>();
                if (partRb != null)
                {
                    Vector3 force = new Vector3(Random.Range(-1f, 1f), 1.5f, Random.Range(-1f, 1f)) * 5f;
                    partRb.AddForce(force, ForceMode.Impulse);
                    partRb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);
                }
            }
        }

        // --- 2. ЛОКАЛЬНАЯ ЧАСТЬ (Только для экрана убитого игрока) ---
        if (PV.IsMine)
        {
            Transform itemHolder = cameraHolder.transform.Find("ItemHolder");
            if (itemHolder != null) itemHolder.gameObject.SetActive(false);

            if (currentCrosshair != null) Destroy(currentCrosshair);
            if (mainHUD != null) mainHUD.SetActive(false);

            currentHealth = 0;
            UpdateHealthDisplay();

            if (killer == null && transform.position.y < -10f)
            {
                KillFeedManager.NotifyEvent($"{PhotonNetwork.NickName} выпал за карту");
            }

            StartCoroutine(Respawn());
        }
        else
        {
            // Отключаем камеру удаленного игрока, если она почему-то активна
            if (cameraHolder != null) cameraHolder.SetActive(false);
        }
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(3f);
        isDead = false;
        playerManager.Die(); // Вызов респавна через PlayerManager
    }

    #endregion

    #region Utilities & Controls

    void EnableCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void DisableCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void DisablePlayerControl()
    {
        // Твои специфические констрейнты
        rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    void EnablePlayerControl()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void AddAmmo(string ammoType, int amount)
    {
        // Проходим по всем слотам оружия (даже по выключенным)
        foreach (GameObject holder in weaponHolders)
        {
            if (holder == null) continue;

            Gun gun = holder.GetComponentInChildren<Gun>(true);

            if (gun != null && gun.ammoType == ammoType)
            {
                gun.AddAmmo(amount);
            }
        }

        UpdateAmmoHUD();
    }

    #endregion

    #region Weapon Context Sanitization

    // Этот метод делает из префаба инвентаря чисто геймплейный префаб
    void StripWeaponUIForGameplay()
    {
        // Ищем все точки кастомизации на всех пушках игрока
        WeaponHotspot[] allHotspots = GetComponentsInChildren<WeaponHotspot>(true);

        foreach (var hotspot in allHotspots)
        {
            // 1. Удаляем SphereCollider, чтобы пули сквозь него пролетали
            Collider col = hotspot.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            // 2. Ищем и полностью удаляем объект UI_Dot
            Transform uiDot = hotspot.transform.Find("UI_Dot");
            if (uiDot != null)
            {
                Destroy(uiDot.gameObject);
            }

            // 3. (Опционально) Удаляем сам скрипт WeaponHotspot, 
            // так как во время геймплея нажимать на него нельзя
            Destroy(hotspot);
        }
    }

    #endregion

    public void SpawnLocalDamageNumber(Vector3 position, float damage)
    {
        if (damageNumberPrefab == null) return;

        // Спавним цифру локально на сцене
        GameObject numberObj = Instantiate(damageNumberPrefab, position, Quaternion.identity);
        DamageNumber dmgScript = numberObj.GetComponent<DamageNumber>();

        if (dmgScript != null)
        {
            dmgScript.Initialize(damage);
        }
    }

    void OnDestroy()
    {
        if (PV.Owner != null && AllPlayers.TryGetValue(PV.Owner, out PlayerController pc))
        {
            if (pc == this)
            {
                AllPlayers.Remove(PV.Owner);
            }
        }
    }
}
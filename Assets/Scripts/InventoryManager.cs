using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Data")]
    public WeaponData[] allWeapons;
    public Transform spawnPoint;

    [Header("UI References")]
    public AttachmentButton attachmentButtonPrefab;
    public RectTransform attachmentContainer;

    [Header("Arc Settings")]
    public float radius = 150f;
    public float angleStep = 25f;
    public float startAngle = 90f;

    [Header("World UI Settings")]
    public Canvas worldAttachmentCanvas;

    [Header("Animation Settings")]
    public float swapDuration = 0.3f; // Время исчезновения и появления (каждого)
    public Vector3 spinAmount = new Vector3(0, 180f, 0); // Оружие прокрутится при смене
    public AnimationCurve scaleOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // Кривая исчезновения
    public AnimationCurve scaleInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);  // Кривая появления

    private bool isSwappingWeapon = false; // Блокиратор кликов во время анимации

    private GameObject currentModel;
    private WeaponData selectedWeapon;
    private Camera mainCam;
    private Transform currentHotspot;

    // Переменные для отслеживания наведения
    private InteractiveBrick lastHoveredBrick;
    private WeaponHotspot lastHoveredHotspot;

    private void Awake()
    {
        Instance = this;
        mainCam = Camera.main;

        if (worldAttachmentCanvas != null && worldAttachmentCanvas.renderMode == RenderMode.WorldSpace)
        {
            worldAttachmentCanvas.worldCamera = mainCam;
        }
    }

    private void OnEnable()
    {
        if (MenuCameraController.Instance != null)
            MenuCameraController.Instance.MoveToInventory();

        ClosePanel();

        // Всегда принудительно выбираем автомат (индекс 0) при открытии меню инвентаря
        SelectWeapon(0, true);
    }

    private void OnDisable()
    {
        if (MenuCameraController.Instance != null)
            MenuCameraController.Instance.MoveToMainView();

        currentHotspot = null;

        isSwappingWeapon = false; // Сбрасываем блокиратор
    }

    public void SelectWeapon(int index, bool instant = false)
    {
        if (index < 0 || index >= allWeapons.Length) return;

        if (isSwappingWeapon) return;

        if (selectedWeapon == allWeapons[index] && currentModel != null) return;

        ClosePanel();

        selectedWeapon = allWeapons[index];
        PlayerPrefs.SetInt("SelectedWeaponIndex", index);

        if (currentModel != null) Destroy(currentModel);

        if (instant)
        {
            SpawnNewWeaponImmediate();
        }
        else
        {
            // Запускаем красивую анимацию смены
            StartCoroutine(WeaponSwapRoutine());
        }
    }

    // [ИСПРАВЛЕНО] Более надежная корутина
    private IEnumerator WeaponSwapRoutine()
    {
        isSwappingWeapon = true; // Блокируем инпут

        // 1. АНИМАЦИЯ ИСЧЕЗНОВЕНИЯ
        if (currentModel != null)
        {
            float time = 0;
            Vector3 startScale = currentModel.transform.localScale;
            Quaternion startRot = currentModel.transform.localRotation;
            Quaternion targetRot = startRot * Quaternion.Euler(spinAmount);

            while (time < swapDuration)
            {
                // Защита: если объект кто-то удалил во время анимации - прерываем цикл
                if (currentModel == null) break;

                time += Time.deltaTime;
                float normalizedTime = time / swapDuration;
                float curveValue = scaleOutCurve.Evaluate(normalizedTime);

                currentModel.transform.localScale = startScale * curveValue;
                currentModel.transform.localRotation = Quaternion.Lerp(startRot, targetRot, normalizedTime);

                yield return null;
            }

            // [ВАЖНО!] Уничтожаем и сразу очищаем ссылку
            if (currentModel != null)
            {
                Destroy(currentModel);
                currentModel = null;
            }
        }

        // 2. СПАВН НОВОГО ОРУЖИЯ
        SpawnNewWeaponImmediate();

        // 3. АНИМАЦИЯ ПОЯВЛЕНИЯ
        // Если префаб назначен, SpawnNewWeaponImmediate создаст его и currentModel больше не null
        if (currentModel != null)
        {
            float time = 0;
            Vector3 finalScale = currentModel.transform.localScale;

            currentModel.transform.localScale = Vector3.zero;
            Quaternion finalRot = currentModel.transform.localRotation;
            Quaternion startRot = finalRot * Quaternion.Euler(-spinAmount);
            currentModel.transform.localRotation = startRot;

            while (time < swapDuration)
            {
                if (currentModel == null) break; // Защита

                time += Time.deltaTime;
                float normalizedTime = time / swapDuration;
                float curveValue = scaleInCurve.Evaluate(normalizedTime);

                currentModel.transform.localScale = finalScale * curveValue;
                currentModel.transform.localRotation = Quaternion.Lerp(startRot, finalRot, normalizedTime);

                yield return null;
            }

            // Гарантируем, что в конце анимации масштаб 100%, чтобы не было погрешностей
            if (currentModel != null)
            {
                currentModel.transform.localScale = finalScale;
                currentModel.transform.localRotation = finalRot;
            }
        }

        // Снимаем блокировку управления в любом случае
        isSwappingWeapon = false;
    }

    // [ИСПРАВЛЕНО] Надежный спавн
    private void SpawnNewWeaponImmediate()
    {
        // Подчищаем за собой, если модель уже существует
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }

        // Проверяем, что Data и Префаб не пустые, иначе будет ошибка
        if (selectedWeapon != null && selectedWeapon.menuModelPrefab != null)
        {
            currentModel = Instantiate(selectedWeapon.menuModelPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            SetLayerRecursive(currentModel, LayerMask.NameToLayer("WeaponPreview"));
            LoadAppliedAttachments();
        }
        else
        {
            Debug.LogWarning("У выбранного оружия не назначен префаб menuModelPrefab!");
        }
    }

    void Update()
    {
        if (isSwappingWeapon) return;

        // Если мышь над UI (кнопками выбора обвеса), не пускаем лучи в 3D мир
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        // Используем RaycastAll, чтобы пробивать меш оружия насквозь
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        WeaponHotspot foundHotspot = null;
        InteractiveBrick foundBrick = null;

        // Ищем первый интерактивный объект среди всех, во что попал луч
        foreach (var hit in hits)
        {
            foundHotspot = hit.collider.GetComponent<WeaponHotspot>();
            if (foundHotspot != null) break;

            foundBrick = hit.collider.GetComponent<InteractiveBrick>();
            if (foundBrick != null) break;
        }

        // --- ЛОГИКА HOVER (НАВЕДЕНИЕ) ---

        // Для кирпичей
        if (foundBrick != lastHoveredBrick)
        {
            if (lastHoveredBrick != null) lastHoveredBrick.SetHover(false);
            if (foundBrick != null) foundBrick.SetHover(true);
            lastHoveredBrick = foundBrick;
        }

        // Для кружочков (Hotspots)
        if (foundHotspot != lastHoveredHotspot)
        {
            if (lastHoveredHotspot != null)
                lastHoveredHotspot.GetComponentInChildren<WorldHoverAnimation>()?.SetHover(false);

            if (foundHotspot != null)
                foundHotspot.GetComponentInChildren<WorldHoverAnimation>()?.SetHover(true);

            lastHoveredHotspot = foundHotspot;
        }

        // --- ЛОГИКА КЛИКА ---
        if (Input.GetMouseButtonDown(0))
        {
            if (foundHotspot != null)
            {
                foundHotspot.GetComponentInChildren<WorldHoverAnimation>()?.Push();

                // Просто берем ссылку, которую мы сами назначили в префабе
                currentHotspot = (foundHotspot.uiPivot != null) ? foundHotspot.uiPivot : foundHotspot.transform;

                OpenAttachmentList(foundHotspot.slotType);
            }
            else if (foundBrick != null)
            {
                foundBrick.Push();

                // ПРОВЕРКА НА ЭКШЕН:
                BrickAction action = foundBrick.GetComponent<BrickAction>();
                if (action != null)
                {
                    // Если у кирпича есть скрипт BrickAction, просто выполняем то, что там настроено
                    action.Execute();
                }
                else
                {
                    // Если экшена нет, значит это обычный кирпич выбора оружия
                    HandleBrickClick(foundBrick.gameObject.name);
                }
            }
            else
            {
                // Клик в пустоту
                ClosePanel();
            }
        }
    }
    void OpenAttachmentList(string slotType)
    {
        if (worldAttachmentCanvas == null || attachmentContainer == null || attachmentButtonPrefab == null) return;

        worldAttachmentCanvas.gameObject.SetActive(true);
        foreach (Transform child in attachmentContainer) Destroy(child.gameObject);

        var compatible = selectedWeapon.compatibleAttachments
            .Where(a => a != null && a.slotType.Trim().Equals(slotType.Trim(), System.StringComparison.OrdinalIgnoreCase))
            .ToList();

        for (int i = 0; i < compatible.Count; i++)
        {
            GameObject btnObj = Instantiate(attachmentButtonPrefab.gameObject, attachmentContainer);
            AttachmentButton btn = btnObj.GetComponent<AttachmentButton>();

            if (btn != null)
            {
                AttachmentData data = compatible[i];
                btn.Setup(data, () => ApplyAttachment(data));

                float currentAngleDeg = startAngle - (i * angleStep);
                float currentAngleRad = currentAngleDeg * Mathf.Deg2Rad;
                float x = Mathf.Cos(currentAngleRad) * radius;
                float y = Mathf.Sin(currentAngleRad) * radius;
                Vector2 targetPos = new Vector2(x, y);

                // Запуск анимации появления кнопки
                StartCoroutine(btn.AnimateShow(targetPos, i * 0.05f));
            }
        }
    }

    void LateUpdate()
    {
        if (worldAttachmentCanvas != null && worldAttachmentCanvas.gameObject.activeSelf && currentHotspot != null)
        {
            worldAttachmentCanvas.transform.position = currentHotspot.position;
            // Билбординг: Канвас всегда смотрит на камеру
            worldAttachmentCanvas.transform.rotation = mainCam.transform.rotation;
        }
    }

    public void ApplyAttachment(AttachmentData att)
    {
        if (att == null) return;
        PlayerPrefs.SetString(selectedWeapon.weaponName + "_" + att.slotType, att.attachmentName);
        VisuallyApply(att);
    }

    void HandleBrickClick(string brickName)
    {
        for (int i = 0; i < allWeapons.Length; i++)
        {
            if (brickName.ToLower().Contains(allWeapons[i].weaponName.ToLower()))
            {
                SelectWeapon(i);
                return;
            }
        }
    }

    void VisuallyApply(AttachmentData att)
    {
        if (currentModel == null) return;
        var hotspots = currentModel.GetComponentsInChildren<WeaponHotspot>(true);
        var slot = hotspots.FirstOrDefault(h => h.slotType.Trim().Equals(att.slotType.Trim(), System.StringComparison.OrdinalIgnoreCase));
        if (slot != null && slot.visualContainer != null)
        {
            foreach (Transform child in slot.visualContainer) Destroy(child.gameObject);
            if (att.prefab != null)
            {
                GameObject v = Instantiate(att.prefab, slot.visualContainer);
                v.transform.localPosition = Vector3.zero;
                v.transform.localRotation = Quaternion.identity;
                SetLayerRecursive(v, LayerMask.NameToLayer("WeaponPreview"));
            }
        }
    }

    void LoadAppliedAttachments()
    {
        string[] types = { "Muzzle", "Scope", "Grip" };
        foreach (var t in types)
        {
            string saved = PlayerPrefs.GetString(selectedWeapon.weaponName + "_" + t, "");
            if (!string.IsNullOrEmpty(saved))
            {
                var att = selectedWeapon.compatibleAttachments.FirstOrDefault(a => a.attachmentName == saved);
                if (att != null) VisuallyApply(att);
            }
        }
    }

    void ClosePanel()
    {
        if (worldAttachmentCanvas != null) worldAttachmentCanvas.gameObject.SetActive(false);
        currentHotspot = null;
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, newLayer);
    }
}
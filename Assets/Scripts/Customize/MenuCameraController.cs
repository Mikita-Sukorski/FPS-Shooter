using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class MenuCameraController : MonoBehaviour
{
    // Добавляем Singleton, чтобы легко вызывать из InventoryManager
    public static MenuCameraController Instance;

    [Header("Позиции камеры")]
    public Transform mainView;
    public Transform customizationView;
    public Transform inventoryView; // НОВАЯ ПОЗИЦИЯ: Точка камеры для инвентаря
    public float transitionSpeed = 3f;

    [Header("Настройки фокуса (Дистанция)")]
    public Transform targetToFocus;        // Манекен (для Главного меню и Кастомизации)
    public Transform inventoryFocusTarget; // НОВАЯ ЦЕЛЬ: Точка спавна оружия на стене
    public PostProcessVolume postProcessVolume;
    public float heightOffset = 0.5f;
    public float surfaceFocusOffset = -0.2f;

    [Header("Умная Апертура (Размытие)")]
    [Tooltip("Апертура в главном меню (маленькое значение = мыльный фон)")]
    public float apertureMain = 4.0f;
    [Tooltip("Апертура в кастомизации (большое значение = вся голова в фокусе)")]
    public float apertureCustomization = 15.0f;
    [Tooltip("Апертура в инвентаре (чтобы оружие было четким, а фон по краям размыт)")]
    public float apertureInventory = 8.0f; // Настрой по вкусу

    private Coroutine moveCoroutine;
    private DepthOfField dofSettings;
    private float targetAperture;
    private Transform currentFocusTarget; // За чем следит фокус прямо сейчас

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGetSettings(out dofSettings);
        }

        // При старте ставим настройки для главного меню
        targetAperture = apertureMain;
        currentFocusTarget = targetToFocus;
    }

    public void MoveToMainView()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveCamera(mainView));

        targetAperture = apertureMain;
        currentFocusTarget = targetToFocus; // Возвращаем фокус на манекен
    }

    public void MoveToCustomization()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveCamera(customizationView));

        targetAperture = apertureCustomization;
        currentFocusTarget = targetToFocus; // Фокус всё ещё на манекене
    }

    // НОВЫЙ МЕТОД ДЛЯ ИНВЕНТАРЯ
    public void MoveToInventory()
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveCamera(inventoryView));

        targetAperture = apertureInventory;
        currentFocusTarget = inventoryFocusTarget; // Переключаем фокус на стену/оружие
    }

    private IEnumerator MoveCamera(Transform target)
    {
        while (Vector3.Distance(transform.position, target.position) > 0.01f ||
               Quaternion.Angle(transform.rotation, target.rotation) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * transitionSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, Time.deltaTime * transitionSpeed);
            yield return null;
        }
    }

    private void Update()
    {
        if (dofSettings != null && currentFocusTarget != null)
        {
            // Если смотрим на манекен, применяем heightOffset. 
            // Если на оружие - смотрим прямо в центр WeaponSpawnPoint (смещение 0).
            float currentOffset = (currentFocusTarget == targetToFocus) ? heightOffset : 0f;

            Vector3 centerFocusPoint = currentFocusTarget.position + Vector3.up * currentOffset;
            float distanceToCenter = Vector3.Distance(transform.position, centerFocusPoint);

            float targetOffset = (currentFocusTarget == targetToFocus) ? surfaceFocusOffset : 0f;
            dofSettings.focusDistance.value = Mathf.Lerp(dofSettings.focusDistance.value, distanceToCenter - targetOffset, Time.deltaTime * transitionSpeed);

            // Плавно меняем апертуру (степень размытия)
            dofSettings.aperture.value = Mathf.Lerp(dofSettings.aperture.value, targetAperture, Time.deltaTime * transitionSpeed);
        }
    }
}
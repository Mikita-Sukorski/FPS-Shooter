using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Настройки анимации")]
    public RectTransform targetToAnimate;

    [Space]
    public bool shiftRight = true;
    public float shiftAmount = 25f;

    [Space]
    public bool scaleUp = true;
    public float scaleMultiplier = 1.2f;

    public float animationSpeed = 15f;

    private Vector2 originalPosition;
    private Vector3 originalScale;

    private Vector2 targetPosition;
    private Vector3 targetScale;

    private void Awake()
    {
        // Используем Awake для инициализации, чтобы OnEnable сработал корректно
        if (targetToAnimate == null)
        {
            if (transform.childCount > 0)
                targetToAnimate = transform.GetChild(0).GetComponent<RectTransform>();
        }

        if (targetToAnimate != null)
        {
            originalPosition = targetToAnimate.anchoredPosition;
            originalScale = targetToAnimate.localScale;
        }
    }

    private void OnEnable()
    {
        // При включении объекта мгновенно сбрасываем его в начальное состояние,
        // чтобы не было "залипших" анимаций из прошлого меню
        ResetToOriginal(true);
    }

    private void OnDisable()
    {
        // Если кнопка выключается, сбрасываем целевые значения, 
        // чтобы при следующем включении она не начала двигаться к "активному" состоянию
        ResetToOriginal(true);
    }

    // Метод для сброса состояния
    public void ResetToOriginal(bool instant)
    {
        targetPosition = originalPosition;
        targetScale = originalScale;

        if (instant && targetToAnimate != null)
        {
            targetToAnimate.anchoredPosition = originalPosition;
            targetToAnimate.localScale = originalScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (shiftRight)
            targetPosition = originalPosition + new Vector2(shiftAmount, 0);

        if (scaleUp)
            targetScale = originalScale * scaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetToOriginal(false);
    }

    private void Update()
    {
        if (targetToAnimate == null) return;

        // Плавно меняем позицию
        if (shiftRight)
        {
            targetToAnimate.anchoredPosition = Vector2.Lerp(targetToAnimate.anchoredPosition, targetPosition, Time.unscaledDeltaTime * animationSpeed);
        }

        // Плавно меняем размер
        if (scaleUp)
        {
            targetToAnimate.localScale = Vector3.Lerp(targetToAnimate.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
        }
    }
}
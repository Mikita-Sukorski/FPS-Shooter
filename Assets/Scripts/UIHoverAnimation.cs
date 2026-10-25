using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UIHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    public float hoverScale = 1.15f;
    public float clickScale = 0.9f;
    public float animationSpeed = 15f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => targetScale = originalScale * hoverScale;
    public void OnPointerExit(PointerEventData eventData) => targetScale = originalScale;
    public void OnPointerDown(PointerEventData eventData) => targetScale = originalScale * clickScale;
    public void OnPointerUp(PointerEventData eventData) => targetScale = originalScale * hoverScale;

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
    }

    // Метод для резкого сброса (например, при закрытии меню)
    void OnDisable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
    }
}
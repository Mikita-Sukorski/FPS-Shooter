using System.Collections;
using UnityEngine;

public class InteractiveBrick : MonoBehaviour
{
    [Header("Настройки")]
    public float pushDepth = 0.15f;  // Глубина утапливания
    public float animSpeed = 15f;    // Скорость анимации

    private Vector3 originalScale;
    private Vector3 originalLocalPos;
    private Vector3 targetLocalPos;

    private bool isHovered = false;

    void Start()
    {
        originalScale = transform.localScale;
        originalLocalPos = transform.localPosition;
        targetLocalPos = originalLocalPos;
    }

    void Update()
    {
        // 1. Анимация утапливания (позиция)
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * animSpeed);

        // 2. Анимация наведения (масштаб увеличивается на 5%)
        float targetScaleMult = isHovered ? 1.05f : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, originalScale * targetScaleMult, Time.deltaTime * animSpeed);
    }

    // Вызывается из InventoryManager при наведении
    public void SetHover(bool state)
    {
        isHovered = state;
    }

    // Вызывается из InventoryManager при клике
    public void Push()
    {
        StartCoroutine(PushRoutine());
    }

    IEnumerator PushRoutine()
    {
        // Утапливаем (если кирпич едет не туда, поменяй Vector3.back на Vector3.right или forward)
        targetLocalPos = originalLocalPos + Vector3.back * pushDepth;
        yield return new WaitForSeconds(0.1f);
        // Возвращаем
        targetLocalPos = originalLocalPos;
    }
}
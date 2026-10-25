using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshPro textMesh;

    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.8f;       // Сколько живет цифра
    [SerializeField] private float upSpeed = 1.5f;        // Скорость полета вверх
    [SerializeField] private float driftRange = 0.5f;     // Насколько сильно уносит вбок

    private float age;
    private Vector3 driftVelocity;
    private Color originalColor;
    private Transform camTransform;

    public void Initialize(float damageAmount)
    {
        // Округляем урон до целого числа и выводим на экран
        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();
        originalColor = textMesh.color;

        // Находим камеру локального игрока для выравнивания текста и расчета дрейфа
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        // Вычисляем случайное направление дрейфа (влево или вправо относительно взгляда камеры)
        Vector3 rightDir = camTransform != null ? camTransform.right : Vector3.right;
        driftVelocity = rightDir * Random.Range(-driftRange, driftRange);

        // Начинаем с нулевого масштаба для эффекта появления из точки
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        age += Time.deltaTime;
        float progress = age / duration;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // 1. Движение вверх и небольшой случайный дрейф вбок
        transform.position += (Vector3.up * upSpeed + driftVelocity) * Time.deltaTime;

        // 2. Анимация размера: быстро увеличиваемся в начале, затем плавно уменьшаемся
        float scale = 1f;
        if (progress < 0.2f)
        {
            scale = Mathf.Lerp(0f, 1.3f, progress / 0.2f); // Быстрый взлет размера до 130%
        }
        else
        {
            scale = Mathf.Lerp(1.3f, 0.9f, (progress - 0.2f) / 0.8f); // Плавное сдувание
        }
        transform.localScale = Vector3.one * scale;

        // 3. Плавное растворение (Fade out) во второй половине жизни
        if (progress > 0.5f)
        {
            float alphaProgress = (progress - 0.5f) / 0.5f; // Значение от 0 до 1
            Color fadedColor = originalColor;
            fadedColor.a = Mathf.Lerp(1f, 0f, alphaProgress);
            textMesh.color = fadedColor;
        }

        // 4. Эффект Billboard: текст всегда развернут лицом к камере
        if (camTransform != null)
        {
            transform.LookAt(transform.position + camTransform.rotation * Vector3.forward, camTransform.rotation * Vector3.up);
        }
    }
}
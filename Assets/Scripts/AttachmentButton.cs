using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;

public class AttachmentButton : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI nameText;
    public Button button;

    [HideInInspector] public AttachmentData data;
    private RectTransform rect;

    public void Setup(AttachmentData attachmentData, Action onClickAction)
    {
        data = attachmentData;
        rect = GetComponent<RectTransform>();

        if (nameText != null)
        {
            nameText.text = attachmentData.attachmentName;
            nameText.color = Color.white;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickAction?.Invoke());
        }
    }

    public IEnumerator AnimateShow(Vector2 targetPos, float delay)
    {
        // Если объект уничтожили до начала анимации
        if (this == null || rect == null) yield break;

        rect.anchoredPosition = Vector2.zero;
        transform.localScale = Vector3.zero;

        yield return new WaitForSeconds(delay);

        // Проверка после ожидания
        if (this == null || rect == null) yield break;

        Vector2 startPos = Vector2.zero;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one;

        float t = 0;
        float duration = 0.3f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float curve = Mathf.Sin(t * Mathf.PI * 0.5f);

            // САМАЯ ВАЖНАЯ ПРОВЕРКА: если во время анимации список закрыли/обновили
            if (rect == null) yield break;

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, curve);
            transform.localScale = Vector3.Lerp(startScale, endScale, curve);

            yield return null;
        }

        if (rect != null)
        {
            rect.anchoredPosition = targetPos;
            transform.localScale = endScale;
        }
    }
}
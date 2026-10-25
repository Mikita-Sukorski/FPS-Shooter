using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSoundTrigger : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    private AudioSource globalSource;

    private void Start()
    {
        // Ищем или создаем временный AudioSource на камере интерфейса
        globalSource = Camera.main.GetComponent<AudioSource>();
        if (globalSource == null)
        {
            globalSource = Camera.main.gameObject.AddComponent<AudioSource>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Button btn = GetComponent<Button>();
        if (btn != null && btn.interactable && hoverSound != null && globalSource != null)
        {
            globalSource.PlayOneShot(hoverSound, 0.5f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Button btn = GetComponent<Button>();
        if (btn != null && btn.interactable && clickSound != null && globalSource != null)
        {
            globalSource.PlayOneShot(clickSound, 0.8f);
        }
    }
}
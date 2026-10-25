using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ShellCasingSound : MonoBehaviour
{
    [SerializeField] private AudioClip[] bounceSounds;
    private AudioSource audioSource;
    private bool hasLanded = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f; // Полное 3D позиционирование
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Воспроизводим звук только при первом сильном столкновении, чтобы избежать спама звуками
        if (!hasLanded && bounceSounds.Length > 0 && collision.relativeVelocity.magnitude > 0.5f)
        {
            AudioClip clip = bounceSounds[Random.Range(0, bounceSounds.Length)];
            audioSource.pitch = Random.Range(0.9f, 1.1f); // Небольшой случайный разброс высоты тона
            audioSource.PlayOneShot(clip, 0.5f);
            hasLanded = true;
        }
    }
}
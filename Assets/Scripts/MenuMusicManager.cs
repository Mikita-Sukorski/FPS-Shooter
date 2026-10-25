using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicManager : MonoBehaviour
{
    [SerializeField] private AudioClip menuTheme;
    [Range(0f, 1f)][SerializeField] private float volume = 0.3f;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = menuTheme;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f; // 2D звук (стерео для музыки)

        if (menuTheme != null)
        {
            audioSource.Play();
        }
    }
}
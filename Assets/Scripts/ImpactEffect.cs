using UnityEngine;
using System.Collections;

public class ImpactEffect : MonoBehaviour
{
    public float lifetime = 5f;     // Сколько висит след
    public float fadeDuration = 2f; // Сколько длится плавное исчезновение
    private MeshRenderer meshRenderer;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(lifetime);

        float elapsed = 0;
        Material mat = meshRenderer.material;
        Color startColor = mat.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        Destroy(gameObject);
    }
}
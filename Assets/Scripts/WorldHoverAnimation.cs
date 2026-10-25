using UnityEngine;

public class WorldHoverAnimation : MonoBehaviour
{
    public float hoverScale = 1.3f;
    public float clickScale = 0.8f;
    public float speed = 15f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    public void SetHover(bool isHovered)
    {
        targetScale = isHovered ? originalScale * hoverScale : originalScale;
    }

    public void Push()
    {
        transform.localScale = originalScale * clickScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }
}
using UnityEngine;

public class CrosshairController : MonoBehaviour
{
    [Header("Parts")]
    public RectTransform top;
    public RectTransform bottom;
    public RectTransform left;
    public RectTransform right;
    public RectTransform dot;

    [Header("Tactical")]
    public RectTransform topLeft;
    public RectTransform topRight;
    public RectTransform bottomLeft;
    public RectTransform bottomRight;

    // offset — это расстояние в пикселях от центра
    public void UpdateCrosshair(float offset)
    {
        // Классика
        if (top) top.anchoredPosition = new Vector2(0, offset);
        if (bottom) bottom.anchoredPosition = new Vector2(0, -offset);
        if (left) left.anchoredPosition = new Vector2(-offset, 0);
        if (right) right.anchoredPosition = new Vector2(offset, 0);

        // Тактический
        if (topLeft) topLeft.anchoredPosition = new Vector2(-offset, offset);
        if (topRight) topRight.anchoredPosition = new Vector2(offset, offset);
        if (bottomLeft) bottomLeft.anchoredPosition = new Vector2(-offset, -offset);
        if (bottomRight) bottomRight.anchoredPosition = new Vector2(offset, -offset);
    }
}
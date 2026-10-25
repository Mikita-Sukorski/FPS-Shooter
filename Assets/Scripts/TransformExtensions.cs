using UnityEngine;
public static class TransformExtensions
{
    public static Transform FindRecursive(this Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = child.FindRecursive(name);
            if (result != null) return result;
        }
        return null;
    }
}
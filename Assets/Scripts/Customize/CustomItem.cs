using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomItem", menuName = "Customization/Item")]
public class CustomItem : ScriptableObject
{
    public int id;
    public string itemName;
    public GameObject prefab;
}
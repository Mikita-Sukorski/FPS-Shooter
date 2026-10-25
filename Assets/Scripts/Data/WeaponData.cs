using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon/Weapon")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public GameObject menuModelPrefab; // Префаб, который спавнится в меню кастомизации
    public List<AttachmentData> compatibleAttachments; // Список доступных обвесов
}
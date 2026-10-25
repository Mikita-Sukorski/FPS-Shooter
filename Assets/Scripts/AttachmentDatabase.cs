using UnityEngine;
using System.Collections.Generic;

// Эта строчка позволяет создать файл через правую кнопку мыши
[CreateAssetMenu(fileName = "New Attachment Database", menuName = "Weapon/Attachment Database")]
public class AttachmentDatabase : ScriptableObject
{
    // Список всех обвесов, которые существуют в твоей игре
    public List<AttachmentData> allAttachments;
}
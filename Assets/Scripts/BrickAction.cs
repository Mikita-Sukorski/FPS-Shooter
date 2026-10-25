using UnityEngine;
using UnityEngine.Events;

public class BrickAction : MonoBehaviour
{
    [Header("Событие при клике")]
    public UnityEvent onClickAction;

    // Этот метод будет вызывать InventoryManager
    public void Execute()
    {
        onClickAction?.Invoke();
    }
}
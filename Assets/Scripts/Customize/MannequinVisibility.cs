using UnityEngine;

public class MannequinVisibility : MonoBehaviour
{
    private void Start()
    {
        // Подписываемся на событие твоего MenuManager
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OnMenuStateChanged += HandleMenuChange;

            CheckInitialState();
        }
    }

    private void OnDestroy()
    {
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.OnMenuStateChanged -= HandleMenuChange;
        }
    }

    private void CheckInitialState()
    {
        // Проходимся по всем меню и ищем открытое
        foreach (var menu in MenuManager.Instance.menus)
        {
            if (menu.open)
            {
                HandleMenuChange(menu.menuName, true);
                break;
            }
        }
    }

    private void HandleMenuChange(string menuName, bool isOpen)
    {
        if (!isOpen) return;

        // Манекен виден ТОЛЬКО в этих двух меню
        bool shouldBeVisible = (menuName == "title" || menuName == "customization");

        // Включаем/выключаем дочерние объекты (Mesh и надетые предметы)
        // Самому объекту нельзя делать SetActive(false), иначе отпишется от событий
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(shouldBeVisible);
        }
    }
}
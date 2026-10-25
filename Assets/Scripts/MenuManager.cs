using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// ВАЖНО: Наследуемся от MonoBehaviourPunCallbacks, чтобы ловить события Photon
public class MenuManager : MonoBehaviourPunCallbacks
{
    public static MenuManager Instance;

    [SerializeField] public Menu[] menus;

    public event Action<string, bool> OnMenuStateChanged;

    void Awake()
    {
        Instance = this;
    }

    public void OpenMenu(string menuName)
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].menuName == menuName)
            {
                menus[i].Open();
                OnMenuStateChanged?.Invoke(menuName, true);
            }
            else if (menus[i].open)
            {
                CloseMenu(menus[i]);
            }
        }
    }

    public void OpenMenu(Menu menu)
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].open)
            {
                CloseMenu(menus[i]);
            }
        }
        menu.Open();
        OnMenuStateChanged?.Invoke(menu.menuName, true);
    }

    public void CloseMenu(Menu menu)
    {
        menu.Close();
        OnMenuStateChanged?.Invoke(menu.menuName, false);
    }

    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ExitSession()
    {
        // 1. Отключаем синхронизацию сцен, чтобы не тащить за собой других игроков!
        PhotonNetwork.AutomaticallySyncScene = false;

        // 2. Удаляем RoomManager, чтобы он не пытался респавнить нас в момент выхода
        if (RoomManager.Instance != null)
        {
            Destroy(RoomManager.Instance.gameObject);
        }

        // 3. Вызываем выход из комнаты (Photon сам удалит наши объекты)
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            // На случай, если мы уже вышли по тайм-ауту/ошибке интернета
            SceneManager.LoadScene(0);
        }
    }

    // 4. ЭТОТ метод сработает автоматически, как только Photon завершит выход из комнаты
    public override void OnLeftRoom()
    {
        // Как ты и предложил - грузим 0-ю сцену (Initializer)
        SceneManager.LoadScene(0);
    }
}
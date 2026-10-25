using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
	public static SpawnManager Instance;

	Spawnpoint[] spawnpoints;

    void Awake()
    {
        // Убедитесь, что это единственный экземпляр SpawnManager.
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject); // Предотвратить уничтожение объекта при смене сцены
        }
        else
        {
            Destroy(gameObject); // Если экземпляр уже существует, уничтожить новый объект
        }

        // Получаем все Spawnpoint в дочерних объектах
        spawnpoints = GetComponentsInChildren<Spawnpoint>();

        // Проверка, что есть хотя бы один Spawnpoint
        if (spawnpoints.Length == 0)
        {
            Debug.LogError("No Spawnpoints found in the scene!");
        }
    }

    public Transform GetSpawnpoint(int teamId)
    {
        var validPoints = spawnpoints.Where(sp => sp.teamType == teamId || sp.teamType == 0).ToArray();

        if (validPoints.Length == 0)
        {
            Debug.LogWarning("Не найдены точки спавна для команды " + teamId + ", берем случайную!");
            validPoints = spawnpoints; // Fallback
        }

        return validPoints[Random.Range(0, validPoints.Length)].transform;
    }
}

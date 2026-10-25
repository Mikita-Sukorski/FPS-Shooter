using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawnpoint : MonoBehaviour
{
    [Tooltip("0 = Любая (FFA), 1 = Красные, 2 = Синие")]
    public int teamType = 0;

    [SerializeField] GameObject graphics;

    void Awake()
    {
        if (graphics != null)
        {
            graphics.SetActive(false);
        }
    }
}

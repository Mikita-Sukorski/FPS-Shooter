using UnityEngine;

public class DeathParticle : MonoBehaviour
{
    void Start()
    {
        // Живут от 2 до 4 секунд, чтобы не забивать память
        Destroy(gameObject, Random.Range(2f, 4f));
    }
}
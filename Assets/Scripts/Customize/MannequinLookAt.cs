using UnityEngine;

public class MannequinLookAt : MonoBehaviour
{
    [Header("Настройки")]
    public Transform cameraToLookAt; // Сюда закинем Main Camera
    public float rotationSpeed = 5f; // Скорость поворота манекена

    private void Start()
    {
        // Если забыли указать камеру, скрипт найдет главную сам
        if (cameraToLookAt == null && Camera.main != null)
        {
            cameraToLookAt = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (cameraToLookAt == null) return;

        // 1. Вычисляем направление от манекена к камере
        Vector3 directionToCamera = cameraToLookAt.position - transform.position;

        // 2. ОБЯЗАТЕЛЬНО обнуляем Y, чтобы манекен не наклонялся вверх или вниз (стоял ровно)
        directionToCamera.y = 0;

        // Проверка, чтобы избежать ошибки, если камера и манекен окажутся в одной точке
        if (directionToCamera != Vector3.zero)
        {
            // 3. Вычисляем финальный поворот (манекен смотрит в сторону камеры)
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);

            // 4. Плавно поворачиваем манекен с помощью Slerp (Сферическая интерполяция)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}
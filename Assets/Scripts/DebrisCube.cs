using UnityEngine;

public class DebrisCube : MonoBehaviour
{
    private Vector3 velocity;
    private float lifeTime;
    private float currentLife;
    private Vector3 initialScale;

    public void Initialize(Vector3 hitNormal, Material surfaceMaterial)
    {
        // 1. Применяем материал стены к кубику
        if (surfaceMaterial != null)
        {
            GetComponent<MeshRenderer>().material = surfaceMaterial;
        }

        // 2. Случайный размер (от маленьких до средних)
        float scale = Random.Range(0.05f, 0.15f);
        transform.localScale = new Vector3(scale, scale, scale);
        initialScale = transform.localScale;

        // 3. Вычисляем направление отскока (нормаль стены + случайный разброс)
        Vector3 randomSpread = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized * 0.5f; // Сила разброса в стороны

        Vector3 bounceDirection = (hitNormal + randomSpread).normalized;

        // 4. Задаём скорость вылета
        float speed = Random.Range(3f, 7f);
        velocity = bounceDirection * speed;

        // 5. Время жизни кубика (от 1 до 2 секунд)
        lifeTime = Random.Range(1f, 2f);
        currentLife = lifeTime;
    }

    void Update()
    {
        // Имитация гравитации (тянет вниз)
        velocity += Physics.gravity * Time.deltaTime;

        // Движение кубика
        transform.position += velocity * Time.deltaTime;

        // Вращение кубика в полёте для красоты
        transform.Rotate(Vector3.one * 360f * Time.deltaTime);

        // Уменьшение масштаба (плавное исчезновение)
        currentLife -= Time.deltaTime;
        float timeRatio = currentLife / lifeTime;

        // Lerp плавно меняет размер от начального до нулевого
        transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, timeRatio);

        // Уничтожаем объект, когда время вышло
        if (currentLife <= 0)
        {
            Destroy(gameObject);
        }
    }
}
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D? rb;
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Создаем переменные для направлений
        float moveX = 0f;
        float moveY = 0f;

        // Проверяем нажатие конкретных клавиш W, A, S, D
        if (Input.GetKey(KeyCode.W)) // Если зажата W
        {
            moveY = 1f; // Движение ВВЕРХ
        }
        else if (Input.GetKey(KeyCode.S)) // Если зажата S
        {
            moveY = -1f; // Движение ВНИЗ
        }

        if (Input.GetKey(KeyCode.D)) // Если зажата D
        {
            moveX = 1f; // Движение ВПРАВО
        }
        else if (Input.GetKey(KeyCode.A)) // Если зажата A
        {
            moveX = -1f; // Движение ВЛЕВО
        }

        // Соединяем в один вектор и нормализуем (чтобы не было ускорения по диагонали)
        moveInput = new Vector2(moveX, moveY).normalized;
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            // Применяем скорость к Rigidbody2D квадрата
            rb.velocity = moveInput * moveSpeed;
        }
    }
}
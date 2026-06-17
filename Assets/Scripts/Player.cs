using UnityEngine;
using UnityEngine.InputSystem; // Добавляем пространство имен новой системы ввода

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb; // В C# для Unity лучше использовать обычный Rigidbody2D (без ?)
    private Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Получаем доступ к текущей клавиатуре
        var keyboard = Keyboard.current;

        // Если клавиатура не подключена, ничего не делаем
        if (keyboard == null) return;

        // Создаем переменные для направлений
        float moveX = 0f;
        float moveY = 0f;

        // Проверяем нажатие конкретных клавиш W, A, S, D через новую систему
        if (keyboard.wKey.isPressed) // Если зажата W
        {
            moveY = 1f; // Движение ВВЕРХ
        }
        else if (keyboard.sKey.isPressed) // Если зажата S
        {
            moveY = -1f; // Движение ВНИЗ
        }

        if (keyboard.dKey.isPressed) // Если зажата D
        {
            moveX = 1f; // Движение ВПРАВО
        }
        else if (keyboard.aKey.isPressed) // Если зажата A
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
<<<<<<< Updated upstream
            // Применяем скорость к Rigidbody2D (в Unity 2024+ используется linearVelocity)
=======
            // Применяем скорость к Rigidbody2D квадрата
>>>>>>> Stashed changes
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }
}
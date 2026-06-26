using UnityEngine;
using UnityEngine.InputSystem; // Добавляем пространство имен новой системы ввода

public class PlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D    rb;
    private Vector2        moveInput;
    private PlayerAnimator playerAnimator;
    private Game.Player.PlayerStats stats; // ТЗ §5 — скорость из пересчитанных статов
    private Game.Combat.StatusEffectHandler status; // Freeze/Stun/Poison от зон и врагов

    void Start()
    {
        rb             = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<PlayerAnimator>();
        stats          = GetComponent<Game.Player.PlayerStats>();
        if (stats != null && stats.Speed > 0f) moveSpeed = stats.Speed;

        // Обработчик статус-эффектов: даёт игроку Freeze/Stun/Poison от HazardZone,
        // модификаторов Frozen/Toxic и босса (DoT идёт через PlayerHealth : IDamageable).
        status = GetComponent<Game.Combat.StatusEffectHandler>();
        if (status == null) status = gameObject.AddComponent<Game.Combat.StatusEffectHandler>();

        // Добавить коллайдер если его нет
        if (GetComponent<Collider2D>() == null)
        {
            var col = gameObject.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.5f, 0.7f);
            col.offset = new Vector2(0f, 0f);
        }

        // Заморозить вращение чтобы персонаж не вращался при столкновении
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
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

        // Stun (ТЗ §4): полная остановка — нельзя двигаться.
        if (status != null && status.IsStunned) moveInput = Vector2.zero;

        if (keyboard.eKey.wasPressedThisFrame)
            playerAnimator?.TriggerAttack();
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            // Freeze (ТЗ §4): SpeedMultiplier < 1 замедляет игрока в ледяных зонах/ауре.
            float slow = status != null ? status.SpeedMultiplier : 1f;
            rb.linearVelocity = moveInput * moveSpeed * slow;
        }
    }
}

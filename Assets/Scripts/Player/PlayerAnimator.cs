using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Спрайты анимаций")]
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private Sprite[] attackSprites;
    [SerializeField] private Sprite[] deathSprites;

    [Header("Скорость анимаций (кадров/сек)")]
    [SerializeField] private float walkFPS   = 9f;
    [SerializeField] private float attackFPS = 10f;
    [SerializeField] private float deathFPS  = 8f;

    private SpriteRenderer sr;
    private Rigidbody2D    rb;
    private PlayerHealth   health;

    private enum State { Idle, Walk, Attack, Death }
    private State  state       = State.Idle;
    private int    frameIndex  = 0;
    private float  frameTimer  = 0f;
    private bool   attackDone  = false;

    void Awake()
    {
        sr     = GetComponent<SpriteRenderer>();
        rb     = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();

        if (health != null)
            health.OnDeath.AddListener(PlayDeath);
    }

    void Update()
    {
        if (state == State.Death) { TickAnimation(deathSprites, deathFPS, loop: false); return; }

        Vector2 vel = rb != null ? rb.linearVelocity : Vector2.zero;
        bool moving = vel.sqrMagnitude > 0.01f;

        // Зеркалим спрайт по горизонтали
        if (Mathf.Abs(vel.x) > 0.01f)
            sr.flipX = vel.x < 0f;

        if (state == State.Attack)
        {
            TickAnimation(attackSprites, attackFPS, loop: false);
            if (attackDone) SetState(moving ? State.Walk : State.Idle);
            return;
        }

        State next = moving ? State.Walk : State.Idle;
        if (next != state) SetState(next);

        if (state == State.Walk)
            TickAnimation(walkSprites, walkFPS, loop: true);
        else
            ShowIdleFrame();
    }

    // Вызывай извне (из скрипта атаки) для запуска анимации атаки
    public void TriggerAttack()
    {
        if (state == State.Death) return;
        SetState(State.Attack);
    }

    public void PlayDeath() => SetState(State.Death);

    // ──────────────────────────────────────────────

    private void SetState(State next)
    {
        state      = next;
        frameIndex = 0;
        frameTimer = 0f;
        attackDone = false;
    }

    private void TickAnimation(Sprite[] sprites, float fps, bool loop)
    {
        if (sprites == null || sprites.Length == 0) return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / fps;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;

            if (frameIndex >= sprites.Length)
            {
                if (loop)
                    frameIndex = 0;
                else
                {
                    frameIndex = sprites.Length - 1;
                    attackDone = true;
                    break;
                }
            }
        }

        sr.sprite = sprites[frameIndex];
    }

    private void ShowIdleFrame()
    {
        if (walkSprites != null && walkSprites.Length > 0)
            sr.sprite = walkSprites[0];
    }
}

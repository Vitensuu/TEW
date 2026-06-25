using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private static readonly int AnimIsMoving   = Animator.StringToHash("isMoving");
    private static readonly int AnimIsAttacking = Animator.StringToHash("isAttacking");
    private static readonly int AnimIsDead      = Animator.StringToHash("isDead");
    private static readonly int AnimDirX        = Animator.StringToHash("dirX");
    private static readonly int AnimDirY        = Animator.StringToHash("dirY");

    private Animator     anim;
    private Rigidbody2D  rb;
    private PlayerHealth health;

    void Awake()
    {
        anim   = GetComponent<Animator>();
        rb     = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();

        if (health != null)
            health.OnDeath.AddListener(OnDeath);

        // Default facing: down
        anim.SetFloat(AnimDirY, -1f);
    }

    void Update()
    {
        if (anim.GetBool(AnimIsDead)) return;

        Vector2 vel   = rb != null ? rb.linearVelocity : Vector2.zero;
        bool    moving = vel.sqrMagnitude > 0.01f;

        anim.SetBool(AnimIsMoving, moving);

        if (moving)
            SetDirection(vel);
    }

    public void TriggerAttack()
    {
        if (anim.GetBool(AnimIsDead)) return;
        anim.SetTrigger(AnimIsAttacking);
        StartCoroutine(ResetAttackAfterClip());
    }

    System.Collections.IEnumerator ResetAttackAfterClip()
    {
        yield return null;
        yield return null;

        while (true)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            // Если это клип атаки и он ещё не закончился — ждём
            if (info.IsName("Attack_Down") || info.IsName("Attack_Up") ||
                info.IsName("Attack_Left") || info.IsName("Attack_Right"))
            {
                if (info.normalizedTime >= 0.95f) break;
            }
            else
            {
                // Уже вышли из атаки сами
                yield break;
            }
            yield return null;
        }

        // Сбрасываем триггер и возвращаем в Idle/Walk
        anim.ResetTrigger(AnimIsAttacking);
        anim.SetBool(AnimIsMoving, rb != null && rb.linearVelocity.sqrMagnitude > 0.01f);
    }

    private void OnDeath()
    {
        anim.SetTrigger(AnimIsDead);
    }

    private void SetDirection(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            anim.SetFloat(AnimDirX, dir.x > 0f ? 1f : -1f);
            anim.SetFloat(AnimDirY, 0f);
        }
        else
        {
            anim.SetFloat(AnimDirX, 0f);
            anim.SetFloat(AnimDirY, dir.y > 0f ? 1f : -1f);
        }
    }
}

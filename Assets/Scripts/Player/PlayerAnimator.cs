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

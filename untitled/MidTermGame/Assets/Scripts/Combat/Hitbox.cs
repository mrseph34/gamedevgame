using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class Hitbox : MonoBehaviour
{
    Vector2 knockback;
    float stunDuration;
    string targetTag;
    public float lifetime;
    Collider2D ownerCollider;
    Transform ownerTransform;
    Vector2 offset;

    bool hasHitSomething;
    int damage;
    bool debug = false;

    const int defaultDamage = 10;

    /// Temp to warn if no damage set while testing
    public void Setup(
        Vector2 kb,
        float stunDur,
        string tag,
        float life,
        Collider2D ownerCol,
        Vector2 offset
    )
    {
        Debug.LogWarning(
            $"{name}.Hitbox.Setup() called without damage. Using defaultDamage = {defaultDamage}."
        );
        Setup(kb, stunDur, tag, life, ownerCol, offset, defaultDamage);
    }

    public void Setup(
        Vector2 kb,
        float stunDur,
        string tag,
        float life,
        Collider2D ownerCol,
        Vector2 offset,
        int damage
    )
    {
        this.knockback = kb;
        this.stunDuration = stunDur;
        this.targetTag = tag;
        this.lifetime = life;
        this.ownerCollider = ownerCol;
        this.ownerTransform = ownerCol.transform;
        this.offset = offset;
        this.damage = damage;

        var myCol = GetComponent<Collider2D>();
        myCol.isTrigger = true;
        if (ownerCollider != null)
            Physics2D.IgnoreCollision(myCol, ownerCollider);

        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (ownerTransform != null)
            transform.position = ownerTransform.position + (Vector3)offset;
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (hasHitSomething)
            return;
        if (col == ownerCollider)
            return;
        if (!col.CompareTag(targetTag))
            return;

        hasHitSomething = true;

        // StateHandler: knockback, stun, AND damage
        var sh = col.GetComponent<StateHandler>();
        Animator plrAnimator = ownerCollider.GetComponent<Animator>();
        if (sh != null && plrAnimator && plrAnimator.GetBool("canAttack"))
        {
            Animator targetAnimator = sh.GetComponent<Animator>();

            // Check if target is performing heavy attack and can tank a hit
            if (targetAnimator != null && targetAnimator.GetBool("isHeavy"))
            {
                // Check if they have the heavyTank bool and if it's true
                bool canTank = false;
                foreach (AnimatorControllerParameter param in targetAnimator.parameters)
                {
                    if (param.name == "heavyTank" && param.type == AnimatorControllerParameterType.Bool)
                    {
                        canTank = targetAnimator.GetBool("heavyTank");
                        break;
                    }
                }

                if (canTank)
                {
                    // Tank the hit - set heavyTank to false and don't apply damage/knockback
                    targetAnimator.SetBool("heavyTank", false);
                    // Debug.Log($"{col.name} tanked a hit during heavy attack!");
                    return; // Exit without applying hit effects
                }
            }

            // Normal hit logic - apply damage, knockback, and stun
            sh.ReceiveHit(knockback, stunDuration, damage);
            targetAnimator.SetTrigger("playerHit");
            
            if (targetAnimator.GetBool("playerAttacking"))
            {
                targetAnimator.SetBool("canAttack", false);
                targetAnimator.SetTrigger("attackExit");
                targetAnimator.SetTrigger("heavyExit");
                StartCoroutine(ReenableAttackAfterDelay(targetAnimator, 0.3f));
            }
        }
    }
    
    private IEnumerator ReenableAttackAfterDelay(Animator anim, float delay)
    {
        yield return new WaitForSeconds(delay);
        anim.SetBool("canAttack", true);
    }

    void OnDrawGizmosSelected()
    {
        var b = GetComponent<BoxCollider2D>();
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3)b.offset, b.size);
    }
}
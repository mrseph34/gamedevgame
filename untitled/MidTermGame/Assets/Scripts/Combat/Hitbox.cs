using UnityEngine;

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
        if (sh != null)
            sh.ReceiveHit(knockback, stunDuration, damage);
    }

    void OnDrawGizmosSelected()
    {
        var b = GetComponent<BoxCollider2D>();
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3)b.offset, b.size);
    }
}

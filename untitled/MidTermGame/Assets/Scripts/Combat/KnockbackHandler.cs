using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KnockbackHandler : MonoBehaviour
{
    Rigidbody2D rb;
    void Awake() => rb = GetComponent<Rigidbody2D>();

    public void ApplyKnockback(Vector2 force)
    {
        float randomMultiplier = Random.Range(0.8f, 1.5f);
        Vector2 finalForce = force * randomMultiplier;
        rb.linearVelocity = finalForce;
    }
}
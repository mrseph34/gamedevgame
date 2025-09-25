using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KnockbackHandler : MonoBehaviour
{
    Rigidbody2D rb;
    void Awake() => rb = GetComponent<Rigidbody2D>();

    public void ApplyKnockback(Vector2 force)
    {
        rb.linearVelocity = force;
    }
}
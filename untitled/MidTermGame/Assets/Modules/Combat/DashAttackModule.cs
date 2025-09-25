using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Combat/Dash Attack")]
public class DashAttackModule : AttackModule
{
    [Header("Dash Component")]
    public DashMoveModule dashModule;

    [Header("Attack Settings")]
    public float attackStartDelay = 0.05f;
    public float hitboxDuration = 0.2f;
    public float knockbackForce = 8f;
    public float stunDuration = 0.3f;
    public string targetTag = "Player";

    [Header("Hitbox Graphics")]
    public Sprite hitboxSprite;
    public Color hitboxColor = new Color(1, 0.5f, 0, 0.6f);

    [Header("Hitbox Size & Offset")]
    public Vector2 hitboxSize = new Vector2(12, 20);
    public Vector2 hitboxOffset = new Vector2(0, 0);

    private GameObject activeHitbox;

    protected override IEnumerator PerformAttack(CombatHandler ch)
    {
        if (dashModule == null)
        {
            Debug.LogError("DashAttackModule requires a DashMoveModule reference!");
            yield break;
        }
        
        Vector2 dashDirection = GetDashDirection(ch);

        // Start the dash and attack simultaneously
        var dashCoroutine = ch.StartCoroutine(dashModule.ExecuteDash(ch));
        var attackCoroutine = ch.StartCoroutine(HandleAttack(ch, dashDirection));

        // Wait for both to complete
        yield return dashCoroutine;
        yield return attackCoroutine;

        // Cleanup
        CleanupAttack();
    }

    private IEnumerator ExecuteDash(CombatHandler ch)
    {
        var cachedRb = ch.GetComponent<Rigidbody2D>();
        if (cachedRb == null)
        {
            Debug.LogError("DashAttackModule requires a Rigidbody2D component!");
            yield break;
        }

        float originalDrag = cachedRb.linearDamping;
        float originalGravityScale = cachedRb.gravityScale;

        ch.StateHandler.ChangeState(StateHandler.State.Dashing);

        Vector2 dashDirection = GetDashDirection(ch);

        Vector2 dashVelocity = dashDirection * dashModule.dashForce;
        cachedRb.linearVelocity = dashVelocity;

        cachedRb.linearDamping = dashModule.dragDuringDash;
        if (dashModule.ignoreGravityDuringDash)
            cachedRb.gravityScale = 0f;

        float elapsedTime = 0f;
        Vector2 initialVelocity = dashVelocity;

        while (elapsedTime < dashModule.dashDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / dashModule.dashDuration;
            float velocityMultiplier = dashModule.dashCurve.Evaluate(normalizedTime);
            Vector2 currentDashVelocity = initialVelocity * velocityMultiplier;

            if (!dashModule.ignoreGravityDuringDash)
            {
                currentDashVelocity.y = cachedRb.linearVelocity.y;
            }

            cachedRb.linearVelocity = currentDashVelocity;

            yield return null;
        }

        cachedRb.linearDamping = originalDrag;
        cachedRb.gravityScale = originalGravityScale;

        // Return to normal state
        ch.StateHandler.ChangeState(StateHandler.State.Moving);
    }

    private IEnumerator HandleAttack(CombatHandler ch, Vector2 dashDirection)
    {
        yield return new WaitForSeconds(attackStartDelay);
        CreateHitbox(ch, dashDirection);
        yield return new WaitForSeconds(hitboxDuration);
        CleanupAttack();
    }

    private void CreateHitbox(CombatHandler ch, Vector2 dashDirection)
    {
        CleanupAttack();
        activeHitbox = new GameObject("DashAttackHitbox"); 
        Vector2 adjustedOffset = hitboxOffset;

        if (dashDirection.x < 0)
            adjustedOffset.x = -adjustedOffset.x;

        if (Mathf.Abs(dashDirection.y) > 0.7f) // Mostly vertical dash
        {
            adjustedOffset = new Vector2(
                0f,
                dashDirection.y > 0 ? hitboxOffset.x : -hitboxOffset.x
            );
        }

        // Position hitbox
        activeHitbox.transform.position = ch.transform.position + (Vector3)adjustedOffset;
        activeHitbox.transform.localScale = hitboxSize;
        activeHitbox.transform.SetParent(ch.transform, worldPositionStays: true);

        // Add visual ?
        if (hitboxSprite != null)
        {
            var spriteRenderer = activeHitbox.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = hitboxSprite;
            spriteRenderer.color = hitboxColor;
            spriteRenderer.sortingOrder = 100;
        }

        var boxCollider = activeHitbox.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

        Vector2 knockbackDirection = dashDirection * knockbackForce;

        var hitboxComponent = activeHitbox.AddComponent<Hitbox>();
        hitboxComponent.Setup(
            knockbackDirection,
            stunDuration,
            targetTag,
            hitboxDuration,
            ch.SelfCollider,
            adjustedOffset
        );
    }

    private Vector2 GetDashDirection(CombatHandler ch)
    {
        // Same logic as DashMoveModule ( refer to dash move modues? eh will look later)
        Vector2 inputDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        if (inputDirection.sqrMagnitude > 0.1f)
        {
            return inputDirection.normalized;
        }

        float facingDirection = ch.transform.localScale.x > 0 ? 1f : -1f;
        return Vector2.right * facingDirection;
    }

    private void CleanupAttack()
    {
        if (activeHitbox != null)
        {
            Destroy(activeHitbox);
            activeHitbox = null;
        }
    }

    // Safety cleanup
    private void OnDisable()
    {
        CleanupAttack();
    }
}

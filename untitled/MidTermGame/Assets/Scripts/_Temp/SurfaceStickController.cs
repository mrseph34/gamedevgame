using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PillController))]
public class SurfaceStickController : MonoBehaviour
{
    [Header("Debug Mode")]
    public bool debugMode = false; // Walk onto walls freely without knockback requirement
    
    [Header("Knockback Threshold")]
    public float minKnockbackSpeed = 8f; // Minimum speed to stick to surface
    public float knockbackAngleRange = 45f; // Degrees from perpendicular to surface
    
    [Header("Surface Detection")]
    public float surfaceCheckDistance = 0.3f;
    public LayerMask surfaceLayer;
    
    [Header("Adhesion Settings")]
    public float maxAdhesionTime = 8f; // Seconds before falling off
    public float adhesionDepletionRate = 1f; // Units per second
    public float movementSpeedMultiplier = 0.75f; // Speed reduction on surfaces
    public float ceilingSpeedMultiplier = 0.5f; // Even slower on ceiling
    
    [Header("Surface Attack Costs")]
    public float attackAdhesionCost = 1.5f; // Extra adhesion cost per attack
    
    [Header("Drop/Slide Settings")]
    public float slideOffForce = 5f;
    public float stunDurationOnAdhesionFail = 0.5f;
    
    [Header("Surface Attacks")]
    public float wallSlamForce = 10f;
    public float ceilingDropChargeTime = 1f;
    public float ceilingDropForce = 15f;
    
    // Internal state
    private Rigidbody2D rb;
    private PillController pillController;
    private StateHandler stateHandler;
    private Animator animator;
    private CombatHandler combatHandler;
    
    private bool isStuckToSurface = false;
    private SurfaceType currentSurface = SurfaceType.None;
    private Vector2 surfaceNormal = Vector2.up;
    private float currentAdhesion = 0f;
    private bool canStick = true;
    
    // Rotation tracking
    private Quaternion targetRotation = Quaternion.identity;
    
    public enum SurfaceType
    {
        None,
        Floor,
        LeftWall,
        RightWall,
        Ceiling
    }
    
    public bool IsStuckToSurface => isStuckToSurface;
    public SurfaceType CurrentSurface => currentSurface;
    public float AdhesionPercent => currentAdhesion / maxAdhesionTime;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pillController = GetComponent<PillController>();
        stateHandler = GetComponent<StateHandler>();
        animator = GetComponent<Animator>();
        combatHandler = GetComponent<CombatHandler>();
    }
    
    void Update()
    {
        // Check if player is dashing or in other states that should break stick
        if (isStuckToSurface)
        {
            if (ShouldDetachFromState())
            {
                DetachFromSurface(false);
                return;
            }
            
            HandleSurfaceMovement();
            DepleteAdhesion();
            CheckForDropInput();
            CheckSurfaceAttacks();
        }
        else
        {
            CheckForSurfaceImpact();
        }
        
        // Debug visualization
        if (debugMode && isStuckToSurface)
        {
            Debug.DrawRay(transform.position, surfaceNormal * 2f, Color.green);
        }
    }
    
    bool ShouldDetachFromState()
    {
        // Detach if entering states that conflict with sticking
        var state = stateHandler.CurrentState;
        
        if (state == StateHandler.State.Dashing ||
            state == StateHandler.State.Sliding ||
            state == StateHandler.State.Stunned ||
            state == StateHandler.State.GettingHit ||
            animator.GetBool("playerAttacking")
            )
        {
            return true;
        }
        
        // Also detach if hit while on surface
        if (animator.GetBool("playerHit"))
        {
            return true;
        }
        
        return false;
    }
    
    void FixedUpdate()
    {
        if (isStuckToSurface)
        {
            ApplySurfacePhysics();
        }
        else
        {
            // Always force reset rotation when not stuck
            ForceResetRotation();
        }
    }
    
    void ForceResetRotation()
    {
        // Completely reset to zero rotation
        transform.rotation = Quaternion.identity;
        transform.eulerAngles = Vector3.zero;
    }
    
    void CheckForSurfaceImpact()
    {
        // Skip if already stuck or can't stick
        if (isStuckToSurface || !canStick) return;
        
        // In debug mode, allow sticking at any speed
        float currentSpeed = rb.linearVelocity.magnitude;
        bool speedThresholdMet = debugMode || currentSpeed >= minKnockbackSpeed;
        
        if (!speedThresholdMet) return;
        
        // Check if player was hit (knocked back)
        bool wasHit = animator != null && animator.GetBool("playerHit");
        if (!debugMode && !wasHit) return;
        
        // Cast rays in all directions to find nearby surfaces
        RaycastHit2D[] hits = new RaycastHit2D[4];
        hits[0] = Physics2D.Raycast(transform.position, Vector2.right, surfaceCheckDistance, surfaceLayer);
        hits[1] = Physics2D.Raycast(transform.position, Vector2.left, surfaceCheckDistance, surfaceLayer);
        hits[2] = Physics2D.Raycast(transform.position, Vector2.up, surfaceCheckDistance, surfaceLayer);
        hits[3] = Physics2D.Raycast(transform.position, Vector2.down, surfaceCheckDistance, surfaceLayer);
        
        foreach (var hit in hits)
        {
            if (hit.collider != null)
            {
                Vector2 velocityDir = rb.linearVelocity.normalized;
                float angle = Vector2.Angle(-velocityDir, hit.normal);
                
                // Check if velocity is roughly perpendicular to surface
                bool angleValid = debugMode || angle <= knockbackAngleRange;
                
                if (angleValid)
                {
                    StickToSurface(hit.normal, hit.point);
                    break;
                }
            }
        }
    }
    
    void StickToSurface(Vector2 normal, Vector2 hitPoint)
    {
        isStuckToSurface = true;
        animator.SetBool("isStuck", isStuckToSurface);
        surfaceNormal = normal;
        currentAdhesion = maxAdhesionTime;
        
        // Determine surface type
        float angle = Vector2.SignedAngle(Vector2.up, normal);
        
        if (Mathf.Abs(angle) < 45f)
            currentSurface = SurfaceType.Floor;
        else if (angle > 135f || angle < -135f)
            currentSurface = SurfaceType.Ceiling;
        else if (angle > 0f)
            currentSurface = SurfaceType.LeftWall;
        else
            currentSurface = SurfaceType.RightWall;
        
        // Calculate target rotation to align "down" with surface normal
        float rotationAngle = Mathf.Atan2(-normal.x, normal.y) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(0, 0, rotationAngle);
        transform.rotation = targetRotation;
        
        // Stop velocity and change state
        rb.linearVelocity = Vector2.zero;
        stateHandler.ChangeState(StateHandler.State.Grounded);
        
        // Set animator to grounded on surface
        animator.SetBool("playerGrounded", true);
        animator.SetBool("playerJumping", false);
        animator.SetBool("playerFalling", false);
        animator.SetBool("playerHit", false);
        
        // Disable standard controller while stuck
        if (pillController != null)
            pillController.enabled = false;
        
        Debug.Log($"Stuck to {currentSurface} surface! Adhesion: {currentAdhesion:F1}s");
    }
    
    void HandleSurfaceMovement()
    {
        Vector2 moveInput = pillController.GetInputAction("Move").ReadValue<Vector2>();
        float horizontalInput = moveInput.x;
        
        // Calculate movement direction along the surface
        Vector2 moveDir = Vector2.zero;
        float speedMult = movementSpeedMultiplier;
        
        switch (currentSurface)
        {
            case SurfaceType.Floor:
                moveDir = Vector2.right * horizontalInput;
                break;
                
            case SurfaceType.Ceiling:
                moveDir = Vector2.right * horizontalInput;
                speedMult = ceilingSpeedMultiplier;
                break;
                
            case SurfaceType.LeftWall:
                moveDir = Vector2.up * horizontalInput;
                break;
                
            case SurfaceType.RightWall:
                moveDir = Vector2.down * horizontalInput;
                break;
        }
        
        // Apply movement in FixedUpdate via velocity
        float moveSpeed = pillController.moveSpeed * speedMult;
        rb.linearVelocity = moveDir * moveSpeed;
        
        // Handle flipping
        if (horizontalInput != 0)
        {
            bool shouldFaceRight = horizontalInput > 0;
            bool isFacingRight = transform.localScale.x > 0;
            
            if (shouldFaceRight != isFacingRight)
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
            }
        }
        
        // Update animator
        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;
        animator.SetBool("playerMove", isMoving);
        animator.SetBool("playerIdle", !isMoving);
    }
    
    void ApplySurfacePhysics()
    {
        // Keep rotation locked to surface
        transform.rotation = targetRotation;
        
        // Disable gravity while stuck
        rb.gravityScale = 0f;
        
        // Apply slight force toward surface to maintain contact
        rb.AddForce(-surfaceNormal * 5f, ForceMode2D.Force);
    }
    
    void DepleteAdhesion()
    {
        currentAdhesion -= adhesionDepletionRate * Time.deltaTime;
        
        if (currentAdhesion <= 0f)
        {
            DetachFromSurface(true); // Stunned on failure
        }
    }
    
    void CheckForDropInput()
    {
        Vector2 moveInput = pillController.GetInputAction("Move").ReadValue<Vector2>();
        bool slideInput = moveInput.y < -0.5f && pillController.GetInputAction("Attack").WasPressedThisFrame();
        bool jumpInput = pillController.GetInputAction("Jump").WasPressedThisFrame();
    
        if (slideInput)
        {
            SlideOffSurface();
        }
        else if (jumpInput)
        {
            JumpOffSurface();
        }
    }
    
    void CheckSurfaceAttacks()
    {
        if (pillController.GetInputAction("Attack").WasPressedThisFrame())
        {
            // Cost adhesion for attacking
            currentAdhesion -= attackAdhesionCost;
            
            switch (currentSurface)
            {
                case SurfaceType.LeftWall:
                case SurfaceType.RightWall:
                    PerformWallSlam();
                    break;
                    
                case SurfaceType.Ceiling:
                    StartCoroutine(ChargeCeilingDrop());
                    break;
            }
        }
    }
    
    void PerformWallSlam()
    {
        Debug.Log("Wall Slam Attack!");
        
        // Create a downward shockwave
        Vector2 slamDirection = -transform.up; // Downward relative to surface orientation
        
        // TODO: Create hitbox/shockwave that knocks opponents away
        // This would integrate with your existing combat system
        
        animator.SetTrigger("attackTrigger");
    }
    
    IEnumerator ChargeCeilingDrop()
    {
        Debug.Log("Charging Ceiling Drop...");
        
        float chargeTime = 0f;
        while (chargeTime < ceilingDropChargeTime && Input.GetButton("Fire1"))
        {
            chargeTime += Time.deltaTime;
            yield return null;
        }
        
        if (chargeTime >= ceilingDropChargeTime * 0.3f) // Minimum charge
        {
            Debug.Log("Ceiling Drop!");
            
            // Detach and drop with force
            Vector2 dropDir = -surfaceNormal;
            DetachFromSurface(false);
            ForceResetRotation();
            rb.linearVelocity = dropDir * ceilingDropForce * (chargeTime / ceilingDropChargeTime);
            
            animator.SetTrigger("attackTrigger");
        }
    }
    
    void SlideOffSurface()
    {
        Debug.Log("Sliding off surface!");
        
        Vector2 slideDir = Vector2.zero;
        
        // Slide back to floor
        switch (currentSurface)
        {
            case SurfaceType.LeftWall:
                slideDir = Vector2.right + Vector2.down;
                break;
            case SurfaceType.RightWall:
                slideDir = Vector2.left + Vector2.down;
                break;
            case SurfaceType.Ceiling:
                slideDir = Vector2.down;
                break;
        }
        
        DetachFromSurface(false);
        ForceResetRotation();
        rb.linearVelocity = slideDir.normalized * slideOffForce;
    }
    
    void JumpOffSurface()
    {
        Debug.Log("Jumping off surface!");
        
        // Jump in the direction of the surface normal
        Vector2 jumpDir = surfaceNormal;
        
        DetachFromSurface(false);
        ForceResetRotation();
        rb.linearVelocity = jumpDir * pillController.jumpForce;
        stateHandler.ChangeState(StateHandler.State.Jumping);
        
        animator.SetTrigger("playerJump");
        animator.SetBool("playerJumping", true);
        animator.SetBool("playerGrounded", false);
    }
    
    void DetachFromSurface(bool stunned)
    {
        isStuckToSurface = false;
        animator.SetBool("isStuck", isStuckToSurface);
        currentSurface = SurfaceType.None;
        
        // FORCE reset rotation completely
        ForceResetRotation();
        
        // Re-enable gravity
        rb.gravityScale = 1f;
        
        // Re-enable standard controller
        if (pillController != null)
            pillController.enabled = true;
        
        // Update animator state
        animator.SetBool("playerGrounded", false);
        
        if (stunned)
        {
            Debug.Log("Adhesion depleted! Stunned!");
            StartCoroutine(StunPlayer(stunDurationOnAdhesionFail));
        }
        else
        {
            // If not stunned, set to falling state
            if (stateHandler.CurrentState != StateHandler.State.Dashing && 
                stateHandler.CurrentState != StateHandler.State.Jumping)
            {
                stateHandler.ChangeState(StateHandler.State.Falling);
                animator.SetBool("playerFalling", true);
            }
        }
        
        // Brief cooldown before can stick again
        StartCoroutine(StickCooldown(0.5f));
    }
    
    IEnumerator StunPlayer(float duration)
    {
        stateHandler.ChangeState(StateHandler.State.Stunned);
        animator.SetBool("playerHit", true);
        
        yield return new WaitForSeconds(duration);
        
        animator.SetBool("playerHit", false);
        stateHandler.ChangeState(StateHandler.State.Falling);
    }
    
    IEnumerator StickCooldown(float duration)
    {
        canStick = false;
        yield return new WaitForSeconds(duration);
        canStick = true;
    }
    
    void OnDrawGizmosSelected()
    {
        if (!isStuckToSurface) return;
        
        // Draw surface normal
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, surfaceNormal * 2f);
        
        // Draw adhesion bar
        Gizmos.color = Color.Lerp(Color.red, Color.green, AdhesionPercent);
        Vector3 barStart = transform.position + Vector3.up * 2f;
        Vector3 barEnd = barStart + Vector3.right * (AdhesionPercent * 2f);
        Gizmos.DrawLine(barStart, barEnd);
    }
}
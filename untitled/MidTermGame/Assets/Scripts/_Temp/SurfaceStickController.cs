using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PillController))]
public class SurfaceStickController : MonoBehaviour
{
    [Header("Debug Mode")]
    public bool debugMode = false;
    
    [Header("Knockback Threshold")]
    public float minKnockbackSpeed = 8f;
    public float knockbackAngleRange = 45f;
    
    [Header("Surface Detection")]
    public float surfaceCheckDistance = 0.3f;
    public LayerMask surfaceLayer;
    
    [Header("Adhesion Settings")]
    public float maxAdhesionTime = 8f;
    public float adhesionDepletionRate = 1f;
    public float movementSpeedMultiplier = 0.75f;
    public float ceilingSpeedMultiplier = 0.5f;
    
    [Header("Surface Attack Costs")]
    public float attackAdhesionCost = 1.5f;
    
    [Header("Drop/Slide Settings")]
    public float slideOffForce = 5f;
    public float stunDurationOnAdhesionFail = 0.5f;
    
    [Header("Surface Attacks")]
    public float wallSlamForce = 10f;
    public float ceilingDropChargeTime = 1f;
    public float ceilingDropForce = 15f;
    
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
    
    private Quaternion targetRotation = Quaternion.identity;
    private Vector2 tangentDirection = Vector2.right; // Direction along surface
    
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
        if (isStuckToSurface)
        {
            if (ShouldDetachFromState())
            {
                DetachFromSurface(false);
                return;
            }
            
            DepleteAdhesion();
            CheckForDropInput();
            CheckSurfaceAttacks();
        }
        else
        {
            CheckForSurfaceImpact();
        }
        
        if (debugMode && isStuckToSurface)
        {
            Debug.DrawRay(transform.position, surfaceNormal * 2f, Color.green);
            Debug.DrawRay(transform.position, tangentDirection * 1.5f, Color.red);
        }
    }
    
    bool ShouldDetachFromState()
    {
        var state = stateHandler.CurrentState;
        
        if (state == StateHandler.State.Dashing ||
            state == StateHandler.State.Sliding ||
            state == StateHandler.State.Stunned ||
            state == StateHandler.State.GettingHit ||
            animator.GetBool("playerAttacking"))
        {
            return true;
        }
        
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
            HandleSurfaceMovement();
            ApplySurfacePhysics();
        }
        else
        {
            ForceResetRotation();
        }
    }
    
    void ForceResetRotation()
    {
        transform.rotation = Quaternion.identity;
        transform.eulerAngles = Vector3.zero;
    }
    
    void CheckForSurfaceImpact()
    {
        if (isStuckToSurface || !canStick) return;
        
        float currentSpeed = rb.linearVelocity.magnitude;
        bool speedThresholdMet = debugMode || currentSpeed >= minKnockbackSpeed;
        
        if (!speedThresholdMet) return;
        
        bool wasHit = animator != null && animator.GetBool("playerHit");
        if (!debugMode && !wasHit) return;
        
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
        
        float angle = Vector2.SignedAngle(Vector2.up, normal);
        
        if (Mathf.Abs(angle) < 45f)
            currentSurface = SurfaceType.Floor;
        else if (angle > 135f || angle < -135f)
            currentSurface = SurfaceType.Ceiling;
        else if (angle > 0f)
            currentSurface = SurfaceType.LeftWall;
        else
            currentSurface = SurfaceType.RightWall;
        
        // Calculate tangent direction (perpendicular to normal, pointing "right" along surface)
        tangentDirection = new Vector2(-normal.y, normal.x);
        
        // Rotate player to align with surface
        float rotationAngle = Mathf.Atan2(-normal.x, normal.y) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(0, 0, rotationAngle);
        transform.rotation = targetRotation;
        
        rb.linearVelocity = Vector2.zero;
        stateHandler.ChangeState(StateHandler.State.Grounded);
        
        animator.SetBool("playerGrounded", true);
        animator.SetBool("playerJumping", false);
        animator.SetBool("playerFalling", false);
        animator.SetBool("playerHit", false);
        
        Debug.Log($"Stuck to {currentSurface} surface! Tangent: {tangentDirection}");
    }
    
    void HandleSurfaceMovement()
    {
        Vector2 moveInput = pillController.GetInputAction("Move").ReadValue<Vector2>();
        float horizontalInput = moveInput.x;
        
        // Get the current facing direction from scale
        float facingDir = transform.localScale.x > 0 ? 1f : -1f;
        
        // Move along tangent in the direction the player is facing
        Vector2 moveVelocity = tangentDirection * horizontalInput * facingDir * pillController.moveSpeed;
        
        float speedMult = (currentSurface == SurfaceType.Ceiling) ? ceilingSpeedMultiplier : movementSpeedMultiplier;
        moveVelocity *= speedMult;
        
        // Set velocity directly - no forces, just pure tangent movement
        rb.linearVelocity = moveVelocity;
        
        // Handle flipping based on input direction
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
        
        bool isMoving = Mathf.Abs(horizontalInput) > 0.1f;
        animator.SetBool("playerMove", isMoving);
        animator.SetBool("playerIdle", !isMoving);
    }
    
    void ApplySurfacePhysics()
    {
        // Lock rotation to surface
        transform.rotation = targetRotation;
        
        // Disable gravity
        rb.gravityScale = 0f;
        
        // NO FORCES - we handle movement entirely through velocity in HandleSurfaceMovement
    }
    
    void DepleteAdhesion()
    {
        currentAdhesion -= adhesionDepletionRate * Time.deltaTime;
        
        if (currentAdhesion <= 0f)
        {
            DetachFromSurface(true);
        }
    }
    
    void CheckForDropInput()
    {
        Vector2 verticalInput = pillController.GetInputAction("Move").ReadValue<Vector2>();
        bool attackPressed = pillController.GetInputAction("Attack").WasPressedThisFrame();
        bool slideInput = verticalInput.y < -0.5f && attackPressed;
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
        animator.SetTrigger("attackTrigger");
    }
    
    IEnumerator ChargeCeilingDrop()
    {
        Debug.Log("Charging Ceiling Drop...");
        
        var attackAction = pillController.GetInputAction("Attack");
        float chargeTime = 0f;
        
        while (chargeTime < ceilingDropChargeTime && attackAction.IsPressed())
        {
            chargeTime += Time.deltaTime;
            yield return null;
        }
        
        if (chargeTime >= ceilingDropChargeTime * 0.3f)
        {
            Debug.Log("Ceiling Drop!");
            
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
        
        // Slide in current facing direction + away from surface
        float facingDir = transform.localScale.x > 0 ? 1f : -1f;
        Vector2 slideDir = tangentDirection * facingDir + surfaceNormal * 0.3f;
        
        DetachFromSurface(false);
        ForceResetRotation();
        rb.linearVelocity = slideDir.normalized * slideOffForce;
    }
    
    void JumpOffSurface()
    {
        Debug.Log("Jumping off surface!");
        
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
        
        ForceResetRotation();
        
        rb.gravityScale = 1f;
        
        animator.SetBool("playerGrounded", false);
        
        if (stunned)
        {
            Debug.Log("Adhesion depleted! Stunned!");
            StartCoroutine(StunPlayer(stunDurationOnAdhesionFail));
        }
        else
        {
            if (stateHandler.CurrentState != StateHandler.State.Dashing && 
                stateHandler.CurrentState != StateHandler.State.Jumping)
            {
                stateHandler.ChangeState(StateHandler.State.Falling);
                animator.SetBool("playerFalling", true);
            }
        }
        
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
        
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, surfaceNormal * 2f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, tangentDirection * 1.5f);
        
        Gizmos.color = Color.Lerp(Color.red, Color.green, AdhesionPercent);
        Vector3 barStart = transform.position + Vector3.up * 2f;
        Vector3 barEnd = barStart + Vector3.right * (AdhesionPercent * 2f);
        Gizmos.DrawLine(barStart, barEnd);
    }
}
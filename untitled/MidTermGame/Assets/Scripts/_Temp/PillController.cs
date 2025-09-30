using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(StateHandler))]
public class PillController : MonoBehaviour
{
    public bool isPlayer1 = true;
    private InputSystem_Actions controls;
    private InputSystem_Actions.Player1Actions player1Controls;
    private InputSystem_Actions.Player2Actions player2Controls;
    
   [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public float stickForce = 10f;

    [Header("BPM Movement Modifier")]
    [Tooltip("Reference to the HeartMonitor component")]
    public HeartMonitor heartMonitor;
    [Tooltip("Minimum speed multiplier at minBPM (e.g., 0.5 = 50% speed)")]
    [Range(0f, 1f)]
    public float minSpeedMultiplier = 0.5f;
    [Tooltip("Maximum speed multiplier at maxBPM (e.g., 1.5 = 150% speed)")]
    [Range(1f, 2f)]
    public float maxSpeedMultiplier = 1.5f;

    [Tooltip("Minimum jump multiplier at minBPM (e.g., 0.5 = 50% speed)")]
    [Range(0f, 1f)]
    public float minJumpMultiplier = 0.5f;
    [Tooltip("Maximum jump multiplier at maxBPM (e.g., 1.5 = 150% speed)")]
    [Range(1f, 2f)]
    public float maxJumpMultiplier = 1.5f;

    [Header("Ground Check")]
    public float groundRayDistance = 0.2f;
    public LayerMask groundLayer;

    [Header("Surface Detection")]
    public float rayDistance = 0.4f;
    public LayerMask groundishLayer;

    [Header("Dash")]
    public CombatHandler combatHandlerDash; 

    [Header("Attack")]
    public CombatHandler combatHandlerAttack;

    Rigidbody2D rb;
    StateHandler stateHandler;
    Vector2 surfaceNormal = Vector2.up;
    bool isAttached = false;
    float horizontalInput;
    bool isGrounded;
    bool facingRight = true;
    private Animator playerAnimator;
    
    // Store the original scale to maintain consistent flipping
    private Vector3 originalScale;
    private float baseScaleX;

    void Awake()
    {
        combatHandlerAttack = GetComponent<CombatHandler>();

        controls = new InputSystem_Actions();
        
        if (isPlayer1)
        {
            player1Controls = controls.Player1;
            player1Controls.Enable();
        }
        else
        {
            player2Controls = controls.Player2;
            player2Controls.Enable();
        }
        
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        stateHandler = GetComponent<StateHandler>();
        stateHandler.ChangeState(StateHandler.State.Grounded);

        playerAnimator = GetComponent<Animator>();

        PhysicsMaterial2D noFriction = new PhysicsMaterial2D();
        noFriction.friction = 0f;
        noFriction.bounciness = 0f;
        rb.sharedMaterial = noFriction;
        
        // Store the original scale values
        originalScale = transform.localScale;
        baseScaleX = Mathf.Abs(originalScale.x);
        
        // Ensure we start facing right with positive scale
        if (originalScale.x < 0)
        {
            facingRight = false;
        }

        // Auto-find HeartMonitor if not assigned
        if (heartMonitor == null)
        {
            heartMonitor = GetComponent<HeartMonitor>();
            if (heartMonitor == null)
            {
                Debug.LogWarning("No HeartMonitor found. BPM-based movement will be disabled.", this);
            }
        }
    }

    void Update()
    {
        // Get horizontal input based on player
        Vector2 moveInput = isPlayer1 
            ? player1Controls.Move.ReadValue<Vector2>() 
            : player2Controls.Move.ReadValue<Vector2>();
        horizontalInput = moveInput.x;

        isGrounded = Physics2D.Raycast(transform.position, -transform.up, groundRayDistance, groundLayer).collider != null;
        playerAnimator.SetBool("playerGrounded", isGrounded);

        float bpmJmpMultiplier = GetBPMJumpMultiplier();
        float localJumpForce = jumpForce * bpmJmpMultiplier;
        Debug.Log(bpmJmpMultiplier);

        var state = stateHandler.CurrentState;

        // Jump input - use appropriate player's jump action
        var jumpAction = isPlayer1 ? player1Controls.Jump : player2Controls.Jump;

        if (state == StateHandler.State.Grounded && isGrounded && jumpAction.WasPressedThisFrame())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, localJumpForce * 1.8f);
            stateHandler.ChangeState(StateHandler.State.Jumping);
        }

        if (state == StateHandler.State.Jumping && rb.linearVelocity.y > 0)
        {
            playerAnimator.SetTrigger("playerJump");
            playerAnimator.SetBool("playerJumping", true);
            playerAnimator.SetBool("playerGrounded", false);
            rb.linearVelocity += Vector2.down * 25f * Time.deltaTime;
        }

        if (state == StateHandler.State.Jumping && jumpAction.WasReleasedThisFrame() && rb.linearVelocity.y > 0)
        {
            playerAnimator.SetBool("playerJumping", false);
            playerAnimator.SetBool("playerFalling", false);
            
            // ensure minimum jump height of 50% when releasing jump early
            float minJumpVelocity = localJumpForce * 1.8f * 0.5f;
            float newYVelocity = Mathf.Max(rb.linearVelocity.y * 0.3f, minJumpVelocity);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, newYVelocity);
            
            stateHandler.ChangeState(StateHandler.State.Falling);
        }

        if (state == StateHandler.State.Falling || playerAnimator.GetBool("playerFalling"))
        {
            playerAnimator.SetBool("playerFalling", true);
            playerAnimator.SetBool("playerGrounded", false);
            rb.linearVelocity += Vector2.down * 20f * Time.deltaTime;
        }

        if (state == StateHandler.State.Grounded)
        { 
            playerAnimator.SetBool("playerGrounded", true);
            playerAnimator.SetBool("playerFalling", false);
            if (!isGrounded)
                stateHandler.ChangeState(StateHandler.State.Falling);
        }
        else if (state == StateHandler.State.Jumping)
        {
            if (rb.linearVelocity.y < 0f)
                stateHandler.ChangeState(StateHandler.State.Falling);
        }
        else if (state == StateHandler.State.Falling)
        {
            if (isGrounded)
                stateHandler.ChangeState(StateHandler.State.Grounded);
        }

        if (!playerAnimator.GetBool("playerAttacking") && !playerAnimator.GetBool("playerHit"))
        {
            if (horizontalInput > 0 && !facingRight)
                Flip();
            else if (horizontalInput < 0 && facingRight)
                Flip();
        }

        bool playerMoving = Mathf.Abs(horizontalInput) > 0f;
        playerAnimator.SetBool("playerMove", playerMoving);
        playerAnimator.SetBool("playerIdle", !playerMoving);
        
        // REMEMBER TO REMOVE THIS FOR PVP
        playerAnimator.SetBool("canAttack", true);
    }

    void FixedUpdate() {
        // keep upright while grounded
        if (stateHandler.CurrentState == StateHandler.State.Grounded)
            transform.rotation = Quaternion.identity;

        // apply horizontal movement unless you're in a blocking state
        var s = stateHandler.CurrentState;
        if (
            s != StateHandler.State.Stunned
            && s != StateHandler.State.GettingHit
            && s != StateHandler.State.Dashing
            && s != StateHandler.State.Sliding
            && s != StateHandler.State.Parrying
        )
        {
            float movementMultiplier = 1f;
            float effectiveInput = horizontalInput;
        
            // reduce movement speed while attacking and prevent backwards movement
            if (playerAnimator.GetBool("playerAttacking"))
            {
                movementMultiplier = 0.25f;
                if (playerAnimator.GetBool("isHeavy")) movementMultiplier = 0;
                
                // prevent moving backwards while attacking
                if ((facingRight && effectiveInput < 0) || (!facingRight && effectiveInput > 0))
                {
                    effectiveInput = 0f;
                }
            }

            // Calculate BPM-based speed multiplier
            float bpmSpeedMultiplier = GetBPMSpeedMultiplier();
        
            rb.linearVelocity = new Vector2(effectiveInput * moveSpeed * movementMultiplier * bpmSpeedMultiplier, rb.linearVelocity.y);
        }

        // slow down vertical descent while attacking
        if (playerAnimator.GetBool("playerAttacking"))
        {
            // only slow descent (negative y velocity)
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.9f);
            }
        }
        
        // detect ceiling collision and cancel upward velocity
        if (rb.linearVelocity.y > 0)
        {
            RaycastHit2D ceilingHit = Physics2D.Raycast(transform.position, transform.up, groundRayDistance, groundLayer);
            if (ceilingHit.collider != null)
            {
                playerAnimator.SetBool("playerFalling", true);
            }
        }
    }
    
    public InputAction GetInputAction(string actionName)
    {
        var controlsObject = isPlayer1 ? (object)player1Controls : (object)player2Controls;
    
        PropertyInfo property = controlsObject.GetType().GetProperty(actionName);
    
        if (property != null && property.PropertyType == typeof(InputAction))
        {
            return property.GetValue(controlsObject) as InputAction;
        }
    
        Debug.LogWarning($"InputAction '{actionName}' not found!");
        return null;
    }

    /// <summary>
    /// Calculates the speed multiplier based on current BPM.
    /// Returns a value between minSpeedMultiplier and maxSpeedMultiplier.
    /// </summary>
    private float GetBPMSpeedMultiplier()
    {
        if (heartMonitor == null)
            return 1f; // No modifier if no heart monitor

        float currentBPM = heartMonitor.BPM;
        float minBPM = heartMonitor.minBPM;
        float maxBPM = heartMonitor.maxBPM;

        // Calculate normalized BPM (0 to 1)
        float normalizedBPM = Mathf.InverseLerp(minBPM, maxBPM, currentBPM);

        // Map normalized BPM to speed multiplier range
        float speedMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, normalizedBPM);

        return speedMultiplier;
    }

    private float GetBPMJumpMultiplier()
    {
        if (heartMonitor == null)
            return 1f; // No modifier if no heart monitor

        float currentBPM = heartMonitor.BPM;
        float minBPM = heartMonitor.minBPM;
        float maxBPM = heartMonitor.maxBPM;

        // Calculate normalized BPM (0 to 1)
        float normalizedBPM = Mathf.InverseLerp(minBPM, maxBPM, currentBPM);

        // Map normalized BPM to jump multiplier range
        float jumpMultiplier = Mathf.Lerp(minJumpMultiplier, maxJumpMultiplier, normalizedBPM);

        return jumpMultiplier;
    }
    
    void Flip()
    {
        facingRight = !facingRight;
        
        // Use the base scale values and apply direction
        Vector3 newScale = originalScale;
        newScale.x = facingRight ? baseScaleX : -baseScaleX;
        transform.localScale = newScale;
    }
    
    // Call this from SurfaceStickController when detaching to ensure scale is correct
    public void ResetScale()
    {
        Vector3 currentScale = transform.localScale;
        currentScale.y = Mathf.Abs(originalScale.y);
        currentScale.z = Mathf.Abs(originalScale.z);
        currentScale.x = facingRight ? baseScaleX : -baseScaleX;
        transform.localScale = currentScale;
    }

    void OnEnable() => controls.Enable();

    void OnDisable()
    {
        if (isPlayer1)
            player1Controls.Disable();
        else
            player2Controls.Disable();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var start = transform.position;
        var end = start - transform.up * groundRayDistance;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.02f);
    }
}
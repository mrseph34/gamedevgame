using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(StateHandler))]
public class PillController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public float stickForce = 10f;


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

    InputSystem_Actions controls;

    Rigidbody2D rb;
    StateHandler stateHandler;
    Vector2 surfaceNormal = Vector2.up;
    bool isAttached = false;
    float horizontalInput;
    bool isGrounded;
    bool facingRight = true;
    private Animator playerAnimator;

    void Awake()
    {
        combatHandlerAttack = GetComponent<CombatHandler>();

        controls = new InputSystem_Actions();
        
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        stateHandler = GetComponent<StateHandler>();
        stateHandler.ChangeState(StateHandler.State.Grounded);

        playerAnimator = GetComponent<Animator>();


    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        isGrounded = Physics2D.Raycast(transform.position, -transform.up, groundRayDistance, groundLayer).collider != null;
        playerAnimator.SetBool("playerGrounded", isGrounded);
        
        var state = stateHandler.CurrentState;

        if (state == StateHandler.State.Grounded && isGrounded && Input.GetButtonDown("Jump"))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 1.8f);
            stateHandler.ChangeState(StateHandler.State.Jumping);
        }

        if (state == StateHandler.State.Jumping && rb.linearVelocity.y > 0)
        { ;
            playerAnimator.SetTrigger("playerJump");
            playerAnimator.SetBool("playerJumping", true);
            playerAnimator.SetBool("playerGrounded", false);
            rb.linearVelocity += Vector2.down * 25f * Time.deltaTime;
        }

        if (state == StateHandler.State.Jumping && Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            playerAnimator.SetBool("playerJumping", false);
            playerAnimator.SetBool("playerFalling", false);
            
            // ensure minimum jump height of 50% when releasing jump early
            float minJumpVelocity = jumpForce * 1.8f * 0.5f;
            float newYVelocity = Mathf.Max(rb.linearVelocity.y * 0.3f, minJumpVelocity);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, newYVelocity);
            
            stateHandler.ChangeState(StateHandler.State.Falling);
        }

        if (state == StateHandler.State.Falling)
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
        
            rb.linearVelocity = new Vector2(effectiveInput * moveSpeed * movementMultiplier, rb.linearVelocity.y);
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
    }
    
    void Flip()
    {
        facingRight = !facingRight;
        var ls = transform.localScale;
        ls.x *= -1;
        transform.localScale = ls;
    }

    void OnEnable() => controls.Enable();

    void OnDisable() => controls.Player.Disable();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        var start = transform.position;
        var end = start - transform.up * groundRayDistance;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.02f);
    }
}

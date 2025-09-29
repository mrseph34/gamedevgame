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
    public DashMoveModule dashModule; 
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
        controls.Player.Dash.performed += ctx => DoDash();
        controls.Player.Attack.performed += ctx => DoAttack();


        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        stateHandler = GetComponent<StateHandler>();
        stateHandler.ChangeState(StateHandler.State.Grounded);

        playerAnimator = GetComponent<Animator>();


    }

    void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 1) Ground/surface check
        isGrounded =
            Physics2D.Raycast(
                transform.position,
                -transform.up,
                groundRayDistance,
                groundLayer
            ).collider != null;

        //RaycastHit2D hit = Physics2D.Raycast(transform.position, -transform.up, rayDistance, groundLayer);

        


        var state = stateHandler.CurrentState;

        // 2) Jump input
        if (state == StateHandler.State.Grounded && isGrounded && Input.GetButtonDown("Jump"))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            stateHandler.ChangeState(StateHandler.State.Jumping);
        }

        // 3) State transitions
        if (state == StateHandler.State.Grounded)
        {
            // walked off a ledge?
            if (!isGrounded)
                stateHandler.ChangeState(StateHandler.State.Falling);
        }
        else if (state == StateHandler.State.Jumping)
        {
            // reached apex, now falling?
            if (rb.linearVelocity.y < 0f)
                stateHandler.ChangeState(StateHandler.State.Falling);
        }
        else if (state == StateHandler.State.Falling)
        {
            // landed?
            if (isGrounded)
                stateHandler.ChangeState(StateHandler.State.Grounded);
        }

        // 4) Flip
        if (horizontalInput > 0 && !facingRight)
            Flip();
        else if (horizontalInput < 0 && facingRight)
            Flip();

        bool playerInput = Mathf.Abs(horizontalInput) > 0f;
        bool playerMoving = playerInput;
        if (playerMoving)
        {
            playerAnimator.SetBool("playerMove", true);
        }
        else
        {
            playerAnimator.SetBool("playerMove", false);
            playerAnimator.SetBool("playerIdle", true);
        }
    }

    void FixedUpdate()
    {
        // keep upright while grounded
        if (stateHandler.CurrentState == StateHandler.State.Grounded)
            transform.rotation = Quaternion.identity;

        // apply horizontal movement unless you’re in a blocking state
        var s = stateHandler.CurrentState;
        if (
            s != StateHandler.State.Stunned
            && s != StateHandler.State.GettingHit
            && s != StateHandler.State.Dashing
            && s != StateHandler.State.Sliding
            && s != StateHandler.State.Parrying
        )
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
    }

    void DoDash()
    {
        // Prevent double dash, falling dash, etc. if you like
        if (dashModule != null && combatHandlerDash != null)
        {
            StartCoroutine(dashModule.ExecuteDash(combatHandlerDash));
        }
    }

    void DoAttack()
    {
        if (combatHandlerAttack == null) return;

        foreach (var atk in combatHandlerAttack.attacks)
        {
            atk.ExecuteAttack(combatHandlerAttack);
            Debug.Log("ATTACK");
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

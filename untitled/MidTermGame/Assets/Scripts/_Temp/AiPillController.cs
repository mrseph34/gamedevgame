using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(StateHandler))]
[RequireComponent(typeof(StunHandler))]
public class AiPillController : MonoBehaviour
{
    [Header("Timings")]
    public float recoveryDelay = 3f; // stun length and also time before walking home

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float homeX = 0f;

    [Header("Fall Detection")]
    public float fallAngleThreshold = 30f;

    Rigidbody2D rb;
    StateHandler stateHandler;
    StunHandler stunHandler;

    // internal
    const float homeThreshold = 0.1f;
    float awayTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stateHandler = GetComponent<StateHandler>();
        stunHandler = GetComponent<StunHandler>();

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        stateHandler.ChangeState(StateHandler.State.Grounded);
        stateHandler.OnStateChanged += HandleStateChanged;
    }

    void OnDestroy()
    {
        stateHandler.OnStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(StateHandler.State oldS, StateHandler.State newS)
    {
        if (newS == StateHandler.State.Stunned)
        {
            // allow physics tipping
            rb.constraints = RigidbodyConstraints2D.None;
            // reset away timer while stunned
            awayTimer = 0f;
        }
        else if (oldS == StateHandler.State.Stunned && newS == StateHandler.State.Grounded)
        {
            // snap upright
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            transform.rotation = Quaternion.identity;
            // start counting away-time again
            awayTimer = 0f;
        }
    }

    void Update()
    {
        var st = stateHandler.CurrentState;
        float xDist = Mathf.Abs(transform.position.x - homeX);

        switch (st)
        {
            case StateHandler.State.Grounded:
                // 1) knockdown check by tilt
                float z = transform.eulerAngles.z;
                if (z > 180f)
                    z -= 360f;
                if (Mathf.Abs(z) > fallAngleThreshold)
                {
                    stunHandler.ApplyStun(recoveryDelay);
                    break;
                }

                // 2) home-return timer
                if (xDist > homeThreshold)
                {
                    awayTimer += Time.deltaTime;
                    if (awayTimer >= recoveryDelay)
                    {
                        stateHandler.ChangeState(StateHandler.State.Moving);
                        awayTimer = 0f;
                    }
                }
                else
                {
                    awayTimer = 0f;
                }
                break;

            case StateHandler.State.Moving:
                // walk toward home
                float dir = Mathf.Sign(homeX - transform.position.x);
                rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

                if (Mathf.Abs(transform.position.x - homeX) < homeThreshold)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    stateHandler.ChangeState(StateHandler.State.Grounded);
                }
                break;

            case StateHandler.State.Stunned:
                // do nothing; StunHandler will recover after recoveryDelay
                break;
            // you can add Falling, Jumping, etc.... LATEER
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(StateHandler))]
public class StunHandler : MonoBehaviour
{
    StateHandler stateHandler;
    private Animator animator;
    float stunTimer = 0f;

    public bool IsStunned => stunTimer > 0f;

    void Awake()
    {
        stateHandler = GetComponent<StateHandler>();
        animator = stateHandler.GetComponent<Animator>();
    }

    void Update()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                stunTimer = 0f;
                // *** STUN ENDED - Do stuff here when character is no longer stunned ***
                OnStunEnded();
                
                // Return to grounded or moving:
                stateHandler.ChangeState(StateHandler.State.Grounded);
            }
        }
    }

    // Call this to stun the character for `duration` seconds.
    // If already stunned, newest duration overwrites the old one
    public void ApplyStun(float duration)
    {
        animator.SetBool("canAttack", false);
        stunTimer = duration;
        stateHandler.ChangeState(StateHandler.State.Stunned);
    }

    // Called when stun duration expires
    void OnStunEnded()
    {
        animator.SetBool("canAttack", true);
    }
}
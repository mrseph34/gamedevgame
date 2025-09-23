using UnityEngine;

[RequireComponent(typeof(StateHandler))]
public class StunHandler : MonoBehaviour
{
    StateHandler stateHandler;
    float stunTimer = 0f;

    public bool IsStunned => stunTimer > 0f;

    void Awake()
    {
        stateHandler = GetComponent<StateHandler>();
    }

    void Update()
    {
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                stunTimer = 0f;
                // Return to grounded or moving:
                stateHandler.ChangeState(StateHandler.State.Grounded);
            }
        }
    }

    // Call this to stun the character for `duration` seconds.
    // If already stunned, newest duration overwrites the old one
    public void ApplyStun(float duration)
    {
        stunTimer = duration;
        stateHandler.ChangeState(StateHandler.State.Stunned);
    }
}
using System;
using UnityEngine;

[RequireComponent(typeof(HeartMonitor))]
public class StateHandler : MonoBehaviour
{
    public enum State
    {
        Moving,
        Jumping,
        Stunned,
        Grounded,
        Dashing,
        Sliding,
        Falling,
        Parrying,
        GettingHit
    }

    public State CurrentState { get; private set; } = State.Grounded;

    KnockbackHandler kbHandler;
    StunHandler stunHandler;
    HeartMonitor heartMonitor;

    public event Action<State, State> OnStateChanged;

    void Awake()
    {
        kbHandler = GetComponent<KnockbackHandler>();
        stunHandler = GetComponent<StunHandler>();
        heartMonitor = GetComponent<HeartMonitor>();
    }

    public void ChangeState(State newState)
    {
        if (newState == CurrentState)
            return;
        var old = CurrentState;
        CurrentState = newState;
        OnStateChanged?.Invoke(old, newState);
    }

    public void ReceiveHit(Vector2 knockback, float stunDuration, int damage)
    {
        // Knockback
        if (kbHandler != null && knockback.magnitude > 0f)
            kbHandler.ApplyKnockback(knockback);

        // Stun
        if (stunHandler != null && stunDuration > 0f)
            stunHandler.ApplyStun(stunDuration);

        // Damage the heart
        if (heartMonitor != null)
            heartMonitor.TakeDamage(damage);

        // Enter GettingHit state
        ChangeState(State.GettingHit);
    }
}
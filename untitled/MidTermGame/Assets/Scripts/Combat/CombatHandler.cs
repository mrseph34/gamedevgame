using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(StateHandler))]
[RequireComponent(typeof(KnockbackHandler))]
[RequireComponent(typeof(StunHandler))]
[RequireComponent(typeof(Collider2D))]
public class CombatHandler : MonoBehaviour
{
    [Header("Attack Modules")]
    public List<AttackModule> attackModules;

    // exposed so modules & hitbox can use them
    [HideInInspector]
    public Collider2D SelfCollider;
    public StateHandler StateHandler { get; private set; }
    public KnockbackHandler KnockbackHandler { get; private set; }
    public StunHandler StunHandler { get; private set; }
    public Animator playerAnimator;

    void Awake()
    {
        SelfCollider = GetComponent<Collider2D>();
        StateHandler = GetComponent<StateHandler>();
        KnockbackHandler = GetComponent<KnockbackHandler>();
        StunHandler = GetComponent<StunHandler>();
    }

    void Update()
    {
        // Let each module poll its key, handle cooldowns/stun-locks, and fire
        foreach (var mod in attackModules)
            if (mod != null)
                mod.HandleInput(this);
    }
}
using UnityEngine;
using System.Collections;
using Modules.Combat;

public class CombatHandler : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    
    [HideInInspector]
    public Collider2D SelfCollider;
    public StateHandler StateHandler { get; private set; }
    public KnockbackHandler KnockbackHandler { get; private set; }
    public StunHandler StunHandler { get; private set; }
    
    private AttackModule currentAttackModule;
    private bool isAttacking = false;
    
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (SelfCollider == null)
            SelfCollider = GetComponent<Collider2D>();
        
        StateHandler = GetComponent<StateHandler>();
        KnockbackHandler = GetComponent<KnockbackHandler>();
        StunHandler = GetComponent<StunHandler>();
    }
    
    public void StartAttack(AttackModule attackModule)
    {
        if (attackModule != null && !isAttacking)
        {
            isAttacking = true;
            currentAttackModule = attackModule;
            
            if (string.IsNullOrEmpty(attackModule.animationTrigger))
            {
                StartCoroutine(attackModule.ExecuteAttack(this));
            }
            else
            {
                animator.SetTrigger(attackModule.animationTrigger);
                StartCoroutine(attackModule.ExecuteAttack(this));
            }
        }
    }
    
    public void TriggerAttack()
    {
        if (currentAttackModule != null)
        {
            StartCoroutine(currentAttackModule.ExecuteAttack(this));
        }
    }
    
    public void TriggerHitbox()
    {
        if (currentAttackModule != null)
        {
            StartCoroutine(currentAttackModule.ExecuteHitbox(this));
        }
    }
    
    public void ClearCurrentAttack()
    {
        currentAttackModule = null;
        isAttacking = false;
    }
    
    public void CanContinueCombo()
    {
        if (currentAttackModule is ComboModule comboModule)
        {
            comboModule.SetCanContinue(true);
        }
    }
    
    public void CannotContinueCombo()
    {
        if (currentAttackModule is ComboModule comboModule)
        {
            comboModule.SetCanContinue(false);
        }
    }
    
    public void PlaySound(string soundName)
    {
        // sound playing logic
    }
    
    public void SpawnEffect(string effectName)
    {
        // effect spawning logic?
    }
}
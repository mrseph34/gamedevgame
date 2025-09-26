using UnityEngine;

public class CombatHandler : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    
    // exposed so modules & hitbox can use them
    [HideInInspector]
    public Collider2D SelfCollider;
    public StateHandler StateHandler { get; private set; }
    public KnockbackHandler KnockbackHandler { get; private set; }
    public StunHandler StunHandler { get; private set; }
    
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
    
    // Method to start an attack with animation trigger (called from input system)
    public void StartAttack(AttackModule attackModule)
    {
        if (attackModule != null && !string.IsNullOrEmpty(attackModule.animationTrigger))
        {
            // Trigger the animation
            animator.SetTrigger(attackModule.animationTrigger);
        }
    }
    
    // Animation Event Method - This gets called from animation events
    // You can pass the AttackModule directly as an Object parameter in animation events
    public void TriggerAttack(AttackModule attackModule)
    {
        if (attackModule != null)
        {
            StartCoroutine(attackModule.ExecuteAttack(this));
        }
        else
        {
            Debug.LogWarning("Attack module is null!");
        }
    }
    
    // Utility methods for animation events
    public void PlaySound(string soundName)
    {
        // Add sound playing logic here
        Debug.Log($"Playing sound: {soundName}");
    }
    
    public void SpawnEffect(string effectName)
    {
        // Add effect spawning logic here
        Debug.Log($"Spawning effect: {effectName}");
    }
}
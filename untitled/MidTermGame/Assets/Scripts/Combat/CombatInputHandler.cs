using Modules.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatInputHandler : MonoBehaviour
{
    [SerializeField] private CombatHandler combatHandler;
    
    [Header("Attack Setup")]
    public InputActionReference[] inputActions;
    public AttackModule[] attackModules;
    
    // Store cloned versions of the attack modules
    private AttackModule[] clonedAttackModules;
    
    private void Awake()
    {
        // Create clones of all attack modules to prevent shared state between players
        if (attackModules != null && attackModules.Length > 0)
        {
            clonedAttackModules = new AttackModule[attackModules.Length];
            
            for (int i = 0; i < attackModules.Length; i++)
            {
                if (attackModules[i] != null)
                {
                    // Use Instantiate to create a clone of the ScriptableObject
                    clonedAttackModules[i] = Instantiate(attackModules[i]);
                }
            }
            
            // Debug.Log($"[{gameObject.name}] Cloned {clonedAttackModules.Length} attack modules");
        }
    }
    
    private void OnEnable()
    {
        // Enable all input actions
        foreach (var actionRef in inputActions)
        {
            if (actionRef != null)
                actionRef.action.Enable();
        }
    }
    
    private void OnDisable()
    {
        // Disable all input actions
        foreach (var actionRef in inputActions)
        {
            if (actionRef != null)
                actionRef.action.Disable();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up cloned ScriptableObjects to prevent memory leaks
        if (clonedAttackModules != null)
        {
            foreach (var module in clonedAttackModules)
            {
                if (module != null)
                {
                    Destroy(module);
                }
            }
        }
    }
    
    private void Update()
    {
        Animator animator = this.GetComponent<Animator>();
        if (animator.GetBool("isDead")) return;
            // Use cloned modules instead of original modules
            int maxIndex = Mathf.Min(inputActions.Length, clonedAttackModules.Length);
        
        for (int i = 0; i < maxIndex; i++)
        {
            if (inputActions[i] != null && 
                inputActions[i].action.WasPressedThisFrame() && 
                clonedAttackModules[i] != null)
            {
                combatHandler.StartAttack(clonedAttackModules[i]);
            }
        }
    }
    
    // Helper method to check if a specific attack module's input was pressed this frame
    public bool IsAttackInputPressed(AttackModule attackModule)
    {
        Animator animator = this.GetComponent<Animator>();
        if (animator.GetBool("isDead")) return false;
        // Check against cloned modules
        for (int i = 0; i < clonedAttackModules.Length; i++)
        {
            if (clonedAttackModules[i] == attackModule && inputActions[i] != null)
            {
                return inputActions[i].action.WasPressedThisFrame();
            }
        }
        return false;
    }
    
    // Helper method to check if a specific attack module's input is being held
    public bool IsAttackInputHeld(AttackModule attackModule)
    {
        Animator animator = this.GetComponent<Animator>();
        if (animator.GetBool("isDead")) return false;
        // Check against cloned modules
        for (int i = 0; i < clonedAttackModules.Length; i++)
        {
            if (clonedAttackModules[i] == attackModule && inputActions[i] != null)
            {
                return inputActions[i].action.IsPressed();
            }
        }
        return false;
    }
    
    // Get a specific cloned attack module by index?
    public AttackModule GetClonedAttackModule(int index)
    {
        if (clonedAttackModules != null && index >= 0 && index < clonedAttackModules.Length)
        {
            return clonedAttackModules[index];
        }
        return null;
    }
}
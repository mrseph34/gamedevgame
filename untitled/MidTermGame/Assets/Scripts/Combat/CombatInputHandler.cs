using Modules.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

public class CombatInputHandler : MonoBehaviour
{
    [SerializeField] private CombatHandler combatHandler;
    
    [Header("Attack Setup")]
    public InputActionReference[] inputActions;
    public AttackModule[] attackModules;
    
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
    
    private void Update()
    {
        // Make sure both arrays have the same length
        int maxIndex = Mathf.Min(inputActions.Length, attackModules.Length);
        
        for (int i = 0; i < maxIndex; i++)
        {
            if (inputActions[i] != null && 
                inputActions[i].action.WasPressedThisFrame() && 
                attackModules[i] != null)
            {
                combatHandler.StartAttack(attackModules[i]);
            }
        }
    }
    
    // Helper method to check if a specific attack module's input was pressed this frame
    public bool IsAttackInputPressed(AttackModule attackModule)
    {
        for (int i = 0; i < attackModules.Length; i++)
        {
            if (attackModules[i] == attackModule && inputActions[i] != null)
            {
                return inputActions[i].action.WasPressedThisFrame();
            }
        }
        return false;
    }
    
    // Helper method to check if a specific attack module's input is being held
    public bool IsAttackInputHeld(AttackModule attackModule)
    {
        for (int i = 0; i < attackModules.Length; i++)
        {
            if (attackModules[i] == attackModule && inputActions[i] != null)
            {
                return inputActions[i].action.IsPressed();
            }
        }
        return false;
    }
}
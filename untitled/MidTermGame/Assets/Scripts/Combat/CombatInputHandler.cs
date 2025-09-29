using Modules.Combat;
using UnityEngine;

public class CombatInputHandler : MonoBehaviour
{
    [SerializeField] private CombatHandler combatHandler;
    
    [Header("Attack Setup")]
    public KeyCode[] inputKeys;
    public AttackModule[] attackModules;
    
    private void Update()
    {
        // Make sure both arrays have the same length
        int maxIndex = Mathf.Min(inputKeys.Length, attackModules.Length);
        
        for (int i = 0; i < maxIndex; i++)
        {
            if (Input.GetKeyDown(inputKeys[i]) && attackModules[i] != null)
            {
                combatHandler.StartAttack(attackModules[i]);
            }
        }
    }
}
using UnityEngine;
using System.Collections;

public abstract class AttackModule : ScriptableObject
{
    [Header("Animation")]
    public string animationTrigger;
    
    // Public method that returns the coroutine for CombatHandler to execute
    public IEnumerator ExecuteAttack(CombatHandler combatHandler)
    {
        yield return combatHandler.StartCoroutine(PerformAttack(combatHandler));
    }
    
    // Abstract method that each attack type implements
    protected abstract IEnumerator PerformAttack(CombatHandler combatHandler);
}
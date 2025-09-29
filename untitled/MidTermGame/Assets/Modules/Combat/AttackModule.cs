using System.Collections;
using UnityEngine;

namespace Modules.Combat
{
    public abstract class AttackModule : ScriptableObject
    {
        [Header("Animation")]
        public string animationTrigger;
    
        public IEnumerator ExecuteAttack(CombatHandler combatHandler)
        {
            yield return combatHandler.StartCoroutine(PerformAttack(combatHandler));
        }
    
        public IEnumerator ExecuteHitbox(CombatHandler combatHandler)
        {
            yield return combatHandler.StartCoroutine(PerformHitbox(combatHandler));
        }
    
        protected abstract IEnumerator PerformAttack(CombatHandler combatHandler);
    
        protected virtual IEnumerator PerformHitbox(CombatHandler combatHandler)
        {
            yield break;
        }
    }
}
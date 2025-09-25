using UnityEngine;
using System.Collections;

public abstract class AttackModule : ScriptableObject
{
    [Header("Input & Cooldown")]
    public KeyCode attackKey = KeyCode.None;
    public float cooldown = 0.5f;

    [HideInInspector]
    public bool onCooldown;
    [HideInInspector]
    public float lastUsedTime;


    public void HandleInput(CombatHandler ch)
    {
        if (ch.StunHandler.IsStunned || onCooldown)
            return;
        if (Input.GetKeyDown(attackKey))
        {
            ch.StartCoroutine(UseAttack(ch));
            ch.playerAnimator.SetTrigger("playerAttack");
        }
    }

    IEnumerator UseAttack(CombatHandler ch)
    {
        onCooldown = true;
        lastUsedTime = Time.time;
        yield return PerformAttack(ch);
        yield return new WaitForSeconds(cooldown);
        onCooldown = false;
    }
    
    protected abstract IEnumerator PerformAttack(CombatHandler ch);
}
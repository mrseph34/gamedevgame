using UnityEngine;
using System.Collections;
using Modules.Combat;

[CreateAssetMenu(menuName = "Combat/Combo Attack")]
public class ComboModule : AttackModule
{
    [Header("Combo Settings")]
    public int maxCombo = 3;
    public float comboContinueWindow = 1.0f;
    public float comboCooldown = 0.5f;
    public string comboIntName = "attackClip";
    public string attackTriggerName = "attackTrigger";
    public string exitTriggerName = "attackExit";
    public bool easyMode = true;
    
    [Header("Hitbox Graphics")]
    public Sprite hitboxSprite;
    public Color hitboxColor = new Color(1, 0, 0, 0.5f);
    
    [Header("Sizes & Offsets")]
    public Vector2 hbHorizontalSize = new Vector2(5, 10);
    public Vector2 hbHorizontalOff = new Vector2(1, 0);
    public Vector2 hbUpSize = new Vector2(10, 5);
    public Vector2 hbUpOff = new Vector2(0f, 1.75f);
    public Vector2 hbDownSize = new Vector2(10, 5);
    public Vector2 hbDownOff = new Vector2(0f, -1.5f);
    public Vector2 hbDiagSize = new Vector2(5, 10);
    public Vector2 hbDiagOff = new Vector2(1, 1);
    
    [Header("Timing & Effects")]
    public float attackDelay = 0.1f;
    public float knockbackForce = 5f;
    public float stunDuration = 0.5f;
    public string targetTag = "Player";
    
    private int comboState = 1;
    private bool comboInputReceived = false;
    private bool canContinueCombo = false;
    private int currentComboState = 1;
    private bool comboActive = false;
    private bool firstAttackTriggered = false;
    private bool comboHitAlready = false;
    
    private bool IsComboKeyPressed()
    {
        CombatInputHandler inputHandler = FindObjectOfType<CombatInputHandler>();
        if (inputHandler != null)
        {
            for (int i = 0; i < inputHandler.attackModules.Length; i++)
            {
                if (inputHandler.attackModules[i] == this)
                {
                    return Input.GetKeyDown(inputHandler.inputKeys[i]);
                }
            }
        }
        return false;
    }
    
    public void SetCanContinue(bool canContinue)
    {
        canContinueCombo = canContinue;
    }
    
    protected override IEnumerator PerformAttack(CombatHandler ch)
    {
        currentComboState = 1;
        comboInputReceived = false;
        canContinueCombo = false;
        comboActive = true;
        firstAttackTriggered = false;
        comboHitAlready = false;
        
        Animator animator = ch.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator found on CombatHandler!");
            yield break;
        }
        
        animator.SetInteger(comboIntName, currentComboState);
        
        yield return ch.StartCoroutine(HandleComboInput(ch));
        
        yield return new WaitForSeconds(comboCooldown);
        
        ch.ClearCurrentAttack();
    }
    
    protected override IEnumerator PerformHitbox(CombatHandler ch)
    {
        yield return ch.StartCoroutine(CreateHitbox(ch));
    }
    
private IEnumerator HandleComboInput(CombatHandler ch)
{
    Animator animator = ch.GetComponent<Animator>();
    if (animator == null)
    {
        Debug.LogError("No Animator found on CombatHandler!");
        yield break;
    }
    
    yield return new WaitUntil(() => canContinueCombo);

    int initHitRandom = Random.Range(1, 3);
    animator.SetInteger(comboIntName, initHitRandom);
    animator.SetTrigger(attackTriggerName);
    firstAttackTriggered = true;
    canContinueCombo = false;
    
    while (comboActive)
    {
        while (!canContinueCombo && comboActive)
        {
            if (IsComboKeyPressed())
            {
                if (easyMode)
                {
                    comboInputReceived = true;
                }
                else
                {
                    // Hard mode: pressing too early ends the combo
                    comboActive = false;
                    break;
                }
            }
            yield return null;
        }
        
        if (!comboActive) break;
        
        if (comboInputReceived)
        {
            animator.SetTrigger(attackTriggerName);
            comboState++;
            
            comboInputReceived = false;
            canContinueCombo = false;
            comboHitAlready = false;
            
            if (comboState >= maxCombo - 1)
            {
                // Easy mode waits, hard mode doesn't
                if (easyMode)
                {
                    yield return new WaitForSeconds(.25f);
                }
                break;
            }
        }
        else
        {
            // Check for immediate input when combo window opens
            if (IsComboKeyPressed())
            {
                animator.SetTrigger(attackTriggerName);
                comboState++;
                
                comboInputReceived = false;
                canContinueCombo = false;
                comboHitAlready = false;
                
                if (comboState >= maxCombo - 1)
                {
                    // Easy mode waits, hard mode doesn't
                    if (easyMode)
                    {
                        yield return new WaitForSeconds(.25f);
                    }
                    break;
                }
            }
            else
            {
                // Wait for input within the combo window
                float timer = 0f;
                bool inputReceived = false;
                
                while (timer < comboContinueWindow && !inputReceived)
                {
                    if (IsComboKeyPressed())
                    {
                        inputReceived = true;
                        // Easy mode waits, hard mode doesn't
                        if (easyMode)
                        {
                            yield return new WaitForSeconds(.25f);
                        }
                        break;
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }
                
                if (inputReceived)
                {
                    animator.SetTrigger(attackTriggerName);
                    comboState++;
                    
                    comboInputReceived = false;
                    canContinueCombo = false;
                    comboHitAlready = false;
                    
                    if (comboState >= maxCombo - 1)
                    {
                        // Easy mode waits, hard mode doesn't
                        if (easyMode)
                        {
                            yield return new WaitForSeconds(.25f);
                        }
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
        
        yield return null;
    }
    
    comboState = 0;
    comboActive = false;
    animator.SetTrigger(exitTriggerName);
    animator.SetInteger(comboIntName, 0);
}
    private IEnumerator CreateHitbox(CombatHandler ch)
    {
        yield return new WaitForSeconds(attackDelay);
        
        Vector2 inDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        
        Vector2 size, offset;
        bool diag = false;
        
        if (inDir.y > 0.5f && Mathf.Abs(inDir.x) < 0.1f)
        {
            size = hbUpSize;
            offset = hbUpOff;
        }
        else if (inDir.y < -0.5f && Mathf.Abs(inDir.x) < 0.1f)
        {
            size = hbDownSize;
            offset = hbDownOff;
        }
        else if (inDir.y > 0.5f && Mathf.Abs(inDir.x) > 0.5f)
        {
            size = hbDiagSize;
            offset = hbDiagOff;
            diag = true;
        }
        else
        {
            size = hbHorizontalSize;
            offset = hbHorizontalOff;
        }
        
        if (ch.transform.localScale.x < 0)
            offset.x = -offset.x;
        
        Vector2 kb = offset.normalized * knockbackForce;
        Debug.DrawRay(ch.transform.position, kb, Color.cyan, 0.5f);
        
        var hbGO = new GameObject($"Hitbox_Combo_State_{currentComboState}");
        
        hbGO.transform.position = ch.transform.position + (Vector3)offset;
        hbGO.transform.localScale = size;
        
        if (diag)
        {
            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            hbGO.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        
        hbGO.transform.SetParent(ch.transform, worldPositionStays: true);
        
        if (hitboxSprite != null)
        {
            var sr = hbGO.AddComponent<SpriteRenderer>();
            sr.sprite = hitboxSprite;
            sr.color = hitboxColor;
            sr.sortingOrder = 100;
        }
        
        var bc = hbGO.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        
        var hb = hbGO.AddComponent<Hitbox>();
        hb.Setup(kb, stunDuration, targetTag, 0.1f, ch.SelfCollider, offset);
        
        Destroy(hbGO, hb.lifetime + 0.05f);
    }
}
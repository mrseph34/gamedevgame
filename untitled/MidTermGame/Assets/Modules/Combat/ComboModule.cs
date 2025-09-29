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
    
    [Header("Safety Settings")]
    public float maxComboTimeout = 10f; // Safety timeout for entire combo sequence
    
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
    
    private int comboHandlerInt = 0;
    private bool canContinueCombo = false;
    private bool inputBuffered = false;
    private float lastInputTime = -999f;
    private const float INPUT_GRACE_PERIOD = 0.1f;
    private int cooldownIndex = 0;
    private int activeCooldownIndex = 0;
    
    private bool IsComboKeyPressed()
    {
        CombatInputHandler inputHandler = FindObjectOfType<CombatInputHandler>();
        if (inputHandler != null)
        {
            return inputHandler.IsAttackInputPressed(this);
        }
        return false;
    }
    
    private bool CanAcceptInput()
    {
        return Time.time >= lastInputTime + INPUT_GRACE_PERIOD;
    }
    
    public void SetCanContinue(bool canContinue)
    {
        canContinueCombo = canContinue;
    }
    
    protected override IEnumerator PerformAttack(CombatHandler ch)
    {
        comboHandlerInt = 0;
        canContinueCombo = false;
        inputBuffered = false;
        lastInputTime = -999f;
        ch.SetModuleCooldown(this, true);
        
        // Assign this combo instance a unique cooldown index
        int myCooldownIndex = ++cooldownIndex;
        activeCooldownIndex = myCooldownIndex;
        
        Animator animator = ch.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("No Animator found on CombatHandler!");
            ForceCleanup(ch, animator, myCooldownIndex);
            yield break;
        }
        
        float comboStartTime = Time.time;
        
        try
        {
            float elapsedTime = Time.time;
            
            // Safety timeout while waiting to start
            float waitStartTime = Time.time;
            while (!canContinueCombo)
            {
                if (Time.time - waitStartTime > 2f) // 2 second timeout for initial wait
                {
                    Debug.LogWarning("ComboModule: Timed out waiting for initial canContinue signal");
                    yield break;
                }
                yield return null;
            }
            elapsedTime = Time.time - elapsedTime;
            
            // FIRST HIT - auto trigger
            int initHitRandom = Random.Range(1, 3);
            animator.SetInteger(comboIntName, initHitRandom);
            animator.SetTrigger(attackTriggerName);
            animator.ResetTrigger(exitTriggerName);
            comboHandlerInt = 1;
            canContinueCombo = false;
            inputBuffered = false;
            lastInputTime = Time.time;
            
            // LOOP for remaining hits
            for (int hit = 1; hit < maxCombo; hit++)
            {
                // Check if entire combo has timed out
                if (Time.time - comboStartTime > maxComboTimeout)
                {
                    Debug.LogWarning("ComboModule: Entire combo sequence timed out");
                    break;
                }
                
                bool comboFailed = false;
                float hitWaitStart = Time.time;
                
                if (easyMode)
                {
                    // EASY MODE: Buffer inputs while waiting for window
                    while (!canContinueCombo)
                    {
                        if (Time.time - hitWaitStart > comboContinueWindow)
                        {
                            Debug.LogWarning($"ComboModule: Timed out waiting for hit {hit} window");
                            comboFailed = true;
                            break;
                        }
                        
                        if (IsComboKeyPressed() && CanAcceptInput())
                        {
                            inputBuffered = true;
                        }
                        yield return null;
                    }
                }
                else
                {
                    // HARD MODE: Pressing before window opens fails the combo
                    while (!canContinueCombo)
                    {
                        if (Time.time - hitWaitStart > comboContinueWindow)
                        {
                            Debug.LogWarning($"ComboModule: Timed out waiting for hit {hit} window");
                            comboFailed = true;
                            break;
                        }
                        
                        if (IsComboKeyPressed() && CanAcceptInput())
                        {
                            comboFailed = true;
                            break;
                        }
                        yield return null;
                    }
                }
                
                if (comboFailed)
                {
                    break;
                }
                
                // Window is open - check if we have buffered input or get new input
                bool gotInput = inputBuffered;
                
                if (!gotInput && CanAcceptInput())
                {
                    gotInput = IsComboKeyPressed();
                }
                
                // Wait for input within the window if we don't have one yet
                if (!gotInput)
                {
                    float timer = 0f;
                    while (timer < (comboContinueWindow - elapsedTime))
                    {
                        if (CanAcceptInput() && IsComboKeyPressed())
                        {
                            gotInput = true;
                            break;
                        }
                        timer += Time.deltaTime;
                        yield return null;
                    }
                }
                
                // If we got input, continue combo
                if (gotInput)
                {
                    animator.SetTrigger(attackTriggerName);
                    comboHandlerInt++;
                    canContinueCombo = false;
                    inputBuffered = false;
                    lastInputTime = Time.time;
                }
                else
                {
                    // No input, end combo
                    break;
                }
            }
            
            // CLEANUP
            yield return new WaitForSeconds(0.5f);
            comboHandlerInt = 0;
            animator.SetTrigger(exitTriggerName);
            animator.SetInteger(comboIntName, 0);
            animator.SetBool("playerAttacking", false);
            
            // Combo cooldown before allowing another attack - ONLY if this is still the active combo
            if (activeCooldownIndex == myCooldownIndex)
            {
                yield return new WaitForSeconds(comboCooldown);
                
                // Double-check we're still the active one before clearing
                if (activeCooldownIndex == myCooldownIndex)
                {
                    ch.SetModuleCooldown(this, false);
                    ch.ClearCurrentAttack();
                }
            }
        }
        finally
        {
            ForceCleanup(ch, animator, myCooldownIndex);
        }
    }
    
    private void ForceCleanup(CombatHandler ch, Animator animator, int myCooldownIndex)
    {
        // Only proceed if we're still the active cooldown holder
        if (activeCooldownIndex != myCooldownIndex || !animator.GetBool("playerAttacking"))
        {
            return;
        }
        
        // Reset combo state
        comboHandlerInt = 0;
        canContinueCombo = false;
        inputBuffered = false;
        
        // Reset animator state
        if (animator != null)
        {
            animator.SetTrigger(exitTriggerName);
            animator.SetInteger(comboIntName, 0);
            animator.SetBool("playerAttacking", false);
        }
        
        // Clear cooldown
        if (ch != null)
        {
            ch.SetModuleCooldown(this, false);
            ch.ClearCurrentAttack();
        }
        
        Debug.Log("ComboModule: Safety cleanup executed");
    }
    
    protected override IEnumerator PerformHitbox(CombatHandler ch)
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
        else if (inDir.y < -0.5f && Mathf.Abs(inDir.x) > 0.5f)
        {
            size = hbDiagSize;
            offset = new Vector2(hbDiagOff.x, -hbDiagOff.y);
            diag = true;
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
        
        var hbGO = new GameObject($"Hitbox_Combo_State_{comboHandlerInt}");
        
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
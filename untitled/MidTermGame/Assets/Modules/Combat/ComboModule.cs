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
    public float lastHitKnockbackForce = 10f; 
    public float stunDuration = 0.5f;
    public string targetTag = "Player";

    [Header("Rotation Settings")]
    public float maxRotationAngle = 30f;
    public float upwardAngleThreshold = 10f;
    public float rotationDurationMultiplier = 0.5f;
    
    private int comboHandlerInt = 0;
    private bool canContinueCombo = false;
    private bool inputBuffered = false;
    private float lastInputTime = -999f;
    private const float INPUT_GRACE_PERIOD = 0.1f;
    private int cooldownIndex = 0;
    private int activeCooldownIndex = 0;
    private Quaternion originalRotation;
    private bool isRotationLocked = false;
    private Quaternion lockedRotation;
    
    private bool IsComboKeyPressed(CombatHandler ch)
    {
        CombatInputHandler inputHandler = ch.GetComponent<CombatInputHandler>();
        if (inputHandler == null)
        {
            inputHandler = ch.GetComponentInChildren<CombatInputHandler>();
        }
    
        if (inputHandler != null)
        {
            return inputHandler.IsAttackInputPressed(this);
        }
    
        Debug.LogWarning($"No CombatInputHandler found for {ch.gameObject.name}");
        return false;
    }
    
    private Vector2 GetPlayerMoveInput(CombatHandler ch)
    {
        // Get the PillController to access the correct player's input
        PillController pillController = ch.GetComponent<PillController>();
        if (pillController == null)
        {
            Debug.LogWarning("No PillController found, falling back to default input");
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }
        
        // Get the Move input action for this specific player
        var moveAction = pillController.GetInputAction("Move");
        if (moveAction != null)
        {
            return moveAction.ReadValue<Vector2>();
        }
        
        // Fallback
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }
    
    private bool CanAcceptInput()
    {
        return Time.time >= lastInputTime + INPUT_GRACE_PERIOD;
    }
    
    public void SetCanContinue(bool canContinue)
    {
        canContinueCombo = canContinue;
    }

    private Vector2 GetAttackDirection(CombatHandler ch)
    {
        Vector2 inputDir = GetPlayerMoveInput(ch);

        float face = ch.transform.localScale.x > 0 ? 1f : -1f;

        // Check for up input - face forward (no horizontal component)
        if (inputDir.y > 0.5f && Mathf.Abs(inputDir.x) < 0.5f)
        {
            return new Vector2(face, 0f);
        }
        
        // Check for down input - bottom diagonal in facing direction
        if (inputDir.y < -0.5f)
        {
            return new Vector2(face, -1f).normalized;
        }

        if (inputDir.sqrMagnitude > 0.1f)
            return inputDir.normalized;

        // fallback → facing
        return Vector2.right * face;
    }

    private void ApplyRotation(CombatHandler ch, Vector2 attackDir)
    {
        // Check if attacking nearly straight up
        float angleFromUp = Vector2.Angle(attackDir, Vector2.up);
        bool isNearVertical = angleFromUp <= upwardAngleThreshold;

        // Don't rotate if nearly vertical
        if (isNearVertical)
        {
            return;
        }

        bool facingLeft = ch.transform.localScale.x < 0;
        
        Vector2 rotationDir = attackDir;
        float angle;
        
        if (facingLeft)
        {
            rotationDir.x = -rotationDir.x;
            angle = Mathf.Atan2(-rotationDir.y, rotationDir.x) * Mathf.Rad2Deg;
        }
        else
        {
            angle = Mathf.Atan2(rotationDir.y, rotationDir.x) * Mathf.Rad2Deg;
        }

        angle = Mathf.Clamp(angle, -maxRotationAngle, maxRotationAngle);
        
        ch.transform.rotation = Quaternion.Euler(0, 0, angle);
        lockedRotation = ch.transform.rotation;
    }
    
    protected override IEnumerator PerformAttack(CombatHandler ch)
    {
        comboHandlerInt = 0;
        canContinueCombo = false;
        inputBuffered = false;
        lastInputTime = -999f;
        ch.SetModuleCooldown(this, true);
        originalRotation = ch.transform.rotation;
        isRotationLocked = false;
        
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
            
            float waitStartTime = Time.time;
            while (!canContinueCombo)
            {
                if (Time.time - waitStartTime > 2f)
                {
                    Debug.LogWarning("ComboModule: Timed out waiting for initial canContinue signal");
                    yield break;
                }
                yield return null;
            }
            elapsedTime = Time.time - elapsedTime;
            
            // FIRST HIT
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
                if (Time.time - comboStartTime > comboContinueWindow * 1.1f)
                {
                    Debug.LogWarning("ComboModule: Entire combo sequence timed out");
                    break;
                }
                
                bool comboFailed = false;
                float hitWaitStart = Time.time;
                
                if (easyMode)
                {
                    while (!canContinueCombo)
                    {
                        if (Time.time - hitWaitStart > comboContinueWindow)
                        {
                            Debug.LogWarning($"ComboModule: Timed out waiting for hit {hit} window");
                            comboFailed = true;
                            break;
                        }
                        
                        if (IsComboKeyPressed(ch) && CanAcceptInput())
                        {
                            inputBuffered = true;
                        }
                        yield return null;
                    }
                }
                else
                {
                    while (!canContinueCombo)
                    {
                        if (Time.time - hitWaitStart > comboContinueWindow)
                        {
                            Debug.LogWarning($"ComboModule: Timed out waiting for hit {hit} window");
                            comboFailed = true;
                            break;
                        }
                        
                        if (IsComboKeyPressed(ch) && CanAcceptInput())
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
                
                bool gotInput = inputBuffered;
                
                if (!gotInput && CanAcceptInput())
                {
                    gotInput = IsComboKeyPressed(ch);
                }
                
                if (!gotInput)
                {
                    float timer = 0f;
                    while (timer < (comboContinueWindow - elapsedTime))
                    {
                        if (CanAcceptInput() && IsComboKeyPressed(ch))
                        {
                            gotInput = true;
                            break;
                        }
                        timer += Time.deltaTime;
                        yield return null;
                    }
                }
                
                if (gotInput)
                {
                    isRotationLocked = false;
                    
                    animator.SetTrigger(attackTriggerName);
                    comboHandlerInt++;
                    canContinueCombo = false;
                    inputBuffered = false;
                    lastInputTime = Time.time;
                }
                else
                {
                    break;
                }
            }
            
            // CLEANUP
            yield return new WaitForSeconds(0.5f);
            comboHandlerInt = 0;
            animator.SetTrigger(exitTriggerName);
            animator.SetInteger(comboIntName, 0);
            animator.SetBool("playerAttacking", false);
            
            ch.transform.rotation = originalRotation;
            
            if (activeCooldownIndex == myCooldownIndex)
            {
                yield return new WaitForSeconds(comboCooldown);
                
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
        if (activeCooldownIndex != myCooldownIndex || !animator.GetBool("playerAttacking"))
        {
            return;
        }
        
        comboHandlerInt = 0;
        canContinueCombo = false;
        inputBuffered = false;
        isRotationLocked = false;
        
        if (ch != null)
        {
            ch.transform.rotation = originalRotation;
        }
        
        if (animator != null)
        {
            animator.SetTrigger(exitTriggerName);
            animator.SetInteger(comboIntName, 0);
            animator.SetBool("playerAttacking", false);
        }
        
        if (ch != null)
        {
            ch.SetModuleCooldown(this, false);
            ch.ClearCurrentAttack();
        }
        
        Debug.Log("ComboModule: Safety cleanup executed");
    }
    
    protected override IEnumerator PerformHitbox(CombatHandler ch)
    {
        if (!isRotationLocked)
        {
            Vector2 attackDir = GetAttackDirection(ch);
            ApplyRotation(ch, attackDir);
            isRotationLocked = true;
            
            ch.StartCoroutine(RotationLockDuration(ch));
        }
        
        yield return new WaitForSeconds(attackDelay);
        
        Vector2 inDir = GetPlayerMoveInput(ch);
        
        Vector2 size, offset;
        bool diag = false;
        
        if (inDir.y > 0.5f && Mathf.Abs(inDir.x) < 0.1f)
        {
            Animator animator = ch.GetComponent<Animator>();
            animator.SetTrigger("playerOverhead");
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
        
        bool isLastHit = (comboHandlerInt == maxCombo);
        float currentKnockback = isLastHit ? lastHitKnockbackForce : knockbackForce;
        Vector2 kb = offset.normalized * currentKnockback;
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

    private IEnumerator RotationLockDuration(CombatHandler ch)
    {
        float rotationDuration = comboContinueWindow * rotationDurationMultiplier;
        float startTime = Time.time;
        
        while (Time.time - startTime < rotationDuration && isRotationLocked)
        {
            ch.transform.rotation = lockedRotation;
            yield return null;
        }
        
        if (isRotationLocked)
        {
            ch.transform.rotation = originalRotation;
        }
    }
}
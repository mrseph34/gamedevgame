using System.Collections;
using UnityEngine;

namespace Modules.Combat
{
    [CreateAssetMenu(menuName = "Combat/Heavy Attack")]
    public class HeavyAttackModule : AttackModule
    {
        [Header("Heavy Attack Settings")]
        public float maxHoldTime = 3.0f;
        public float windupDuration = 0.5f;
        public string attackTrigger = "heavyAttack";
        // public string comboIntName = "heavyClip"; // use later for random heavy attacks?
        public string heavyExitTrigger = "heavyExit";
    
        [Header("Difficulty Settings")]
        public bool easyMode = true;
    
        [Header("Knockback Settings")]
        public float minKnockbackForce = 5f;
        public float maxKnockbackForce = 20f;
        public float stunDuration = 1.0f;
    
        [Header("Hitbox Graphics")]
        public Sprite hitboxSprite;
        public Color hitboxColor = new Color(1, 0.5f, 0, 0.5f);
    
        [Header("Sizes & Offsets")]
        public Vector2 hbHorizontalSize = new Vector2(6, 12);
        public Vector2 hbHorizontalOff = new Vector2(1.5f, 0);
        public Vector2 hbUpSize = new Vector2(12, 6);
        public Vector2 hbUpOff = new Vector2(0f, 2f);
        public Vector2 hbDownSize = new Vector2(12, 6);
        public Vector2 hbDownOff = new Vector2(0f, -2f);
        public Vector2 hbDiagSize = new Vector2(6, 12);
        public Vector2 hbDiagOff = new Vector2(1.5f, 1.5f);
    
        [Header("Timing & Effects")]
        public float attackDelay = 0.1f;
        public string targetTag = "Player";
    
        private bool isCharging = false;
        private float chargeTime = 0f;
    
        private bool IsHeavyKeyHeld(CombatHandler ch)
        {
            CombatInputHandler inputHandler = FindObjectOfType<CombatInputHandler>();
            if (inputHandler != null)
            {
                for (int i = 0; i < inputHandler.attackModules.Length; i++)
                {
                    if (inputHandler.attackModules[i] == this)
                    {
                        return Input.GetKey(inputHandler.inputKeys[i]);
                    }
                }
            }
            return false;
        }
    
        protected override IEnumerator PerformAttack(CombatHandler ch)
        {
            Animator animator = ch.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("No Animator found on CombatHandler!");
                yield break;
            }
        
            isCharging = false;
            chargeTime = 0f;
        
            animator.SetTrigger(animationTrigger);
            yield return new WaitForSeconds(windupDuration);
        
            if (easyMode)
            {
                yield return ch.StartCoroutine(ExecuteEasyMode(ch, animator));
            }
            else
            {
                yield return ch.StartCoroutine(ExecuteHardMode(ch, animator));
            }
        }
    
        private IEnumerator ExecuteEasyMode(CombatHandler ch, Animator animator)
        {
            isCharging = true;
        
            while (isCharging && chargeTime < maxHoldTime)
            {
                if (IsHeavyKeyHeld(ch))
                {
                    chargeTime += Time.deltaTime;
                }
                else
                {
                    break;
                }
                yield return null;
            }
        
            isCharging = false;
            chargeTime = Mathf.Clamp(chargeTime, 0f, maxHoldTime);
        
            animator.SetTrigger(attackTrigger);
            ch.ClearCurrentAttack();
        }
    
        private IEnumerator ExecuteHardMode(CombatHandler ch, Animator animator)
        {
            isCharging = true;
            bool keyHeld = true;
        
            while (isCharging && chargeTime < maxHoldTime)
            {
                if (IsHeavyKeyHeld(ch))
                {
                    chargeTime += Time.deltaTime;
                }
                else
                {
                    keyHeld = false;
                    break;
                }
                yield return null;
            }
        
            isCharging = false;
        
            // Fixed hard mode timing: must release between windupDuration and maxHoldTime
            bool validTiming = !keyHeld && chargeTime >= windupDuration && chargeTime <= maxHoldTime;
        
            if (validTiming)
            {
                chargeTime = Mathf.Clamp(chargeTime, 0f, maxHoldTime);
                animator.SetTrigger(attackTrigger);
            }
            else
            {
                animator.SetTrigger(heavyExitTrigger);
            }
        
            ch.ClearCurrentAttack();
        }
    
        protected override IEnumerator PerformHitbox(CombatHandler ch)
        {
            yield return ch.StartCoroutine(CreateHitbox(ch));
        }
    
        private IEnumerator CreateHitbox(CombatHandler ch)
        {
            yield return new WaitForSeconds(attackDelay);
        
            float chargeRatio = chargeTime / maxHoldTime;
            float knockbackForce = Mathf.Lerp(minKnockbackForce, maxKnockbackForce, chargeRatio);
        
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
            Debug.DrawRay(ch.transform.position, kb, Color.red, 1.0f);
        
            var hbGO = new GameObject($"Hitbox_Heavy_Charge_{chargeRatio:F2}");
        
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
            hb.Setup(kb, stunDuration, targetTag, 0.2f, ch.SelfCollider, offset);
        
            Destroy(hbGO, hb.lifetime + 0.05f);
        }
    }
}
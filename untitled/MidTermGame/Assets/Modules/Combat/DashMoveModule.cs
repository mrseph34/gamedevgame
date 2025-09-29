using System.Collections;
using UnityEngine;

namespace Modules.Combat
{
    [CreateAssetMenu(menuName = "Combat/Dash Move")]
    public class DashMoveModule : AttackModule
    {
        [Header("Dash Settings")]
        public float dashForce = 15f; // Force applied for the dash
        public float dashDuration = 0.3f; // How long the dash lasts
        public AnimationCurve dashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public float dragDuringDash = 0.5f; // Rigidbody2D.drag during dash
        public float dashCooldown = 1f;

        [Header("Dash Physics")]
        public bool ignoreGravityDuringDash = true;
        public LayerMask collisionLayers = ~0; // what layers to collide with

        [Header("Jump Detection")]
        public float upwardAngleThreshold = 10f; // If dash is within this many degrees of straight up, treat as jump

        // temporary storage
        Rigidbody2D cachedRb;
        float originalDrag;
        float originalGravity;
        Quaternion originalRotation;

        protected override IEnumerator PerformAttack(CombatHandler ch)
        {
            yield return ExecuteDash(ch);
        }

        public IEnumerator ExecuteDash(CombatHandler ch)
        {
            Animator animator = ch.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("No Animator found on CombatHandler!");
                yield break;
            }
    
            cachedRb = ch.GetComponent<Rigidbody2D>();
            if (cachedRb == null)
            {
                Debug.LogError("DashMoveModule requires a Rigidbody2D on the CombatHandler!");
                yield break;
            }

            originalDrag = cachedRb.linearDamping;
            originalGravity = cachedRb.gravityScale;
            originalRotation = ch.transform.rotation;

            Vector2 dashDir = GetDashDirection(ch);
            
            // Check if dashing nearly straight up
            float angleFromUp = Vector2.Angle(dashDir, Vector2.up);
            bool isNearVertical = angleFromUp <= upwardAngleThreshold;

            if (isNearVertical)
            {
                animator.SetTrigger("playerJump");
                animator.SetBool("playerJumping", true);
            }
            else
            {
                animator.SetBool("isDashing", true);
                // Only rotate if doing a regular dash (not a jump)
                
                bool facingLeft = ch.transform.localScale.x < 0;
                
                // If facing left, mirror the dash direction to right side for rotation calculation
                Vector2 rotationDir = dashDir;
                float angle;
                if (facingLeft)
                {
                    rotationDir.x = -rotationDir.x; // Flip X to treat it as if facing right
                    angle = Mathf.Atan2(-rotationDir.y, rotationDir.x) * Mathf.Rad2Deg;
                }
                else {
                    angle = Mathf.Atan2(rotationDir.y, rotationDir.x) * Mathf.Rad2Deg;
                }
                
                ch.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            ch.StateHandler.ChangeState(StateHandler.State.Dashing);

            Vector2 startVel = dashDir * dashForce;
            cachedRb.linearVelocity = startVel;

            cachedRb.linearDamping = dragDuringDash;
            if (ignoreGravityDuringDash)
                cachedRb.gravityScale = 0f;

            float t = 0f;
            while (t < dashDuration)
            {
                t += Time.deltaTime;
                float pct = Mathf.Clamp01(t / dashDuration);
                float mul = dashCurve.Evaluate(pct);

                Vector2 newVel = startVel * mul;
                if (!ignoreGravityDuringDash)
                    newVel.y = cachedRb.linearVelocity.y;

                cachedRb.linearVelocity = newVel;
                yield return null;
            }

            RestorePhysics();
            
            // Only restore rotation if we rotated (i.e., not a jump)
            if (!isNearVertical)
            {
                ch.transform.rotation = originalRotation;
            }
            
            if (isNearVertical)
            {
                animator.SetBool("playerJumping", false);
            }
            else
            {
                animator.SetBool("isDashing", false);
            }
            
            ch.StateHandler.ChangeState(StateHandler.State.Grounded);
    
            // Add cooldown
            ch.ClearCurrentAttack();
            ch.SetModuleCooldown(this, true);
            yield return new WaitForSeconds(dashCooldown);
            ch.SetModuleCooldown(this, false);
        }

        /// If the player is holding a direction, dash that way;
        /// otherwise dash in the facing‐direction.
        private Vector2 GetDashDirection(CombatHandler ch)
        {
            Vector2 inputDir = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            if (inputDir.sqrMagnitude > 0.1f)
                return inputDir.normalized;

            // fallback → facing
            float face = ch.transform.localScale.x > 0 ? 1f : -1f;
            return Vector2.right * face;
        }

        private void RestorePhysics()
        {
            if (cachedRb != null)
            {
                cachedRb.linearDamping = originalDrag;
                cachedRb.gravityScale = originalGravity;
            }
        }

        private void OnDisable()
        {
            RestorePhysics();
        }
    }
}
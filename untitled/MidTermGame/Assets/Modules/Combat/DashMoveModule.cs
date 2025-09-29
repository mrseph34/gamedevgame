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

        [Header("Dash Physics")]
        public bool ignoreGravityDuringDash = true;
        public LayerMask collisionLayers = ~0; // what layers to collide with

        // temporary storage
        Rigidbody2D cachedRb;
        float originalDrag;
        float originalGravity;

        protected override IEnumerator PerformAttack(CombatHandler ch)
        {
            yield return ExecuteDash(ch);
        }

        public IEnumerator ExecuteDash(CombatHandler ch)
        {
            cachedRb = ch.GetComponent<Rigidbody2D>();
            if (cachedRb == null)
            {
                Debug.LogError("DashMoveModule requires a Rigidbody2D on the CombatHandler!");
                yield break;
            }

            originalDrag = cachedRb.linearDamping;
            originalGravity = cachedRb.gravityScale;

            ch.StateHandler.ChangeState(StateHandler.State.Dashing);

            Vector2 dashDir = GetDashDirection(ch);
            Vector2 startVel = dashDir * dashForce;
            cachedRb.linearVelocity = startVel;

            // tweak drag/gravity
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

            ch.StateHandler.ChangeState(StateHandler.State.Grounded);
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
            // in case the SO is destroyed while dashing
            RestorePhysics();
        }
    }
}

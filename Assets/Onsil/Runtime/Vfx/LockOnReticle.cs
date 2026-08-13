using System.Collections;
using UnityEngine;

namespace Onsil.Vfx
{
    /// <summary>
    /// A lock-on mark that PERSISTS on a target until an attack spends it.
    /// </summary>
    /// <remarks>
    /// Design rule: only the scan skill calls <see cref="Apply"/>. Basic attacks and
    /// damage skills call <see cref="Consume"/> and never create a mark themselves.
    /// <see cref="AbilityRunner"/> enforces this via the appliesMark / consumesMark
    /// flags on each <see cref="Onsil.Abilities.Ability"/>.
    /// </remarks>
    public class LockOnReticle : MonoBehaviour
    {
        [Header("refs")]
        public SpriteRenderer bracket;

        [Header("snap")]
        public float snapTime = 0.4f;
        public float startScale = 3.0f;
        public float lockedScale = 1.0f;
        public float spinDegrees = 200f;
        public float holdPulse = 0.05f;
        public float pulseHz = 8f;

        [Header("colour")]
        public Color scanColor = new Color(0.45f, 0.95f, 1f, 1f);
        public Color lockedColor = new Color(1f, 0.32f, 0.28f, 1f);

        [Header("consume")]
        public float consumeTime = 0.18f;
        public float consumeFlare = 1.7f;

        Transform target;
        float t = -1f;
        bool locked;

        /// <summary>True while a target carries the mark.</summary>
        public bool HasMark => target != null && t >= 0f;
        public Transform MarkedTarget => HasMark ? target : null;

        /// <summary>Raised when the mark is applied / spent. Hook combat logic here.</summary>
        public event System.Action<Transform> MarkApplied;
        public event System.Action<Transform> MarkConsumed;

        void Awake()
        {
            if (bracket == null) bracket = GetComponentInChildren<SpriteRenderer>();
            SetVisible(false);
        }

        void SetVisible(bool v) { if (bracket != null) bracket.enabled = v; }

        /// <summary>Place the mark. Scan skills only.</summary>
        public void Apply(Transform newTarget)
        {
            if (newTarget == null) return;
            target = newTarget;
            t = 0f;
            locked = false;
            SetVisible(true);
            if (bracket != null) { var c = bracket.color; c.a = 1f; bracket.color = c; }
            MarkApplied?.Invoke(target);
        }

        /// <summary>Spend the mark with a short flare. No-op when unmarked.</summary>
        public void Consume()
        {
            if (!HasMark) { Clear(); return; }
            var spent = target;
            StartCoroutine(ConsumeRoutine());
            MarkConsumed?.Invoke(spent);
        }

        IEnumerator ConsumeRoutine()
        {
            float e = 0f;
            Vector3 s0 = transform.localScale;
            while (e < consumeTime)
            {
                e += Time.deltaTime;
                float k = e / consumeTime;
                transform.localScale = s0 * Mathf.Lerp(1f, consumeFlare, k);
                if (bracket != null) { var c = bracket.color; c.a = 1f - k; bracket.color = c; }
                yield return null;
            }
            Clear();
        }

        /// <summary>Remove the mark immediately, no flare.</summary>
        public void Clear()
        {
            t = -1f;
            locked = false;
            target = null;
            SetVisible(false);
            if (bracket != null) { var c = bracket.color; c.a = 1f; bracket.color = c; }
            transform.localScale = Vector3.one * lockedScale;
            transform.localRotation = Quaternion.identity;
        }

        void LateUpdate()
        {
            if (t < 0f) return;
            if (target != null) transform.position = target.position;

            if (!locked)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / snapTime);
                float e = 1f - Mathf.Pow(1f - k, 3f);          // ease-out cubic
                transform.localScale = Vector3.one * Mathf.Lerp(startScale, lockedScale, e);
                transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(spinDegrees, 0f, e));
                if (bracket != null) bracket.color = Color.Lerp(scanColor, lockedColor, e);
                if (k >= 1f) locked = true;
            }
            else
            {
                float p = 1f + Mathf.Sin(Time.time * pulseHz) * holdPulse;
                transform.localScale = Vector3.one * lockedScale * p;
                transform.localRotation = Quaternion.identity;
            }
        }
    }
}

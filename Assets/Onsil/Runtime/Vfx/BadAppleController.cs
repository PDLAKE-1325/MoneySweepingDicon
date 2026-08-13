using System.Collections;
using UnityEngine;

namespace Onsil.Vfx
{
    /// <summary>
    /// Drives the black-and-white threshold pass.
    /// </summary>
    /// <remarks>
    /// Uses ONE dedicated coroutine handle. An earlier version called
    /// StopAllCoroutines inside FadeOut, so an unrelated coroutine on the same
    /// object could cancel the fade and leave the screen stuck in monochrome.
    /// </remarks>
    [ExecuteAlways]
    public class BadAppleController : MonoBehaviour
    {
        public static BadAppleController Instance { get; private set; }

        [Tooltip("Material using Onsil/BadAppleThreshold")]
        public Material material;

        [Tooltip("0 is a perfect passthrough. 1 is full binarisation.")]
        public float amount = 0f;
        [Tooltip("Luminance split. MEASURED in the cutaway: background 0.035, " +
                 "buildings ~0.006, target 0.10-0.70. The shader default of 0.5 puts " +
                 "everything below the split, so the whole cut goes flat black.")]
        public float threshold = 0.5f;
        [Tooltip("Edge softness around the split.")]
        public float softness = 0.05f;
        [Tooltip("1 swaps bright and dark.")]
        public float invert = 0f;
        [Tooltip("Desaturation, scaled by amount so 0 stays full colour.")]
        public float desaturate = 1f;
        public Color bright = Color.white;
        public Color dark = Color.black;

        static readonly int ID_Amount = Shader.PropertyToID("_Amount");
        static readonly int ID_Threshold = Shader.PropertyToID("_Threshold");
        static readonly int ID_Softness = Shader.PropertyToID("_Softness");
        static readonly int ID_Invert = Shader.PropertyToID("_Invert");
        static readonly int ID_Desat = Shader.PropertyToID("_Desat");
        static readonly int ID_Bright = Shader.PropertyToID("_Bright");
        static readonly int ID_Dark = Shader.PropertyToID("_Dark");

        Coroutine anim;

        void OnEnable() { Instance = this; Push(); }
        void OnDisable() { amount = 0f; Push(); }   // never leave the screen stuck
        void OnValidate() { Push(); }
        void LateUpdate() { Push(); }

        public void Push()
        {
            if (material == null) return;
            material.SetFloat(ID_Amount, amount);
            material.SetFloat(ID_Threshold, threshold);
            material.SetFloat(ID_Softness, softness);
            material.SetFloat(ID_Invert, invert);
            material.SetFloat(ID_Desat, desaturate);
            material.SetColor(ID_Bright, bright);
            material.SetColor(ID_Dark, dark);
        }

        void Run(IEnumerator routine)
        {
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(routine);
        }

        /// <summary>Jump straight to a value, cancelling any fade in flight.</summary>
        public void SetAmount(float v)
        {
            if (anim != null) { StopCoroutine(anim); anim = null; }
            amount = Mathf.Clamp01(v);
            Push();
        }

        public void FadeIn(float dur = 0.25f) { Run(Ramp(amount, 1f, dur)); }
        public void FadeOut(float dur = 0.4f) { Run(Ramp(amount, 0f, dur)); }
        public void Hit(float hold = 0.35f, float release = 0.5f) { Run(HitRoutine(hold, release)); }

        IEnumerator Ramp(float a, float b, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;        // must survive slow motion
                amount = Mathf.Lerp(a, b, Mathf.Clamp01(t / dur));
                Push();
                yield return null;
            }
            amount = b; Push(); anim = null;
        }

        IEnumerator HitRoutine(float hold, float release)
        {
            amount = 1f; Push();
            yield return new WaitForSecondsRealtime(hold);
            float t = 0f;
            while (t < release)
            {
                t += Time.unscaledDeltaTime;
                amount = 1f - Mathf.Clamp01(t / release);
                Push();
                yield return null;
            }
            amount = 0f; Push(); anim = null;
        }
    }
}

using System.Collections;
using UnityEngine;

namespace Onsil.Vfx
{
    /// <summary>
    /// Drives the chromatic aberration punch used on parries and heavy impacts.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT a full-screen colour wash. The offset is weighted toward
    /// the edges (see _Falloff in the shader) so the contact point stays readable
    /// while the frame fringes - which is what sells a hard block without hiding
    /// the sprite that caused it.
    ///
    /// Unscaled time throughout: the punch has to resolve during the hitstop it
    /// is paired with, not freeze alongside it.
    /// </remarks>
    [ExecuteAlways]
    public class ChromaticPunch : MonoBehaviour
    {
        public static ChromaticPunch Instance { get; private set; }

        [Tooltip("Material using Onsil/ChromaticPunch")]
        public Material material;

        [Tooltip("0 is a perfect passthrough.")]
        public float amount = 0f;
        [Tooltip("Pixel offset at the frame edge, before amount scales it.")]
        public float strength = 18f;
        [Tooltip("Higher keeps the centre clean and pushes the fringe outward.")]
        public float falloff = 1.6f;
        [Tooltip("Warm bias at the rim. Small values only - this is not a wash.")]
        public Color tint = new Color(1f, 0.9f, 0.5f, 1f);
        public float tintAmount = 0.25f;
        public float vignette = 0.5f;

        static readonly int ID_Amount = Shader.PropertyToID("_Amount");
        static readonly int ID_Strength = Shader.PropertyToID("_Strength");
        static readonly int ID_Falloff = Shader.PropertyToID("_Falloff");
        static readonly int ID_Tint = Shader.PropertyToID("_Tint");
        static readonly int ID_TintAmount = Shader.PropertyToID("_TintAmount");
        static readonly int ID_Vignette = Shader.PropertyToID("_Vignette");

        Coroutine anim;

        void OnEnable() { Instance = this; Push(); }
        void OnDisable() { amount = 0f; Push(); }      // never leave the frame fringed
        void OnValidate() { Push(); }
        void LateUpdate() { Push(); }

        public void Push()
        {
            if (material == null) return;
            material.SetFloat(ID_Amount, amount);
            material.SetFloat(ID_Strength, strength);
            material.SetFloat(ID_Falloff, falloff);
            material.SetColor(ID_Tint, tint);
            material.SetFloat(ID_TintAmount, tintAmount);
            material.SetFloat(ID_Vignette, vignette);
        }

        /// <summary>Snap to full, hold, then ease back out.</summary>
        public void Punch(float peak = 1f, float hold = 0.06f, float fade = 0.35f)
        {
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Routine(peak, hold, fade));
        }

        public void SetAmount(float v)
        {
            if (anim != null) { StopCoroutine(anim); anim = null; }
            amount = Mathf.Clamp01(v);
            Push();
        }

        IEnumerator Routine(float peak, float hold, float fade)
        {
            amount = Mathf.Clamp01(peak);
            Push();

            float t = 0f;
            while (t < hold) { t += Time.unscaledDeltaTime; yield return null; }

            t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fade);
                amount = peak * (1f - k) * (1f - k);   // ease-out quad
                Push();
                yield return null;
            }
            amount = 0f;
            Push();
            anim = null;
        }
    }
}

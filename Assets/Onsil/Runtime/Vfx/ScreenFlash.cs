using System.Collections;
using UnityEngine;

namespace Onsil.Vfx
{
    /// <summary>
    /// Full-screen colour flash and vignette-style darkening, drawn on an overlay
    /// canvas so it needs no render feature and cannot be culled by a camera mask.
    /// </summary>
    /// <remarks>
    /// A screen flash is the cheapest way to sell an impact and the one thing the
    /// threshold shader cannot do, since that pass binarises rather than adds. Kept
    /// separate so a parry can flash without touching the black-and-white beat.
    ///
    /// Unscaled time throughout: the flash must resolve during a hitstop, not freeze
    /// on screen with it.
    /// </remarks>
    public class ScreenFlash : MonoBehaviour
    {
        public static ScreenFlash Instance { get; private set; }

        [Tooltip("Draw order. Above sprites, below the letterbox bars at 500.")]
        public int sortingOrder = 400;

        Canvas canvas;
        UnityEngine.UI.Image image;
        Coroutine running;

        void Awake()
        {
            Instance = this;
            Build();
        }

        void Build()
        {
            if (canvas != null) return;

            var go = new GameObject("ScreenFlashCanvas");
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var img = new GameObject("img", typeof(RectTransform),
                                     typeof(UnityEngine.UI.Image));
            img.transform.SetParent(go.transform, false);
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            image = img.GetComponent<UnityEngine.UI.Image>();
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0f);
        }

        /// <summary>Flash to <paramref name="colour"/> then fade out.</summary>
        public void Flash(Color colour, float peak = 0.55f, float holdTime = 0.02f,
                          float fadeTime = 0.22f)
        {
            Build();
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Routine(colour, peak, holdTime, fadeTime));
        }

        IEnumerator Routine(Color colour, float peak, float holdTime, float fadeTime)
        {
            var c = colour; c.a = peak;
            image.color = c;

            float t = 0f;
            while (t < holdTime) { t += Time.unscaledDeltaTime; yield return null; }

            t = 0f;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                c.a = peak * (1f - Mathf.Clamp01(t / fadeTime));
                image.color = c;
                yield return null;
            }
            c.a = 0f;
            image.color = c;
            running = null;
        }

        /// <summary>Clear immediately.</summary>
        public void Clear()
        {
            if (running != null) { StopCoroutine(running); running = null; }
            if (image != null) image.color = new Color(1f, 1f, 1f, 0f);
        }
    }
}

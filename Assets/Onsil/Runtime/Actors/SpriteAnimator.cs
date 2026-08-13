using System.Collections;
using UnityEngine;

namespace Onsil.Actors
{
    /// <summary>
    /// Owns a character's <see cref="SpriteRenderer"/> and swaps frames directly.
    /// </summary>
    /// <remarks>
    /// Deliberately does NOT use Unity's Animator. Animator.Play re-evaluates the
    /// state machine on every call, which produced visible stutter when a cinematic
    /// drove frames by hand. A plain sprite assignment cannot stutter and lets an
    /// ability hold any single cell for an arbitrary time.
    /// </remarks>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class SpriteAnimator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteClip idleClip;

        [Tooltip("Rate used when no explicit fps is passed")]
        [Range(1f, 60f)] public float defaultFps = 14f;

        Coroutine idleRoutine;

        public SpriteRenderer Body => body;
        public SpriteClip IdleClip => idleClip;

        /// <summary>True while an ability owns the renderer.</summary>
        public bool Suspended { get; private set; }

        void Reset() { body = GetComponent<SpriteRenderer>(); }

        void Awake()
        {
            if (body == null) body = GetComponent<SpriteRenderer>();
            var animator = GetComponent<Animator>();
            if (animator != null) animator.enabled = false;   // we drive the renderer
        }

        void OnEnable() { if (!Suspended) PlayIdle(); }

        // ---------------------------------------------------------------- idle

        public void PlayIdle()
        {
            StopIdle();
            Suspended = false;
            if (idleClip != null && idleClip.FrameCount > 0)
                idleRoutine = StartCoroutine(IdleLoop());
        }

        void StopIdle()
        {
            if (idleRoutine != null) { StopCoroutine(idleRoutine); idleRoutine = null; }
        }

        IEnumerator IdleLoop()
        {
            int i = 0;
            while (true)
            {
                float per = 1f / Mathf.Max(idleClip.fps, 0.01f);
                Show(idleClip, i++ % idleClip.FrameCount);
                float t = 0f;
                while (t < per) { t += Time.deltaTime; yield return null; }
            }
        }

        /// <summary>Hand the renderer to an ability. Call <see cref="Release"/> when done.</summary>
        public void Suspend()
        {
            StopIdle();
            Suspended = true;
        }

        /// <summary>Return control and resume the idle loop.</summary>
        public void Release() => PlayIdle();

        // ------------------------------------------------------------ playback

        public void Show(SpriteClip clip, int cell)
        {
            if (body == null || clip == null) return;
            var s = clip.Frame(cell);
            if (s != null) body.sprite = s;
        }

        /// <summary>Play cells first..last at <paramref name="fps"/>.</summary>
        /// <param name="onStep">0..1 progress, called every frame. Use for movement.</param>
        public IEnumerator Play(SpriteClip clip, int first, int last, float fps = 0f,
                               System.Action<float> onStep = null)
        {
            if (clip == null || clip.FrameCount == 0) yield break;
            if (fps <= 0f) fps = clip.fps > 0f ? clip.fps : defaultFps;

            int count = Mathf.Abs(last - first) + 1;
            float per = 1f / Mathf.Max(fps, 0.01f);
            int dir = last >= first ? 1 : -1;

            for (int i = 0; i < count; i++)
            {
                int cell = first + dir * i;
                float t = 0f;
                while (t < per)
                {
                    Show(clip, cell);                       // re-assert: nothing else may win
                    t += Time.deltaTime;
                    onStep?.Invoke(Mathf.Clamp01((i + t / per) / count));
                    yield return null;
                }
            }
            onStep?.Invoke(1f);
        }

        public IEnumerator Play(SpriteClip clip, string rangeId, float fps = 0f,
                               System.Action<float> onStep = null)
        {
            var r = clip.RangeOrAll(rangeId);
            return Play(clip, r.first, r.last, fps, onStep);
        }

        /// <summary>Hold one cell for a duration, re-asserting it every frame.</summary>
        public IEnumerator Hold(SpriteClip clip, int cell, float seconds,
                                System.Action<float> onStep = null)
        {
            float t = 0f;
            while (t < seconds)
            {
                Show(clip, cell);
                t += Time.deltaTime;
                onStep?.Invoke(Mathf.Clamp01(t / Mathf.Max(seconds, 0.0001f)));
                yield return null;
            }
        }
    }
}

using System;
using System.Collections;
using UnityEngine;
using Onsil.Actors;
using Onsil.Vfx;

namespace Onsil.Combat
{
    /// <summary>
    /// Holds the parry input for a character: the timing window, the animation and
    /// the flash that fires on a success.
    /// </summary>
    /// <remarks>
    /// Timing is expressed in SECONDS BEFORE IMPACT, not in animation frames,
    /// because the incoming attack decides when impact happens. An attack calls
    /// <see cref="OpenWindow"/> with the flight time; this component then judges
    /// any press against that deadline.
    ///
    /// <see cref="parryFps"/> and <see cref="parrySpeed"/> scale the WHOLE parry
    /// animation and are deliberately independent of the timing window, so the
    /// guard can be made snappier or heavier without changing how hard it is.
    ///
    /// Everything here runs on UNSCALED time so the hitstop does not distort the
    /// judgement or freeze the flash that triggered it.
    /// </remarks>
    [RequireComponent(typeof(SpriteAnimator))]
    public class ParryReceiver : MonoBehaviour
    {
        public enum Result { None, Perfect, Good, Late, Hit }

        [Header("clip")]
        public SpriteClip parryClip;                  // nora_parry, 10 cells
        [Tooltip("Playback rate for the whole parry animation. Higher is snappier.")]
        public float parryFps = 18f;
        [Tooltip("Extra multiplier on top of parryFps. 1 normal, 0.5 half speed.")]
        public float parrySpeed = 1f;
        public string guardRange = "guard";
        public string holdRange = "hold";
        public string recoverRange = "recover";

        [Header("timing window (seconds around impact)")]
        [Tooltip("A press this close to impact counts as Perfect.")]
        public float perfectWindow = 0.10f;
        [Tooltip("A press this close counts as Good. Should be larger than perfect.")]
        public float goodWindow = 0.22f;
        [Tooltip("Presses further out than this score nothing.")]
        public float earlyLimit = 0.60f;
        [Tooltip("Guard stays up this long after a press.")]
        public float guardHold = 0.28f;

        [Header("clash fx")]
        [Tooltip("Thin lens spark drawn at the contact point. vfx_clash.")]
        public Sprite clashSprite;
        [Tooltip("Expanding hoop. vfx_ring.")]
        public Sprite ringSprite;
        [Tooltip("Debris slivers. vfx_shard.")]
        public Sprite shardSprite;
        [Tooltip("Where the clash happens, relative to the sprite pivot.")]
        public Vector2 flashOffset = new Vector2(0.35f, 0.55f);
        public float flashSize = 0.9f;
        public float flashDuration = 0.3f;
        [Tooltip("Crossed sparks at the contact point.")]
        public int clashSparks = 3;
        [Tooltip("Slivers thrown off the block.")]
        public int shardCount = 10;
        [Tooltip("Half-angle the shards spray into, degrees.")]
        public float shardSpread = 55f;
        public float shardReach = 1.4f;
        public Color perfectColor = new Color(1f, 0.95f, 0.55f, 1f);
        public Color goodColor = new Color(0.55f, 0.9f, 1f, 1f);
        public int flashSortingOrder = 60;

        [Header("screen fx")]
        [Tooltip("Chromatic aberration strength on a Perfect. 0 disables it.")]
        public float perfectChroma = 1f;
        public float goodChroma = 0.5f;
        [Tooltip("Seconds the fringe holds at full before easing out.")]
        public float chromaHold = 0.07f;
        public float chromaFade = 0.4f;
        [Tooltip("Camera snap-zoom on a Perfect. 1 disables it.")]
        public float perfectZoom = 0.8f;
        public float goodZoom = 0.92f;
        [Tooltip("Camera roll, degrees. Sign is randomised each parry.")]
        public float perfectTilt = 5f;
        public float goodTilt = 2f;
        public float zoomIn = 0.05f;
        public float zoomOut = 0.45f;

        [Header("feedback")]
        public float shakeAmount = 0.22f;
        public float shakeTime = 0.16f;
        [Tooltip("Time scale on a Perfect parry. 1 disables the hitstop.")]
        public float perfectHitstopScale = 0.05f;
        [Tooltip("Real seconds the hitstop lasts.")]
        public float perfectHitstopTime = 0.22f;
        public float goodHitstopScale = 0.25f;
        public float goodHitstopTime = 0.10f;

        SpriteAnimator anim;
        CameraShaker shaker;
        CameraPunch punch;

        float impactAt = -1f;      // unscaled time the hit lands, -1 when idle
        bool windowOpen;
        bool consumed;
        float guardUntil = -1f;

        /// <summary>Fires with the judged result the moment a press is resolved.</summary>
        public event Action<Result> Judged;

        public bool WindowOpen => windowOpen;
        public bool GuardUp => Time.unscaledTime <= guardUntil;

        /// <summary>Seconds until impact, or -1 when nothing is incoming.</summary>
        public float TimeToImpact =>
            impactAt < 0f ? -1f : Mathf.Max(0f, impactAt - Time.unscaledTime);

        void Awake()
        {
            anim = GetComponent<SpriteAnimator>();
            if (Camera.main != null)
            {
                shaker = Camera.main.GetComponent<CameraShaker>();
                punch = Camera.main.GetComponent<CameraPunch>();
            }
        }

        /// <summary>Called by an incoming attack. <paramref name="delay"/> is how
        /// long until the round arrives.</summary>
        public void OpenWindow(float delay)
        {
            impactAt = Time.unscaledTime + delay;
            windowOpen = true;
            consumed = false;
        }

        /// <summary>Called by the attack once the hit resolves.</summary>
        public void CloseWindow()
        {
            windowOpen = false;
            impactAt = -1f;
        }

        /// <summary>Judge a press. Returns what it scored.</summary>
        public Result Press()
        {
            if (consumed) return Result.None;

            Result r;
            if (!windowOpen)
            {
                r = Result.None;                       // nothing incoming
            }
            else
            {
                float dt = Mathf.Abs(impactAt - Time.unscaledTime);
                if (dt <= perfectWindow) r = Result.Perfect;
                else if (dt <= goodWindow) r = Result.Good;
                else if (dt <= earlyLimit) r = Result.Late;
                else r = Result.None;
            }

            guardUntil = Time.unscaledTime + guardHold;
            if (r == Result.Perfect || r == Result.Good) consumed = true;

            StartCoroutine(PlayGuard(r));
            if (Judged != null) Judged(r);
            return r;
        }

        /// <summary>True when the guard is up and the press already scored.</summary>
        public bool Blocks()
        {
            return consumed && GuardUp;
        }

        IEnumerator PlayGuard(Result r)
        {
            if (parryClip == null || anim == null) yield break;

            float fps = Mathf.Max(0.01f, parryFps * Mathf.Max(0.01f, parrySpeed));
            bool scored = r == Result.Perfect || r == Result.Good;
            bool perfect = r == Result.Perfect;

            anim.Suspend();

            // The screen reacts on the SAME frame as the press. Waiting for the
            // guard animation to finish first made the parry feel unresponsive,
            // so the beat order is: judge -> slow -> zoom -> fx, all at once,
            // and the animation then plays out inside the slowed time.
            if (scored)
            {
                float stopScale = perfect ? perfectHitstopScale : goodHitstopScale;
                float stopTime = perfect ? perfectHitstopTime : goodHitstopTime;

                if (stopScale < 1f && stopTime > 0f) Time.timeScale = stopScale;

                if (punch != null)
                    punch.PunchAt(perfect ? perfectZoom : goodZoom,
                                  zoomIn, zoomOut,
                                  perfect ? perfectTilt : goodTilt,
                                  // pivot on the clash, not the camera centre -
                                  // otherwise the zoom always shoves the frame the
                                  // same way and swamps the randomised roll
                                  transform.position + (Vector3)flashOffset);

                if (ChromaticPunch.Instance != null)
                {
                    float peak = perfect ? perfectChroma : goodChroma;
                    if (peak > 0f) ChromaticPunch.Instance.Punch(peak, chromaHold, chromaFade);
                }

                Clash(perfect ? perfectColor : goodColor, perfect);

                if (shaker != null)
                    shaker.Shake(shakeTime, shakeAmount * (perfect ? 1f : 0.6f));

                // guard snaps up while everything is already crawling
                yield return anim.Play(parryClip, guardRange, fps);

                if (stopScale < 1f && stopTime > 0f)
                {
                    float held = 0f;
                    while (held < stopTime) { held += Time.unscaledDeltaTime; yield return null; }
                    Time.timeScale = 1f;
                }
            }
            else
            {
                yield return anim.Play(parryClip, guardRange, fps);
            }

            var hold = parryClip.RangeOrAll(holdRange);
            float remain = Mathf.Max(0f, guardUntil - Time.unscaledTime);
            yield return anim.Hold(parryClip, hold.last, remain);

            yield return anim.Play(parryClip, recoverRange, fps);
            anim.Release();
        }

        /// <summary>
        /// The For Honor read: crossed lens sparks at the contact point, a thin
        /// hoop snapping outward, and slivers thrown back along the block. No
        /// radial starburst - steel on steel is directional.
        /// </summary>
        void Clash(Color tint, bool perfect)
        {
            if (clashSprite == null && ringSprite == null && shardSprite == null) return;
            StartCoroutine(ClashRoutine(tint, perfect));
        }

        IEnumerator ClashRoutine(Color tint, bool perfect)
        {
            var pivot = new GameObject("ParryClash");
            pivot.transform.position =
                transform.position + (Vector3)flashOffset + Vector3.back * 0.3f;

            float scale = flashSize * (perfect ? 1f : 0.72f);

            // --- crossed lens sparks, the actual point of contact ---
            int sparkN = Mathf.Max(0, clashSparks);
            var sparks = new Transform[sparkN];
            var sparkLen = new float[sparkN];
            for (int i = 0; i < sparkN; i++)
            {
                var g = new GameObject("clash" + i);
                g.transform.SetParent(pivot.transform, false);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = clashSprite != null ? clashSprite : shardSprite;
                sr.sortingOrder = flashSortingOrder + 2;
                sr.color = Color.Lerp(tint, Color.white, 0.5f);
                // fan them across the block angle rather than all the way round
                float ang = UnityEngine.Random.Range(-70f, 70f);
                g.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                sparkLen[i] = UnityEngine.Random.Range(0.85f, 1.35f);
                sparks[i] = g.transform;
            }

            // --- hoop ---
            SpriteRenderer ring = null;
            if (ringSprite != null)
            {
                var g = new GameObject("ring");
                g.transform.SetParent(pivot.transform, false);
                ring = g.AddComponent<SpriteRenderer>();
                ring.sprite = ringSprite;
                ring.sortingOrder = flashSortingOrder;
                ring.color = tint;
            }

            // --- slivers sprayed back along the guard ---
            int shardN = Mathf.Max(0, shardCount);
            var shards = new Transform[shardN];
            var dirs = new Vector2[shardN];
            var reach = new float[shardN];
            for (int i = 0; i < shardN; i++)
            {
                var g = new GameObject("shard" + i);
                g.transform.SetParent(pivot.transform, false);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = shardSprite != null ? shardSprite : clashSprite;
                sr.sortingOrder = flashSortingOrder + 1;
                sr.color = Color.Lerp(tint, Color.white, UnityEngine.Random.value * 0.7f);
                float ang = UnityEngine.Random.Range(-shardSpread, shardSpread) * Mathf.Deg2Rad;
                dirs[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                reach[i] = UnityEngine.Random.Range(0.5f, 1f) * shardReach * scale;
                g.transform.localRotation = Quaternion.Euler(0f, 0f, ang * Mathf.Rad2Deg);
                shards[i] = g.transform;
            }

            float t = 0f;
            while (t < flashDuration)
            {
                t += Time.unscaledDeltaTime;            // must animate during hitstop
                float k = Mathf.Clamp01(t / flashDuration);

                // sparks: snap to full length instantly, then thin out
                float sparkK = Mathf.Clamp01(k / 0.18f);
                for (int i = 0; i < sparkN; i++)
                {
                    float len = scale * sparkLen[i] * Mathf.Lerp(0.25f, 2.1f, sparkK);
                    float thin = Mathf.Lerp(0.5f, 0.05f, k);
                    sparks[i].localScale = new Vector3(len, scale * thin, 1f);
                    var sr = sparks[i].GetComponent<SpriteRenderer>();
                    var c = sr.color; c.a = Mathf.Pow(1f - k, 2.2f); sr.color = c;
                }

                // ring: hard snap out, gone quickly
                if (ring != null)
                {
                    float rk = Mathf.Clamp01(k / 0.45f);
                    float s = scale * Mathf.Lerp(0.15f, 2.6f, Mathf.Sqrt(rk));
                    ring.transform.localScale = new Vector3(s, s * 0.85f, 1f);
                    var c = ring.color; c.a = Mathf.Pow(1f - rk, 1.8f) * 0.9f; ring.color = c;
                }

                // shards: fly out, decelerate, stretch
                float ease = Mathf.Pow(k, 0.45f);
                for (int i = 0; i < shardN; i++)
                {
                    shards[i].localPosition = (Vector3)(dirs[i] * reach[i] * ease);
                    shards[i].localScale =
                        new Vector3(scale * Mathf.Lerp(0.5f, 1.5f, ease),
                                    scale * Mathf.Lerp(0.55f, 0.12f, k), 1f);
                    var sr = shards[i].GetComponent<SpriteRenderer>();
                    var c = sr.color; c.a = Mathf.Pow(1f - k, 2f); sr.color = c;
                }
                yield return null;
            }
            Destroy(pivot);
        }
    }
}

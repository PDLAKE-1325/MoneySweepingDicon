using System;
using System.Collections;
using UnityEngine;
using Onsil.Abilities;
using Onsil.Actors;
using Onsil.Vfx;

namespace Onsil.Combat
{
    /// <summary>
    /// A melee attack whose parry window is derived from its own clip, so the
    /// sword and the judge can never drift apart.
    /// </summary>
    /// <remarks>
    /// This ability replaces the DummyEnemy + EnemyAttacker pair: the mob itself
    /// is now the threat. It resolves its defender from <c>Ctx.Target</c> at cast
    /// time, opens the window with <see cref="TimeToImpact"/> - a number COMPUTED
    /// from the clip's cell counts - and closes it on the fire cell. Retiming the
    /// animation retunes the parry automatically; there is no telegraph constant
    /// to forget.
    ///
    /// Parry success is read from <see cref="ParryReceiver.Judged"/>, not from
    /// GuardUp at landing. A perfect parry triggers a hitstop that stretches this
    /// clip's real-time playback; polling the guard at the (delayed) landing would
    /// mis-score a perfect press as a hit. The event cannot.
    ///
    /// Two modes share one class: a ground advance (walk cells while closing the
    /// gap) and a leap (crouch / rise / aim / fall arc onto the target). Which one
    /// plays is the <see cref="leap"/> flag; the phase names default to the ranges
    /// already authored on the Morser clips.
    ///
    /// Fields are unclamped; sane ranges live in the tooltips.
    /// </remarks>
    public class MeleeStrikeAbility : Ability
    {
        [Header("clip")]
        public SpriteClip clip;
        [Tooltip("0 uses the clip's own fps.")]
        public float clipFps = 0f;

        [Header("ranges - ground advance")]
        public string walkRange = "walk";
        public string windupRange = "windup";

        [Header("ranges - leap")]
        [Tooltip("On: crouch/rise/aim/fall arc. Off: walk in, wind up.")]
        public bool leap = false;
        public string crouchRange = "crouch";
        public string riseRange = "rise";
        public string aimRange = "aim";
        public string fallRange = "fall";
        [Tooltip("Apex height of the leap in world units. Typical 0.8-1.6.")]
        public float arcHeight = 1.1f;

        [Header("ranges - shared")]
        public string fireRange = "fire";
        public string recoverRange = "recover";

        [Header("motion")]
        [Tooltip("Gap kept between the two pivots at the strike.")]
        public float stopGap = 1.0f;
        [Tooltip("Extra settle on the last windup cell, ground mode only. " +
                 "Included in the parry window math.")]
        public float windupHold = 0.12f;
        [Tooltip("Held on the fire cell so the pose registers.")]
        public float strikePause = 0.10f;

        [Header("resolve - parried")]
        [Tooltip("How far the block shoves the mob back.")]
        public float parriedPushback = 0.35f;
        public float parriedPause = 0.22f;

        [Header("resolve - hit")]
        public float hitShakeTime = 0.20f;
        public float hitShakeAmount = 0.30f;
        [Tooltip("Impact flare drawn on the defender. Empty disables it.")]
        public Sprite hitSprite;
        [Tooltip("Where the blow lands, relative to the DEFENDER pivot. " +
                 "X flips to the attacker's side automatically.")]
        public Vector2 hitOffset = new Vector2(0.30f, 0.55f);
        public int hitSortingOrder = 42;

        /// <summary>Seconds from cast to the blow, derived from the clip.</summary>
        public float TimeToImpact
        {
            get
            {
                if (clip == null) return 0f;
                float fps = clipFps > 0f ? clipFps : Mathf.Max(clip.fps, 0.01f);
                int cells = leap
                    ? clip.RangeOrAll(crouchRange).Length + clip.RangeOrAll(riseRange).Length
                      + clip.RangeOrAll(aimRange).Length + clip.RangeOrAll(fallRange).Length
                    : clip.RangeOrAll(walkRange).Length + clip.RangeOrAll(windupRange).Length;
                return cells / fps + (leap ? 0f : windupHold);
            }
        }

        /// <summary>Raised with the window length the moment the threat starts.</summary>
        public event Action<float> WindowOpened;
        /// <summary>Raised when the blow resolves. True when the defender parried.
        /// Hook damage here.</summary>
        public event Action<bool> Resolved;

        void Reset() { abilityId = "mob_attack"; consumesMark = false; appliesMark = false; }

        public override bool CanCast() => clip != null;

        public override IEnumerator Run()
        {
            var anim = Ctx.Animator;
            float fps = clipFps > 0f ? clipFps : Mathf.Max(clip.fps, 0.01f);

            Vector3 home = Ctx.Self.position;
            float dir = Ctx.Target != null
                      ? Mathf.Sign(Ctx.Target.position.x - home.x)
                      : -1f;
            Vector3 strikePos = Ctx.Target != null
                ? new Vector3(Ctx.Target.position.x - dir * stopGap, home.y, home.z)
                : home + new Vector3(dir * 1.5f, 0f, 0f);

            // ---- the threat is announced with the clip-derived deadline ----
            var defender = Ctx.Target != null
                ? Ctx.Target.GetComponent<ParryReceiver>() : null;
            bool parried = false;
            Action<ParryReceiver.Result> onJudged = r =>
            {
                if (r == ParryReceiver.Result.Perfect || r == ParryReceiver.Result.Good)
                    parried = true;
            };
            if (defender != null)
            {
                defender.Judged += onJudged;
                defender.OpenWindow(TimeToImpact);
            }
            WindowOpened?.Invoke(TimeToImpact);

            // ---- approach ----
            if (leap)
            {
                Vector3 apex = Vector3.Lerp(home, strikePos, 0.6f) + Vector3.up * arcHeight;
                yield return anim.Play(clip, crouchRange, fps);
                yield return anim.Play(clip, riseRange, fps, k =>
                    Ctx.Self.position = Vector3.Lerp(home, apex, 1f - (1f - k) * (1f - k)));
                yield return anim.Play(clip, aimRange, fps);
                yield return anim.Play(clip, fallRange, fps, k =>
                    Ctx.Self.position = Vector3.Lerp(apex, strikePos, k * k));
            }
            else
            {
                yield return anim.Play(clip, walkRange, fps, k =>
                    Ctx.Self.position = Vector3.Lerp(home, strikePos, k));
                yield return anim.Play(clip, windupRange, fps);
                yield return anim.Hold(clip, clip.RangeOrAll(windupRange).last, windupHold);
            }
            Ctx.Self.position = strikePos;

            // ---- the blow ----
            var fire = clip.RangeOrAll(fireRange);
            anim.Show(clip, fire.first);
            if (defender != null)
            {
                defender.CloseWindow();
                defender.Judged -= onJudged;
            }
            Resolved?.Invoke(parried);

            if (parried)
            {
                // the block shoves the mob back; its own flash lives on the defender
                Vector3 shoved = strikePos - new Vector3(dir * parriedPushback, 0f, 0f);
                float t = 0f;
                while (t < parriedPause)
                {
                    t += Time.deltaTime;
                    float k = Mathf.Clamp01(t / parriedPause);
                    Ctx.Self.position = Vector3.Lerp(strikePos, shoved, 1f - (1f - k) * (1f - k));
                    anim.Show(clip, fire.first);
                    yield return null;
                }
                strikePos = shoved;
            }
            else
            {
                if (Ctx.Shaker != null) Ctx.Shaker.Shake(hitShakeTime, hitShakeAmount);
                if (hitSprite != null && Ctx.Target != null)
                    StartCoroutine(HitFlare(Ctx.Target.position
                        + new Vector3(hitOffset.x * -dir, hitOffset.y, -0.3f)));
                yield return anim.Hold(clip, fire.first, strikePause);
            }

            // ---- recover while walking home ----
            var rec = clip.RangeOrAll(recoverRange);
            Vector3 from = Ctx.Self.position;
            yield return anim.Play(clip, rec.first, rec.last, fps, k =>
                Ctx.Self.position = Vector3.Lerp(from, home, k * k * (3f - 2f * k)));
            Ctx.Self.position = home;
        }

        /// Same read as the old EnemyAttacker impact: a flare that snaps out and
        /// fades. Unscaled so a late-press hitstop cannot freeze it on screen.
        IEnumerator HitFlare(Vector3 at)
        {
            var go = new GameObject("MeleeHit");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = hitSprite;
            sr.sortingOrder = hitSortingOrder;
            sr.color = new Color(1f, 0.62f, 0.35f, 1f);
            go.transform.position = at;

            float t = 0f;
            const float dur = 0.26f;
            while (go != null && t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = Mathf.Lerp(0.35f, 2.1f, Mathf.Sqrt(k));
                go.transform.localScale = new Vector3(s, s, 1f);
                var c = sr.color; c.a = 1f - k; sr.color = c;
                yield return null;
            }
            if (go != null) Destroy(go);
        }
    }
}

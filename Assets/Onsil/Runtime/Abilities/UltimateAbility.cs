using System.Collections;
using UnityEngine;
using Onsil.Actors;
using Onsil.Vfx;

namespace Onsil.Abilities
{
    /// <summary>
    /// Ultimate - drop to a knee, fire, cut to the round's flight, then a
    /// black-and-white slow-motion impact.
    /// </summary>
    /// <remarks>
    /// Every knob for the whole performance lives here. On cast the values are
    /// pushed into <see cref="CinematicDirector"/>, <see cref="ImpactJet"/> and
    /// <see cref="BadAppleController"/>, so tuning happens in ONE inspector
    /// instead of three. Turn an override group off to leave that component's
    /// own settings alone.
    ///
    /// Fields are deliberately unclamped: the sane ranges are written in the
    /// tooltips, but extreme values are often exactly what a stylised hit needs.
    /// </remarks>
    public class UltimateAbility : Ability
    {
        // ------------------------------------------------------------- actors
        [Header("actors")]
        public SpriteClip clip;                 // nora_kneel, 13 cells
        public ImpactJet jet;

        // ------------------------------------------------------------- stance
        [Header("stance timing")]
        [Tooltip("Playback rate for the kneel cells. Typical 8-18.")]
        public float clipFps = 12f;
        [Tooltip("Held on the last pre-fire cell while she settles. Typical 0.3-0.8.")]
        public float aimHold = 0.5f;
        [Tooltip("Held ON the fire cell before the cut. Typical 0.03-0.15.")]
        public float firePause = 0.05f;
        [Tooltip("Rate for the recoil + stand-up run. 0 uses clipFps.")]
        public float recoverFps = 0f;

        // ------------------------------------------------------------ cutaway
        [Header("cutaway  (overrides the director)")]
        public bool overrideCutaway = true;
        [Tooltip("Round flight time. Typical 0.5-1.5.")]
        public float flyTime = 0.8f;
        [Tooltip("1 = the round meets the target exactly at the end of the flight. " +
                 ">1 punches through, <1 stops short.")]
        public float tracerReach = 1f;
        [Tooltip("Negative fires the blast early, positive delays it (seconds).")]
        public float impactOffset = 0f;
        [Tooltip("Where the target starts, in cut-camera units.")]
        public float targetStartX = 7f;
        [Tooltip("Where it stops. Camera half-width is ~2.2, so -0.75 is a third in " +
                 "from the left.")]
        public float targetStopX = -0.75f;
        [Tooltip(">1 holds it back, then it rushes in at the end.")]
        public float approachCurve = 2.2f;
        public float targetEndScale = 1.5f;
        public int buildingCount = 16;
        public float buildingSpeed = 9f;
        public float cutOrthoSize = 1.25f;
        [Tooltip("Bar height as a fraction of the screen. 0 disables the bars.")]
        public float letterbox = 0.14f;

        // ------------------------------------------------------ black & white
        [Header("black and white  (overrides the controller)")]
        public bool overrideBadApple = true;
        [Tooltip("Luminance split. MEASURED: cut background 0.035, buildings ~0.006, " +
                 "target 0.10-0.70. At or above the target's darkest pixel it vanishes " +
                 "into the background - that is what 0.5 (the shader default) does.")]
        public float bwThreshold = 0.07f;
        [Tooltip("Edge softness around the split. Small keeps it hard-edged.")]
        public float bwSoftness = 0.02f;
        [Tooltip("1 swaps bright and dark.")]
        public float bwInvert = 0f;
        public Color bwBright = Color.white;
        public Color bwDark = Color.black;
        public float bwFadeIn = 0.04f;
        public float bwFadeOut = 0.4f;

        // ----------------------------------------------------------- the jet
        [Header("impact jet  (overrides the jet)")]
        public bool overrideJet = true;
        [Tooltip("Overall size multiplier.")]
        public float jetVolume = 3.5f;
        [Tooltip("Length relative to width.")]
        public float jetLength = 1.4f;
        [Tooltip("Where along the cone the lobe is widest. 0 = at the hit, 1 = at the tip.")]
        public float jetBulgePosition = 0.5f;
        public float jetBulgeStrength = 1f;
        [Tooltip("Half-angle of the debris cone, degrees.")]
        public float jetSpread = 26f;
        public float jetReach = 3f;
        public int jetDebris = 40;
        public float jetLife = 1.9f;
        [Tooltip("Fraction of its life held fully opaque before fading. " +
                 "1 means it never fades on its own.")]
        public float jetHold = 0.7f;
        [Tooltip("Cut camera pulls back by this factor so the blast fits.")]
        public float jetZoomOut = 2.2f;

        // ------------------------------------------------------ slow motion
        [Header("slow motion")]
        public bool overrideSlowMotion = true;
        [Tooltip("Time scale during the beat. 0.12 is a heavy hit, 1 disables it.")]
        public float slowScale = 0.12f;
        [Tooltip("Real seconds the beat lasts.")]
        public float slowSeconds = 2f;
        [Tooltip("Pause after the cut before she stands up.")]
        public float returnPause = 0.25f;

        // ------------------------------------------------------------- shake
        [Header("shake")]
        public float fireShakeAmount = 0.18f;
        public float fireShakeTime = 0.18f;
        public float impactShakeAmount = 0.43f;
        public float impactShakeTime = 0.4f;

        void Reset() { abilityId = "ultimate"; consumesMark = true; appliesMark = false; }

        public override bool CanCast()
        {
            return clip != null && Ctx.Director != null;
        }

        /// <summary>Copy the inspector values onto the shared components.</summary>
        void PushSettings()
        {
            var dir = Ctx.Director;
            if (overrideCutaway && dir != null)
            {
                dir.flyTime = flyTime;
                dir.tracerReach = tracerReach;
                dir.impactOffset = impactOffset;
                dir.targetStartX = targetStartX;
                dir.targetStopX = targetStopX;
                dir.approachCurve = approachCurve;
                dir.targetEndScale = targetEndScale;
                dir.buildingCount = buildingCount;
                dir.buildingSpeed = buildingSpeed;
                dir.cutOrthoSize = cutOrthoSize;
                dir.letterbox = letterbox;
            }
            if (overrideSlowMotion && dir != null)
            {
                dir.slowScale = slowScale;
                dir.slowSeconds = slowSeconds;
                dir.returnPause = returnPause;
            }

            var bad = BadAppleController.Instance;
            if (overrideBadApple && bad != null)
            {
                bad.threshold = bwThreshold;
                bad.softness = bwSoftness;
                bad.invert = bwInvert;
                bad.bright = bwBright;
                bad.dark = bwDark;
                bad.Push();
            }

            if (overrideJet && jet != null)
            {
                jet.volume = jetVolume;
                jet.length = jetLength;
                jet.bulgePosition = jetBulgePosition;
                jet.bulgeStrength = jetBulgeStrength;
                jet.spread = jetSpread;
                jet.reach = jetReach;
                jet.debrisCount = jetDebris;
                jet.life = jetLife;
                jet.hold = jetHold;
                jet.zoomOut = jetZoomOut;
            }
        }

        public override IEnumerator Run()
        {
            PushSettings();

            var anim = Ctx.Animator;
            var dir = Ctx.Director;

            // cells 0-1 drop, 2-5 settle. Hold on the LAST settle cell, not the fire
            // cell - cell 6 is the firing pose and holding it looks like she
            // already shot.
            yield return anim.Play(clip, "drop", clipFps);
            yield return anim.Play(clip, "settle", clipFps);
            yield return anim.Hold(clip, clip.RangeOrAll("settle").last, aimHold);

            // THE SHOT
            var fire = clip.RangeOrAll("fire");
            anim.Show(clip, fire.first);
            if (Ctx.Muzzle != null) Ctx.Muzzle.Fire(MuzzleRig.Stance.Kneeling);
            if (Ctx.Shaker != null) Ctx.Shaker.Shake(fireShakeTime, fireShakeAmount);
            yield return anim.Hold(clip, fire.first, firePause);

            // ---------------- cut away ----------------
            dir.Build(Ctx.BattleCamera);
            yield return dir.RunRound();

            var bad = BadAppleController.Instance;
            if (bad != null) bad.FadeIn(bwFadeIn);

            var cutShaker = dir.CutCamera != null
                ? dir.CutCamera.GetComponent<CameraShaker>() : null;
            if (cutShaker != null) cutShaker.Shake(impactShakeTime, impactShakeAmount);

            if (jet != null)
                jet.Spawn(dir.ImpactPoint, dir.CutCamera.transform.parent,
                          dir.cutawayLayer, dir.CutCamera);

            yield return dir.RunSlowMotion();          // sets and restores timeScale

            if (bad != null) bad.FadeOut(bwFadeOut);

            dir.Teardown();
            yield return new WaitForSeconds(dir.returnPause);
            dir.SetLetterbox(false);

            // safety: a cancelled fade must never leave the screen monochrome
            if (bad != null) bad.SetAmount(0f);

            // cells 7-12 in ONE continuous run. A hold in the middle reads as a freeze.
            var rec = clip.RangeOrAll("recoil");
            var end = clip.RangeOrAll("recover");
            yield return anim.Play(clip, rec.first, end.last,
                                   recoverFps > 0f ? recoverFps : clipFps);
        }
    }
}

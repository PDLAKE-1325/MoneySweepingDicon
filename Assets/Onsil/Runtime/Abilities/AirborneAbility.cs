using System.Collections;
using UnityEngine;
using Onsil.Actors;
using Onsil.Vfx;

namespace Onsil.Abilities
{
    /// <summary>
    /// Shared jetpack flight used by the scan and the airborne shot.
    /// </summary>
    /// <remarks>
    /// The climb is deliberately vertical: the jumpshot art is drawn as a straight
    /// ascent, so <see cref="jumpBack"/> must stay 0 unless the art changes.
    ///
    /// <see cref="liftDelay"/> gates BOTH the body lift and the thruster ignition.
    /// Cells 5-7 are still a deep compression (measured in Blender: top_z flat at
    /// 1.011), so nothing may happen during them - the plume lighting up before the
    /// feet leave the ground reads as a mistake.
    /// </remarks>
    public abstract class AirborneAbility : Ability
    {
        [Header("clip")]
        public SpriteClip clip;                           // nora_jumpshot, 36 cells
        public float clipFps = 18f;

        [Header("flight")]
        public float jumpHeight = 2.4f;
        [Tooltip("Keep 0 - the art is a vertical climb")]
        public float jumpBack = 0f;
        [Tooltip("Fraction of the rise segment spent compressed on the ground. " +
                 "Gates BOTH the body lift and the thruster ignition.")]
        public float liftDelay = 0.45f;
        [Tooltip("Higher = snappier burst off the ground (logarithmic).")]
        public float riseSharpness = 9f;
        [Tooltip("Degrees the body tilts toward the target while airborne.")]
        public float airLean = 12f;

        protected Vector3 Home { get; private set; }
        protected Vector3 Apex { get; private set; }

        /// Logarithmic rise: hard burst off the ground, easing into the hover.
        protected static float LogRise(float k, float sharp)
        {
            sharp = Mathf.Max(sharp, 0.01f);
            return Mathf.Log(1f + sharp * Mathf.Clamp01(k)) / Mathf.Log(1f + sharp);
        }

        protected void Lean(float amount)
        {
            var body = Ctx.Animator.Body;
            if (body != null)
                body.transform.localRotation = Quaternion.Euler(0f, 0f, -airLean * amount);
        }

        protected void Burn(float power)
        {
            if (Ctx.Thruster != null) Ctx.Thruster.SetPower(power);
        }

        /// <summary>Crouch, then climb to the apex. Body and plume start together.</summary>
        protected IEnumerator TakeOff()
        {
            Home = Ctx.Self.position;
            Apex = Home + new Vector3(-jumpBack, jumpHeight, 0f);

            yield return Ctx.Animator.Play(clip, "crouch", clipFps + 4f, _ => Burn(0f));

            yield return Ctx.Animator.Play(clip, "rise", clipFps + 4f, k =>
            {
                float kk = Mathf.InverseLerp(liftDelay, 1f, k);
                Ctx.Self.position = Vector3.Lerp(Home, Apex, LogRise(kk, riseSharpness));
                Burn(kk <= 0f ? 0f : Mathf.Min(1f, kk * 4f));
                Lean(kk);
            });
            Ctx.Self.position = Apex;
            Lean(1f);
        }

        /// <summary>Fall back to the start and land.</summary>
        protected IEnumerator Land()
        {
            yield return Ctx.Animator.Play(clip, "descend", clipFps + 4f, k =>
            {
                Ctx.Self.position = Vector3.Lerp(Apex, Home, k * k);
                Burn(0.25f * (1f - k));
                Lean(1f - k);
            });
            Ctx.Self.position = Home;
            Lean(0f);
            Burn(0f);
            if (Ctx.Shaker != null) Ctx.Shaker.Shake(0.16f);
            yield return Ctx.Animator.Play(clip, "land", clipFps + 4f);
            Ctx.Self.position = Home;
        }
    }
}

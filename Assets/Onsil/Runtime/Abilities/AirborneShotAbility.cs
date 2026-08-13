using System.Collections;
using UnityEngine;
using Onsil.Vfx;

namespace Onsil.Abilities
{
    /// <summary>
    /// Skill 2 - jetpack up and fire. Spends the mark for bonus damage.
    /// </summary>
    /// <remarks>
    /// The shot lands on the clip's dedicated fire cell (17 in nora_jumpshot,
    /// source frame 27, inside the 26 to 28 recoil snap). Firing on the last aim
    /// cell instead puts the muzzle flash several frames ahead of the recoil and
    /// reads as desynced.
    /// </remarks>
    public class AirborneShotAbility : AirborneAbility
    {
        [Header("timing")]
        [Tooltip("Hang at the apex before the aim run.")]
        public float hoverTime = 0.10f;
        [Tooltip("Settle on the last pre-fire cell.")]
        public float aimHold = 0.28f;
        [Tooltip("Held ON the fire cell.")]
        public float firePause = 0.12f;

        void Reset() { abilityId = "airshot"; consumesMark = true; appliesMark = false; }

        public override IEnumerator Run()
        {
            yield return TakeOff();

            var anim = Ctx.Animator;
            var aim = clip.RangeOrAll("aim");
            var fire = clip.RangeOrAll("fire");

            yield return anim.Hold(clip, aim.first, hoverTime, _ => Burn(0.45f));
            yield return anim.Play(clip, aim.first, fire.first - 1, clipFps + 2f, _ => Burn(0.45f));
            yield return anim.Hold(clip, fire.first - 1, aimHold, _ => Burn(0.45f));

            // THE SHOT
            anim.Show(clip, fire.first);
            if (Ctx.Muzzle != null) Ctx.Muzzle.Fire(MuzzleRig.Stance.Airborne);
            if (Ctx.Shaker != null) Ctx.Shaker.Shake(0.18f);
            yield return anim.Hold(clip, fire.first, firePause, _ => Burn(0.5f));

            yield return anim.Play(clip, "recoil", clipFps + 4f, _ => Burn(0.45f));
            yield return Land();
        }
    }
}

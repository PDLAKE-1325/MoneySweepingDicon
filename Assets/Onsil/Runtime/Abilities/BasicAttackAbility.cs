using System.Collections;
using UnityEngine;
using Onsil.Actors;
using Onsil.Vfx;

namespace Onsil.Abilities
{
    /// <summary>Standing aimed shot. Spends the lock-on mark if one is present.</summary>
    public class BasicAttackAbility : Ability
    {
        public SpriteClip clip;                 // nora_fire, 13 cells
        [Tooltip("Settle before the trigger.")]
        public float aimHold = 0.32f;
        [Tooltip("Held ON the fire cell.")]
        public float firePause = 0.08f;
        public float clipFps = 14f;

        void Reset() { abilityId = "basic"; consumesMark = true; appliesMark = false; }

        public override IEnumerator Run()
        {
            var anim = Ctx.Animator;

            // cells 0-5 : raise and settle on target
            yield return anim.Play(clip, "aim", clipFps);
            yield return anim.Hold(clip, clip.RangeOrAll("aim").last, aimHold);

            // cell 6 : THE SHOT
            var fire = clip.RangeOrAll("fire");
            anim.Show(clip, fire.first);
            if (Ctx.Muzzle != null) Ctx.Muzzle.Fire(MuzzleRig.Stance.Standing);
            if (Ctx.Shaker != null) Ctx.Shaker.Shake(0.16f);
            yield return anim.Hold(clip, fire.first, firePause);

            // cells 7-12 : recoil and recover
            yield return anim.Play(clip, "recover", clipFps);
        }
    }
}

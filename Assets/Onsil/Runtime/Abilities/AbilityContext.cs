using UnityEngine;
using Onsil.Actors;

// Explicit aliases: the prototype in Assets/asd also declares these type names in
// the global namespace. Without these the compiler binds to the old versions.
using LockOnReticle = Onsil.Vfx.LockOnReticle;
using MuzzleRig = Onsil.Vfx.MuzzleRig;
using ThrusterRig = Onsil.Vfx.ThrusterRig;
using CinematicDirector = Onsil.Vfx.CinematicDirector;
using CameraShaker = Onsil.Vfx.CameraShaker;

namespace Onsil.Abilities
{
    /// <summary>
    /// Everything an <see cref="Ability"/> is allowed to touch, handed to it by the
    /// runner. Keeps abilities from calling FindObjectOfType or reaching into the scene.
    /// </summary>
    public class AbilityContext
    {
        public Transform Self { get; }
        public SpriteAnimator Animator { get; }
        public Transform Target { get; }
        public Camera BattleCamera { get; }
        public MuzzleRig Muzzle { get; }
        public ThrusterRig Thruster { get; }
        public LockOnReticle Reticle { get; }
        public CinematicDirector Director { get; }
        public CameraShaker Shaker { get; }

        public AbilityContext(Transform self, SpriteAnimator animator, Transform target,
                              Camera battleCamera, MuzzleRig muzzle, ThrusterRig thruster,
                              LockOnReticle reticle, CinematicDirector director,
                              CameraShaker shaker)
        {
            Self = self;
            Animator = animator;
            Target = target;
            BattleCamera = battleCamera;
            Muzzle = muzzle;
            Thruster = thruster;
            Reticle = reticle;
            Director = director;
            Shaker = shaker;
        }

        public bool HasMark => Reticle != null && Reticle.HasMark;
    }
}

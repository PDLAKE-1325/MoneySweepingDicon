using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Onsil.Actors;

// Explicit aliases - see AbilityContext.cs for why.
using LockOnReticle = Onsil.Vfx.LockOnReticle;
using MuzzleRig = Onsil.Vfx.MuzzleRig;
using ThrusterRig = Onsil.Vfx.ThrusterRig;
using CinematicDirector = Onsil.Vfx.CinematicDirector;
using CameraShaker = Onsil.Vfx.CameraShaker;

namespace Onsil.Abilities
{
    /// <summary>
    /// Single entry point for casting. Owns the "one ability at a time" rule and
    /// wires every ability to its <see cref="AbilityContext"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var runner = nora.GetComponent&lt;AbilityRunner&gt;();
    /// if (runner.Cast("ultimate")) { /* accepted */ }
    /// </code>
    /// </example>
    [RequireComponent(typeof(SpriteAnimator))]
    [DisallowMultipleComponent]
    public class AbilityRunner : MonoBehaviour
    {
        [Header("scene refs")]
        [SerializeField] Transform target;
        [SerializeField] Camera battleCamera;
        [SerializeField] LockOnReticle reticle;
        [SerializeField] CinematicDirector director;

        [Header("rigs")]
        [SerializeField] MuzzleRig muzzle;
        [SerializeField] ThrusterRig thruster;
        [SerializeField] CameraShaker shaker;

        SpriteAnimator animator;
        AbilityContext ctx;
        readonly Dictionary<string, Ability> abilities = new Dictionary<string, Ability>();
        Coroutine running;

        public bool IsBusy => running != null;
        public string CurrentAbility { get; private set; }

        /// <summary>Setting this retargets the live context too, so abilities cast
        /// after a battle-slot swap aim at the new occupant.</summary>
        public Transform Target
        {
            get => target;
            set { target = value; if (ctx != null) ctx.Target = value; }
        }

        /// <summary>Raised when any ability starts / finishes. Hook UI here.</summary>
        public event System.Action<string> AbilityStarted;
        public event System.Action<string> AbilityFinished;

        void Awake()
        {
            animator = GetComponent<SpriteAnimator>();
            if (battleCamera == null) battleCamera = Camera.main;
            if (shaker == null && battleCamera != null)
                shaker = battleCamera.GetComponent<CameraShaker>();

            var built = new AbilityContext(transform, animator, target, battleCamera,
                                           muzzle, thruster, reticle, director, shaker);
            ctx = built;

            foreach (var a in GetComponents<Ability>())
            {
                a.Bind(built);
                if (string.IsNullOrEmpty(a.abilityId))
                {
                    Debug.LogWarning($"[Onsil] {a.GetType().Name} has no abilityId", a);
                    continue;
                }
                if (abilities.ContainsKey(a.abilityId))
                {
                    Debug.LogWarning($"[Onsil] duplicate abilityId '{a.abilityId}'", a);
                    continue;
                }
                abilities.Add(a.abilityId, a);
            }
        }

        public IEnumerable<string> AbilityIds => abilities.Keys;

        /// <summary>Try to cast. Returns false when busy, unknown, or gated.</summary>
        public bool Cast(string abilityId)
        {
            if (IsBusy) return false;
            if (!abilities.TryGetValue(abilityId, out var ability)) return false;
            if (!ability.CanCast()) return false;
            running = StartCoroutine(Perform(ability));
            return true;
        }

        IEnumerator Perform(Ability ability)
        {
            CurrentAbility = ability.abilityId;
            AbilityStarted?.Invoke(CurrentAbility);

            animator.Suspend();
            Time.timeScale = 1f;

            yield return ability.Run();

            // resolve the mark AFTER the performance so the flare reads on the hit
            if (reticle != null)
            {
                if (ability.consumesMark) reticle.Consume();
                else if (ability.appliesMark && target != null) reticle.Apply(target);
            }

            Time.timeScale = 1f;
            animator.Release();

            var id = CurrentAbility;
            CurrentAbility = null;
            running = null;
            AbilityFinished?.Invoke(id);
        }
    }
}

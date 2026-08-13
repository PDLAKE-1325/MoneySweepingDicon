using System.Collections;
using UnityEngine;

namespace Onsil.Abilities
{
    /// <summary>
    /// Base class for everything a character can do on its turn.
    /// </summary>
    /// <remarks>
    /// To add an ability: subclass this, override <see cref="Run"/>, drop the
    /// component on the character, and register it in <see cref="AbilityRunner"/>.
    /// The runner guarantees only one ability plays at a time and that the
    /// sprite animator is suspended and released around it.
    /// </remarks>
    [RequireComponent(typeof(AbilityRunner))]
    public abstract class Ability : MonoBehaviour
    {
        [Header("identity")]
        [Tooltip("Shown in UI and used by AbilityRunner.Cast(id)")]
        public string abilityId = "ability";

        [Tooltip("Consumes the lock-on mark when it resolves")]
        public bool consumesMark = true;

        [Tooltip("Applies a lock-on mark when it resolves")]
        public bool appliesMark;

        protected AbilityContext Ctx { get; private set; }

        internal void Bind(AbilityContext ctx) => Ctx = ctx;

        /// <summary>Override to gate the ability (cooldown, resources, mark state).</summary>
        public virtual bool CanCast() => true;

        /// <summary>The whole performance. Yield like any coroutine.</summary>
        public abstract IEnumerator Run();
    }
}

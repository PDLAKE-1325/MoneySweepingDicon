using UnityEngine;
using UnityEngine.InputSystem;
using Onsil.Abilities;

namespace Onsil.Debugging
{
    /// <summary>
    /// Keyboard driver for a mob's abilities. Test-only.
    /// </summary>
    /// <remarks>
    /// This component used to own the parry telegraph as a hand-tuned constant,
    /// which drifted whenever the animation was retimed. The window now belongs
    /// to <see cref="Onsil.Combat.MeleeStrikeAbility"/>, derived from the clip,
    /// so all that is left here is pressing the buttons. The real turn system
    /// will call <see cref="AbilityRunner.Cast"/> directly and skip this class.
    /// </remarks>
    [RequireComponent(typeof(AbilityRunner))]
    public class MobAttackDriver : MonoBehaviour
    {
        [Header("keys")]
        public Key basicKey = Key.E;
        public Key skillKey = Key.R;

        [Header("ability ids")]
        public string basicId = "mob_attack";
        public string skillId = "mob_skill1";

        AbilityRunner runner;

        void Awake() { runner = GetComponent<AbilityRunner>(); }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb[basicKey].wasPressedThisFrame) runner.Cast(basicId);
            if (kb[skillKey].wasPressedThisFrame) runner.Cast(skillId);
        }
    }
}

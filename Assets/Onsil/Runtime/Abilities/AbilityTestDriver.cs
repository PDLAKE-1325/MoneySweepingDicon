using UnityEngine;
using UnityEngine.InputSystem;
using Onsil.Abilities;
using Onsil.Vfx;

namespace Onsil.Debugging
{
    /// <summary>
    /// Keyboard driver for the asset test scene. Not shipped with the game -
    /// the real turn system calls <see cref="AbilityRunner.Cast"/> instead.
    /// </summary>
    [RequireComponent(typeof(AbilityRunner))]
    public class AbilityTestDriver : MonoBehaviour
    {
        [System.Serializable]
        public struct Binding
        {
            public Key key;
            public string abilityId;
            public string label;
        }

        public Binding[] bindings = {
            new Binding { key = Key.Digit1, abilityId = "basic",    label = "1  basic attack" },
            new Binding { key = Key.Digit2, abilityId = "scan",     label = "2  skill 1 - scan  (SETS mark)" },
            new Binding { key = Key.Digit3, abilityId = "airshot",  label = "3  skill 2 - air shot" },
            new Binding { key = Key.Digit4, abilityId = "ultimate", label = "4  ultimate - kneel" },
        };

        [Tooltip("Toggles the black-and-white pass for a quick shader check")]
        public Key badAppleTestKey = Key.B;
        [Tooltip("Clears the mark and forces the shader back to normal")]
        public Key resetKey = Key.C;

        AbilityRunner runner;
        LockOnReticle reticle;

        void Awake()
        {
            runner = GetComponent<AbilityRunner>();
            reticle = FindFirstObjectByType<LockOnReticle>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || runner == null) return;

            for (int i = 0; i < bindings.Length; i++)
                if (kb[bindings[i].key].wasPressedThisFrame)
                    runner.Cast(bindings[i].abilityId);

            if (kb[badAppleTestKey].wasPressedThisFrame && BadAppleController.Instance != null)
                BadAppleController.Instance.Hit(0.4f, 0.5f);

            if (kb[resetKey].wasPressedThisFrame)
            {
                if (reticle != null) reticle.Clear();
                if (BadAppleController.Instance != null) BadAppleController.Instance.SetAmount(0f);
            }
        }

        void OnGUI()
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            s.normal.textColor = Color.white;

            GUILayout.BeginArea(new Rect(10, 10, 340, 40 + bindings.Length * 22 + 60), GUI.skin.box);
            GUILayout.Label(runner.IsBusy ? "state : " + runner.CurrentAbility : "state : idle", s);
            GUILayout.Label("mark  : " + (reticle != null && reticle.HasMark ? "LOCKED" : "none"), s);
            GUILayout.Space(6);
            for (int i = 0; i < bindings.Length; i++) GUILayout.Label(bindings[i].label, s);
            GUILayout.Label("B  b/w test        C  clear", s);
            GUILayout.EndArea();
        }
    }
}

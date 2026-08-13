using UnityEngine;
using UnityEngine.InputSystem;
using Onsil.Abilities;
using Onsil.Combat;
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

        [Header("abilities")]
        public Binding[] bindings = {
            new Binding { key = Key.Digit1, abilityId = "basic",    label = "1  basic attack" },
            new Binding { key = Key.Digit2, abilityId = "scan",     label = "2  skill 1 - scan  (SETS mark)" },
            new Binding { key = Key.Digit3, abilityId = "airshot",  label = "3  skill 2 - air shot" },
            new Binding { key = Key.Digit4, abilityId = "ultimate", label = "4  ultimate - kneel" },
        };

        [Header("combat")]
        [Tooltip("Makes the enemy start a telegraphed attack.")]
        public Key enemyAttackKey = Key.E;
        [Tooltip("Parry. Press as the projectile lands.")]
        public Key parryKey = Key.Space;
        public EnemyAttacker enemy;
        public ParryReceiver parry;

        [Header("misc")]
        public Key badAppleTestKey = Key.B;
        public Key resetKey = Key.C;

        AbilityRunner runner;
        LockOnReticle reticle;

        ParryReceiver.Result lastResult = ParryReceiver.Result.None;
        float lastResultAt = -10f;

        void Awake()
        {
            runner = GetComponent<AbilityRunner>();
            reticle = FindFirstObjectByType<LockOnReticle>();
            if (parry == null) parry = GetComponent<ParryReceiver>();
            if (enemy == null) enemy = FindFirstObjectByType<EnemyAttacker>();
        }

        void OnEnable()
        {
            if (parry != null) parry.Judged += OnJudged;
        }
        void OnDisable()
        {
            if (parry != null) parry.Judged -= OnJudged;
        }

        void OnJudged(ParryReceiver.Result r)
        {
            lastResult = r;
            lastResultAt = Time.unscaledTime;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || runner == null) return;

            for (int i = 0; i < bindings.Length; i++)
                if (kb[bindings[i].key].wasPressedThisFrame)
                    runner.Cast(bindings[i].abilityId);

            if (kb[enemyAttackKey].wasPressedThisFrame && enemy != null)
                enemy.Attack();

            // parry is allowed even mid-ability: getting shot at is not optional
            if (kb[parryKey].wasPressedThisFrame && parry != null)
                parry.Press();

            if (kb[badAppleTestKey].wasPressedThisFrame && BadAppleController.Instance != null)
                BadAppleController.Instance.Hit(0.4f, 0.5f);

            if (kb[resetKey].wasPressedThisFrame)
            {
                if (reticle != null) reticle.Clear();
                if (BadAppleController.Instance != null) BadAppleController.Instance.SetAmount(0f);
                Time.timeScale = 1f;
            }
        }

        void OnGUI()
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            s.normal.textColor = Color.white;

            float h = 150f + bindings.Length * 22f;
            GUILayout.BeginArea(new Rect(10, 10, 360, h), GUI.skin.box);

            GUILayout.Label(runner.IsBusy ? "state : " + runner.CurrentAbility : "state : idle", s);
            GUILayout.Label("mark  : " + (reticle != null && reticle.HasMark ? "LOCKED" : "none"), s);

            if (parry != null && parry.WindowOpen)
            {
                var w = new GUIStyle(s);
                w.normal.textColor = new Color(1f, 0.6f, 0.5f);
                GUILayout.Label("INCOMING  " + parry.TimeToImpact.ToString("F2") + "s", w);
            }
            else GUILayout.Label(" ", s);

            if (Time.unscaledTime - lastResultAt < 1.2f && lastResult != ParryReceiver.Result.None)
            {
                var g = new GUIStyle(s) { fontSize = 20 };
                g.normal.textColor =
                    lastResult == ParryReceiver.Result.Perfect ? new Color(1f, 0.92f, 0.5f) :
                    lastResult == ParryReceiver.Result.Good ? new Color(0.55f, 0.9f, 1f) :
                                                              new Color(0.8f, 0.8f, 0.8f);
                var punch = Camera.main != null ? Camera.main.GetComponent<CameraPunch>() : null;
                string tiltInfo = punch != null
                    ? "   tilt " + punch.LastTilt.ToString("F1") + "deg"
                    : "   (no CameraPunch)";
                GUILayout.Label(lastResult.ToString().ToUpper() + tiltInfo, g);
            }
            else GUILayout.Label(" ", s);

            GUILayout.Space(4);
            for (int i = 0; i < bindings.Length; i++) GUILayout.Label(bindings[i].label, s);
            GUILayout.Label("E      enemy attacks", s);
            GUILayout.Label("Space  parry", s);
            GUILayout.Label("B  b/w test        C  clear", s);
            GUILayout.EndArea();
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using Onsil.Abilities;

namespace Onsil.Debugging
{
    /// <summary>
    /// One battle slot, many mobs. F1-F4 chooses which enemy occupies it; the
    /// others sleep. The player's runner is retargeted on every swap, so marks,
    /// scans and strikes all follow the active occupant.
    /// </summary>
    /// <remarks>
    /// Swaps are refused while either side is mid-ability - yanking a mob out of
    /// its own swing leaves tweens and parry windows orphaned.
    /// </remarks>
    public class MobRoster : MonoBehaviour
    {
        [Tooltip("The player whose target follows the active mob.")]
        public AbilityRunner player;

        [Tooltip("Every swappable enemy, in F1..F4 order.")]
        public Transform[] mobs;

        [Tooltip("Where the active mob stands.")]
        public Vector3 slot = new Vector3(1.55f, 0.03f, 0f);

        public Key[] keys = { Key.F1, Key.F2, Key.F3, Key.F4 };
        public int active = 0;
        public float uiScale = 1.5f;

        void Start()
        {
            Select(Mathf.Clamp(active, 0, mobs.Length - 1), true);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            for (int i = 0; i < mobs.Length && i < keys.Length; i++)
                if (kb[keys[i]].wasPressedThisFrame)
                    Select(i, false);
        }

        public void Select(int index, bool force)
        {
            if (index < 0 || index >= mobs.Length || mobs[index] == null) return;

            if (!force)
            {
                if (player != null && player.IsBusy) return;
                var cur = mobs[active] != null
                        ? mobs[active].GetComponent<AbilityRunner>() : null;
                if (cur != null && cur.IsBusy) return;
            }

            active = index;
            for (int i = 0; i < mobs.Length; i++)
            {
                if (mobs[i] == null) continue;
                bool on = i == index;
                mobs[i].gameObject.SetActive(on);
                if (on) mobs[i].position = slot;
            }
            if (player != null) player.Target = mobs[index];
        }

        void OnGUI()
        {
            var prev = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * uiScale);
            string name = mobs != null && active < mobs.Length && mobs[active] != null
                        ? mobs[active].name : "?";
            GUI.Label(new Rect(6f, 330f, 420f, 24f),
                      "enemy [F1-F4]: " + name + "   (E attacks)");
            GUI.matrix = prev;
        }
    }
}

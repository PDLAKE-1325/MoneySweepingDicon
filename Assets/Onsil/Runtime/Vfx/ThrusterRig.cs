using UnityEngine;

namespace Onsil.Vfx
{
    /// <summary>
    /// Backpack-mounted twin exhaust plumes.
    /// </summary>
    /// <remarks>
    /// Mounted on the pack, not the boots: the legs bend mid-air, so a foot anchor
    /// can never track them. The pack rides the torso and stays put, which also
    /// matches the canon Barbatos-style rig.
    ///
    /// Call <see cref="SetPower"/> every frame while airborne; 0 fully hides the plume.
    /// </remarks>
    public class ThrusterRig : MonoBehaviour
    {
        [Header("art")]
        [Tooltip("Plume sprite. Pivot must be TopCenter so it hangs downward.")]
        public Sprite plumeSprite;
        public int sortingOrder = -2;

        [Header("placement")]
        [Tooltip("Nozzle position relative to the sprite pivot")]
        public Vector2 offset = new Vector2(-0.14f, 0.72f);
        [Tooltip("Vertical gap between the two nozzles")]
        public float spread = 0.07f;
        [Tooltip("Nozzle facing in degrees; 200 points down-and-back")]
        public float angle = 200f;
        public float scale = 0.8f;

        [Header("flicker")]
        public float flickerAmount = 0.10f;
        public float flickerHz = 42f;

        Transform root;
        Transform[] plumes;
        // Renderers and core transforms are cached at Build time. SetPower runs
        // every frame while a thruster burns, and it used to re-resolve two
        // SpriteRenderers plus a GetChild per plume on every one of those calls -
        // all for objects this component created itself and already had handles to.
        SpriteRenderer[] plumeSr;
        Transform[] cores;
        SpriteRenderer[] coreSr;
        float power;

        public float Power => power;

        void Awake() => Build();

        void Build()
        {
            if (root != null) return;
            root = new GameObject("Thrusters").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(offset.x, offset.y, 0.1f);

            plumes = new Transform[2];
            plumeSr = new SpriteRenderer[2];
            cores = new Transform[2];
            coreSr = new SpriteRenderer[2];
            float[] ys = { spread, -spread };
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("plume" + i);
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(0f, ys[i], 0f);
                go.transform.localRotation = Quaternion.Euler(0f, 0f, angle + (i == 0 ? 4f : -4f));

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = plumeSprite;
                sr.sortingOrder = sortingOrder;
                sr.color = Color.white;

                var core = new GameObject("core");
                core.transform.SetParent(go.transform, false);
                var csr = core.AddComponent<SpriteRenderer>();
                csr.sprite = plumeSprite;
                csr.sortingOrder = sortingOrder + 1;
                csr.color = new Color(1f, 1f, 1f, 0.95f);

                plumes[i] = go.transform;
                plumeSr[i] = sr;
                cores[i] = core.transform;
                coreSr[i] = csr;
            }
            SetPower(0f);
        }

        /// <summary>0 = off, 1 = full burn. Safe to call every frame.</summary>
        public void SetPower(float value)
        {
            Build();
            power = Mathf.Clamp01(value);
            root.localPosition = new Vector3(offset.x, offset.y, 0.1f);

            for (int i = 0; i < plumes.Length; i++)
            {
                var t = plumes[i];
                t.localPosition = new Vector3(0f, i == 0 ? spread : -spread, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, angle + (i == 0 ? 4f : -4f));

                float flick = 1f
                    + Mathf.Sin(Time.time * flickerHz + i * 2.1f) * flickerAmount
                    + Mathf.Sin(Time.time * flickerHz * 2.3f + i) * flickerAmount * 0.5f;

                float len = Mathf.Lerp(0.02f, 1.15f, power) * flick;
                t.localScale = new Vector3(scale * 0.5f, len * scale, 1f);

                var sr = plumeSr[i];
                var c = sr.color; c.a = power; sr.color = c;

                var core = cores[i];
                if (core != null)
                {
                    core.localScale = new Vector3(0.45f, 0.5f, 1f);
                    var cs = coreSr[i];
                    var cc = cs.color; cc.a = power * 0.9f; cs.color = cc;
                }
            }
        }

        public void Off() => SetPower(0f);
    }
}

using System.Collections;
using UnityEngine;

namespace Onsil.Vfx
{
    /// <summary>
    /// Muzzle flash and shell ejection, anchored to measured barrel offsets.
    /// </summary>
    /// <remarks>
    /// Offsets were read straight off the gun layer of each sheet (rightmost opaque
    /// pixel of the cell, minus the sprite pivot, divided by PPU). Re-measure with
    /// Tools > Onsil > Measure Muzzle if the art changes.
    /// </remarks>
    public class MuzzleRig : MonoBehaviour
    {
        public enum Stance { Standing, Kneeling, Airborne }

        [System.Serializable]
        public struct Anchor
        {
            public Stance stance;
            [Tooltip("Barrel tip in local units, relative to the sprite pivot")]
            public Vector2 offset;
        }

        [Header("anchors")]
        public Anchor[] anchors = {
            new Anchor { stance = Stance.Standing, offset = new Vector2(0.621f, 0.887f) },
            new Anchor { stance = Stance.Kneeling, offset = new Vector2(0.594f, 0.523f) },
            new Anchor { stance = Stance.Airborne, offset = new Vector2(0.536f, 0.551f) },
        };

        [Header("flash")]
        public Sprite gasSprite;
        public Sprite sparkSprite;
        public float size = 0.75f;
        [Tooltip("Total spread of the forward fan, degrees. 360 sprays all round.")]
        public float arc = 102f;
        public int spikes = 15;
        public float duration = 0.13f;
        public int sortingOrder = 30;

        [Header("shell")]
        public Sprite shellSprite;
        public Vector2 shellOrigin = new Vector2(0.05f, 0.62f);
        public float shellScale = 1.6f;
        public float shellLife = 1.6f;

        public Vector2 OffsetFor(Stance s)
        {
            for (int i = 0; i < anchors.Length; i++)
                if (anchors[i].stance == s) return anchors[i].offset;
            return Vector2.zero;
        }

        /// <summary>Fire the flash and eject a casing in one call.</summary>
        public void Fire(Stance stance)
        {
            Flash(stance);
            EjectShell();
        }

        public void Flash(Stance stance)
        {
            if (sparkSprite == null && gasSprite == null) return;
            StartCoroutine(FlashRoutine(OffsetFor(stance)));
        }

        IEnumerator FlashRoutine(Vector2 offset)
        {
            // A pivot object sits ON the barrel tip; children are pushed out along
            // their own facing so nothing ever renders behind the muzzle.
            var pivot = new GameObject("MuzzleFlash");
            pivot.transform.position = transform.position + (Vector3)offset + Vector3.back * 0.2f;

            SpriteRenderer gas = null;
            if (gasSprite != null)
            {
                var g = new GameObject("gas");
                g.transform.SetParent(pivot.transform, false);
                gas = g.AddComponent<SpriteRenderer>();
                gas.sprite = gasSprite;
                gas.sortingOrder = sortingOrder - 1;
                gas.color = new Color(1f, 0.9f, 0.6f, 1f);
            }

            int n = Mathf.Max(1, spikes);
            var tips = new Transform[n];
            var lens = new float[n];
            float half = arc * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var q = new GameObject("spike" + i);
                q.transform.SetParent(pivot.transform, false);
                var sr = q.AddComponent<SpriteRenderer>();
                sr.sprite = sparkSprite != null ? sparkSprite : gasSprite;
                sr.sortingOrder = sortingOrder + 1;
                sr.color = Color.Lerp(new Color(1f, 0.7f, 0.2f, 1f), Color.white, Random.value * 0.7f);
                float ang = n == 1 ? 0f
                          : Mathf.Lerp(-half, half, i / (float)(n - 1)) + Random.Range(-2.5f, 2.5f);
                q.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
                lens[i] = Mathf.Lerp(1f, 0.55f, Mathf.Abs(ang) / Mathf.Max(half, 1f))
                        * Random.Range(0.9f, 1.15f);
                tips[i] = q.transform;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = t / duration;

                if (gas != null)
                {
                    float gl = size * Mathf.Lerp(0.5f, 1.9f, Mathf.Sqrt(k));
                    gas.transform.localScale = new Vector3(gl, gl * 0.85f, 1f);
                    var gc = gas.color; gc.a = Mathf.Pow(1f - k, 1.4f); gas.color = gc;
                }
                for (int i = 0; i < n; i++)
                {
                    float len = size * lens[i] * Mathf.Lerp(0.3f, 1.6f, k);
                    tips[i].localScale = new Vector3(len, size * 0.34f * lens[i], 1f);
                    tips[i].localPosition = tips[i].localRotation * new Vector3(len, 0f, 0f);
                    var sr = tips[i].GetComponent<SpriteRenderer>();
                    var c = sr.color; c.a = 1f - k; sr.color = c;
                }
                yield return null;
            }
            Destroy(pivot);
        }

        public void EjectShell()
        {
            if (shellSprite == null) return;
            StartCoroutine(ShellRoutine());
        }

        IEnumerator ShellRoutine()
        {
            var go = new GameObject("Shell");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = shellSprite;
            sr.sortingOrder = sortingOrder - 5;
            go.transform.localScale = Vector3.one * shellScale;

            Vector3 p = transform.position + (Vector3)shellOrigin + Vector3.back * 0.3f;
            go.transform.position = p;

            Vector2 v = new Vector2(Random.Range(-1.6f, -1.0f), Random.Range(2.2f, 2.9f));
            float spin = Random.Range(600f, 1100f);
            float ground = transform.position.y + 0.03f;

            float t = 0f;
            while (go != null && t < shellLife)
            {
                float dt = Time.deltaTime;
                t += dt;
                v.y -= 8.5f * dt;
                p += (Vector3)(v * dt);
                if (p.y < ground) { p.y = ground; v.y = -v.y * 0.35f; v.x *= 0.55f; spin *= 0.4f; }
                go.transform.position = p;
                go.transform.Rotate(0f, 0f, spin * dt);
                float fadeAt = shellLife * 0.7f;
                if (t > fadeAt)
                {
                    var c = sr.color;
                    c.a = 1f - (t - fadeAt) / Mathf.Max(shellLife - fadeAt, 0.001f);
                    sr.color = c;
                }
                yield return null;
            }
            if (go != null) Destroy(go);
        }
    }
}

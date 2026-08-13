using System.Collections;
using UnityEngine;

namespace Onsil.Vfx
{
    /// <summary>
    /// The shaped-charge impact jet: a white cone that erupts backward from the hit,
    /// with debris streaks trailing it.
    /// </summary>
    /// <remarks>
    /// Runs on SCALED time on purpose. The blast is meant to crawl through the
    /// slow-motion beat, so an unscaled timer would finish before the beat began.
    /// Alpha holds at 1 for <see cref="hold"/> of its life, then falls off; a plain
    /// pow() fade made it vanish while the slow-mo was still running.
    /// </remarks>
    public class ImpactJet : MonoBehaviour
    {
        [Header("art")]
        [Tooltip("Cone sprite. Pivot must be LeftCenter so scaling grows it forward.")]
        public Sprite jetSprite;
        public Sprite debrisSprite;
        public int sortingOrder = 183;

        [Header("size")]
        [Tooltip("Overall size multiplier")]
        public float volume = 3.5f;
        [Tooltip("Length relative to width")]
        [Range(0.5f, 4f)] public float length = 1.4f;
        [Tooltip("Where along the cone the lobe is widest. 0 = at the hit, 1 = at the tip")]
        [Range(0f, 1f)] public float bulgePosition = 0.5f;
        [Range(0f, 2f)] public float bulgeStrength = 1f;

        [Header("debris")]
        [Range(0, 80)] public int debrisCount = 40;
        [Tooltip("Half-angle of the debris cone, degrees")]
        [Range(5f, 90f)] public float spread = 26f;
        public float reach = 3f;

        [Header("timing")]
        [Range(0.3f, 5f)] public float life = 1.9f;
        [Tooltip("Fraction of its life held fully opaque before fading")]
        [Range(0f, 1f)] public float hold = 0.7f;

        [Header("camera")]
        [Tooltip("Cut camera pulls back by this factor so the blast fits")]
        public float zoomOut = 2.2f;

        /// <summary>Spawn a blast. <paramref name="parent"/> keeps it on the cut stage.</summary>
        public void Spawn(Vector3 worldPosition, Transform parent, int layer, Camera cam)
        {
            StartCoroutine(Routine(worldPosition, parent, layer, cam));
        }

        IEnumerator Routine(Vector3 pos, Transform parent, int layer, Camera cam)
        {
            var root = new GameObject("ImpactJet");
            if (parent != null) root.transform.SetParent(parent, true);
            root.transform.position = pos;

            // three overlapping cones give the lobe depth without any round core
            var lobes = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var g = new GameObject("lobe" + i);
                g.layer = layer;
                g.transform.SetParent(root.transform, false);
                var s = g.AddComponent<SpriteRenderer>();
                s.sprite = jetSprite;
                s.sortingOrder = sortingOrder + i;
                s.color = Color.white;
                g.transform.localRotation = Quaternion.Euler(0, 0, (i - 1) * 7f);
                lobes[i] = g.transform;
            }

            int n = Mathf.Max(0, debrisCount);
            var streaks = new Transform[n];
            var dirs = new Vector2[n];
            var dist = new float[n];
            var width = new float[n];
            for (int i = 0; i < n; i++)
            {
                var s = new GameObject("debris" + i);
                s.layer = layer;
                s.transform.SetParent(root.transform, false);
                var sr = s.AddComponent<SpriteRenderer>();
                sr.sprite = debrisSprite != null ? debrisSprite : jetSprite;
                sr.sortingOrder = sortingOrder + 3;
                sr.color = Color.white;
                float ang = Random.Range(-spread, spread) * Mathf.Deg2Rad;
                dirs[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                dist[i] = Random.Range(6f, 16f) * reach;
                width[i] = Random.Range(0.6f, 1.8f) * volume * 0.4f;
                s.transform.localRotation = Quaternion.Euler(0f, 0f, ang * Mathf.Rad2Deg);
                streaks[i] = s.transform;
            }

            float baseSize = cam != null ? cam.orthographicSize : 1f;
            float t = 0f;
            while (root != null && t < life)
            {
                t += Time.deltaTime;                     // scaled: must crawl in slow-mo
                float k = Mathf.Clamp01(t / life);
                float ease = Mathf.Pow(k, 0.45f);
                float alpha = 1f - Mathf.Clamp01((k - hold) / Mathf.Max(1f - hold, 0.001f));

                if (cam != null)
                    cam.orthographicSize = Mathf.Lerp(baseSize, baseSize * zoomOut,
                                                      Mathf.Pow(k, 0.35f));

                for (int i = 0; i < 3; i++)
                {
                    float m = 1f + i * 0.22f;
                    float len = Mathf.Lerp(0.2f, 4.5f * volume * m * length, ease);
                    float wid = Mathf.Lerp(0.2f, 3.2f * volume * m, ease);
                    lobes[i].localScale = new Vector3(len, wid, 1f);
                    lobes[i].localPosition =
                        new Vector3(-len * (bulgePosition - 0.5f) * bulgeStrength * 0.6f, 0f, 0f);
                    var sr = lobes[i].GetComponent<SpriteRenderer>();
                    sr.color = new Color(1f, 1f, 1f, alpha);
                }

                for (int i = 0; i < n; i++)
                {
                    streaks[i].localPosition = (Vector3)(dirs[i] * dist[i] * ease);
                    streaks[i].localScale = new Vector3(Mathf.Lerp(0.5f, 16f * reach, ease),
                                                        Mathf.Lerp(width[i], 0.1f, k), 1f);
                    streaks[i].GetComponent<SpriteRenderer>().color =
                        new Color(1f, 1f, 1f, alpha);
                }
                yield return null;
            }
            if (root != null) Destroy(root);
        }
    }
}

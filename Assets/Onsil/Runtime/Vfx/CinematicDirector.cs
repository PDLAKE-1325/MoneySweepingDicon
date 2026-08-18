using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Onsil.Vfx
{
    /// <summary>
    /// Owns the ultimate's cut-away: an isolated stage the battle camera cannot see,
    /// plus the letterbox, the black-and-white pass and the slow-motion beat.
    /// </summary>
    /// <remarks>
    /// Isolation works by layer, not by distance alone. The cut camera renders ONLY
    /// <see cref="cutawayLayer"/>; the battle camera must exclude that layer in its
    /// culling mask (the runner asserts this on Awake). The stage is also parked far
    /// from the battle so stray world-space effects cannot leak into frame.
    /// </remarks>
    public class CinematicDirector : MonoBehaviour
    {
        [Header("layer isolation")]
        [Tooltip("Layer index rendered ONLY by the cut camera")]
        public int cutawayLayer = 31;
        [Tooltip("Where the stage is built, far from the battle")]
        public Vector3 stageOrigin = new Vector3(0f, 500f, 0f);

        [Header("camera")]
        public float cutOrthoSize = 1.25f;
        public Color cutBackground = new Color(0.03f, 0.035f, 0.05f, 1f);
        [Tooltip("Bar height as a fraction of the screen. 0 disables the bars.")]
        public float letterbox = 0.14f;

        [Header("scenery")]
        public Sprite buildingSprite;
        public Sprite targetSprite;
        public int buildingCount = 16;
        public float buildingSpeed = 9f;
        [Tooltip("Tint on the cutaway target. Its luminance must sit ABOVE the " +
                 "black-and-white threshold or the silhouette vanishes during the blackout.")]
        public Color targetTint = Color.white;
        [Tooltip("Runtime override for the cutaway silhouette, pushed in by the " +
                 "ability from the live battle target. Empty falls back to targetSprite.")]
        public Sprite targetOverride;
        [Tooltip("Extra scale on the override so a battle sprite reads at cutaway " +
                 "size. Multiplies the whole approach curve.")]
        public float targetOverrideScale = 1f;
        [Tooltip("Flip the override horizontally. Battle sheets face left and the " +
                 "round flies in from the left, so off is usually correct.")]
        public bool targetOverrideFlipX = false;
        [Tooltip("Local offset of the override sprite inside the approach rig, so " +
                 "an off-centre pivot still sits centred in frame. Pushed by the ability.")]
        public Vector2 targetOverrideOffset = Vector2.zero;
        public Color nearBuildingTint = new Color(0.05f, 0.055f, 0.075f, 1f);
        public Color farBuildingTint = new Color(0.09f, 0.10f, 0.13f, 1f);

        [Header("target approach")]
        public float targetStartX = 7f;
        [Tooltip("Camera half-width is ~2.2, so -0.75 sits about a third in from the left")]
        public float targetStopX = -0.75f;
        [Tooltip(">1 holds the target back, then it rushes in at the end")]
        public float approachCurve = 2.2f;
        public float targetEndScale = 1.5f;

        [Header("round")]
        public Sprite tracerSprite;
        public float flyTime = 0.8f;
        [Tooltip("1 = the round meets the target exactly at the end of flyTime")]
        public float tracerReach = 1f;
        [Tooltip("Negative fires the blast early, positive delays it (seconds)")]
        public float impactOffset = 0f;

        [Header("slow motion")]
        [Tooltip("Time scale during the beat. 1 disables it.")]
        public float slowScale = 0.12f;
        public float slowSeconds = 2f;
        public float returnPause = 0.25f;

        Camera battleCamera;
        Camera cutCamera;
        GameObject stage;
        GameObject barTop, barBottom;
        Transform target;
        float targetScaleMul = 1f;

        public Camera CutCamera => cutCamera;
        public bool Active => stage != null;

        /// <summary>World position of the impact, valid while the cut is up.</summary>
        public Vector3 ImpactPoint => stageOrigin + new Vector3(targetStopX, 0f, 10f);

        // ------------------------------------------------------------ lifecycle

        public void Build(Camera battle)
        {
            battleCamera = battle;
            stage = new GameObject("ULT_Stage");
            stage.transform.position = stageOrigin;

            var camGO = new GameObject("ULT_CutCam");
            camGO.transform.SetParent(stage.transform, false);
            cutCamera = camGO.AddComponent<Camera>();
            cutCamera.orthographic = true;
            cutCamera.orthographicSize = cutOrthoSize;
            cutCamera.transform.localPosition = new Vector3(0f, 0f, -10f);
            cutCamera.clearFlags = CameraClearFlags.SolidColor;
            cutCamera.backgroundColor = cutBackground;
            cutCamera.cullingMask = 1 << cutawayLayer;
            cutCamera.depth = (battleCamera != null ? battleCamera.depth : 0) + 10;

            var extra = camGO.AddComponent<UniversalAdditionalCameraData>();
            extra.renderPostProcessing = true;      // the b/w pass must reach the cut
            camGO.AddComponent<CameraShaker>();

            if (battleCamera != null) battleCamera.enabled = false;

            BuildScenery();
            SetLetterbox(true);
        }

        void BuildScenery()
        {
            for (int i = 0; i < buildingCount; i++)
            {
                bool near = (i % 2 == 0);
                var b = new GameObject("bld" + i);
                b.layer = cutawayLayer;
                b.transform.SetParent(stage.transform, false);
                var sr = b.AddComponent<SpriteRenderer>();
                sr.sprite = buildingSprite;
                sr.color = near ? nearBuildingTint : farBuildingTint;
                sr.sortingOrder = near ? 8 : -8;
                float depth = near ? 1f : 0.5f;
                float h = near ? Random.Range(2.2f, 3.4f) : Random.Range(1.3f, 2.0f);
                b.transform.localScale = new Vector3(near ? 0.85f : 0.6f, h, 1f);
                float top = near ? Random.Range(0.35f, 1.15f) : Random.Range(-0.15f, 0.55f);
                b.transform.localPosition = new Vector3(-3.2f + i * 1.15f, top - h, depth);
            }

            var tgt = new GameObject("ULT_Target");
            tgt.layer = cutawayLayer;
            tgt.transform.SetParent(stage.transform, false);

            // the renderer lives on a child so an off-centre pivot can be
            // recentred once, while the approach code keeps scaling the parent
            var vis = new GameObject("vis");
            vis.layer = cutawayLayer;
            vis.transform.SetParent(tgt.transform, false);
            var tsr = vis.AddComponent<SpriteRenderer>();
            bool overridden = targetOverride != null;
            tsr.sprite = overridden ? targetOverride : targetSprite;
            tsr.flipX = overridden && targetOverrideFlipX;
            tsr.color = targetTint;
            tsr.sortingOrder = 12;
            vis.transform.localPosition = overridden
                ? (Vector3)targetOverrideOffset : Vector3.zero;
            targetScaleMul = overridden ? targetOverrideScale : 1f;
            tgt.transform.localPosition = new Vector3(targetStartX, -0.1f, 0.9f);
            tgt.transform.localScale = Vector3.one * 0.75f * targetScaleMul;
            target = tgt.transform;
        }

        public void Teardown()
        {
            if (battleCamera != null) battleCamera.enabled = true;
            if (stage != null) Destroy(stage);
            stage = null;
            cutCamera = null;
            target = null;
        }

        // -------------------------------------------------------------- content

        /// <summary>Fly the round in. Honours impactOffset by trimming or padding.</summary>
        public IEnumerator RunRound()
        {
            float dur = impactOffset < 0f ? Mathf.Max(0.05f, flyTime + impactOffset) : flyTime;
            yield return TracerFlight(dur);
            if (impactOffset > 0f) yield return new WaitForSeconds(impactOffset);
        }

        IEnumerator TracerFlight(float dur)
        {
            var tracer = new GameObject("ULT_Tracer");
            tracer.layer = cutawayLayer;
            tracer.transform.SetParent(stage.transform, false);
            var tr = tracer.AddComponent<SpriteRenderer>();
            tr.sprite = tracerSprite;
            tr.sortingOrder = 20;
            tracer.transform.localScale = new Vector3(3.2f, 2.2f, 1f);

            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);

                foreach (Transform ch in stage.transform)
                {
                    if (!ch.name.StartsWith("bld")) continue;
                    var p = ch.localPosition;
                    p.x -= Time.deltaTime * buildingSpeed * p.z;
                    if (p.x < -5.5f) p.x += buildingCount * 1.15f;
                    ch.localPosition = p;
                }

                if (target != null)
                {
                    float ak = Mathf.Pow(k, approachCurve);
                    var p = target.localPosition;
                    p.x = Mathf.Lerp(targetStartX, targetStopX, Mathf.SmoothStep(0f, 1f, ak));
                    target.localPosition = p;
                    target.localScale = Vector3.one
                        * (Mathf.Lerp(0.75f, targetEndScale, ak) * targetScaleMul);
                }

                tracer.transform.localPosition =
                    new Vector3(Mathf.Lerp(-3.2f, targetStopX, k * tracerReach), 0f, 0f);
                cutCamera.orthographicSize = Mathf.Lerp(cutOrthoSize, cutOrthoSize * 0.76f, k);
                yield return null;
            }
            Destroy(tracer);
        }

        /// <summary>Hold slow motion for the configured beat, unscaled.</summary>
        public IEnumerator RunSlowMotion()
        {
            Time.timeScale = slowScale;
            yield return new WaitForSecondsRealtime(slowSeconds);
            Time.timeScale = 1f;
        }

        // ----------------------------------------------------------- letterbox

        public void SetLetterbox(bool on)
        {
            if (on)
            {
                if (barTop != null) return;
                barTop = MakeBar(1f);
                barBottom = MakeBar(-1f);
            }
            else
            {
                if (barTop != null) Destroy(barTop);
                if (barBottom != null) Destroy(barBottom);
                barTop = barBottom = null;
            }
        }

        GameObject MakeBar(float dir)
        {
            var go = new GameObject("ULT_Bar");
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 500;
            var img = new GameObject("img", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            img.transform.SetParent(go.transform, false);
            var rt = img.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, dir > 0 ? 1f - letterbox : 0f);
            rt.anchorMax = new Vector2(1f, dir > 0 ? 1f : letterbox);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            img.GetComponent<UnityEngine.UI.Image>().color = Color.black;
            return go;
        }
    }
}

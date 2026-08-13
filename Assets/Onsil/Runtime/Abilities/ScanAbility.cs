using System.Collections;
using UnityEngine;
using Onsil.Vfx;

namespace Onsil.Abilities
{
    /// <summary>
    /// Skill 1 - jetpack up, sweep the target, leave a lock-on mark behind.
    /// The ONLY ability that applies a mark.
    /// </summary>
    /// <remarks>
    /// She never pulls the trigger here, so playback must stop at the cell BEFORE
    /// the fire cell. Running to the end of the "aim" range would include the
    /// firing pose and make the scan look like a shot.
    /// </remarks>
    public class ScanAbility : AirborneAbility
    {
        [Header("scan")]
        public Sprite scanLine;
        public float sweepTime = 0.9f;
        [Tooltip("How many times the band travels the target.")]
        public int sweepPasses = 2;
        public Color scanColor = new Color(0.45f, 0.95f, 1f, 1f);

        void Reset() { abilityId = "scan"; consumesMark = false; appliesMark = true; }

        public override IEnumerator Run()
        {
            yield return TakeOff();

            var aim = clip.RangeOrAll("aim");
            var fire = clip.RangeOrAll("fire");

            // stop one cell short of the trigger
            yield return Ctx.Animator.Play(clip, aim.first, fire.first - 1, clipFps + 2f,
                                           _ => Burn(0.45f));

            yield return Sweep(fire.first - 1);
            // the mark itself is applied by AbilityRunner via appliesMark

            yield return Land();
        }

        /// Sight line held on target while a band sweeps up and down it.
        IEnumerator Sweep(int holdCell)
        {
            if (Ctx.Target == null) yield break;

            Vector3 origin = Ctx.Self.position;
            if (Ctx.Muzzle != null)
                origin += (Vector3)Ctx.Muzzle.OffsetFor(MuzzleRig.Stance.Airborne);
            Vector3 focus = Ctx.Target.position + Vector3.up * 0.45f;

            var beam = NewSprite("ScanBeam", scanLine, 40);
            Vector3 d = focus - origin;
            beam.transform.position = origin + d * 0.5f;
            beam.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            beam.transform.localScale = new Vector3(d.magnitude * 1.9f, 0.05f, 1f);

            var band = NewSprite("ScanBand", scanLine, 41);

            float t = 0f;
            while (t < sweepTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / sweepTime);
                float fade = Mathf.Clamp01(k / 0.12f) * Mathf.Clamp01((1f - k) / 0.15f);

                SetAlpha(beam, 0.75f * fade);

                float p = Mathf.PingPong(k * sweepPasses, 1f);
                band.transform.position =
                    new Vector3(focus.x, Mathf.Lerp(focus.y + 0.6f, focus.y - 0.6f, p), -0.25f);
                band.transform.localScale = new Vector3(1.6f, 0.05f, 1f);
                SetAlpha(band, 0.95f * fade);

                Ctx.Animator.Show(clip, holdCell);     // stay on the aim pose
                Burn(0.45f);
                yield return null;
            }
            Destroy(beam);
            Destroy(band);
        }

        GameObject NewSprite(string name, Sprite s, int order)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            var c = scanColor; c.a = 0f; sr.color = c;
            return go;
        }

        static void SetAlpha(GameObject go, float a)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            var c = sr.color; c.a = a; sr.color = c;
        }
    }
}

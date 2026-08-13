using System;
using System.Collections;
using UnityEngine;
using Onsil.Vfx;

namespace Onsil.Combat
{
    /// <summary>
    /// A simple telegraphed attack: wind up, launch a projectile, and let the
    /// defender's <see cref="ParryReceiver"/> judge the press against the arrival.
    /// </summary>
    /// <remarks>
    /// The parry window is opened at LAUNCH with the exact flight time, so what the
    /// player sees (the projectile crossing the gap) and what the judge measures are
    /// the same number. Driving the window off animation frames instead would drift
    /// the moment anyone retimed the clip.
    ///
    /// Flight runs on unscaled time so a hitstop from a previous parry cannot slow
    /// the next round and silently widen the window.
    /// </remarks>
    public class EnemyAttacker : MonoBehaviour
    {
        [Header("target")]
        public ParryReceiver defender;

        [Header("telegraph")]
        [Tooltip("Warning time before the projectile launches.")]
        public float windUp = 0.55f;
        [Tooltip("Colour the attacker flashes while winding up.")]
        public Color tellColor = new Color(1f, 0.45f, 0.4f, 1f);
        public float tellPulseHz = 6f;

        [Header("projectile")]
        public Sprite projectileSprite;
        [Tooltip("Seconds the projectile takes to cross. This IS the parry window.")]
        public float flightTime = 0.85f;
        public float projectileScale = 1f;
        public Vector2 muzzleOffset = new Vector2(-0.35f, 0.5f);
        [Tooltip("Where it lands on the defender, relative to their pivot.")]
        public Vector2 impactOffset = new Vector2(0.3f, 0.55f);
        [Tooltip("Height of the arc. 0 flies flat.")]
        public float arcHeight = 0.25f;
        public int sortingOrder = 40;

        [Header("on hit")]
        public float hitShakeAmount = 0.3f;
        public float hitShakeTime = 0.2f;
        public Sprite hitSprite;

        [Header("cooldown")]
        public float cooldown = 0.6f;

        SpriteRenderer body;
        CameraShaker shaker;
        Color restColor;
        float readyAt;

        /// <summary>Fires when the round resolves: true when the defender parried.</summary>
        public event Action<bool> Resolved;

        public bool Busy { get; private set; }
        public bool Ready => !Busy && Time.unscaledTime >= readyAt;

        void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            if (body != null) restColor = body.color;
            if (Camera.main != null) shaker = Camera.main.GetComponent<CameraShaker>();
        }

        /// <summary>Start one attack. Ignored while busy or cooling down.</summary>
        public bool Attack()
        {
            if (!Ready || defender == null) return false;
            StartCoroutine(Routine());
            return true;
        }

        IEnumerator Routine()
        {
            Busy = true;

            // ---- wind up: pulse so the player can read the tell ----
            float t = 0f;
            while (t < windUp)
            {
                t += Time.unscaledDeltaTime;
                if (body != null)
                {
                    float p = 0.5f + 0.5f * Mathf.Sin(t * tellPulseHz * Mathf.PI * 2f);
                    body.color = Color.Lerp(restColor, tellColor, p);
                }
                yield return null;
            }
            if (body != null) body.color = restColor;

            // ---- launch: the window matches the flight exactly ----
            Vector3 from = transform.position + (Vector3)muzzleOffset;
            Vector3 to = defender.transform.position + (Vector3)impactOffset;
            defender.OpenWindow(flightTime);

            var shot = new GameObject("EnemyShot");
            var sr = shot.AddComponent<SpriteRenderer>();
            sr.sprite = projectileSprite;
            sr.sortingOrder = sortingOrder;
            shot.transform.localScale = Vector3.one * projectileScale;
            Vector3 dir = to - from;
            shot.transform.rotation =
                Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

            float f = 0f;
            while (f < flightTime)
            {
                f += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(f / flightTime);
                Vector3 p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * arcHeight;      // gentle arc
                shot.transform.position = p;

                // a parry that lands mid-flight stops the round early
                if (defender.Blocks())
                {
                    Deflect(shot, dir);
                    defender.CloseWindow();
                    Busy = false;
                    readyAt = Time.unscaledTime + cooldown;
                    if (Resolved != null) Resolved(true);
                    yield break;
                }
                yield return null;
            }

            // ---- arrival ----
            bool parried = defender.Blocks();
            defender.CloseWindow();

            if (parried) Deflect(shot, dir);
            else
            {
                Impact(to);
                Destroy(shot);
                if (shaker != null) shaker.Shake(hitShakeTime, hitShakeAmount);
            }

            Busy = false;
            readyAt = Time.unscaledTime + cooldown;
            if (Resolved != null) Resolved(parried);
        }

        /// <summary>Send the round back the way it came.</summary>
        void Deflect(GameObject shot, Vector3 incoming)
        {
            StartCoroutine(DeflectRoutine(shot, incoming));
        }

        IEnumerator DeflectRoutine(GameObject shot, Vector3 incoming)
        {
            if (shot == null) yield break;
            var sr = shot.GetComponent<SpriteRenderer>();
            Vector3 start = shot.transform.position;
            Vector3 away = start - incoming.normalized * 6f
                         + Vector3.up * UnityEngine.Random.Range(0.4f, 1.2f);
            float spin = UnityEngine.Random.Range(-720f, 720f);

            float t = 0f, dur = 0.45f;
            while (shot != null && t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = t / dur;
                shot.transform.position = Vector3.Lerp(start, away, k * k);
                shot.transform.Rotate(0f, 0f, spin * Time.unscaledDeltaTime);
                if (sr != null) { var c = sr.color; c.a = 1f - k; sr.color = c; }
                yield return null;
            }
            if (shot != null) Destroy(shot);
        }

        void Impact(Vector3 at)
        {
            if (hitSprite == null) return;
            StartCoroutine(ImpactRoutine(at));
        }

        IEnumerator ImpactRoutine(Vector3 at)
        {
            var go = new GameObject("EnemyHit");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = hitSprite;
            sr.sortingOrder = sortingOrder + 2;
            sr.color = new Color(1f, 0.5f, 0.4f, 1f);
            go.transform.position = at;

            float t = 0f, dur = 0.28f;
            while (go != null && t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = t / dur;
                float s = Mathf.Lerp(0.3f, 2.4f, Mathf.Sqrt(k));
                go.transform.localScale = new Vector3(s, s, 1f);
                var c = sr.color; c.a = 1f - k; sr.color = c;
                yield return null;
            }
            if (go != null) Destroy(go);
        }
    }
}

using System.Collections;
using UnityEngine;

namespace Onsil.Vfx
{
    /// <summary>
    /// Snap zoom for impact beats. Punches the orthographic size in, then eases back.
    /// </summary>
    /// <remarks>
    /// Runs on unscaled time so it still reads during a hitstop - that is the whole
    /// point of the effect. Stacking calls is safe: a new punch replaces the old one
    /// and always returns to the size captured on the FIRST punch, so repeated
    /// parries cannot ratchet the camera inward.
    /// </remarks>
    [RequireComponent(typeof(Camera))]
    public class CameraPunch : MonoBehaviour
    {
        [Tooltip("How far in the snap goes. 0.85 is a 15% zoom in.")]
        public float defaultZoom = 0.85f;
        [Tooltip("Seconds to reach the zoomed size.")]
        public float defaultIn = 0.05f;
        [Tooltip("Seconds to ease back out.")]
        public float defaultOut = 0.35f;
        [Tooltip("Roll applied with the zoom, degrees. Randomised per punch.")]
        public float defaultTilt = 4f;
        [Tooltip("How much the magnitude can vary. 0.4 gives 60%-140% of the value.")]
        public float tiltJitter = 0.4f;
        [Tooltip("Chance the roll flips away from the previous one. 1 always " +
                 "alternates, 0.5 is free coin flip. High values stop the camera " +
                 "leaning the same way twice in a row.")]
        public float tiltAlternateBias = 0.8f;

        Camera cam;
        CameraShaker shaker;
        Coroutine running;
        float restSize;
        Vector3 restPosition;
        Quaternion restRotation;
        bool captured;
        float lastTiltSign;          // remembers which way the last parry leaned

        /// <summary>Signed roll used by the most recent punch, degrees.</summary>
        public float LastTilt { get; private set; }

        void Awake()
        {
            cam = GetComponent<Camera>();
            shaker = GetComponent<CameraShaker>();
            lastTiltSign = Random.value < 0.5f ? -1f : 1f;
            Capture();
        }

        /// <summary>Re-read the resting size, position and rotation.</summary>
        public void Capture()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (shaker == null) shaker = GetComponent<CameraShaker>();
            restSize = cam.orthographicSize;
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            captured = true;
        }

        public void Punch() { Punch(defaultZoom, defaultIn, defaultOut, defaultTilt); }

        public void Punch(float zoom, float inTime, float outTime)
        {
            Punch(zoom, inTime, outTime, defaultTilt);
        }

        public void Punch(float zoom, float inTime, float outTime, float tilt)
        {
            PunchAt(zoom, inTime, outTime, tilt, null);
        }

        /// <summary>Punch pivoting on a world point so it stays put on screen.</summary>
        public void PunchAt(float zoom, float inTime, float outTime, float tilt,
                            Vector3? focusWorld)
        {
            if (!captured) Capture();
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Routine(zoom, inTime, outTime, tilt, focusWorld));
        }

        void Apply(float size, Vector3 pos, Quaternion rot)
        {
            cam.orthographicSize = size;
            transform.localRotation = rot;
            if (shaker != null) shaker.BasePosition = pos;   // shaker adds its offset
            else transform.localPosition = pos;
        }

        /// <summary>
        /// Camera placement that keeps <paramref name="focus"/> at the same screen
        /// spot for the given size and rotation.
        /// </summary>
        Vector3 PositionFor(Vector3 focus, float size, Quaternion rot)
        {
            Vector3 local = Quaternion.Inverse(restRotation) * (focus - restPosition);
            float k = size / Mathf.Max(restSize, 0.0001f);
            Vector3 scaled = new Vector3(local.x * k, local.y * k, local.z);
            return focus - rot * scaled;
        }

        /// <summary>
        /// Pick a roll that reads as a fresh reaction rather than a repeat: the
        /// sign is biased away from the previous parry and the magnitude jitters,
        /// so back-to-back blocks never lean identically.
        /// </summary>
        float NextTilt(float baseTilt)
        {
            if (Mathf.Approximately(baseTilt, 0f)) return 0f;

            float sign = Random.value < Mathf.Clamp01(tiltAlternateBias)
                       ? -lastTiltSign            // flip away from last time
                       : lastTiltSign;
            lastTiltSign = sign;

            float j = Mathf.Max(0f, tiltJitter);
            float mag = Mathf.Abs(baseTilt) * Random.Range(1f - j, 1f + j);
            LastTilt = sign * mag;
            return LastTilt;
        }

        IEnumerator Routine(float zoom, float inTime, float outTime, float tilt,
                            Vector3? focusWorld)
        {
            float fromSize = cam.orthographicSize;
            Vector3 fromPos = shaker != null ? shaker.BasePosition : transform.localPosition;
            Quaternion fromRot = transform.localRotation;

            float toSize = restSize * Mathf.Max(0.05f, zoom);
            Quaternion toRot = restRotation * Quaternion.Euler(0f, 0f, NextTilt(tilt));
            Vector3 toPos = focusWorld.HasValue
                          ? PositionFor(focusWorld.Value, toSize, toRot)
                          : restPosition;

            float t = 0f;
            while (t < inTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / inTime);
                Apply(Mathf.Lerp(fromSize, toSize, k),
                      Vector3.Lerp(fromPos, toPos, k),
                      Quaternion.Slerp(fromRot, toRot, k));
                yield return null;
            }
            Apply(toSize, toPos, toRot);

            t = 0f;
            while (t < outTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / outTime);
                // ease-out cubic so it settles instead of snapping back
                float e = 1f - Mathf.Pow(1f - k, 3f);
                Apply(Mathf.Lerp(toSize, restSize, e),
                      Vector3.Lerp(toPos, restPosition, e),
                      Quaternion.Slerp(toRot, restRotation, e));
                yield return null;
            }
            Apply(restSize, restPosition, restRotation);
            running = null;
        }
    }
}

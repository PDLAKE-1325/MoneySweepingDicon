using System.Collections;
using UnityEngine;

namespace Onsil.Vfx
{
    /// <summary>
    /// Additive positional shake. Lives on the camera so multiple sources can
    /// stack without fighting over the transform.
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        [Range(0f, 1f)] public float defaultAmplitude = 0.18f;

        Vector3 basePosition;
        Vector3 offset;
        int active;

        void Awake() => basePosition = transform.localPosition;
        void OnEnable() => basePosition = transform.localPosition;

        void LateUpdate()
        {
            transform.localPosition = basePosition + offset;
        }

        /// <summary>Re-read the rest pose. Call after deliberately moving the camera.</summary>
        public void Rebase() => basePosition = transform.localPosition;

        public void Shake(float duration, float amplitude = -1f)
        {
            if (amplitude < 0f) amplitude = defaultAmplitude;
            StartCoroutine(ShakeRoutine(duration, amplitude));
        }

        IEnumerator ShakeRoutine(float duration, float amplitude)
        {
            active++;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float falloff = 1f - t / duration;
                offset = (Vector3)(Random.insideUnitCircle * amplitude * falloff);
                yield return null;
            }
            active--;
            if (active <= 0) { active = 0; offset = Vector3.zero; }
        }
    }
}

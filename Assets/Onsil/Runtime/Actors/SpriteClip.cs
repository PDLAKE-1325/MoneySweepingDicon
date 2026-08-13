using UnityEngine;

namespace Onsil.Actors
{
    /// <summary>
    /// One playable animation: an ordered sprite list plus the named cell ranges
    /// inside it. Authored as an asset so designers can retime without touching code.
    /// </summary>
    /// <remarks>
    /// Sheets are sliced 288x256 with a custom pivot on the character's feet.
    /// Cell indices below are indices into <see cref="frames"/>, not source
    /// Blender frames.
    /// </remarks>
    [CreateAssetMenu(menuName = "Onsil/Sprite Clip", fileName = "SpriteClip")]
    public class SpriteClip : ScriptableObject
    {
        [System.Serializable]
        public struct Range
        {
            [Tooltip("Identifier used by abilities, e.g. \"aim\", \"fire\", \"recover\"")]
            public string id;
            [Min(0)] public int first;
            [Min(0)] public int last;

            public int Length => Mathf.Abs(last - first) + 1;
        }

        [Header("frames")]
        [Tooltip("Sprites in playback order. Fill with the Onsil/Slice Sheet tool.")]
        public Sprite[] frames;

        [Tooltip("Default playback rate. Abilities may override per range.")]
        [Range(1f, 60f)] public float fps = 14f;

        [Tooltip("Idle-style clips loop; action clips usually do not.")]
        public bool loop;

        [Header("named ranges")]
        public Range[] ranges;

        public int FrameCount => frames != null ? frames.Length : 0;

        public Sprite Frame(int i)
        {
            if (frames == null || frames.Length == 0) return null;
            return frames[Mathf.Clamp(i, 0, frames.Length - 1)];
        }

        /// <summary>Look up a named range. Returns false when the id is absent.</summary>
        public bool TryGetRange(string id, out Range range)
        {
            if (ranges != null)
            {
                for (int i = 0; i < ranges.Length; i++)
                {
                    if (ranges[i].id == id) { range = ranges[i]; return true; }
                }
            }
            range = default;
            return false;
        }

        /// <summary>Range lookup that falls back to the whole clip.</summary>
        public Range RangeOrAll(string id)
        {
            if (TryGetRange(id, out var r)) return r;
            return new Range { id = id, first = 0, last = Mathf.Max(0, FrameCount - 1) };
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (ranges == null) return;
            int max = Mathf.Max(0, FrameCount - 1);
            for (int i = 0; i < ranges.Length; i++)
            {
                ranges[i].first = Mathf.Clamp(ranges[i].first, 0, max);
                ranges[i].last = Mathf.Clamp(ranges[i].last, 0, max);
            }
        }
#endif
    }
}

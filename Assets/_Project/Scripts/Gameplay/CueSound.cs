using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>The clips of one cue and how loud it plays. One clip is picked at random each time.</summary>
    [Serializable]
    public sealed class CueSound
    {
        [Tooltip("Variations of the same sound. Empty means the cue is silent.")]
        [SerializeField]
        private AudioClip[] _clips = new AudioClip[0];

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        public float Volume => _volume;

        /// <summary>A random variation, so repeated sounds (troops, dice) never feel mechanical. Null if empty.</summary>
        public AudioClip PickClip() =>
            _clips.Length == 0 ? null : _clips[UnityEngine.Random.Range(0, _clips.Length)];
    }
}

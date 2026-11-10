using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Plays the match's sound effects through two sources. Every roll (combat dice, Destiny Die,
    /// Offering re-roll) is the same clip on its own voice: a new roll restarts it and nothing ever
    /// stacks. The clip is exactly as long as the dice tumble and ends with the landing clack, so
    /// the sound starts with the dice and ends as they land. Every other cue plays as an
    /// overlapping one-shot.
    /// It only knows cues, never the rules: MatchPresenter decides when each one plays and
    /// <see cref="SfxCueChooser"/> picks the landing stings.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        // A roll must never be inaudible by a stray Inspector value.
        private const float MinRollVolume = 0.1f;

        [SerializeField]
        private AudioCues _cues;

        [Tooltip("Rolls only: one voice that always plays the one roll clip from its start.")]
        [SerializeField]
        private AudioSource _rollSource;

        [Tooltip("Every other cue, as overlapping one-shots.")]
        [SerializeField]
        private AudioSource _effectsSource;

        [Tooltip(
            "Volume multiplier for secondary cues (temple chime, a bot nation falling), so they stay under the main ones."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _subtleVolume = 0.4f;

        private AudioClip _rollClip;

        /// <summary>Mutes the sources rather than AudioListener.volume, so no global state outlives the scene.</summary>
        public void SetMuted(bool muted)
        {
            _rollSource.mute = muted;
            _effectsSource.mute = muted;
        }

        /// <summary>Plays a one-shot cue. Rolls go through <see cref="PlayRoll"/>.</summary>
        public void Play(SfxCue cue) => Play(cue, 1f);

        /// <summary>Plays a cue quieter, as an accent under the main cue of the same moment.</summary>
        public void PlaySubtle(SfxCue cue) => Play(cue, _subtleVolume);

        private void Awake()
        {
            // The clip is set once and never swapped: changing .clip on a source that is playing or
            // just stopped can drop the next Play, which is how rolls used to go silent.
            _rollClip = _cues.DiceRoll;
            if (_rollClip == null)
            {
                Debug.LogWarning("AudioCues has no dice roll clip: rolls will be silent.", _cues);
                return;
            }

            _rollSource.clip = _rollClip;
            _rollSource.playOnAwake = false;
            _rollSource.loop = false;
            _rollSource.spatialBlend = 0f;
            // Highest priority, so the mixer never steals the voice when effects pile up.
            _rollSource.priority = 0;
            // Normally loaded with the scene (Preload Audio Data); this is the net for a build that
            // did not, so the first roll still starts on its frame instead of after a load.
            if (_rollClip.loadState == AudioDataLoadState.Unloaded)
                _rollClip.LoadAudioData();
        }

        private void Play(SfxCue cue, float volumeScale)
        {
            CueSound sound = _cues.Get(cue);
            AudioClip clip = sound.PickClip();
            if (clip == null)
                return;
            _effectsSource.PlayOneShot(clip, sound.Volume * volumeScale);
        }

        /// <summary>
        /// Starts the roll sound now, replacing any roll still playing, and returns how long the
        /// dice must tumble to land with its final clack: the clip's own length. The caller starts
        /// the dice tumble in the same call, so sound and motion begin on the same frame.
        /// </summary>
        /// <param name="fallbackSeconds">The tumble to use when there is no roll clip.</param>
        public float PlayRoll(float fallbackSeconds)
        {
            if (_rollClip == null)
                return fallbackSeconds;

            _rollSource.volume = Mathf.Clamp(_cues.DiceRollVolume, MinRollVolume, 1f);
            // Stop first so a roll that is still sounding restarts cleanly from sample 0.
            _rollSource.Stop();
            _rollSource.Play();
            return _rollClip.length;
        }

        /// <summary>Silences a roll whose dice will never land (a skipped Destiny Die, the match ending).</summary>
        public void StopRoll() => _rollSource.Stop();
    }
}

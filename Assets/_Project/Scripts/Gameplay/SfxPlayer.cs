using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Plays the match's sound effects through two sources. Rolls (dice, Destiny Die, Offering
    /// shake) share one voice that only a new roll replaces, so clatter never stacks; each roll is
    /// timed to end exactly when the dice land, so it is never cut. Every other cue plays as an
    /// overlapping one-shot.
    /// It only knows cues, never the rules: MatchController decides when each one plays and
    /// <see cref="SfxCueChooser"/> picks the landing stings.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField]
        private AudioCues _cues;

        [Tooltip("Rolls: one at a time, ending when the dice land.")]
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

        private void Play(SfxCue cue, float volumeScale)
        {
            CueSound sound = _cues.Get(cue);
            AudioClip clip = sound.PickClip();
            if (clip == null)
                return;
            _effectsSource.PlayOneShot(clip, sound.Volume * volumeScale);
        }

        /// <summary>
        /// Starts a roll cue (combat throw, Destiny Die, Offering shake), replacing any roll still
        /// playing, and returns how long the dice should tumble so they land with the sound: a clip
        /// longer than the shortest tumble stretches the tumble; a shorter one starts late, so the
        /// dice fly silently and clatter as they land.
        /// </summary>
        public float PlayRoll(SfxCue cue, float minTumbleSeconds)
        {
            CueSound sound = _cues.Get(cue);
            AudioClip clip = sound.PickClip();
            if (clip == null)
                return minTumbleSeconds;

            float tumble = Mathf.Max(minTumbleSeconds, clip.length);
            _rollSource.clip = clip;
            _rollSource.volume = sound.Volume;
            _rollSource.PlayDelayed(tumble - clip.length);
            return tumble;
        }

        /// <summary>Silences a roll whose dice will never land (a skipped Destiny Die, the match ending).</summary>
        public void StopRoll() => _rollSource.Stop();
    }
}

using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Plays the match's sound effects through two sources. Rolls (dice, Destiny Die, Offering
    /// shake) share one voice that a new roll replaces and the landing stops, so clatter never
    /// stacks or outlasts the dice; every other cue plays as an overlapping one-shot.
    /// It only knows cues, never the rules: MatchController decides when each one plays and
    /// <see cref="SfxCueChooser"/> picks the landing stings.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField]
        private AudioCues _cues;

        [Tooltip("Rolls: one at a time, stopped when the dice land.")]
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

        public void Play(SfxCue cue) => Play(cue, 1f);

        /// <summary>Plays a cue quieter, as an accent under the main cue of the same moment.</summary>
        public void PlaySubtle(SfxCue cue) => Play(cue, _subtleVolume);

        private void Play(SfxCue cue, float volumeScale)
        {
            CueSound sound = _cues.Get(cue);
            AudioClip clip = sound.PickClip();
            if (clip == null)
                return;

            if (IsRoll(cue))
            {
                _rollSource.clip = clip;
                _rollSource.volume = sound.Volume * volumeScale;
                _rollSource.Play();
                return;
            }
            _effectsSource.PlayOneShot(clip, sound.Volume * volumeScale);
        }

        /// <summary>The dice landed: the roll sound ends with them.</summary>
        public void StopRoll() => _rollSource.Stop();

        private static bool IsRoll(SfxCue cue) =>
            cue == SfxCue.CombatRoll || cue == SfxCue.DestinyRoll || cue == SfxCue.Offering;
    }
}

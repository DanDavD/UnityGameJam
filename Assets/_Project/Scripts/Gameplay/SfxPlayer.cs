using System.Collections;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Plays the match's sound effects through two sources. Every roll (combat dice, Destiny Die,
    /// Offering re-roll) is the same clip on its own voice: a new roll restarts it and nothing ever
    /// stacks. The clip is exactly as long as the dice tumble and ends with the landing clack, so
    /// the sound starts with the dice and ends as they land. Every other cue plays as an
    /// overlapping one-shot. The main theme loops on a third, low-priority source created at runtime.
    /// It only knows cues, never the rules: MatchPresenter decides when each one plays and
    /// <see cref="SfxCueChooser"/> picks the landing stings.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        // A roll must never be inaudible by a stray Inspector value.
        private const float MinRollVolume = 0.1f;

        // Lower is more important: under the roll (0), above default one-shots (128).
        private const int MusicPriority = 64;

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

        [Tooltip("Seconds an interrupted roll takes to fade out instead of cutting off.")]
        [SerializeField, Min(0.01f)]
        private float _fadeSeconds = 0.1f;

        [Tooltip(
            "Seconds the music takes to fade out when the match ends, so the end sting plays on silence."
        )]
        [SerializeField, Min(0.01f)]
        private float _musicFadeSeconds = 1.5f;

        private AudioClip _rollClip;

        // Carries the tail of an interrupted roll so the new roll can start on its own frame.
        private AudioSource _tailSource;
        private Coroutine _tailFade;
        private Coroutine _stopFade;

        // Created in Awake, so the scene needs no extra component; null when there is no music clip.
        private AudioSource _musicSource;
        private Coroutine _musicFade;

        /// <summary>Mutes the sources rather than AudioListener.volume, so no global state outlives the scene.</summary>
        public void SetMuted(bool muted)
        {
            _rollSource.mute = muted;
            _effectsSource.mute = muted;
            if (_tailSource != null)
                _tailSource.mute = muted;
            if (_musicSource != null)
                _musicSource.mute = muted;
        }

        /// <summary>Plays a one-shot cue. Rolls go through <see cref="PlayRoll"/>.</summary>
        public void Play(SfxCue cue) => Play(cue, 1f);

        /// <summary>Plays a cue quieter, as an accent under the main cue of the same moment.</summary>
        public void PlaySubtle(SfxCue cue) => Play(cue, _subtleVolume);

        private void Awake()
        {
            // Before the roll setup, whose early return must not leave the match without music.
            SetUpMusic();

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
            _tailSource = gameObject.AddComponent<AudioSource>();
            _tailSource.playOnAwake = false;
            _tailSource.spatialBlend = 0f;
            _tailSource.priority = 0;
            _tailSource.clip = _rollClip;
            _tailSource.outputAudioMixerGroup = _rollSource.outputAudioMixerGroup;
            // Normally loaded with the scene (Preload Audio Data); this is the net for a build that
            // did not, so the first roll still starts on its frame instead of after a load.
            if (_rollClip.loadState == AudioDataLoadState.Unloaded)
                _rollClip.LoadAudioData();
        }

        private void SetUpMusic()
        {
            AudioClip music = _cues.Music;
            if (music == null)
            {
                Debug.LogWarning(
                    "AudioCues has no music clip: the match will have no music.",
                    _cues
                );
                return;
            }

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.clip = music;
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = _cues.MusicVolume;
            // Lower number = more important. Below the rolls (0) so a busy moment never steals a
            // roll's voice for the music, above the default (128) so effects never cut the theme.
            _musicSource.priority = MusicPriority;
            _musicSource.outputAudioMixerGroup = _effectsSource.outputAudioMixerGroup;
            StartCoroutine(PlayMusicWhenLoaded(music));
        }

        // The theme loads in background so it never delays the scene. A Play() issued while the
        // clip is still loading is dropped and leaves the source stuck silent, so wait for it.
        private IEnumerator PlayMusicWhenLoaded(AudioClip music)
        {
            if (music.loadState == AudioDataLoadState.Unloaded)
                music.LoadAudioData();
            while (music.loadState == AudioDataLoadState.Loading)
                yield return null;
            if (music.loadState == AudioDataLoadState.Loaded)
                _musicSource.Play();
            else
                Debug.LogWarning(
                    "The music clip failed to load: the match will have no music.",
                    music
                );
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
            // A roll still sounding hands its tail to the fade source, then this one restarts from
            // sample 0 on the same frame: the old sound fades out instead of cutting off.
            if (_stopFade != null)
            {
                StopCoroutine(_stopFade);
                _stopFade = null;
            }
            if (_rollSource.isPlaying)
                HandOverTail();
            _rollSource.Stop();
            _rollSource.Play();
            return _rollClip.length;
        }

        /// <summary>Silences a roll whose dice will never land (a skipped Destiny Die, the match ending).</summary>
        public void StopRoll()
        {
            if (!_rollSource.isPlaying || _stopFade != null)
                return;
            _stopFade = StartCoroutine(FadeOut(_rollSource, _fadeSeconds, () => _stopFade = null));
        }

        /// <summary>Fades the theme out (the match ended) so the victory or defeat sting plays on silence.</summary>
        public void FadeOutMusic()
        {
            if (_musicSource == null || !_musicSource.isPlaying || _musicFade != null)
                return;
            _musicFade = StartCoroutine(
                FadeOut(_musicSource, _musicFadeSeconds, () => _musicFade = null)
            );
        }

        private void HandOverTail()
        {
            if (_tailFade != null)
                StopCoroutine(_tailFade);
            _tailSource.Stop();
            _tailSource.volume = _rollSource.volume;
            _tailSource.Play();
            _tailSource.time = Mathf.Min(_rollSource.time, _rollClip.length - 0.01f);
            _tailFade = StartCoroutine(FadeOut(_tailSource, _fadeSeconds, () => _tailFade = null));
        }

        // Real time, so a fade still finishes if the game's time scale is ever changed.
        private IEnumerator FadeOut(AudioSource source, float seconds, System.Action done)
        {
            float start = source.volume;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                source.volume = start * (1f - t / seconds);
                yield return null;
            }
            source.Stop();
            source.volume = start;
            done();
        }
    }
}

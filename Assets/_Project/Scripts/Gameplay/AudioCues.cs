using System;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Which clips play for each <see cref="SfxCue"/> and how loud, so sounds are tuned in the
    /// Inspector without touching code. Clips and their licenses live in Assets/_Project/Audio.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioCues", menuName = "GodBoard/Audio Cues")]
    public sealed class AudioCues : ScriptableObject
    {
        [Header("Dice")]
        [Tooltip(
            "The one sound of every roll (combat, Destiny Die, Offering re-roll). Its length is the tumble: it starts with the dice and ends with the landing clack."
        )]
        [SerializeField]
        private AudioClip _diceRoll;

        [SerializeField, Range(0.1f, 1f)]
        private float _diceRollVolume = 0.9f;

        [Header("Dice landing (stings after the roll)")]
        [Tooltip("A landed roll decided by a sacred number (or a Destiny that favors the player).")]
        [SerializeField]
        private CueSound _sacred = new CueSound();

        [Tooltip("A landed roll with a cursed number (or a Destiny that hurts the player).")]
        [SerializeField]
        private CueSound _cursed = new CueSound();

        [Header("Board")]
        [SerializeField]
        private CueSound _troopPlaced = new CueSound();

        [SerializeField]
        private CueSound _capture = new CueSound();

        [SerializeField]
        private CueSound _turnChange = new CueSound();

        [Header("Match end")]
        [SerializeField]
        private CueSound _victory = new CueSound();

        [SerializeField]
        private CueSound _defeat = new CueSound();

        [Header("Interface")]
        [SerializeField]
        private CueSound _click = new CueSound();

        [Tooltip("A board click that cannot do anything; the prompt says why.")]
        [SerializeField]
        private CueSound _invalid = new CueSound();

        public AudioClip DiceRoll => _diceRoll;

        public float DiceRollVolume => _diceRollVolume;

        public CueSound Get(SfxCue cue) =>
            cue switch
            {
                SfxCue.Sacred => _sacred,
                SfxCue.Cursed => _cursed,
                SfxCue.TroopPlaced => _troopPlaced,
                SfxCue.Capture => _capture,
                SfxCue.TurnChange => _turnChange,
                SfxCue.Victory => _victory,
                SfxCue.Defeat => _defeat,
                SfxCue.Click => _click,
                SfxCue.Invalid => _invalid,
                _ => throw new ArgumentOutOfRangeException(nameof(cue), cue, null),
            };
    }
}

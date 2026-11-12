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
        [Header("Music")]
        [Tooltip("The main theme. Loops for the whole scene (menu and match); restarts on Play again / Main menu.")]
        [SerializeField]
        private AudioClip _music;

        [Tooltip("Kept low: the theme is mastered loud and must sit under every effect.")]
        [SerializeField, Range(0f, 1f)]
        private float _musicVolume = 0.25f;

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

        [Header("God powers")]
        [SerializeField]
        private CueSound _dayZeus = new CueSound();

        [SerializeField]
        private CueSound _dayRa = new CueSound();

        [SerializeField]
        private CueSound _dayThor = new CueSound();

        [SerializeField]
        private CueSound _dayMorrigan = new CueSound();

        [Tooltip("Destiny Die lands on 4. Replaces the sacred/cursed sting.")]
        [SerializeField]
        private CueSound _badOmen = new CueSound();

        [Tooltip("Destiny Die lands on 3. Replaces the sacred/cursed sting.")]
        [SerializeField]
        private CueSound _goodFortune = new CueSound();

        [SerializeField]
        private CueSound _wrathZeus = new CueSound();

        [SerializeField]
        private CueSound _wrathThor = new CueSound();

        [SerializeField]
        private CueSound _wrathRa = new CueSound();

        [SerializeField]
        private CueSound _wrathMorrigan = new CueSound();

        [Header("Menu and bots")]
        [Tooltip("Pointer enters a god card in the main menu. Calm and quiet.")]
        [SerializeField]
        private CueSound _cardHover = new CueSound();

        [Tooltip("A combat between two bots, and each duel verdict of the turn-order draw.")]
        [SerializeField]
        private CueSound _botResult = new CueSound();

        [Tooltip("Soft good news: the human wins a combat, or gains more troops than the base.")]
        [SerializeField]
        private CueSound _softGood = new CueSound();

        [Tooltip("Soft bad news: the human loses a combat, or gains fewer troops than the base.")]
        [SerializeField]
        private CueSound _softBad = new CueSound();

        [Tooltip("The human wins a combat without a sacred die of their own.")]
        [SerializeField]
        private CueSound _neutralWin = new CueSound();

        [Tooltip("The human loses a combat and no sacred die was involved.")]
        [SerializeField]
        private CueSound _neutralLose = new CueSound();

        public AudioClip Music => _music;

        public float MusicVolume => _musicVolume;

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
                SfxCue.DayZeus => _dayZeus,
                SfxCue.DayRa => _dayRa,
                SfxCue.DayThor => _dayThor,
                SfxCue.DayMorrigan => _dayMorrigan,
                SfxCue.BadOmen => _badOmen,
                SfxCue.GoodFortune => _goodFortune,
                SfxCue.WrathZeus => _wrathZeus,
                SfxCue.WrathThor => _wrathThor,
                SfxCue.WrathRa => _wrathRa,
                SfxCue.WrathMorrigan => _wrathMorrigan,
                SfxCue.CardHover => _cardHover,
                SfxCue.BotResult => _botResult,
                SfxCue.SoftGood => _softGood,
                SfxCue.SoftBad => _softBad,
                SfxCue.NeutralWin => _neutralWin,
                SfxCue.NeutralLose => _neutralLose,
                _ => throw new ArgumentOutOfRangeException(nameof(cue), cue, null),
            };
    }
}

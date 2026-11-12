using System;
using System.Collections;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace GameJam.UI
{
    /// <summary>
    /// The Destiny Die at each round start: a large die tumbles in the center, lands on the face
    /// Destiny rolled and shows what it does this round. A click anywhere skips it.
    /// A god's face takes that god's color, so the favored god is visible at a glance. A last line
    /// in the human's color says what the face does to them ("You: +2"). With the god powers on, a
    /// god's face also shows that god's Day ("DAY OF ZEUS · Storm: …") in its color.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DestinyView : MonoBehaviour
    {
        [Header("Timing (seconds)")]
        [Tooltip(
            "Fallback only, used when there is no roll clip. Normally the tumble is exactly the roll clip's length, so the die lands with its final clack."
        )]
        [SerializeField, Min(0f)]
        private float _tumbleDuration = 1.3f;

        [Tooltip("Seconds between random faces while tumbling.")]
        [SerializeField, Min(0.01f)]
        private float _faceInterval = 0.08f;

        [SerializeField, Min(0.01f)]
        private float _landDuration = 0.3f;

        [Tooltip("How long the landed face and its effect stay readable.")]
        [SerializeField, Min(0f)]
        private float _hold = 2.5f;

        [Tooltip(
            "Extra hold when the round brings news worth reading twice: a god's Day line, or the bad omen taking troops."
        )]
        [SerializeField, Min(0f)]
        private float _newsExtraHold = 1.5f;

        [Header("Motion")]
        [Tooltip("Max tilt in degrees while tumbling.")]
        [SerializeField, Min(0f)]
        private float _tumbleAngle = 20f;

        [Tooltip("Peak scale of the landing bounce.")]
        [SerializeField, Min(1f)]
        private float _landOvershoot = 1.25f;

        private UIDocument _document;
        private VisualElement _overlay;
        private Label _title;
        private VisualElement _die;
        private VisualElement _chip;
        private Label _face;
        private Label _effect;
        private VisualElement _nameGodIcon;
        private VisualElement _nameOmenIcon;
        private Label _day;
        private Label _you;
        private Label _skip;
        private WaitForSeconds _holdWait;
        private WaitForSeconds _newsHoldWait;

        // The die has landed and its lines show; a Day line set after that shows at once.
        private bool _landed;

        /// <summary>True from the first tumble until the hold ends or a click skips it.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Raised when the die lands on its face; not raised when a click skips it.</summary>
        public event Action Landed;

        /// <summary>The tumble when the roll sound is missing; otherwise the sound sets it.</summary>
        public float FallbackTumbleDuration => _tumbleDuration;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _holdWait = new WaitForSeconds(_hold);
            _newsHoldWait = new WaitForSeconds(_newsExtraHold);
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _overlay = root.Q<VisualElement>("destiny-overlay");
            _title = root.Q<Label>("destiny-title");
            _die = root.Q<VisualElement>("destiny-die");
            _chip = root.Q<VisualElement>("destiny-chip");
            _face = root.Q<Label>("destiny-face");
            _effect = root.Q<Label>("destiny-effect");
            _nameGodIcon = root.Q<VisualElement>("destiny-god-icon");
            _nameOmenIcon = root.Q<VisualElement>("destiny-omen-icon");
            _day = root.Q<Label>("destiny-day");
            _you = root.Q<Label>("destiny-you");
            _skip = root.Q<Label>("destiny-skip");
            _skip.text = MatchText.SkipHint;
            _overlay.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            Hide();
        }

        private void OnDisable()
        {
            _overlay.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            Hide();
        }

        /// <param name="youLine">
        /// What the face does to the human, shown in <paramref name="human"/>'s color; null hides the
        /// line.
        /// </param>
        /// <param name="tumbleSeconds">How long the die tumbles: the roll clip's length, see SfxPlayer.PlayRoll.</param>
        public void Show(
            int round,
            int face,
            string effect,
            God? human,
            string youLine,
            float tumbleSeconds
        )
        {
            StopAllCoroutines();
            StartCoroutine(Play(round, face, effect, human, youLine, tumbleSeconds));
        }

        /// <summary>
        /// Adds the Day line to the die shown by <see cref="Show"/>; it is revealed with the effect
        /// when the die lands. Match raises the Day right after the round starts, so it comes second.
        /// </summary>
        public void SetDayLine(string text, God god)
        {
            _day.text = text;
            GodUss.Apply(_day, god);
            _day.style.display = DisplayStyle.Flex;
            _day.style.visibility = _landed ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Replaces the "You" line of the die shown by <see cref="Show"/>, for news that arrives with
        /// the Day (the Storm striking the human). Revealed on landing like the rest.
        /// </summary>
        public void SetYouLine(string text) => _you.text = text;

        /// <summary>Stops the die at once, for example when the match ends during it.</summary>
        public void Hide()
        {
            StopAllCoroutines();
            IsPlaying = false;
            _overlay.style.display = DisplayStyle.None;
        }

        private IEnumerator Play(
            int round,
            int face,
            string effect,
            God? human,
            string youLine,
            float tumbleSeconds
        )
        {
            IsPlaying = true;
            _landed = false;
            _overlay.style.display = DisplayStyle.Flex;
            // No Day until SetDayLine says so (3, 4, Classic, or an eliminated god's face).
            _day.style.display = DisplayStyle.None;
            _title.text = MatchText.RoundTitle(round);
            _effect.text = effect;
            _you.text = youLine ?? string.Empty;
            _you.style.display = youLine != null ? DisplayStyle.Flex : DisplayStyle.None;
            GodUss.Apply(_you, human);
            // Both hidden until the die lands, so the effect never spoils the roll.
            _effect.style.visibility = Visibility.Hidden;
            _you.style.visibility = Visibility.Hidden;
            GodUss.Apply(_die, null);
            GodUss.Apply(_chip, null);
            _nameGodIcon.style.display = DisplayStyle.None;
            _nameOmenIcon.style.display = DisplayStyle.None;

            yield return Tumble(tumbleSeconds);

            // The favored god's color and chip (icon) arrive together, so it is never color alone.
            God? favored = Pantheon.GodOfFace(face);
            _face.text = MatchText.FaceText(face);
            GodUss.Apply(_die, favored);
            GodUss.Apply(_chip, favored);
            GodUss.ApplyDestinyIcon(_nameGodIcon, _nameOmenIcon, face);
            yield return Land();

            _landed = true;
            _effect.style.visibility = Visibility.Visible;
            _you.style.visibility = Visibility.Visible;
            _day.style.visibility = Visibility.Visible;
            Landed?.Invoke();
            yield return _holdWait;
            // Playtests showed a Day or the bad omen went by before it could be read: both change
            // the board right away, so they get the extra hold. A click still skips at any time.
            // The Day line is checked only now because SetDayLine arrives after Show.
            if (_day.style.display == DisplayStyle.Flex || Destiny.IsBadOmen(face))
                yield return _newsHoldWait;
            Hide();
        }

        // The faces shown while tumbling are cosmetic, so they use Unity's random, not the match's.
        // Timed on the real clock, like the combat dice, so the die lands when the roll clip ends.
        private IEnumerator Tumble(float duration)
        {
            float start = Time.realtimeSinceStartup;
            float nextFace = 0f;
            while (true)
            {
                float elapsed = Time.realtimeSinceStartup - start;
                if (elapsed >= duration)
                    break;
                if (elapsed >= nextFace)
                {
                    nextFace += _faceInterval;
                    _face.text = MatchText.FaceText(Random.Range(1, Die.Faces + 1));
                    _die.style.rotate = new Rotate(Random.Range(-_tumbleAngle, _tumbleAngle));
                }
                yield return null;
            }
            _die.style.rotate = new Rotate(0f);
        }

        private IEnumerator Land()
        {
            float elapsed = 0f;
            while (elapsed < _landDuration)
            {
                elapsed += Time.deltaTime;
                float bump = Mathf.Sin(Mathf.Clamp01(elapsed / _landDuration) * Mathf.PI);
                float scale = 1f + (_landOvershoot - 1f) * bump;
                _die.style.scale = new Scale(new Vector3(scale, scale, 1f));
                yield return null;
            }
            _die.style.scale = new Scale(Vector3.one);
        }

        // Skipping hides the die at once; the HUD round line still says what Destiny did.
        private void HandlePointerDown(PointerDownEvent evt) => Hide();
    }
}

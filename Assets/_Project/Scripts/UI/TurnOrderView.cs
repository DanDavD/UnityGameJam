using System;
using System.Collections;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace GameJam.UI
{
    /// <summary>
    /// The turn-order draw, shown before round 1: each duel's two dice tumble and land with a short
    /// tag ("4 · NEUTRAL") and a boxed verdict, ties roll again, the loser's die fades; then the
    /// final order appears as four numbered god cards. A click skips to the final order.
    /// A pure view: it plays a <see cref="TurnOrderDraw"/> the Core already decided.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TurnOrderView : MonoBehaviour
    {
        // Added to the serialized hold so every verdict reads one second longer, whatever the scene says.
        private const float VerdictExtraSeconds = 1f;
        private const string DieSacredClass = "die--sacred";
        private const string DieCursedClass = "die--cursed";
        private const string DieLoserClass = "die--loser";
        private const string OutcomeSacredClass = "outcome--sacred";
        private const string OutcomeCursedClass = "outcome--cursed";
        private const string VerdictShownClass = "verdict--shown";
        private const string PlaceShownClass = "draw-place--shown";
        private const string HiddenClass = "draw-overlay--hidden";

        [Tooltip("Seconds between random faces while tumbling.")]
        [SerializeField, Min(0.01f)]
        private float _faceInterval = 0.06f;

        [SerializeField, Min(0.01f)]
        private float _landSeconds = 0.25f;

        [Tooltip("Seconds a landed duel (or tie) stays readable before the next roll.")]
        [SerializeField, Min(0f)]
        private float _duelHoldSeconds = 1.5f;

        [Tooltip("Seconds between one final-order card and the next.")]
        [SerializeField, Min(0f)]
        private float _placeStepSeconds = 0.45f;

        [Tooltip("Seconds the final order stays before the overlay fades.")]
        [SerializeField, Min(0f)]
        private float _finalHoldSeconds = 2.5f;

        private UIDocument _document;
        private VisualElement _overlay;
        private Label _duelTitle;
        private VisualElement _duelBlock;
        private VisualElement _final;
        private VisualElement _verdictBox;
        private VisualElement _verdictChip;
        private Label _verdict;
        private Label _skipLabel;
        private readonly Slot[] _slots = new Slot[2];
        private VisualElement[] _places;
        private bool _skip;

        private sealed class Slot
        {
            public VisualElement Chip;
            public Label Name;
            public VisualElement Die;
            public Label Face;
            public VisualElement Outcome;
            public Label OutcomeLabel;
        }

        /// <summary>
        /// Called when a duel's dice start tumbling; returns how many seconds they tumble (the roll
        /// sound's length), so the dice land with its last clack. Set by MatchController.
        /// </summary>
        public Func<float> RollStarted { get; set; }

        /// <summary>The dice landed: the controller may play a sting.</summary>
        public event Action Landed;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _overlay = root.Q<VisualElement>("draw-overlay");
            root.Q<Label>("draw-title").text = MatchText.DrawTitle;
            _duelTitle = root.Q<Label>("draw-duel");
            _duelBlock = root.Q<VisualElement>("draw-duel-block");
            _final = root.Q<VisualElement>("draw-final");
            _verdictBox = root.Q<VisualElement>("draw-verdict-box");
            _verdictChip = root.Q<VisualElement>("draw-verdict-chip");
            _verdict = root.Q<Label>("draw-verdict");
            _skipLabel = root.Q<Label>("draw-skip");
            _skipLabel.text = MatchText.DrawSkipHint;
            _slots[0] = ReadSlot(root, "a");
            _slots[1] = ReadSlot(root, "b");
            _places = _final.Query<VisualElement>(className: "draw-place").ToList().ToArray();
            _overlay.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            _overlay.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            _overlay.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            StopAllCoroutines();
            IsPlaying = false;
        }

        private static Slot ReadSlot(VisualElement root, string side) =>
            new Slot
            {
                Chip = root.Q<VisualElement>($"draw-chip-{side}"),
                Name = root.Q<Label>($"draw-name-{side}"),
                Die = root.Q<VisualElement>($"draw-die-{side}"),
                Face = root.Q<Label>($"draw-face-{side}"),
                Outcome = root.Q<VisualElement>($"draw-outcome-{side}"),
                OutcomeLabel = root.Q<Label>($"draw-outcome-label-{side}"),
            };

        private void HandlePointerDown(PointerDownEvent evt)
        {
            _skip = true;
            evt.StopPropagation();
        }

        /// <summary>Plays the whole draw; the coroutine ends when the overlay has faded out.</summary>
        /// <param name="pace">Multiplies every hold: 1 normally, a third with fast bots.</param>
        public IEnumerator Play(TurnOrderDraw draw, float pace)
        {
            IsPlaying = true;
            _skip = false;
            _overlay.RemoveFromClassList(HiddenClass);
            _overlay.style.display = DisplayStyle.Flex;
            _skipLabel.style.display = DisplayStyle.Flex;
            ResetFinal();

            for (int d = 0; d < draw.Duels.Count && !_skip; d++)
                yield return PlayDuel(d, draw.Duels[d], pace);

            yield return ShowFinal(draw, pace);

            _overlay.AddToClassList(HiddenClass);
            yield return Wait(0.4f);
            _overlay.style.display = DisplayStyle.None;
            IsPlaying = false;
        }

        private IEnumerator PlayDuel(int index, DrawDuel duel, float pace)
        {
            _duelBlock.style.display = DisplayStyle.Flex;
            _final.style.display = DisplayStyle.None;
            _duelTitle.text = MatchText.DrawDuelTitle(index, duel);

            foreach (DrawAttempt attempt in duel.Attempts)
            {
                if (_skip)
                    yield break;
                PrepareSlot(_slots[0], attempt.First);
                PrepareSlot(_slots[1], attempt.Second);
                // The chip carries an inline visibility from the last verdict, which would show it
                // even though its box is hidden: reset it with the box.
                _verdictBox.RemoveFromClassList(VerdictShownClass);
                _verdictChip.style.visibility = Visibility.Hidden;
                _verdict.text = string.Empty;

                float tumble = RollStarted != null ? RollStarted() : 1.3f;
                yield return Tumble(tumble);
                if (_skip)
                    yield break;

                _slots[0].Face.text = MatchText.FaceText(attempt.FirstRoll);
                _slots[1].Face.text = MatchText.FaceText(attempt.SecondRoll);
                yield return Land();
                ShowOutcome(_slots[0], attempt.FirstRoll, attempt.FirstOutcome);
                ShowOutcome(_slots[1], attempt.SecondRoll, attempt.SecondOutcome);
                ShowVerdict(attempt);
                Landed?.Invoke();
                yield return Wait((_duelHoldSeconds + VerdictExtraSeconds) * pace);
            }
        }

        private IEnumerator ShowFinal(TurnOrderDraw draw, float pace)
        {
            _duelBlock.style.display = DisplayStyle.None;
            _final.style.display = DisplayStyle.Flex;
            _duelTitle.text = string.Empty;
            for (int i = 0; i < _places.Length; i++)
            {
                VisualElement place = _places[i];
                God god = draw.Order[i];
                GodUss.Apply(place, god);
                GodUss.Apply(place.Q(className: "god-chip"), god);
                place.Q<Label>(className: "draw-place__badge").text = MatchText.PlaceBadge(i);
                place.Q<Label>(className: "draw-place__name").text = MatchText.GodName(god);
            }

            // After a skip all four appear at once.
            for (int i = 0; i < _places.Length; i++)
            {
                _places[i].AddToClassList(PlaceShownClass);
                if (!_skip)
                    yield return Wait(_placeStepSeconds * pace);
            }
            _skipLabel.style.display = DisplayStyle.None;
            _skip = false;
            yield return Wait(_finalHoldSeconds * pace);
        }

        private void ResetFinal()
        {
            foreach (VisualElement place in _places)
                place.RemoveFromClassList(PlaceShownClass);
        }

        // Real-time wait that a click cuts short (the skip jumps on at once).
        private IEnumerator Wait(float seconds)
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < seconds && !_skip)
                yield return null;
        }

        private void PrepareSlot(Slot slot, God god)
        {
            GodUss.Apply(slot.Chip, god);
            GodUss.Apply(slot.Die, god);
            GodUss.Apply(slot.Outcome, god);
            slot.Name.text = MatchText.GodName(god);
            slot.Die.RemoveFromClassList(DieSacredClass);
            slot.Die.RemoveFromClassList(DieCursedClass);
            slot.Die.RemoveFromClassList(DieLoserClass);
            slot.Outcome.RemoveFromClassList(OutcomeSacredClass);
            slot.Outcome.RemoveFromClassList(OutcomeCursedClass);
            slot.Outcome.style.visibility = Visibility.Hidden;
            slot.Die.style.scale = new Scale(Vector3.one);
        }

        // The faces shown while tumbling are cosmetic: Unity's random, timed on the real clock like
        // the combat dice, because the sound runs in real time.
        private IEnumerator Tumble(float seconds)
        {
            float start = Time.realtimeSinceStartup;
            float next = 0f;
            while (!_skip)
            {
                float elapsed = Time.realtimeSinceStartup - start;
                if (elapsed >= seconds)
                    break;
                if (elapsed >= next)
                {
                    next += _faceInterval;
                    foreach (Slot slot in _slots)
                    {
                        slot.Face.text = MatchText.FaceText(Random.Range(1, Die.Faces + 1));
                        slot.Die.style.rotate = new Rotate(Random.Range(-15f, 15f));
                    }
                }
                yield return null;
            }
            foreach (Slot slot in _slots)
                slot.Die.style.rotate = new Rotate(0f);
        }

        private IEnumerator Land()
        {
            float start = Time.realtimeSinceStartup;
            for (float t = 0f; t < _landSeconds; t = Time.realtimeSinceStartup - start)
            {
                float scale = 1f + 0.2f * Mathf.Sin(Mathf.Clamp01(t / _landSeconds) * Mathf.PI);
                foreach (Slot slot in _slots)
                    slot.Die.style.scale = new Scale(new Vector3(scale, scale, 1f));
                yield return null;
            }
            foreach (Slot slot in _slots)
                slot.Die.style.scale = new Scale(Vector3.one);
        }

        private static void ShowOutcome(Slot slot, int roll, RollOutcome outcome)
        {
            if (outcome == RollOutcome.Sacred)
            {
                slot.Die.AddToClassList(DieSacredClass);
                slot.Outcome.AddToClassList(OutcomeSacredClass);
            }
            else if (outcome == RollOutcome.Cursed)
            {
                slot.Die.AddToClassList(DieCursedClass);
                slot.Outcome.AddToClassList(OutcomeCursedClass);
            }
            slot.OutcomeLabel.text = MatchText.OutcomeTag(roll, outcome);
            slot.Outcome.style.visibility = Visibility.Visible;
        }

        // The winner's chip and tint on the box, the loser's die faded; a tie names no winner.
        private void ShowVerdict(DrawAttempt attempt)
        {
            God? winner = attempt.Winner;
            GodUss.Apply(_verdictBox, winner);
            GodUss.Apply(_verdictChip, winner);
            _verdictChip.style.visibility = winner.HasValue
                ? Visibility.Visible
                : Visibility.Hidden;
            _verdict.text = MatchText.DrawVerdict(attempt);
            _verdictBox.AddToClassList(VerdictShownClass);
            if (!winner.HasValue)
                return;
            (winner == attempt.First ? _slots[1] : _slots[0]).Die.AddToClassList(DieLoserClass);
        }
    }
}

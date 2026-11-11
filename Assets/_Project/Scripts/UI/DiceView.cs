using System;
using System.Collections;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace GameJam.UI
{
    /// <summary>
    /// The combat dice: attacker left, defender right. They tumble, land with a small overshoot,
    /// then tag each roll ("1 · SACRED") and box the verdict explaining why ("SACRED BEATS NEUTRAL"), with the
    /// winner's chip, the loser faded and badges for an Offering re-roll or Ragnarök.
    /// After an Offering only the attacker's die rolls again, matching the rule.
    /// A die that just landed sacred pulses; one that landed cursed shakes, so the omen reads before the words.
    /// While the human loses a combat they may re-roll, an optional Offering button sits under their die
    /// with a draining bar; MatchController owns its window and decides what a click means.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DiceView : MonoBehaviour
    {
        private const string DieSacredClass = "die--sacred";
        private const string DieCursedClass = "die--cursed";
        private const string OutcomeSacredClass = "outcome--sacred";
        private const string OutcomeCursedClass = "outcome--cursed";
        private const string DieLoserClass = "die--loser";
        private const string VerdictShownClass = "verdict--shown";
        private const string LineShownClass = "verdict__line--shown";

        // TODO(MatchText): move to MatchText once the HUD text pass is merged.
        private const string OfferingLabel = "OFFERING · RE-ROLL (−1)";

        [Header("Tumble")]
        [Tooltip(
            "Fallback only, used when there is no roll clip. Normally the tumble is exactly the roll clip's length, so the dice land with its final clack."
        )]
        [SerializeField, Min(0f)]
        private float _tumbleDuration = 1.3f;

        [Tooltip("Seconds between random faces while tumbling.")]
        [SerializeField, Min(0.01f)]
        private float _faceInterval = 0.06f;

        [Tooltip("Max tilt in degrees while tumbling.")]
        [SerializeField, Min(0f)]
        private float _tumbleAngle = 15f;

        [Header("Landing")]
        [SerializeField, Min(0.01f)]
        private float _landDuration = 0.25f;

        [Tooltip("Peak scale of the landing bounce.")]
        [SerializeField, Min(1f)]
        private float _landOvershoot = 1.2f;

        [Header("Outcome emphasis, right after landing")]
        [Tooltip("Seconds of the sacred pulse and the cursed shake.")]
        [SerializeField, Min(0.01f)]
        private float _emphasisDuration = 0.6f;

        [Tooltip("Extra scale at the top of each sacred pulse.")]
        [SerializeField, Min(0f)]
        private float _sacredPulseScale = 0.08f;

        [SerializeField, Min(1)]
        private int _sacredPulses = 2;

        [Tooltip("Peak sideways offset of the cursed shake, in panel pixels; it dies out.")]
        [SerializeField, Min(0f)]
        private float _cursedShakePixels = 6f;

        [SerializeField, Min(1)]
        private int _cursedShakes = 3;

        private UIDocument _document;
        private VisualElement _panel;
        private Label _title;
        private Label _verdict;
        private VisualElement _verdictBox;
        private VisualElement _verdictChip;
        private Label _verdictKicker;
        private Label _offeringBadge;
        private Label _ragnarokBadge;
        private Label _attackerName;
        private VisualElement _attackerDie;
        private Label _attackerFace;
        private VisualElement _attackerOutcome;
        private Label _attackerOutcomeLabel;
        private Label _defenderName;
        private VisualElement _defenderDie;
        private Label _defenderFace;
        private VisualElement _defenderOutcome;
        private Label _defenderOutcomeLabel;
        private Label _continue;
        private Button _offeringButton;
        private VisualElement _offeringTimer;

        /// <summary>True while the dice are tumbling or landing; the result is not readable yet.</summary>
        public bool IsAnimating { get; private set; }

        /// <summary>Raised the moment the dice land and their outcomes show, so sound can match the motion.</summary>
        public event Action<CombatResult> Landed;

        /// <summary>The human pressed the Offering button under their die.</summary>
        public event Action OfferingClicked;

        /// <summary>The tumble when the roll sound is missing; otherwise the sound sets it.</summary>
        public float FallbackTumbleDuration => _tumbleDuration;

        /// <summary>True while the dice panel is on screen (a combat's dice stay until the next roll or turn).</summary>
        public bool IsShown => _panel.style.display == DisplayStyle.Flex;

        /// <summary>One side's die, where a star for its sacred roll starts.</summary>
        public VisualElement DieOf(CombatSide side) =>
            side == CombatSide.Attacker ? _attackerDie : _defenderDie;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _panel = root.Q<VisualElement>("dice-panel");
            _title = root.Q<Label>("dice-title");
            _verdict = root.Q<Label>("dice-verdict");
            _verdictBox = root.Q<VisualElement>("dice-verdict-box");
            _verdictChip = root.Q<VisualElement>("dice-verdict-chip");
            _verdictKicker = root.Q<Label>("dice-verdict-kicker");
            _offeringBadge = root.Q<Label>("dice-badge-offering");
            _ragnarokBadge = root.Q<Label>("dice-badge-ragnarok");
            _attackerName = root.Q<Label>("attacker-name");
            _attackerDie = root.Q<VisualElement>("attacker-die");
            _attackerFace = root.Q<Label>("attacker-face");
            _attackerOutcome = root.Q<VisualElement>("attacker-outcome");
            _attackerOutcomeLabel = root.Q<Label>("attacker-outcome-label");
            _defenderName = root.Q<Label>("defender-name");
            _defenderDie = root.Q<VisualElement>("defender-die");
            _defenderFace = root.Q<Label>("defender-face");
            _defenderOutcome = root.Q<VisualElement>("defender-outcome");
            _defenderOutcomeLabel = root.Q<Label>("defender-outcome-label");
            _continue = root.Q<Label>("dice-continue");
            _continue.text = MatchText.ContinueHint;
            _offeringButton = root.Q<Button>("dice-offering-button");
            _offeringTimer = root.Q<VisualElement>("dice-offering-timer");
            root.Q<Label>("dice-offering-label").text = OfferingLabel;
            _offeringButton.clicked += HandleOfferingClicked;
            Hide();
        }

        private void OnDisable()
        {
            _offeringButton.clicked -= HandleOfferingClicked;
            StopAllCoroutines();
            IsAnimating = false;
        }

        /// <param name="tumbleSeconds">How long the dice tumble: the roll clip's length, see SfxPlayer.PlayRoll.</param>
        public void Show(Combat combat, float tumbleSeconds)
        {
            StopAllCoroutines();
            StartCoroutine(Roll(combat, tumbleSeconds));
        }

        /// <summary>"Click to continue" under the verdict, while a result waits for the human.</summary>
        public void ShowContinueHint(bool shown) =>
            // Visibility, not display: the panel keeps its final height from the first roll, so the dice never jump.
            _continue.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;

        /// <summary>The optional Offering under the attacker's die, its timer bar full.</summary>
        public void ShowOffering(bool shown)
        {
            _offeringButton.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;
            SetOfferingTimeLeft(1f);
        }

        /// <summary>Drains the Offering's timer bar; 1 is the whole window left, 0 none.</summary>
        public void SetOfferingTimeLeft(float fraction) =>
            _offeringTimer.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);

        public void Hide()
        {
            StopAllCoroutines();
            IsAnimating = false;
            _panel.style.display = DisplayStyle.None;
            HideVerdict();
            ShowContinueHint(false);
            ShowOffering(false);
            ResetMotion(_attackerDie);
            ResetMotion(_defenderDie);
        }

        // Clears the last combat's verdict and the faded loser; a re-roll starts from a clean table.
        private void HideVerdict()
        {
            _verdictBox.RemoveFromClassList(VerdictShownClass);
            _verdict.text = string.Empty;
            SetLine(_verdictKicker, null);
            SetLine(_offeringBadge, null);
            SetLine(_ragnarokBadge, null);
            _attackerDie.RemoveFromClassList(DieLoserClass);
            _defenderDie.RemoveFromClassList(DieLoserClass);
        }

        // The rule that decided it in one boxed line, the winner's chip and tint (so no sentence names
        // the winner), the loser faded, and badges for an Offering re-roll or Ragnarök.
        private void ShowVerdict(Combat combat)
        {
            bool attackerWon = combat.Result.Winner == CombatSide.Attacker;
            God winner = attackerWon ? combat.Attacker : combat.Defender;
            GodUss.Apply(_verdictBox, winner);
            GodUss.Apply(_verdictChip, winner);
            SetLine(_verdictKicker, MatchText.CombatVerdictKicker(combat));
            _verdict.text = MatchText.CombatVerdict(combat);
            SetLine(_offeringBadge, MatchText.CombatOfferingBadge(combat));
            SetLine(_ragnarokBadge, MatchText.CombatRagnarokBadge(combat));
            _verdictBox.AddToClassList(VerdictShownClass);

            (attackerWon ? _defenderDie : _attackerDie).AddToClassList(DieLoserClass);
        }

        // An optional line of the verdict box (the kicker, a badge): shown only while it has text.
        private static void SetLine(Label line, string text)
        {
            line.text = text ?? string.Empty;
            line.EnableInClassList(LineShownClass, !string.IsNullOrEmpty(text));
        }

        private IEnumerator Roll(Combat combat, float tumbleSeconds)
        {
            IsAnimating = true;
            bool rollDefender = !combat.OfferingMade;
            // A new roll can cut the previous one's emphasis mid-motion.
            ResetMotion(_attackerDie);
            ResetMotion(_defenderDie);

            _panel.style.display = DisplayStyle.Flex;
            _title.text = MatchText.CombatTitle(combat);
            HideVerdict();
            ShowContinueHint(false);
            ShowOffering(false);
            _attackerName.text = MatchText.GodName(combat.Attacker);
            _defenderName.text = MatchText.GodName(combat.Defender);
            SetGod(_attackerDie, _attackerOutcome, combat.Attacker);
            SetGod(_defenderDie, _defenderOutcome, combat.Defender);
            ClearOutcome(_attackerDie, _attackerOutcome);
            if (rollDefender)
                ClearOutcome(_defenderDie, _defenderOutcome);

            yield return Tumble(rollDefender, tumbleSeconds);

            CombatResult result = combat.Result;
            _attackerFace.text = MatchText.FaceText(result.AttackerRoll);
            _defenderFace.text = MatchText.FaceText(result.DefenderRoll);
            yield return Land(rollDefender);

            ShowOutcome(
                _attackerDie,
                _attackerOutcome,
                _attackerOutcomeLabel,
                result.AttackerOutcome,
                MatchText.OutcomeTag(result.AttackerRoll, result.AttackerOutcome)
            );
            ShowOutcome(
                _defenderDie,
                _defenderOutcome,
                _defenderOutcomeLabel,
                result.DefenderOutcome,
                MatchText.OutcomeTag(result.DefenderRoll, result.DefenderOutcome)
            );
            ShowVerdict(combat);
            IsAnimating = false;
            Landed?.Invoke(result);

            // Only the dice that just landed react; the defender's kept die already did.
            yield return Emphasize(result, rollDefender);
        }

        private IEnumerator Emphasize(CombatResult result, bool rollDefender)
        {
            for (float elapsed = 0f; elapsed < _emphasisDuration; elapsed += Time.deltaTime)
            {
                float t = elapsed / _emphasisDuration;
                Emphasize(_attackerDie, result.AttackerOutcome, t);
                if (rollDefender)
                    Emphasize(_defenderDie, result.DefenderOutcome, t);
                yield return null;
            }
            ResetMotion(_attackerDie);
            ResetMotion(_defenderDie);
        }

        // Sacred: smooth breaths of scale (the tint and thick border are the glow). Cursed: a
        // sideways shake that dies out. Neutral: still. t runs 0 to 1.
        private void Emphasize(VisualElement die, RollOutcome outcome, float t)
        {
            if (outcome == RollOutcome.Sacred)
            {
                float breath = 0.5f - 0.5f * Mathf.Cos(t * 2f * Mathf.PI * _sacredPulses);
                SetScale(die, 1f + _sacredPulseScale * breath);
            }
            else if (outcome == RollOutcome.Cursed)
            {
                float offset =
                    _cursedShakePixels * (1f - t) * Mathf.Sin(t * 2f * Mathf.PI * _cursedShakes);
                die.style.translate = new Translate(offset, 0f);
            }
        }

        private static void ResetMotion(VisualElement die)
        {
            SetScale(die, 1f);
            die.style.translate = new Translate(0f, 0f);
        }

        // The faces shown while tumbling are cosmetic, so they use Unity's random, not the match's.
        // Timed on the real clock, not by summing Time.deltaTime (which Unity caps after a hitch),
        // because the sound runs in real time: the dice must land when the clip ends.
        private IEnumerator Tumble(bool rollDefender, float duration)
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
                    RandomFace(_attackerDie, _attackerFace);
                    if (rollDefender)
                        RandomFace(_defenderDie, _defenderFace);
                }
                yield return null;
            }
            _attackerDie.style.rotate = new Rotate(0f);
            _defenderDie.style.rotate = new Rotate(0f);
        }

        private IEnumerator Land(bool rollDefender)
        {
            float elapsed = 0f;
            while (elapsed < _landDuration)
            {
                elapsed += Time.deltaTime;
                // Up to the overshoot and back to rest in one smooth arc.
                float bump = Mathf.Sin(Mathf.Clamp01(elapsed / _landDuration) * Mathf.PI);
                float scale = 1f + (_landOvershoot - 1f) * bump;
                SetScale(_attackerDie, scale);
                if (rollDefender)
                    SetScale(_defenderDie, scale);
                yield return null;
            }
            SetScale(_attackerDie, 1f);
            SetScale(_defenderDie, 1f);
        }

        private void RandomFace(VisualElement die, Label face)
        {
            face.text = MatchText.FaceText(Random.Range(1, Die.Faces + 1));
            die.style.rotate = new Rotate(Random.Range(-_tumbleAngle, _tumbleAngle));
        }

        private void HandleOfferingClicked() => OfferingClicked?.Invoke();

        private static void SetScale(VisualElement element, float scale) =>
            element.style.scale = new Scale(new Vector3(scale, scale, 1f));

        private static void SetGod(VisualElement die, VisualElement outcome, God god)
        {
            GodUss.Apply(die, god);
            GodUss.Apply(outcome, god);
        }

        private static void ClearOutcome(VisualElement die, VisualElement outcome)
        {
            die.RemoveFromClassList(DieSacredClass);
            die.RemoveFromClassList(DieCursedClass);
            outcome.RemoveFromClassList(OutcomeSacredClass);
            outcome.RemoveFromClassList(OutcomeCursedClass);
            outcome.style.visibility = Visibility.Hidden;
        }

        // The outcome classes show the matching drawn icon on the die corner and beside the word.
        private static void ShowOutcome(
            VisualElement die,
            VisualElement outcome,
            Label label,
            RollOutcome value,
            string text
        )
        {
            ClearOutcome(die, outcome);
            if (value == RollOutcome.Sacred)
            {
                die.AddToClassList(DieSacredClass);
                outcome.AddToClassList(OutcomeSacredClass);
            }
            else if (value == RollOutcome.Cursed)
            {
                die.AddToClassList(DieCursedClass);
                outcome.AddToClassList(OutcomeCursedClass);
            }

            label.text = text;
            outcome.style.visibility = Visibility.Visible;
        }
    }
}

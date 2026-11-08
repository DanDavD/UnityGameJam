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
    /// then show each roll as SACRED / CURSED / NEUTRAL for its god and the verdict explaining why.
    /// After an Offering only the attacker's die rolls again, matching the rule.
    /// A die that just landed sacred pulses; one that landed cursed shakes, so the omen reads before the words.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DiceView : MonoBehaviour
    {
        private const string DieSacredClass = "die--sacred";
        private const string DieCursedClass = "die--cursed";
        private const string OutcomeSacredClass = "outcome--sacred";
        private const string OutcomeCursedClass = "outcome--cursed";

        [Header("Tumble")]
        [SerializeField, Min(0f)]
        private float _tumbleDuration = 0.6f;

        [Tooltip("Seconds between random faces while tumbling.")]
        [SerializeField, Min(0.01f)]
        private float _faceInterval = 0.06f;

        [Tooltip("Max tilt in degrees while tumbling.")]
        [SerializeField, Min(0f)]
        private float _tumbleAngle = 15f;

        [Header("Landing")]
        [SerializeField, Min(0.01f)]
        private float _landDuration = 0.18f;

        [Tooltip("Peak scale of the landing bounce.")]
        [SerializeField, Min(1f)]
        private float _landOvershoot = 1.2f;

        [Header("Outcome emphasis, right after landing")]
        [Tooltip("Seconds of the sacred pulse and the cursed shake.")]
        [SerializeField, Min(0.01f)]
        private float _emphasisDuration = 0.4f;

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

        /// <summary>True while the dice are tumbling or landing; the result is not readable yet.</summary>
        public bool IsAnimating { get; private set; }

        /// <summary>Raised the moment the dice land and their outcomes show, so sound can match the motion.</summary>
        public event Action<CombatResult> Landed;

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
            Hide();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            IsAnimating = false;
        }

        public void Show(Combat combat)
        {
            StopAllCoroutines();
            StartCoroutine(Roll(combat));
        }

        public void Hide()
        {
            StopAllCoroutines();
            IsAnimating = false;
            _panel.style.display = DisplayStyle.None;
            ResetMotion(_attackerDie);
            ResetMotion(_defenderDie);
        }

        private IEnumerator Roll(Combat combat)
        {
            IsAnimating = true;
            bool rollDefender = !combat.OfferingMade;
            // A new roll can cut the previous one's emphasis mid-motion.
            ResetMotion(_attackerDie);
            ResetMotion(_defenderDie);

            _panel.style.display = DisplayStyle.Flex;
            _title.text = MatchText.CombatTitle(combat);
            _verdict.text = string.Empty;
            _attackerName.text = MatchText.GodName(combat.Attacker);
            _defenderName.text = MatchText.GodName(combat.Defender);
            SetGod(_attackerDie, _attackerOutcome, combat.Attacker);
            SetGod(_defenderDie, _defenderOutcome, combat.Defender);
            ClearOutcome(_attackerDie, _attackerOutcome);
            if (rollDefender)
                ClearOutcome(_defenderDie, _defenderOutcome);

            yield return Tumble(rollDefender);

            CombatResult result = combat.Result;
            _attackerFace.text = MatchText.FaceText(result.AttackerRoll);
            _defenderFace.text = MatchText.FaceText(result.DefenderRoll);
            yield return Land(rollDefender);

            ShowOutcome(
                _attackerDie,
                _attackerOutcome,
                _attackerOutcomeLabel,
                result.AttackerOutcome
            );
            ShowOutcome(
                _defenderDie,
                _defenderOutcome,
                _defenderOutcomeLabel,
                result.DefenderOutcome
            );
            _verdict.text = MatchText.CombatVerdict(combat);
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
        private IEnumerator Tumble(bool rollDefender)
        {
            float elapsed = 0f;
            float nextFace = 0f;
            while (elapsed < _tumbleDuration)
            {
                if (elapsed >= nextFace)
                {
                    nextFace += _faceInterval;
                    RandomFace(_attackerDie, _attackerFace);
                    if (rollDefender)
                        RandomFace(_defenderDie, _defenderFace);
                }
                elapsed += Time.deltaTime;
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
            RollOutcome value
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

            label.text = MatchText.OutcomeLabel(value);
            outcome.style.visibility = Visibility.Visible;
        }
    }
}

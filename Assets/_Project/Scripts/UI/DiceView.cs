using System.Collections;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// The combat dice: attacker left, defender right. They tumble, land with a small overshoot,
    /// then show each roll as SACRED / CURSED / NEUTRAL for its god and the verdict explaining why.
    /// After an Offering only the attacker's die rolls again, matching the rule.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DiceView : MonoBehaviour
    {
        private const string DieSacredClass = "die--sacred";
        private const string DieCursedClass = "die--cursed";

        private static readonly string[] FaceTexts = { "", "1", "2", "3", "4", "5", "6" };

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

        private UIDocument _document;
        private VisualElement _panel;
        private Label _title;
        private Label _verdict;
        private Label _attackerName;
        private VisualElement _attackerDie;
        private Label _attackerFace;
        private Label _attackerOutcome;
        private Label _defenderName;
        private VisualElement _defenderDie;
        private Label _defenderFace;
        private Label _defenderOutcome;

        /// <summary>True while the dice are tumbling or landing; the result is not readable yet.</summary>
        public bool IsAnimating { get; private set; }

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
            _attackerOutcome = root.Q<Label>("attacker-outcome");
            _defenderName = root.Q<Label>("defender-name");
            _defenderDie = root.Q<VisualElement>("defender-die");
            _defenderFace = root.Q<Label>("defender-face");
            _defenderOutcome = root.Q<Label>("defender-outcome");
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
        }

        private IEnumerator Roll(Combat combat)
        {
            IsAnimating = true;
            bool rollDefender = !combat.OfferingMade;

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
            _attackerFace.text = FaceTexts[result.AttackerRoll];
            _defenderFace.text = FaceTexts[result.DefenderRoll];
            yield return Land(rollDefender);

            ShowOutcome(_attackerDie, _attackerOutcome, result.AttackerOutcome);
            ShowOutcome(_defenderDie, _defenderOutcome, result.DefenderOutcome);
            _verdict.text = MatchText.CombatVerdict(combat);
            IsAnimating = false;
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
            face.text = FaceTexts[Random.Range(1, Die.Faces + 1)];
            die.style.rotate = new Rotate(Random.Range(-_tumbleAngle, _tumbleAngle));
        }

        private static void SetScale(VisualElement element, float scale) =>
            element.style.scale = new Scale(new Vector3(scale, scale, 1f));

        private static void SetGod(VisualElement die, Label outcome, God god)
        {
            GodUss.Apply(die, god);
            GodUss.Apply(outcome, god);
        }

        private static void ClearOutcome(VisualElement die, Label outcome)
        {
            die.RemoveFromClassList(DieSacredClass);
            die.RemoveFromClassList(DieCursedClass);
            outcome.style.visibility = Visibility.Hidden;
        }

        private static void ShowOutcome(VisualElement die, Label outcome, RollOutcome value)
        {
            ClearOutcome(die, outcome);
            if (value == RollOutcome.Sacred)
                die.AddToClassList(DieSacredClass);
            else if (value == RollOutcome.Cursed)
                die.AddToClassList(DieCursedClass);

            outcome.text = MatchText.OutcomeLabel(value);
            outcome.style.visibility = Visibility.Visible;
        }
    }
}

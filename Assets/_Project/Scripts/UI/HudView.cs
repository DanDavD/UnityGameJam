using System;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// The match HUD: the human's god chip and numbers, round and Destiny line, goal, action prompt,
    /// onboarding hint, action buttons, the fast-bots toggle and the end screen.
    /// A pure view: it shows what it is told and reports button clicks as C# events.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudView : MonoBehaviour
    {
        [Tooltip("Seconds an onboarding hint stays, unless the next action hides it first.")]
        [SerializeField, Min(0f)]
        private float _hintDuration = 4f;

        private UIDocument _document;
        private VisualElement _identityCard;
        private VisualElement _identityChip;
        private Label _identityLabel;
        private Label _roundLabel;
        private Label _goalLabel;
        private Label _promptLabel;
        private Label _hintLabel;
        private Button _offeringButton;
        private Button _acceptButton;
        private Button _endTurnButton;
        private Button _fastBotsButton;
        private VisualElement _endBanner;
        private VisualElement _endCard;
        private VisualElement _winnerChip;
        private Label _winnerTitle;
        private Label _winnerName;
        private Label _reasonLabel;
        private Label _roundsValue;
        private VisualElement _templesStat;
        private Label _templesValue;
        private Label _templesLabel;
        private Label _combatsValue;
        private Button _playAgainButton;
        private Button _mainMenuButton;
        private IVisualElementScheduledItem _hintTimeout;

        public event Action OfferingClicked;
        public event Action AcceptClicked;
        public event Action EndTurnClicked;
        public event Action FastBotsClicked;
        public event Action PlayAgainClicked;
        public event Action MainMenuClicked;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        // UIDocument rebuilds its tree when enabled, so elements are queried here, not in Awake.
        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            // The full-screen root must not swallow board clicks; only real controls should.
            root.pickingMode = PickingMode.Ignore;
            _identityCard = root.Q<VisualElement>("identity-card");
            _identityChip = root.Q<VisualElement>("identity-chip");
            _identityLabel = root.Q<Label>("identity-label");
            _roundLabel = root.Q<Label>("round-label");
            _goalLabel = root.Q<Label>("goal-label");
            _promptLabel = root.Q<Label>("prompt-label");
            _hintLabel = root.Q<Label>("hint-label");
            _offeringButton = root.Q<Button>("offering-button");
            _acceptButton = root.Q<Button>("accept-button");
            _endTurnButton = root.Q<Button>("end-turn-button");
            _fastBotsButton = root.Q<Button>("fast-bots-button");
            _endBanner = root.Q<VisualElement>("end-banner");
            _endCard = root.Q<VisualElement>("end-card");
            _winnerChip = root.Q<VisualElement>("winner-chip");
            _winnerTitle = root.Q<Label>("winner-label");
            _winnerName = root.Q<Label>("winner-name");
            _reasonLabel = root.Q<Label>("reason-label");
            _roundsValue = root.Q<Label>("stat-rounds-value");
            _templesStat = root.Q<VisualElement>("stat-temples");
            _templesValue = root.Q<Label>("stat-temples-value");
            _templesLabel = root.Q<Label>("stat-temples-label");
            _combatsValue = root.Q<Label>("stat-combats-value");
            _playAgainButton = root.Q<Button>("play-again-button");
            _mainMenuButton = root.Q<Button>("main-menu-button");

            // Player-facing text lives in MatchText; the UXML text is only the UI Builder preview.
            _goalLabel.text = MatchText.GoalLine;
            _offeringButton.text = MatchText.OfferingButton;
            _acceptButton.text = MatchText.AcceptButton;
            _endTurnButton.text = MatchText.EndTurnButton;
            _playAgainButton.text = MatchText.PlayAgainButton;
            _mainMenuButton.text = MatchText.MainMenuButton;
            root.Q<Label>("stat-rounds-label").text = MatchText.RoundsStat;
            root.Q<Label>("stat-combats-label").text = MatchText.CombatsWonStat;

            _offeringButton.clicked += HandleOfferingClicked;
            _acceptButton.clicked += HandleAcceptClicked;
            _endTurnButton.clicked += HandleEndTurnClicked;
            _fastBotsButton.clicked += HandleFastBotsClicked;
            _playAgainButton.clicked += HandlePlayAgainClicked;
            _mainMenuButton.clicked += HandleMainMenuClicked;

            // Nothing about the match shows until a god is chosen and the first round starts.
            SetVisible(_identityCard, false);
            SetVisible(_fastBotsButton, false);
            SetRound(string.Empty);
            SetPrompt(string.Empty);
            HideHint();
            ShowCombatChoice(false);
            ShowEndTurn(false);
            SetVisible(_endBanner, false);
        }

        private void OnDisable()
        {
            _offeringButton.clicked -= HandleOfferingClicked;
            _acceptButton.clicked -= HandleAcceptClicked;
            _endTurnButton.clicked -= HandleEndTurnClicked;
            _fastBotsButton.clicked -= HandleFastBotsClicked;
            _playAgainButton.clicked -= HandlePlayAgainClicked;
            _mainMenuButton.clicked -= HandleMainMenuClicked;
            _hintTimeout?.Pause();
        }

        /// <summary>The human's god: its chip (color + icon) and the line with its sacred / cursed numbers.</summary>
        public void SetIdentity(God god, string text)
        {
            GodUss.Apply(_identityChip, god);
            _identityLabel.text = text;
            SetVisible(_identityCard, true);
            // Hidden while the menu is open: it only means something once a match is running.
            SetVisible(_fastBotsButton, true);
        }

        public void SetRound(string text)
        {
            _roundLabel.text = text;
            SetVisible(_roundLabel, !string.IsNullOrEmpty(text));
        }

        public void SetPrompt(string text) => _promptLabel.text = text;

        /// <summary>Shows an onboarding hint that hides itself after a few seconds.</summary>
        public void ShowHint(string text)
        {
            _hintLabel.text = text;
            SetVisible(_hintLabel, true);
            // UI Toolkit's scheduler times it, so no coroutine or Update is needed.
            _hintTimeout?.Pause();
            _hintTimeout = _hintLabel
                .schedule.Execute(HideHint)
                .StartingIn(SecondsToMs(_hintDuration));
        }

        /// <summary>True while an onboarding hint is on screen, so a new one can wait instead of replacing it.</summary>
        public bool IsHintVisible => _hintLabel.style.display == DisplayStyle.Flex;

        public void HideHint()
        {
            _hintTimeout?.Pause();
            SetVisible(_hintLabel, false);
        }

        public void SetFastBots(bool on) => _fastBotsButton.text = MatchText.FastBotsButton(on);

        public void ShowCombatChoice(bool visible)
        {
            SetVisible(_offeringButton, visible);
            SetVisible(_acceptButton, visible);
        }

        public void ShowEndTurn(bool visible) => SetVisible(_endTurnButton, visible);

        /// <summary>
        /// Title is the verdict for the human (YOU WIN / YOU LOSE); detail says who won and why.
        /// The card takes the winner's color, icon and name. With no winner yet (human eliminated)
        /// it stays neutral and the winner's temples stat is hidden.
        /// </summary>
        public void ShowEndBanner(
            string title,
            string detail,
            God? winner,
            int rounds,
            int winnerTemples,
            int combatsWon
        )
        {
            GodUss.Apply(_endCard, winner);
            GodUss.Apply(_winnerChip, winner);
            SetVisible(_winnerChip, winner.HasValue);
            SetVisible(_winnerName, winner.HasValue);
            SetVisible(_templesStat, winner.HasValue);
            if (winner.HasValue)
            {
                _winnerName.text = MatchText.WinnerName(winner.Value);
                _templesLabel.text = MatchText.WinnerTemplesStat(winner.Value);
            }
            _winnerTitle.text = title;
            _reasonLabel.text = detail;
            _roundsValue.text = rounds.ToString();
            _templesValue.text = winnerTemples.ToString();
            _combatsValue.text = combatsWon.ToString();
            HideHint();
            SetVisible(_fastBotsButton, false);
            SetVisible(_endBanner, true);
        }

        /// <summary>True when a screen position (Input System, origin bottom-left) is over a pickable HUD element.</summary>
        public bool IsPointerOverUi(Vector2 screenPosition)
        {
            IPanel panel = _document.rootVisualElement.panel;
            if (panel == null)
                return false;

            // Panels use a top-left origin.
            var flipped = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, flipped);
            return panel.Pick(panelPosition) != null;
        }

        private static long SecondsToMs(float seconds) => (long)(seconds * 1000f);

        private static void SetVisible(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private void HandleOfferingClicked() => OfferingClicked?.Invoke();

        private void HandleAcceptClicked() => AcceptClicked?.Invoke();

        private void HandleEndTurnClicked() => EndTurnClicked?.Invoke();

        private void HandleFastBotsClicked() => FastBotsClicked?.Invoke();

        private void HandlePlayAgainClicked() => PlayAgainClicked?.Invoke();

        private void HandleMainMenuClicked() => MainMenuClicked?.Invoke();
    }
}

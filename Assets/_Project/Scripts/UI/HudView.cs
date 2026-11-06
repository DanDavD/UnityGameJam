using System;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// Placeholder match HUD: the human's god chip and numbers, round and Destiny line, goal, action prompt,
    /// action buttons and the end banner.
    /// A pure view: it shows what it is told and reports button clicks as C# events.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudView : MonoBehaviour
    {
        private UIDocument _document;
        private VisualElement _identityChip;
        private Label _identityLabel;
        private Label _roundLabel;
        private Label _goalLabel;
        private Label _promptLabel;
        private Button _offeringButton;
        private Button _acceptButton;
        private Button _endTurnButton;
        private VisualElement _endBanner;
        private VisualElement _endCard;
        private VisualElement _winnerChip;
        private Label _winnerLabel;
        private Label _reasonLabel;
        private Button _playAgainButton;

        public event Action OfferingClicked;
        public event Action AcceptClicked;
        public event Action EndTurnClicked;
        public event Action PlayAgainClicked;

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
            _identityChip = root.Q<VisualElement>("identity-chip");
            _identityLabel = root.Q<Label>("identity-label");
            _roundLabel = root.Q<Label>("round-label");
            _goalLabel = root.Q<Label>("goal-label");
            _promptLabel = root.Q<Label>("prompt-label");
            _offeringButton = root.Q<Button>("offering-button");
            _acceptButton = root.Q<Button>("accept-button");
            _endTurnButton = root.Q<Button>("end-turn-button");
            _endBanner = root.Q<VisualElement>("end-banner");
            _endCard = root.Q<VisualElement>("end-card");
            _winnerChip = root.Q<VisualElement>("winner-chip");
            _winnerLabel = root.Q<Label>("winner-label");
            _reasonLabel = root.Q<Label>("reason-label");
            _playAgainButton = root.Q<Button>("play-again-button");

            // Player-facing text lives in MatchText; the UXML text is only the UI Builder preview.
            _goalLabel.text = MatchText.GoalLine;
            _offeringButton.text = MatchText.OfferingButton;
            _acceptButton.text = MatchText.AcceptButton;
            _endTurnButton.text = MatchText.EndTurnButton;
            _playAgainButton.text = MatchText.PlayAgainButton;

            _offeringButton.clicked += HandleOfferingClicked;
            _acceptButton.clicked += HandleAcceptClicked;
            _endTurnButton.clicked += HandleEndTurnClicked;
            _playAgainButton.clicked += HandlePlayAgainClicked;

            ShowCombatChoice(false);
            ShowEndTurn(false);
            SetVisible(_endBanner, false);
        }

        private void OnDisable()
        {
            _offeringButton.clicked -= HandleOfferingClicked;
            _acceptButton.clicked -= HandleAcceptClicked;
            _endTurnButton.clicked -= HandleEndTurnClicked;
            _playAgainButton.clicked -= HandlePlayAgainClicked;
        }

        /// <summary>The human's god: its chip (color + icon) and the line with its sacred / cursed numbers.</summary>
        public void SetIdentity(God god, string text)
        {
            GodUss.Apply(_identityChip, god);
            _identityLabel.text = text;
        }

        public void SetRound(string text) => _roundLabel.text = text;

        public void SetPrompt(string text) => _promptLabel.text = text;

        public void ShowCombatChoice(bool visible)
        {
            SetVisible(_offeringButton, visible);
            SetVisible(_acceptButton, visible);
        }

        public void ShowEndTurn(bool visible) => SetVisible(_endTurnButton, visible);

        /// <summary>
        /// Title is the verdict for the human (YOU WIN / YOU LOSE); detail says who won and why.
        /// The card takes the winner's color and icon; with no winner yet (human eliminated) it stays neutral.
        /// </summary>
        public void ShowEndBanner(string title, string detail, God? winner)
        {
            GodUss.Apply(_endCard, winner);
            GodUss.Apply(_winnerChip, winner);
            SetVisible(_winnerChip, winner.HasValue);
            _winnerLabel.text = title;
            _reasonLabel.text = detail;
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

        private static void SetVisible(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private void HandleOfferingClicked() => OfferingClicked?.Invoke();

        private void HandleAcceptClicked() => AcceptClicked?.Invoke();

        private void HandleEndTurnClicked() => EndTurnClicked?.Invoke();

        private void HandlePlayAgainClicked() => PlayAgainClicked?.Invoke();
    }
}

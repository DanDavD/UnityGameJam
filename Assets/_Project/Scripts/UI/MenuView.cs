using System;
using System.Collections.Generic;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// The main menu over the board: title, tagline, the Pantheon powers / Classic toggle with a line
    /// that says what it changes, one card per god with its sacred number, cursed number, rival and
    /// (with powers) its Day and Wrath, and one fixed line under them that explains sacred and cursed.
    /// Beside the modes, Solo / Hot-seat: in hot-seat each card wears a HUMAN / BOT tag and a START
    /// button appears. Clicks are only reported; the controller decides what a card click means
    /// (start solo, or switch the seat) and the view never starts or stores anything.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuView : MonoBehaviour
    {
        private const string PrimaryClass = "button--primary";
        private const string SecondaryClass = "button--secondary";
        private const string SeatHumanClass = "god-card__seat--human";
        private const string SeatBotClass = "god-card__seat--bot";

        private UIDocument _document;
        private VisualElement _menu;
        private Button _powersButton;
        private Button _classicButton;
        private Label _modeLine;
        private Button _soloButton;
        private Button _hotSeatButton;
        private Label _prompt;
        private Button _startButton;

        // Indexed like Match.BaseTurnOrder.
        private readonly VisualElement[] _cards = new VisualElement[Match.BaseTurnOrder.Count];
        private readonly VisualElement[] _cardPowers = new VisualElement[Match.BaseTurnOrder.Count];
        private readonly Label[] _cardSeats = new Label[Match.BaseTurnOrder.Count];

        /// <summary>A god card was clicked: solo starts as that god, hot-seat switches its seat.</summary>
        public event Action<God> GodCardClicked;

        /// <summary>A mode button was clicked: true for Pantheon powers, false for Classic.</summary>
        public event Action<bool> PowersChosen;

        /// <summary>A players button was clicked: true for Hot-seat, false for Solo.</summary>
        public event Action<bool> HotSeatChosen;

        /// <summary>START was clicked (hot-seat only).</summary>
        public event Action StartClicked;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _menu = root.Q<VisualElement>("menu");
            root.Q<Label>("menu-title").text = MatchText.GameTitle;
            root.Q<Label>("menu-tagline").text = MatchText.Tagline;
            _prompt = root.Q<Label>("menu-prompt");
            root.Q<Label>("rule-sacred-lead").text = MatchText.OmenRuleSacredLead;
            root.Q<Label>("rule-sacred").text = MatchText.OmenRuleSacred;
            root.Q<Label>("rule-cursed-lead").text = MatchText.OmenRuleCursedLead;
            root.Q<Label>("rule-cursed").text = MatchText.OmenRuleCursed;
            root.Q<Label>("rule-rival").text = MatchText.OmenRuleRival;
            _powersButton = root.Q<Button>("mode-powers-button");
            _classicButton = root.Q<Button>("mode-classic-button");
            _modeLine = root.Q<Label>("mode-line");
            _soloButton = root.Q<Button>("seats-solo-button");
            _hotSeatButton = root.Q<Button>("seats-hotseat-button");
            _startButton = root.Q<Button>("menu-start-button");
            _startButton.text = MatchText.StartButton;
            _powersButton.clicked += HandlePowersClicked;
            _classicButton.clicked += HandleClassicClicked;
            _soloButton.clicked += HandleSoloClicked;
            _hotSeatButton.clicked += HandleHotSeatClicked;
            _startButton.clicked += HandleStartClicked;

            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
            {
                God god = Match.BaseTurnOrder[i];
                VisualElement card = root.Q<VisualElement>(CardName(god)).Q(className: "god-card");
                Fill(card, god);
                // The god travels with the card, so one handler serves all four.
                card.userData = god;
                card.RegisterCallback<ClickEvent>(HandleCardClicked);
                _cards[i] = card;
                _cardPowers[i] = card.Q<Label>(className: "god-card__powers");
                _cardSeats[i] = card.Q<Label>(className: "god-card__seat");
            }
            // Solo until the controller says otherwise.
            SetSeats(false, null, false);
            Hide();
        }

        private void OnDisable()
        {
            foreach (VisualElement card in _cards)
                card?.UnregisterCallback<ClickEvent>(HandleCardClicked);
            _powersButton.clicked -= HandlePowersClicked;
            _classicButton.clicked -= HandleClassicClicked;
            _soloButton.clicked -= HandleSoloClicked;
            _hotSeatButton.clicked -= HandleHotSeatClicked;
            _startButton.clicked -= HandleStartClicked;
        }

        /// <summary>Marks the selected mode (primary button), its line, and the cards' powers lines.</summary>
        public void SetPowers(bool enabled)
        {
            SetSelected(_powersButton, enabled);
            SetSelected(_classicButton, !enabled);
            _powersButton.text = MatchText.PowersModeButton(enabled);
            _classicButton.text = MatchText.ClassicModeButton(!enabled);
            _modeLine.text = enabled ? MatchText.PowersModeLine : MatchText.ClassicModeLine;
            foreach (VisualElement powers in _cardPowers)
                powers.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Solo (a card click starts) or hot-seat: every card shows who plays it, the prompt explains
        /// the switch and START shows, enabled only when <paramref name="canStart"/>.
        /// </summary>
        /// <param name="playerSeats">Per god in Match.BaseTurnOrder, true for HUMAN; ignored in solo.</param>
        public void SetSeats(bool hotSeat, IReadOnlyList<bool> playerSeats, bool canStart)
        {
            SetSelected(_soloButton, !hotSeat);
            SetSelected(_hotSeatButton, hotSeat);
            _soloButton.text = MatchText.SoloButton(!hotSeat);
            _hotSeatButton.text = MatchText.HotSeatButton(hotSeat);
            _prompt.text = hotSeat ? MatchText.HotSeatPrompt : MatchText.ChooseGodPrompt;
            _startButton.style.display = hotSeat ? DisplayStyle.Flex : DisplayStyle.None;
            _startButton.SetEnabled(canStart);
            // People are numbered in base turn order, bots skipped, exactly like the match's P1..P4 tags.
            int person = 0;
            for (int i = 0; i < _cardSeats.Length; i++)
            {
                Label seat = _cardSeats[i];
                seat.style.display = hotSeat ? DisplayStyle.Flex : DisplayStyle.None;
                if (!hotSeat)
                    continue;
                bool human = playerSeats[i];
                if (human)
                    person++;
                seat.text = human ? MatchText.SeatHumanTag(person) : MatchText.SeatBot;
                seat.EnableInClassList(SeatHumanClass, human);
                seat.EnableInClassList(SeatBotClass, !human);
            }
        }

        private static void SetSelected(Button button, bool selected)
        {
            button.EnableInClassList(PrimaryClass, selected);
            button.EnableInClassList(SecondaryClass, !selected);
        }

        public void Show() => _menu.style.display = DisplayStyle.Flex;

        public void Hide() => _menu.style.display = DisplayStyle.None;

        // Card instances are named after the god's enum value: "god-card-zeus", "god-card-morrigan".
        private static string CardName(God god) => $"god-card-{god.ToString().ToLowerInvariant()}";

        private static void Fill(VisualElement card, God god)
        {
            GodUss.Apply(card, god);
            GodUss.Apply(card.Q(className: "god-chip"), god);
            card.Q<Label>(className: "god-card__name").text = MatchText.GodName(god);
            card.Q<Label>(className: "god-card__sacred-text").text = MatchText.SacredLine(god);
            card.Q<Label>(className: "god-card__cursed-text").text = MatchText.CursedLine(god);
            card.Q<Label>(className: "god-card__rival").text = MatchText.RivalLine(god);
            card.Q<Label>(className: "god-card__powers").text = MatchText.CardPowersLine(god);
        }

        private void HandlePowersClicked() => PowersChosen?.Invoke(true);

        private void HandleClassicClicked() => PowersChosen?.Invoke(false);

        private void HandleSoloClicked() => HotSeatChosen?.Invoke(false);

        private void HandleHotSeatClicked() => HotSeatChosen?.Invoke(true);

        private void HandleStartClicked() => StartClicked?.Invoke();

        private void HandleCardClicked(ClickEvent evt)
        {
            var card = (VisualElement)evt.currentTarget;
            GodCardClicked?.Invoke((God)card.userData);
        }
    }
}

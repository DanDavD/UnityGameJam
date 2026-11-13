using System;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// The main menu over the board: title, tagline, the Pantheon powers / Classic toggle with a line
    /// that says what it changes, one card per god with its sacred number, cursed number, rival and
    /// (with powers) its Day and Wrath, and one fixed line under them that explains sacred and cursed.
    /// Clicks are only reported; a card click means "play as this god" and the view never starts or
    /// stores anything.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuView : MonoBehaviour
    {
        private const string PrimaryClass = "button--primary";
        private const string SecondaryClass = "button--secondary";

        private UIDocument _document;
        private VisualElement _menu;
        private VisualElement _homeScreen;
        private VisualElement _selectScreen;
        private VisualElement _rulesScreen;
        private VisualElement _pantheonScreen;
        private VisualElement _themeScreen;

        private Button _btnPlay;
        private Button _btnRules;
        private Button _btnPantheon;
        private Button _btnTheme;

        private Button _btnSelectBack;
        private Button _btnRulesBack;
        private Button _btnPantheonBack;
        private Button _btnThemeBack;

        private Button _powersButton;
        private Button _classicButton;
        private Label _modeLine;

        // Indexed like Match.BaseTurnOrder.
        private readonly VisualElement[] _cards = new VisualElement[Match.BaseTurnOrder.Count];
        private readonly VisualElement[] _cardPowers = new VisualElement[Match.BaseTurnOrder.Count];

        /// <summary>A god card was clicked: the match starts with the human as that god.</summary>
        public event Action<God> GodCardClicked;

        /// <summary>The pointer entered a god card (once per enter).</summary>
        public event Action CardHovered;

        /// <summary>A mode button was clicked: true for Pantheon powers, false for Classic.</summary>
        public event Action<bool> PowersChosen;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _menu = root.Q<VisualElement>("menu");
            _homeScreen = root.Q<VisualElement>("menu-home-screen");
            _selectScreen = root.Q<VisualElement>("menu-select-screen");
            _rulesScreen = root.Q<VisualElement>("menu-rules-screen");
            _pantheonScreen = root.Q<VisualElement>("menu-pantheon-screen");
            _themeScreen = root.Q<VisualElement>("menu-theme-screen");

            _btnPlay = root.Q<Button>("menu-btn-play");
            _btnRules = root.Q<Button>("menu-btn-rules");
            _btnPantheon = root.Q<Button>("menu-btn-pantheon");
            _btnTheme = root.Q<Button>("menu-btn-theme");

            _btnSelectBack = root.Q<Button>("menu-select-back");
            _btnRulesBack = root.Q<Button>("menu-rules-back");
            _btnPantheonBack = root.Q<Button>("menu-pantheon-back");
            _btnThemeBack = root.Q<Button>("menu-theme-back");

            if (_btnPlay != null) _btnPlay.clicked += HandlePlayClicked;
            if (_btnRules != null) _btnRules.clicked += HandleRulesClicked;
            if (_btnPantheon != null) _btnPantheon.clicked += HandlePantheonClicked;
            if (_btnTheme != null) _btnTheme.clicked += HandleThemeClicked;

            if (_btnSelectBack != null) _btnSelectBack.clicked += HandleBackClicked;
            if (_btnRulesBack != null) _btnRulesBack.clicked += HandleBackClicked;
            if (_btnPantheonBack != null) _btnPantheonBack.clicked += HandleBackClicked;
            if (_btnThemeBack != null) _btnThemeBack.clicked += HandleBackClicked;

            RegisterHover(_btnPlay);
            RegisterHover(_btnRules);
            RegisterHover(_btnPantheon);
            RegisterHover(_btnTheme);
            RegisterHover(_btnSelectBack);
            RegisterHover(_btnRulesBack);
            RegisterHover(_btnPantheonBack);
            RegisterHover(_btnThemeBack);

            root.Q<Label>("menu-title").text = MatchText.GameTitle;
            root.Q<Label>("menu-tagline").text = MatchText.Tagline;
            root.Q<Label>("menu-prompt").text = MatchText.ChooseGodPrompt;
            root.Q<Label>("rule-sacred-lead").text = MatchText.OmenRuleSacredLead;
            root.Q<Label>("rule-sacred").text = MatchText.OmenRuleSacred;
            root.Q<Label>("rule-cursed-lead").text = MatchText.OmenRuleCursedLead;
            root.Q<Label>("rule-cursed").text = MatchText.OmenRuleCursed;
            root.Q<Label>("rule-rival").text = MatchText.OmenRuleRival;
            _powersButton = root.Q<Button>("mode-powers-button");
            _classicButton = root.Q<Button>("mode-classic-button");
            _modeLine = root.Q<Label>("mode-line");
            _powersButton.clicked += HandlePowersClicked;
            _classicButton.clicked += HandleClassicClicked;
            _powersButton.RegisterCallback<PointerEnterEvent>(HandleCardEntered);
            _classicButton.RegisterCallback<PointerEnterEvent>(HandleCardEntered);

            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
            {
                God god = Match.BaseTurnOrder[i];
                VisualElement card = root.Q<VisualElement>(CardName(god)).Q(className: "god-card");
                Fill(card, god);
                // The god travels with the card, so one handler serves all four.
                card.userData = god;
                card.RegisterCallback<ClickEvent>(HandleCardClicked);
                card.RegisterCallback<PointerEnterEvent>(HandleCardEntered);
                _cards[i] = card;
                _cardPowers[i] = card.Q<Label>(className: "god-card__powers");
            }
            Hide();
        }

        private void OnDisable()
        {
            if (_btnPlay != null) _btnPlay.clicked -= HandlePlayClicked;
            if (_btnRules != null) _btnRules.clicked -= HandleRulesClicked;
            if (_btnPantheon != null) _btnPantheon.clicked -= HandlePantheonClicked;
            if (_btnTheme != null) _btnTheme.clicked -= HandleThemeClicked;

            if (_btnSelectBack != null) _btnSelectBack.clicked -= HandleBackClicked;
            if (_btnRulesBack != null) _btnRulesBack.clicked -= HandleBackClicked;
            if (_btnPantheonBack != null) _btnPantheonBack.clicked -= HandleBackClicked;
            if (_btnThemeBack != null) _btnThemeBack.clicked -= HandleBackClicked;

            UnregisterHover(_btnPlay);
            UnregisterHover(_btnRules);
            UnregisterHover(_btnPantheon);
            UnregisterHover(_btnTheme);
            UnregisterHover(_btnSelectBack);
            UnregisterHover(_btnRulesBack);
            UnregisterHover(_btnPantheonBack);
            UnregisterHover(_btnThemeBack);

            foreach (VisualElement card in _cards)
            {
                card?.UnregisterCallback<ClickEvent>(HandleCardClicked);
                card?.UnregisterCallback<PointerEnterEvent>(HandleCardEntered);
            }
            _powersButton.clicked -= HandlePowersClicked;
            _classicButton.clicked -= HandleClassicClicked;
            _powersButton.UnregisterCallback<PointerEnterEvent>(HandleCardEntered);
            _classicButton.UnregisterCallback<PointerEnterEvent>(HandleCardEntered);
        }

        private void RegisterHover(Button btn)
        {
            btn?.RegisterCallback<PointerEnterEvent>(HandleCardEntered);
        }

        private void UnregisterHover(Button btn)
        {
            btn?.UnregisterCallback<PointerEnterEvent>(HandleCardEntered);
        }

        private void HandlePlayClicked() => SwitchScreen(_selectScreen);
        private void HandleRulesClicked() => SwitchScreen(_rulesScreen);
        private void HandlePantheonClicked() => SwitchScreen(_pantheonScreen);
        private void HandleThemeClicked() => SwitchScreen(_themeScreen);
        private void HandleBackClicked() => SwitchScreen(_homeScreen);

        private void SwitchScreen(VisualElement screen)
        {
            CardHovered?.Invoke();
            if (_homeScreen != null) _homeScreen.style.display = DisplayStyle.None;
            if (_selectScreen != null) _selectScreen.style.display = DisplayStyle.None;
            if (_rulesScreen != null) _rulesScreen.style.display = DisplayStyle.None;
            if (_pantheonScreen != null) _pantheonScreen.style.display = DisplayStyle.None;
            if (_themeScreen != null) _themeScreen.style.display = DisplayStyle.None;

            if (screen != null)
                screen.style.display = DisplayStyle.Flex;
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
            {
                powers.style.display = DisplayStyle.Flex;
                powers.style.visibility = enabled ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private static void SetSelected(Button button, bool selected)
        {
            button.EnableInClassList(PrimaryClass, selected);
            button.EnableInClassList(SecondaryClass, !selected);
        }

        public void Show()
        {
            _menu.style.display = DisplayStyle.Flex;
            SwitchScreen(_homeScreen);
        }

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

        private void HandlePowersClicked()
        {
            CardHovered?.Invoke();
            PowersChosen?.Invoke(true);
        }

        private void HandleClassicClicked()
        {
            CardHovered?.Invoke();
            PowersChosen?.Invoke(false);
        }

        private void HandleCardEntered(PointerEnterEvent evt) => CardHovered?.Invoke();

        private void HandleCardClicked(ClickEvent evt)
        {
            var card = (VisualElement)evt.currentTarget;
            GodCardClicked?.Invoke((God)card.userData);
        }
    }
}

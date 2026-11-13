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
        private const string DotActiveClass = "howto__dot--active";

        private UIDocument _document;
        private VisualElement _menu;
        private VisualElement _homeScreen;
        private VisualElement _selectScreen;
        private VisualElement _rulesScreen;

        private Button _btnPlay;
        private Button _btnRules;
        private Button _btnSelectBack;
        private Button _btnRulesBack;

        // How to play tabs, pages, dots and next button
        private readonly Button[] _howtoTabs = new Button[3];
        private readonly VisualElement[] _howtoPages = new VisualElement[3];
        private readonly VisualElement[] _howtoDots = new VisualElement[3];
        private Button _howtoNext;
        private int _currentHowtoTab;

        private Button _powersButton;
        private Button _classicButton;
        private Label _modeLine;

        // Indexed like Match.BaseTurnOrder.
        private readonly VisualElement[] _cards = new VisualElement[Match.BaseTurnOrder.Count];
        private readonly VisualElement[] _cardPowers = new VisualElement[Match.BaseTurnOrder.Count];

        /// <summary>A god card was clicked: the match starts with the human as that god.</summary>
        public event Action<God> GodCardClicked;

        /// <summary>The pointer entered a god card or interactive button (once per enter).</summary>
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

            _btnPlay = root.Q<Button>("menu-btn-play");
            _btnRules = root.Q<Button>("menu-btn-rules");
            _btnSelectBack = root.Q<Button>("menu-select-back");
            _btnRulesBack = root.Q<Button>("menu-rules-back");

            if (_btnPlay != null) _btnPlay.clicked += HandlePlayClicked;
            if (_btnRules != null) _btnRules.clicked += HandleRulesClicked;
            if (_btnSelectBack != null) _btnSelectBack.clicked += HandleBackClicked;
            if (_btnRulesBack != null) _btnRulesBack.clicked += HandleBackClicked;

            RegisterHover(_btnPlay);
            RegisterHover(_btnRules);
            RegisterHover(_btnSelectBack);
            RegisterHover(_btnRulesBack);

            // How to play 3-tab navigation
            for (int i = 0; i < 3; i++)
            {
                int tabIndex = i;
                _howtoTabs[i] = root.Q<Button>($"howto-tab-{i}");
                _howtoPages[i] = root.Q<VisualElement>($"howto-page-{i}");
                _howtoDots[i] = root.Q<VisualElement>($"howto-dot-{i}");

                if (_howtoTabs[i] != null)
                {
                    _howtoTabs[i].clicked += () => SwitchHowtoTab(tabIndex, playSound: true);
                    RegisterHover(_howtoTabs[i]);
                }
            }

            _howtoNext = root.Q<Button>("howto-next");
            if (_howtoNext != null)
            {
                _howtoNext.clicked += HandleHowtoNextClicked;
                RegisterHover(_howtoNext);
            }

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
            if (_powersButton != null)
            {
                _powersButton.clicked += HandlePowersClicked;
                _powersButton.RegisterCallback<PointerEnterEvent>(HandleCardEntered);
            }
            if (_classicButton != null)
            {
                _classicButton.clicked += HandleClassicClicked;
                _classicButton.RegisterCallback<PointerEnterEvent>(HandleCardEntered);
            }

            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
            {
                God god = Match.BaseTurnOrder[i];
                VisualElement card = root.Q<VisualElement>(CardName(god)).Q(className: "god-card");
                Fill(card, god);
                card.userData = god;
                card.RegisterCallback<ClickEvent>(HandleCardClicked);
                card.RegisterCallback<PointerEnterEvent>(HandleCardEntered);
                _cards[i] = card;
                _cardPowers[i] = card.Q<Label>(className: "god-card__powers");
            }

            SwitchHowtoTab(0, playSound: false);
            Hide();
        }

        private void OnDisable()
        {
            if (_btnPlay != null) _btnPlay.clicked -= HandlePlayClicked;
            if (_btnRules != null) _btnRules.clicked -= HandleRulesClicked;
            if (_btnSelectBack != null) _btnSelectBack.clicked -= HandleBackClicked;
            if (_btnRulesBack != null) _btnRulesBack.clicked -= HandleBackClicked;

            UnregisterHover(_btnPlay);
            UnregisterHover(_btnRules);
            UnregisterHover(_btnSelectBack);
            UnregisterHover(_btnRulesBack);

            for (int i = 0; i < 3; i++)
            {
                UnregisterHover(_howtoTabs[i]);
            }
            if (_howtoNext != null)
            {
                _howtoNext.clicked -= HandleHowtoNextClicked;
                UnregisterHover(_howtoNext);
            }

            foreach (VisualElement card in _cards)
            {
                card?.UnregisterCallback<ClickEvent>(HandleCardClicked);
                card?.UnregisterCallback<PointerEnterEvent>(HandleCardEntered);
            }
            if (_powersButton != null)
            {
                _powersButton.clicked -= HandlePowersClicked;
                _powersButton.UnregisterCallback<PointerEnterEvent>(HandleCardEntered);
            }
            if (_classicButton != null)
            {
                _classicButton.clicked -= HandleClassicClicked;
                _classicButton.UnregisterCallback<PointerEnterEvent>(HandleCardEntered);
            }
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

        private void HandleRulesClicked()
        {
            SwitchHowtoTab(0, playSound: false);
            SwitchScreen(_rulesScreen);
        }

        private void HandleBackClicked()
        {
            SwitchHowtoTab(0, playSound: false);
            SwitchScreen(_homeScreen);
        }

        private void HandleHowtoNextClicked()
        {
            CardHovered?.Invoke();
            if (_currentHowtoTab < 2)
            {
                SwitchHowtoTab(_currentHowtoTab + 1, playSound: false);
            }
            else
            {
                HandlePlayClicked();
            }
        }

        private void SwitchHowtoTab(int index, bool playSound)
        {
            if (playSound)
                CardHovered?.Invoke();

            _currentHowtoTab = Mathf.Clamp(index, 0, 2);

            for (int i = 0; i < 3; i++)
            {
                bool isActive = (i == _currentHowtoTab);
                if (_howtoTabs[i] != null)
                {
                    _howtoTabs[i].EnableInClassList(PrimaryClass, isActive);
                    _howtoTabs[i].EnableInClassList(SecondaryClass, !isActive);
                }
                if (_howtoPages[i] != null)
                {
                    _howtoPages[i].style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
                }
                if (_howtoDots[i] != null)
                {
                    _howtoDots[i].EnableInClassList(DotActiveClass, isActive);
                }
            }

            if (_howtoNext != null)
            {
                _howtoNext.text = _currentHowtoTab switch
                {
                    0 => "NEXT: DICE & OMENS →",
                    1 => "NEXT: GOD POWERS →",
                    _ => "PLAY MATCH"
                };
            }
        }

        private void SwitchScreen(VisualElement screen)
        {
            CardHovered?.Invoke();
            if (_homeScreen != null) _homeScreen.style.display = DisplayStyle.None;
            if (_selectScreen != null) _selectScreen.style.display = DisplayStyle.None;
            if (_rulesScreen != null) _rulesScreen.style.display = DisplayStyle.None;

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
            if (button == null) return;
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

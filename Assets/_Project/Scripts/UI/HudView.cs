using System;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// The match HUD: the human's god chip and numbers, the round and Destiny line with its info panel
    /// (Destiny per god, who opens, the Day, the goal), action prompt, reinforcement counter,
    /// onboarding hint, action buttons, the sound and fast-bots toggles and the end screen. With the
    /// god powers on it also shows the human's Wrath and Favor, the Wrath button, the badge of a
    /// round-long Day and a news line that says what a power just did.
    /// A pure view: it shows what it is told and reports button clicks as C# events.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudView : MonoBehaviour
    {
        private const string WrathReadyClass = "button--wrath-ready";
        private const string PrimaryButtonClass = "button--primary";
        private const string SecondaryButtonClass = "button--secondary";
        private const string PromptRoadClass = "hud__prompt--road";
        private const string NewsSacredClass = "hud__news--sacred";
        private const string NewsCursedClass = "hud__news--cursed";
        private const string InfoOpenClass = "info-toggle--open";
        private const string CountPopClass = "reinforce-counter__count--pop";
        private const string RoundInfoGodTextClass = "round-info__god-text";
        private const string WrathPulseClass = "button--wrath-pulse";
        private const string FavorStarClass = "favor-star";
        private const string FavorStarBackClass = "favor-star__back";
        private const string FavorStarFrontClass = "favor-star__front";
        private const string FavorCrackClass = "favor-crack";

        // Two of each: one combat can move both sides' Favor at once (two sacred dice, or two cursed).
        private const int FlyersPerKind = 2;

        [Header("Favor juice")]
        [Tooltip(
            "Seconds of a sacred star: pop over the die, fly to its god's Favor stars, burst as it lands."
        )]
        [SerializeField, Min(0.1f)]
        private float _favorStarSeconds = 1f;

        [Tooltip("How high the star's arc rises above the straight line, in panel pixels.")]
        [SerializeField, Min(0f)]
        private float _favorStarArc = 120f;

        [Tooltip("Seconds of the cracked mark that sinks over a meter that lost a star.")]
        [SerializeField, Min(0.1f)]
        private float _favorCrackSeconds = 0.8f;

        [Tooltip("Seconds of each half of the ready Wrath button's breath (grow, then rest).")]
        [SerializeField, Min(0.1f)]
        private float _wrathPulseSeconds = 0.8f;

        [Tooltip(
            "How long the reinforcement count stays grown after it drops; USS eases it in and out."
        )]
        [SerializeField, Min(0f)]
        private float _countPopSeconds = 0.2f;

        [Tooltip("Seconds an onboarding hint stays, unless the next action hides it first.")]
        [SerializeField, Min(0f)]
        private float _hintDuration = 4f;

        [Tooltip("Seconds a power's news line stays, unless the next one replaces it.")]
        [SerializeField, Min(0f)]
        private float _newsDuration = 4f;

        private UIDocument _document;
        private VisualElement _identityCard;
        private VisualElement _identityChip;
        private Label _identityLabel;
        private Label _identitySacredLabel;
        private Label _identityCursedLabel;
        private VisualElement _roundBar;
        private Label _roundLabel;
        private Label _roundCounter;
        private VisualElement _roundInfo;
        private Label _roundInfoTitle;

        // Indexed like Match.BaseTurnOrder, which is also the row order in the UXML.
        private readonly Label[] _roundInfoGods = new Label[Match.BaseTurnOrder.Count];
        private Label _roundInfoOpener;
        private Label _roundInfoDayHeading;
        private Label _roundInfoDay;
        private VisualElement _reinforceCounter;
        private Label _reinforceCount;
        private Label _reinforceCaption;
        private Label _reinforceFormula;
        private int _shownReinforcements;
        private IVisualElementScheduledItem _countPopEnd;
        private VisualElement _identityWrath;
        private Label _identityWrathName;
        private Label _identityFavor;
        private IVisualElementScheduledItem _wrathPulse;
        private readonly FavorFlyer[] _favorStars = new FavorFlyer[FlyersPerKind];
        private readonly FavorFlyer[] _favorCracks = new FavorFlyer[FlyersPerKind];

        // Leads every prompt of a person's turn ("YOUR TURN" / "ZEUS'S TURN"); null on a bot's turn.
        private string _turnPrefix;
        private Label _dayBadge;
        private VisualElement _news;
        private Label _newsLabel;
        private Button _wrathButton;
        private Label _promptLabel;
        private Label _hintLabel;
        private Button _endTurnButton;
        private VisualElement _toggles;
        private Button _muteButton;
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
        private Label _combatsLabel;
        private Button _playAgainButton;
        private Button _mainMenuButton;
        private IVisualElementScheduledItem _hintTimeout;
        private IVisualElementScheduledItem _newsTimeout;

        public event Action EndTurnClicked;
        public event Action WrathClicked;
        public event Action FastBotsClicked;
        public event Action MuteClicked;
        public event Action PlayAgainClicked;
        public event Action MainMenuClicked;

        /// <summary>The round info panel opened, so any other info card can close (one at a time).</summary>
        public event Action RoundInfoOpened;

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
            _identitySacredLabel = root.Q<Label>("identity-sacred-label");
            _identityCursedLabel = root.Q<Label>("identity-cursed-label");
            _roundBar = root.Q<VisualElement>("round-bar");
            _roundLabel = root.Q<Label>("round-label");
            _roundCounter = root.Q<Label>("round-counter");
            _roundInfo = root.Q<VisualElement>("round-info");
            _roundInfoTitle = root.Q<Label>("round-info-title");
            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
                _roundInfoGods[i] = root.Q<VisualElement>(RoundInfoGodName(Match.BaseTurnOrder[i]))
                    .Q<Label>(className: RoundInfoGodTextClass);
            _roundInfoOpener = root.Q<Label>("round-info-opener");
            _roundInfoDayHeading = root.Q<Label>("round-info-day-heading");
            _roundInfoDay = root.Q<Label>("round-info-day");
            _reinforceCounter = root.Q<VisualElement>("reinforce-counter");
            _reinforceCount = root.Q<Label>("reinforce-count");
            _reinforceCaption = root.Q<Label>("reinforce-caption");
            _reinforceFormula = root.Q<Label>("reinforce-formula");
            _identityWrath = root.Q<VisualElement>("identity-wrath");
            _identityWrathName = root.Q<Label>("identity-wrath-name");
            _identityFavor = root.Q<Label>("identity-favor");
            _dayBadge = root.Q<Label>("day-badge");
            _news = root.Q<VisualElement>("news");
            _newsLabel = root.Q<Label>("news-label");
            _wrathButton = root.Q<Button>("wrath-button");
            _promptLabel = root.Q<Label>("prompt-label");
            _hintLabel = root.Q<Label>("hint-label");
            _endTurnButton = root.Q<Button>("end-turn-button");
            _toggles = root.Q<VisualElement>("hud-toggles");
            _muteButton = root.Q<Button>("mute-button");
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
            _combatsLabel = root.Q<Label>("stat-combats-label");
            _playAgainButton = root.Q<Button>("play-again-button");
            _mainMenuButton = root.Q<Button>("main-menu-button");

            // Player-facing text lives in MatchText; the UXML text is only the UI Builder preview.
            root.Q<Label>("round-info-opener-heading").text = MatchText.OpenerHeading;
            root.Q<Label>("round-info-goal-heading").text = MatchText.GoalHeading;
            root.Q<Label>("round-info-goal").text = MatchText.RoundInfoGoal;
            root.Q<Label>("round-info-close").text = MatchText.InfoCloseHint;
            _endTurnButton.text = MatchText.EndTurnButton;
            _playAgainButton.text = MatchText.PlayAgainButton;
            _mainMenuButton.text = MatchText.MainMenuButton;
            root.Q<Label>("stat-rounds-label").text = MatchText.RoundsStat;
            _endTurnButton.clicked += HandleEndTurnClicked;
            _wrathButton.clicked += HandleWrathClicked;
            _fastBotsButton.clicked += HandleFastBotsClicked;
            _muteButton.clicked += HandleMuteClicked;
            _playAgainButton.clicked += HandlePlayAgainClicked;
            _mainMenuButton.clicked += HandleMainMenuClicked;
            // The whole round line toggles its panel; a click on the open panel closes it.
            _roundBar.RegisterCallback<ClickEvent>(HandleRoundBarClicked);
            _roundInfo.RegisterCallback<ClickEvent>(HandleRoundInfoClicked);
            // Created once and re-armed on every drop, so a placement never allocates.
            _countPopEnd = _reinforceCount
                .schedule.Execute(() => _reinforceCount.RemoveFromClassList(CountPopClass))
                .StartingIn(0);
            _countPopEnd.Pause();
            // The ready Wrath breathes: the class flips each half-breath and USS eases the scale.
            _wrathPulse = _wrathButton
                .schedule.Execute(() => _wrathButton.ToggleInClassList(WrathPulseClass))
                .Every(SecondsToMs(_wrathPulseSeconds));
            _wrathPulse.Pause();
            CreateFavorFlyers(root.Q<VisualElement>("hud"));

            // Nothing about the match shows until a god is chosen and the first round starts.
            SetVisible(_identityCard, false);
            // The sound toggle already works on the main menu; fast bots only means something in a match.
            SetVisible(_toggles, true);
            SetVisible(_fastBotsButton, false);
            SetRound(string.Empty, string.Empty);
            SetPrompt(string.Empty);
            HideHint();
            ShowEndTurn(false);
            HideReinforcements();
            ShowWrath(false);
            ShowFavor(false);
            SetDayBadge(null, null);
            HideNews();
            SetVisible(_endBanner, false);
        }

        private void OnDisable()
        {
            _endTurnButton.clicked -= HandleEndTurnClicked;
            _wrathButton.clicked -= HandleWrathClicked;
            _fastBotsButton.clicked -= HandleFastBotsClicked;
            _muteButton.clicked -= HandleMuteClicked;
            _playAgainButton.clicked -= HandlePlayAgainClicked;
            _mainMenuButton.clicked -= HandleMainMenuClicked;
            _roundBar.UnregisterCallback<ClickEvent>(HandleRoundBarClicked);
            _roundInfo.UnregisterCallback<ClickEvent>(HandleRoundInfoClicked);
            _hintTimeout?.Pause();
            _newsTimeout?.Pause();
            _countPopEnd?.Pause();
            _wrathPulse?.Pause();
            StopFavorFlyers();
        }

        // Pooled once per enable (the UIDocument tree is rebuilt then): a few elements, never one per Favor change.
        private void CreateFavorFlyers(VisualElement hud)
        {
            for (int i = 0; i < FlyersPerKind; i++)
            {
                var star = new VisualElement();
                star.AddToClassList(FavorStarClass);
                star.Add(NewIgnoredChild(FavorStarBackClass));
                star.Add(NewIgnoredChild(FavorStarFrontClass));
                _favorStars[i] = new FavorFlyer(hud, star);

                var crack = new VisualElement();
                crack.AddToClassList(FavorCrackClass);
                _favorCracks[i] = new FavorFlyer(hud, crack);
            }
        }

        private static VisualElement NewIgnoredChild(string className)
        {
            var child = new VisualElement { pickingMode = PickingMode.Ignore };
            child.AddToClassList(className);
            return child;
        }

        private void StopFavorFlyers()
        {
            for (int i = 0; i < FlyersPerKind; i++)
            {
                _favorStars[i]?.Stop();
                _favorCracks[i]?.Stop();
            }
        }

        /// <summary>
        /// The human's god: its chip (color + icon) and the line with its sacred / cursed numbers.
        /// In hot-seat it is called again on every person's turn, so the card follows the mouse.
        /// </summary>
        /// <param name="seatTag">"YOU" solo, "P1".."P4" in hot-seat (the nations panel tag).</param>
        public void SetIdentity(God god, string seatTag)
        {
            GodUss.Apply(_identityChip, god);
            _identityLabel.text = MatchText.IdentityName(god, seatTag);
            _identitySacredLabel.text = MatchText.IdentitySacred(god);
            _identityCursedLabel.text = MatchText.IdentityCursed(god);
            SetVisible(_identityCard, true);
            SetVisible(_fastBotsButton, true);
        }

        /// <summary>Hides the identity card until the next <see cref="SetIdentity"/> (hot-seat: its person is out).</summary>
        public void HideIdentity() => SetVisible(_identityCard, false);

        /// <summary>Whether the identity card is on screen (a star can fly to its Favor stars).</summary>
        public bool IsIdentityShown => _identityCard.style.display == DisplayStyle.Flex;

        /// <summary>The identity card's Favor stars, where a star from the viewer's sacred die lands.</summary>
        public VisualElement IdentityFavor => _identityFavor;

        /// <summary>
        /// The round bar: the Destiny line ("Destiny 2: Day of Ra, Morrígan −2") over the round
        /// counter ("ROUND 1/15"). An empty Destiny line hides the bar.
        /// </summary>
        public void SetRound(string destinyLine, string roundCounter)
        {
            bool shown = !string.IsNullOrEmpty(destinyLine);
            _roundLabel.text = destinyLine;
            _roundCounter.text = roundCounter;
            SetVisible(_roundBar, shown);
            if (!shown)
                CloseRoundInfo();
        }

        /// <summary>
        /// Fills the round info panel: the Destiny face, one line per god (indexed like
        /// Match.BaseTurnOrder), who opens and why, and the Day (null heading hides that block).
        /// </summary>
        public void SetRoundInfo(
            string title,
            string[] godLines,
            string opener,
            string dayHeading,
            string day
        )
        {
            _roundInfoTitle.text = title;
            for (int i = 0; i < _roundInfoGods.Length; i++)
                _roundInfoGods[i].text = godLines[i];
            _roundInfoOpener.text = opener;
            bool hasDay = dayHeading != null;
            _roundInfoDayHeading.text = dayHeading ?? string.Empty;
            _roundInfoDay.text = day ?? string.Empty;
            SetVisible(_roundInfoDayHeading, hasDay);
            SetVisible(_roundInfoDay, hasDay);
        }

        public bool IsRoundInfoOpen => _roundInfo.style.display == DisplayStyle.Flex;

        public void CloseRoundInfo() => SetRoundInfoOpen(false);

        /// <summary>
        /// The big "5 TROOPS TO PLACE" counter while the human reinforces, with where the troops came
        /// from under it (<paramref name="formula"/>). When the number drops (a troop was placed) it
        /// pops, so each click is answered where the eye already is.
        /// </summary>
        public void ShowReinforcements(int troopsLeft, string formula)
        {
            _reinforceFormula.text = formula;
            bool visible = _reinforceCounter.style.display == DisplayStyle.Flex;
            if (visible && troopsLeft == _shownReinforcements)
                return;
            if (visible && troopsLeft < _shownReinforcements)
            {
                _reinforceCount.AddToClassList(CountPopClass);
                _countPopEnd.ExecuteLater(SecondsToMs(_countPopSeconds));
            }
            _shownReinforcements = troopsLeft;
            _reinforceCount.text = troopsLeft.ToString();
            _reinforceCaption.text = MatchText.TroopsToPlace(troopsLeft);
            SetVisible(_reinforceCounter, true);
        }

        public void HideReinforcements()
        {
            _countPopEnd?.Pause();
            _reinforceCount.RemoveFromClassList(CountPopClass);
            SetVisible(_reinforceCounter, false);
        }

        /// <summary>
        /// The prompt line. On a person's turn it leads with <see cref="SetTurnPrefix"/>'s
        /// "YOUR TURN" / "ZEUS'S TURN"; an empty prompt then shows only that (Reinforce).
        /// </summary>
        public void SetPrompt(string text)
        {
            _promptLabel.RemoveFromClassList(PromptRoadClass);
            ShowPromptText(text);
        }

        /// <summary>
        /// A prompt in Sacred Road mode: the call to act, larger and bold in ink. The next
        /// <see cref="SetPrompt"/> returns to the normal style.
        /// </summary>
        public void SetRoadPrompt(string text)
        {
            _promptLabel.AddToClassList(PromptRoadClass);
            ShowPromptText(text);
        }

        private void ShowPromptText(string text)
        {
            if (_turnPrefix == null)
                _promptLabel.text = text;
            else if (string.IsNullOrEmpty(text))
                _promptLabel.text = _turnPrefix;
            else
                _promptLabel.text = $"{_turnPrefix} · {text}";
        }

        /// <summary>
        /// Whose turn every following prompt names (MatchText.TurnPrefix), or null on a bot's turn
        /// and after the match, so hot-seat prompts never say "YOUR TURN" to the wrong person.
        /// </summary>
        public void SetTurnPrefix(string prefix) => _turnPrefix = prefix;

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

        public void SetMuted(bool muted) => _muteButton.text = MatchText.MuteButton(muted);

        public void ShowEndTurn(bool visible) => SetVisible(_endTurnButton, visible);

        /// <summary>Shows the Wrath line on the identity card; hidden in Classic.</summary>
        public void ShowFavor(bool visible) => SetVisible(_identityWrath, visible);

        /// <summary>The identity card's Wrath line: the god's Wrath name and its Favor stars, on its tint.</summary>
        public void SetFavor(God god, int favor)
        {
            GodUss.Apply(_identityWrath, god);
            _identityWrathName.text = MatchText.WrathName(god);
            _identityFavor.text = MatchText.FavorMeter(favor);
        }

        /// <summary>
        /// A star pops over <paramref name="die"/> (a sacred combat die) and flies to
        /// <paramref name="meter"/>; <paramref name="arrived"/> fills the meter as it lands. With no
        /// free star (or nothing laid out) the change shows at once, so Favor is never left stale.
        /// </summary>
        /// <param name="pace">Shortens a bot's star, like its other timings (1 = normal).</param>
        public void PlayFavorStar(
            VisualElement die,
            VisualElement meter,
            God god,
            float pace,
            Action arrived
        )
        {
            foreach (FavorFlyer star in _favorStars)
            {
                if (
                    star.PlayStar(die, meter, god, _favorStarSeconds * pace, _favorStarArc, arrived)
                )
                    return;
            }
            arrived();
        }

        /// <summary>The quiet counterpart of a cursed roll: a cracked mark sinks over the meter while it dims.</summary>
        public void PlayFavorCrack(VisualElement meter, float pace)
        {
            foreach (FavorFlyer crack in _favorCracks)
            {
                if (crack.PlayCrack(meter, _favorCrackSeconds * pace))
                    return;
            }
        }

        public void ShowWrath(bool visible)
        {
            SetVisible(_wrathButton, visible);
            if (!visible)
                SetWrathPulse(false);
        }

        /// <summary>
        /// The Wrath button: its text ("THUNDERBOLT · 2/3", "★ THUNDERBOLT", "CANCEL THUNDERBOLT"),
        /// whether it can be pressed, and its look. Ready, it wears the god's tint and color; while
        /// aiming it turns secondary, so it reads as "back out" rather than "strike".
        /// </summary>
        public void SetWrath(string text, bool enabled, God god, bool aiming)
        {
            _wrathButton.text = text;
            _wrathButton.SetEnabled(enabled);
            GodUss.Apply(_wrathButton, god);
            bool ready = enabled && !aiming;
            _wrathButton.EnableInClassList(WrathReadyClass, ready);
            _wrathButton.EnableInClassList(PrimaryButtonClass, !aiming);
            _wrathButton.EnableInClassList(SecondaryButtonClass, aiming);
            SetWrathPulse(ready);
        }

        // Only a ready Wrath breathes; any other state stands still at rest scale.
        private void SetWrathPulse(bool on)
        {
            if (on)
            {
                _wrathPulse.Resume();
                return;
            }
            _wrathPulse.Pause();
            _wrathButton.RemoveFromClassList(WrathPulseClass);
        }

        /// <summary>The badge under the round line while a round-long Day lasts; null hides it.</summary>
        public void SetDayBadge(string text, God? god)
        {
            _dayBadge.text = text ?? string.Empty;
            GodUss.Apply(_dayBadge, god);
            SetVisible(_dayBadge, !string.IsNullOrEmpty(text));
        }

        /// <summary>
        /// What a power just did, in that god's colors; it hides itself after a few seconds.
        /// A sacred or cursed <paramref name="omen"/> leads the line with its drawn icon (a Favor change).
        /// </summary>
        public void ShowNews(string text, God god, RollOutcome omen = RollOutcome.Neutral)
        {
            _newsLabel.text = text;
            GodUss.Apply(_news, god);
            _news.EnableInClassList(NewsSacredClass, omen == RollOutcome.Sacred);
            _news.EnableInClassList(NewsCursedClass, omen == RollOutcome.Cursed);
            SetVisible(_news, true);
            _newsTimeout?.Pause();
            _newsTimeout = _news.schedule.Execute(HideNews).StartingIn(SecondsToMs(_newsDuration));
        }

        public void HideNews()
        {
            _newsTimeout?.Pause();
            SetVisible(_news, false);
        }

        /// <summary>
        /// Title is the verdict (YOU WIN / YOU LOSE solo, THOR WINS in hot-seat); detail says why.
        /// The card takes the winner's color, icon and <paramref name="winnerName"/>. With no winner
        /// yet (every person eliminated) it stays neutral and the winner's temples stat is hidden.
        /// </summary>
        /// <param name="combatsLabel">Whose combats <paramref name="combatsWon"/> counts ("combats you won").</param>
        public void ShowEndBanner(
            string title,
            string detail,
            God? winner,
            string winnerName,
            int rounds,
            int winnerTemples,
            int combatsWon,
            string combatsLabel
        )
        {
            GodUss.Apply(_endCard, winner);
            GodUss.Apply(_winnerChip, winner);
            SetVisible(_winnerChip, winner.HasValue);
            SetVisible(_winnerName, winner.HasValue);
            SetVisible(_templesStat, winner.HasValue);
            if (winner.HasValue)
            {
                _winnerName.text = winnerName;
                _templesLabel.text = MatchText.WinnerTemplesStat(winner.Value);
            }
            _winnerTitle.text = title;
            _reasonLabel.text = detail;
            _roundsValue.text = rounds.ToString();
            _templesValue.text = winnerTemples.ToString();
            _combatsValue.text = combatsWon.ToString();
            _combatsLabel.text = combatsLabel;
            HideHint();
            HideNews();
            HideReinforcements();
            CloseRoundInfo();
            ShowWrath(false);
            StopFavorFlyers();
            _turnPrefix = null;
            SetVisible(_toggles, false);
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

        private void HandleEndTurnClicked() => EndTurnClicked?.Invoke();

        private void HandleWrathClicked() => WrathClicked?.Invoke();

        private void HandleFastBotsClicked() => FastBotsClicked?.Invoke();

        private void HandleMuteClicked() => MuteClicked?.Invoke();

        private void HandlePlayAgainClicked() => PlayAgainClicked?.Invoke();

        private void HandleMainMenuClicked() => MainMenuClicked?.Invoke();

        private void HandleRoundBarClicked(ClickEvent evt) => SetRoundInfoOpen(!IsRoundInfoOpen);

        private void HandleRoundInfoClicked(ClickEvent evt) => SetRoundInfoOpen(false);

        private void SetRoundInfoOpen(bool open)
        {
            bool wasOpen = IsRoundInfoOpen;
            SetVisible(_roundInfo, open);
            _roundBar.EnableInClassList(InfoOpenClass, open);
            if (open && !wasOpen)
                RoundInfoOpened?.Invoke();
        }

        private static string RoundInfoGodName(God god) =>
            $"round-info-{god.ToString().ToLowerInvariant()}";
    }
}

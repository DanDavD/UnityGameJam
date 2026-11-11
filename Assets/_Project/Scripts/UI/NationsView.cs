using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// The left column: one row per god in base cycle order with its territories, troops and temples
    /// (●●○○, the dot that wins ringed as the goal), a tag on each person's nation (YOU solo, P1..P4
    /// in hot-seat), the god whose turn it is highlighted and eliminated gods greyed out.
    /// With the god powers on, each row also has its Favor stars (★★☆), which bump when they change
    /// and are only redrawn on a Favor change (so a star flying to them can fill them as it lands).
    /// A pure view: <see cref="Refresh"/> redraws every row from the match state it is given.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class NationsView : MonoBehaviour
    {
        private const string TurnClass = "nation-row--turn";
        private const string EliminatedClass = "nation-row--eliminated";
        private const string TempleDotClass = "nation-row__temple-dot";
        private const string GoalDotClass = "nation-row__temple-dot--goal";
        private const string GoalNumberClass = "nation-row__temple-dot--goal-number";
        private const string FavorFullClass = "nation-row__favor--full";
        private const string FavorBumpClass = "favor--bump";
        private const string InfoToggleClass = "info-toggle";
        private const string InfoOpenClass = "info-toggle--open";
        private const int NoCard = -1;

        [Tooltip("How long a Favor meter stays grown after it changes; USS eases it in and out.")]
        [SerializeField, Min(0f)]
        private float _favorBumpSeconds = 0.25f;

        private UIDocument _document;
        private VisualElement _panel;

        // Indexed like Match.BaseTurnOrder, which is also the row order.
        private readonly VisualElement[] _rows = new VisualElement[Match.BaseTurnOrder.Count];
        private readonly VisualElement[] _instances = new VisualElement[Match.BaseTurnOrder.Count];
        private readonly Label[] _places = new Label[Match.BaseTurnOrder.Count];
        private readonly Label[] _stats = new Label[Match.BaseTurnOrder.Count];
        private readonly VisualElement[] _temples = new VisualElement[Match.BaseTurnOrder.Count];

        // [row, temple]: one dot label per temple, filled in order (held first).
        private readonly Label[,] _templeDots = new Label[
            Match.BaseTurnOrder.Count,
            Board.TotalTemples
        ];
        private readonly Label[] _youTags = new Label[Match.BaseTurnOrder.Count];
        private readonly VisualElement[] _favorLines = new VisualElement[Match.BaseTurnOrder.Count];
        private readonly Label[] _favorMeters = new Label[Match.BaseTurnOrder.Count];

        // One scheduled "shrink back" per row, created once, so a Favor change never allocates.
        private readonly IVisualElementScheduledItem[] _favorBumpEnds =
            new IVisualElementScheduledItem[Match.BaseTurnOrder.Count];

        // Each god's powers card, under its row; at most one is open (_openCard, or NoCard).
        private readonly VisualElement[] _powerCards = new VisualElement[Match.BaseTurnOrder.Count];
        private int _openCard = NoCard;

        private bool _powersShown;

        /// <summary>A powers card opened, so any other info card can close (one at a time).</summary>
        public event System.Action InfoOpened;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _panel = root.Q<VisualElement>("nations-panel");
            root.Q<Label>("nations-goal").text = MatchText.NationsGoal;
            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
            {
                God god = Match.BaseTurnOrder[i];
                VisualElement instance = root.Q<VisualElement>(RowName(god));
                VisualElement row = instance.Q(className: "nation-row");
                _powerCards[i] = CreatePowersCard(instance.Q(className: "nation-powers"), god);
                row.RegisterCallback<ClickEvent, int>(HandleRowClicked, i);
                _powerCards[i].RegisterCallback<ClickEvent, int>(HandleRowClicked, i);
                GodUss.Apply(row.Q(className: "god-chip"), god);
                row.Q<Label>(className: "nation-row__name").text = MatchText.GodName(god);
                Label youTag = row.Q<Label>(className: "nation-row__you");

                _rows[i] = row;
                _instances[i] = instance;
                _places[i] = row.Q<Label>(className: "nation-row__place");
                // Empty until the draw has decided; the template's text is only a placeholder.
                _places[i].text = string.Empty;
                _places[i].style.display = DisplayStyle.None;
                _stats[i] = row.Q<Label>(className: "nation-row__stats");
                _temples[i] = row.Q<VisualElement>(className: "nation-row__temples");
                _youTags[i] = youTag;
                CreateTempleDots(i);

                Label meter = row.Q<Label>(className: "nation-row__favor");
                GodUss.Apply(meter, god);
                _favorLines[i] = row.Q<VisualElement>(className: "nation-row__favor-line");
                _favorMeters[i] = meter;
                _favorBumpEnds[i] = meter
                    .schedule.Execute(() => meter.RemoveFromClassList(FavorBumpClass))
                    .StartingIn(0);
                _favorBumpEnds[i].Pause();
            }
            ShowPowers(false);
            // Hidden until the seats are chosen: before that there is no "YOU" to show.
            _panel.style.display = DisplayStyle.None;
        }

        private void OnDisable()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].UnregisterCallback<ClickEvent, int>(HandleRowClicked);
                _powerCards[i].UnregisterCallback<ClickEvent, int>(HandleRowClicked);
            }
        }

        /// <summary>
        /// Sorts the rows into the drawn turn order, 1st at the top, each with its place tag. The
        /// indexes of every other method stay the base cycle's; only the rows move.
        /// </summary>
        public void SetTurnOrder(System.Collections.Generic.IReadOnlyList<God> order)
        {
            for (int place = 0; place < order.Count; place++)
            {
                int i = Match.BaseTurnIndex(order[place]);
                // Adding an existing child moves it to the end, so the four rows end up in order.
                _panel.Add(_instances[i]);
                _places[i].text = MatchText.PlaceBadge(place);
                _places[i].style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>Closes the open powers card, if any (Esc, or another info card opened).</summary>
        public void CloseInfo() => SetOpenCard(NoCard);

        /// <summary>
        /// Tags the nations people play ("YOU", or "P1".."P4" in hot-seat) and shows the panel.
        /// </summary>
        /// <param name="tags">Per god in Match.BaseTurnOrder; null for a bot (no tag).</param>
        public void SetPlayerTags(string[] tags)
        {
            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
            {
                bool tagged = tags[i] != null;
                _youTags[i].text = tags[i] ?? string.Empty;
                _youTags[i].style.display = tagged ? DisplayStyle.Flex : DisplayStyle.None;
            }
            _panel.style.display = DisplayStyle.Flex;
        }

        /// <summary>Shows the Favor meters (Pantheon powers) or hides them (Classic).</summary>
        public void ShowPowers(bool shown)
        {
            _powersShown = shown;
            foreach (VisualElement line in _favorLines)
                line.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            // In Classic the rows are plain read-outs: no "i", no click, no card.
            foreach (VisualElement row in _rows)
            {
                row.EnableInClassList(InfoToggleClass, shown);
                row.pickingMode = shown ? PickingMode.Position : PickingMode.Ignore;
            }
            CloseInfo();
        }

        // A click on a row opens its card (closing any other); a click on the open row or card closes it.
        private void HandleRowClicked(ClickEvent evt, int row)
        {
            if (!_powersShown)
                return;
            SetOpenCard(_openCard == row ? NoCard : row);
        }

        private void SetOpenCard(int open)
        {
            bool opened = open != NoCard && open != _openCard;
            _openCard = open;
            for (int i = 0; i < _powerCards.Length; i++)
            {
                _powerCards[i].style.display = i == open ? DisplayStyle.Flex : DisplayStyle.None;
                _rows[i].EnableInClassList(InfoOpenClass, i == open);
            }
            if (opened)
                InfoOpened?.Invoke();
        }

        // The card's text never changes during a match, so it is written once per enable.
        private static VisualElement CreatePowersCard(VisualElement card, God god)
        {
            GodUss.Apply(card, god);
            card.Q<Label>(className: "nation-powers__day-heading").text =
                MatchText.PowersDayHeading(god);
            card.Q<Label>(className: "nation-powers__day").text = MatchText.PowersDay(god);
            card.Q<Label>(className: "nation-powers__wrath-heading").text =
                MatchText.PowersWrathHeading;
            card.Q<Label>(className: "nation-powers__wrath").text = MatchText.PowersWrath(god);
            card.Q<Label>(className: "nation-powers__favor").text = MatchText.PowersFavor(god);
            card.Q<Label>(className: "nation-powers__road").text = MatchText.PowersRoad(god);
            return card;
        }

        /// <summary>Redraws one god's meter and bumps it, so the change is seen where it happened.</summary>
        public void ShowFavorChanged(God god, int favor)
        {
            int row = Match.BaseTurnIndex(god);
            SetFavor(row, favor);
            _favorMeters[row].AddToClassList(FavorBumpClass);
            _favorBumpEnds[row].ExecuteLater(SecondsToMs(_favorBumpSeconds));
        }

        /// <summary>
        /// Redraws one god's meter without a bump (match start). Favor only changes on Match.FavorChanged,
        /// so <see cref="Refresh"/> never rewrites it: a meter can then wait for the star flying to it.
        /// </summary>
        public void SetFavor(God god, int favor) => SetFavor(Match.BaseTurnIndex(god), favor);

        /// <summary>A god's Favor stars, where a star from its sacred die lands.</summary>
        public VisualElement FavorMeter(God god) => _favorMeters[Match.BaseTurnIndex(god)];

        public void Refresh(Match match)
        {
            Board board = match.Board;
            bool started = match.Phase != TurnPhase.NotStarted;
            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
            {
                God god = Match.BaseTurnOrder[i];
                bool eliminated = match.IsEliminated(god);
                _rows[i].EnableInClassList(EliminatedClass, eliminated);
                _rows[i]
                    .EnableInClassList(
                        TurnClass,
                        started && !eliminated && match.CurrentGod == god
                    );
                _temples[i].style.display = eliminated ? DisplayStyle.None : DisplayStyle.Flex;

                if (eliminated)
                {
                    _stats[i].text = MatchText.EliminatedTag;
                    continue;
                }
                _stats[i].text = MatchText.NationStats(
                    board.TerritoryCount(god),
                    board.TotalTroops(god)
                );
                int held = board.TempleCount(god);
                for (int t = 0; t < Board.TotalTemples; t++)
                    SetTempleDot(_templeDots[i, t], t, t < held);
            }
        }

        // The goal dot shows its number while empty ("3" in the ring), a plain dot once held.
        private static void SetTempleDot(Label dot, int index, bool held)
        {
            bool goal = index == Match.TemplesToWin - 1;
            dot.text = goal ? MatchText.GoalTempleDot(held) : MatchText.TempleDot(held);
            dot.EnableInClassList(GoalNumberClass, goal && !held);
        }

        private void SetFavor(int row, int favor)
        {
            _favorMeters[row].text = MatchText.FavorMeter(favor);
            _favorMeters[row].EnableInClassList(FavorFullClass, favor >= GodPowers.MaxFavor);
        }

        private static long SecondsToMs(float seconds) => (long)(seconds * 1000f);

        // Built once per enable (the UIDocument tree is rebuilt then), never on Refresh.
        private void CreateTempleDots(int row)
        {
            _temples[row].Clear();
            for (int t = 0; t < Board.TotalTemples; t++)
            {
                var dot = new Label { pickingMode = PickingMode.Ignore };
                dot.AddToClassList(TempleDotClass);
                // The TemplesToWin-th dot: filling it wins the match.
                dot.EnableInClassList(GoalDotClass, t == Match.TemplesToWin - 1);
                SetTempleDot(dot, t, false);
                _temples[row].Add(dot);
                _templeDots[row, t] = dot;
            }
        }

        private static string RowName(God god) => $"nation-{god.ToString().ToLowerInvariant()}";
    }
}

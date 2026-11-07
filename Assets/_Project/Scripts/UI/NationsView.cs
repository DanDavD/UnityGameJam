using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// The left column: one row per god in base cycle order with its territories, troops and temples
    /// (●●○○, the dot that wins ringed as the goal), a YOU tag on the human, the god whose turn it is highlighted and eliminated gods greyed out.
    /// A pure view: <see cref="Refresh"/> redraws every row from the match state it is given.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class NationsView : MonoBehaviour
    {
        private const string TurnClass = "nation-row--turn";
        private const string EliminatedClass = "nation-row--eliminated";
        private const string TempleDotClass = "nation-row__temple-dot";
        private const string GoalDotClass = "nation-row__temple-dot--goal";

        private UIDocument _document;
        private VisualElement _panel;

        // Indexed like Match.BaseTurnOrder, which is also the row order.
        private readonly VisualElement[] _rows = new VisualElement[Match.BaseTurnOrder.Count];
        private readonly Label[] _stats = new Label[Match.BaseTurnOrder.Count];
        private readonly VisualElement[] _temples = new VisualElement[Match.BaseTurnOrder.Count];

        // [row, temple]: one dot label per temple, filled in order (held first).
        private readonly Label[,] _templeDots = new Label[
            Match.BaseTurnOrder.Count,
            Board.TotalTemples
        ];
        private readonly VisualElement[] _youTags = new VisualElement[Match.BaseTurnOrder.Count];

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _panel = root.Q<VisualElement>("nations-panel");
            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
            {
                God god = Match.BaseTurnOrder[i];
                VisualElement row = root.Q<VisualElement>(RowName(god)).Q(className: "nation-row");
                GodUss.Apply(row.Q(className: "god-chip"), god);
                row.Q<Label>(className: "nation-row__name").text = MatchText.GodName(god);
                Label youTag = row.Q<Label>(className: "nation-row__you");
                youTag.text = MatchText.YouTag;

                _rows[i] = row;
                _stats[i] = row.Q<Label>(className: "nation-row__stats");
                _temples[i] = row.Q<VisualElement>(className: "nation-row__temples");
                _youTags[i] = youTag;
                CreateTempleDots(i);
            }
            // Hidden until a god is chosen: before that there is no "YOU" to show.
            _panel.style.display = DisplayStyle.None;
        }

        public void SetHuman(God human)
        {
            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
            {
                bool isHuman = Match.BaseTurnOrder[i] == human;
                _youTags[i].style.display = isHuman ? DisplayStyle.Flex : DisplayStyle.None;
            }
            _panel.style.display = DisplayStyle.Flex;
        }

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
                    _templeDots[i, t].text = MatchText.TempleDot(t < held);
            }
        }

        // Built once per enable (the UIDocument tree is rebuilt then), never on Refresh.
        private void CreateTempleDots(int row)
        {
            _temples[row].Clear();
            for (int t = 0; t < Board.TotalTemples; t++)
            {
                var dot = new Label(MatchText.TempleDot(false))
                {
                    pickingMode = PickingMode.Ignore,
                };
                dot.AddToClassList(TempleDotClass);
                // The TemplesToWin-th dot: filling it wins the match.
                dot.EnableInClassList(GoalDotClass, t == Match.TemplesToWin - 1);
                _temples[row].Add(dot);
                _templeDots[row, t] = dot;
            }
        }

        private static string RowName(God god) => $"nation-{god.ToString().ToLowerInvariant()}";
    }
}

using System;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// The main menu over the board: title, tagline and one card per god with its sacred number,
    /// cursed number and rival. Clicking a card reports the chosen god; the view never starts anything.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuView : MonoBehaviour
    {
        private UIDocument _document;
        private VisualElement _menu;
        private readonly VisualElement[] _cards = new VisualElement[Match.BaseTurnOrder.Count];

        public event Action<God> GodChosen;

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
            root.Q<Label>("menu-prompt").text = MatchText.ChooseGodPrompt;

            for (int i = 0; i < Match.BaseTurnOrder.Count; i++)
            {
                God god = Match.BaseTurnOrder[i];
                VisualElement card = root.Q<VisualElement>(CardName(god)).Q(className: "god-card");
                Fill(card, god);
                // The god travels with the card, so one handler serves all four.
                card.userData = god;
                card.RegisterCallback<ClickEvent>(HandleCardClicked);
                _cards[i] = card;
            }
            Hide();
        }

        private void OnDisable()
        {
            foreach (VisualElement card in _cards)
                card?.UnregisterCallback<ClickEvent>(HandleCardClicked);
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
        }

        private void HandleCardClicked(ClickEvent evt)
        {
            var card = (VisualElement)evt.currentTarget;
            GodChosen?.Invoke((God)card.userData);
        }
    }
}

using System;
using System.Collections.Generic;
using GameJam.Core;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Draws a <see cref="Board"/>: one <see cref="TerritoryView"/> per territory placed by the
    /// <see cref="BoardLayout"/>, plus a line for every adjacency so legal moves are visible.
    /// Also turns a screen click into the territory under it.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField]
        private BoardLayout _layout;

        [SerializeField]
        private GodVisuals _visuals;

        [SerializeField]
        private TerritoryView _territoryPrefab;

        [SerializeField]
        private Camera _camera;

        [Header("Connection lines")]
        [SerializeField]
        private Material _lineMaterial;

        [Tooltip("--ink-soft")]
        [SerializeField]
        private Color _lineColor = Color.gray;

        [SerializeField, Min(0.01f)]
        private float _lineWidth = 0.08f;

        [Tooltip("Lines are drawn under the territories.")]
        [SerializeField]
        private int _lineSortingOrder = -5;

        private readonly List<TerritoryView> _views = new List<TerritoryView>();

        /// <summary>Spawns the views once. Territory ids equal list indices, so views are indexed the same way.</summary>
        public void Build(Board board)
        {
            // A half-built board would fail later with confusing index errors, so stop here.
            if (_layout.Count < board.Territories.Count)
                throw new InvalidOperationException(
                    $"{_layout.name} has {_layout.Count} positions but the board has {board.Territories.Count} territories."
                );

            foreach (Territory territory in board.Territories)
            {
                TerritoryView view = Instantiate(_territoryPrefab, transform);
                view.transform.localPosition = _layout.GetPosition(territory.Id);
                view.Bind(territory, _visuals, _layout.GetSize(territory.Kind));
                _views.Add(view);

                // Each adjacency is stored on both territories; draw it once, from the lower id.
                foreach (Territory neighbor in territory.Neighbors)
                {
                    if (neighbor.Id > territory.Id)
                        CreateLine(territory, neighbor);
                }
            }
        }

        public void Refresh(Territory territory) => _views[territory.Id].Refresh();

        public void ShowTroopLoss(Territory territory) => _views[territory.Id].ShowTroopLoss();

        public void ShowCaptureRing(Territory territory) => _views[territory.Id].ShowCaptureRing();

        public void SetHighlight(Territory territory, TerritoryHighlight highlight) =>
            _views[territory.Id].SetHighlight(highlight);

        public void ClearHighlights()
        {
            foreach (TerritoryView view in _views)
                view.SetHighlight(TerritoryHighlight.None);
        }

        public bool TryPick(Vector2 screenPosition, out Territory territory)
        {
            territory = null;
            Vector2 world = _camera.ScreenToWorldPoint(screenPosition);
            Collider2D hit = Physics2D.OverlapPoint(world);
            if (hit == null)
                return false;

            foreach (TerritoryView view in _views)
            {
                if (view.Collider != hit)
                    continue;
                territory = view.Territory;
                return true;
            }
            return false;
        }

        private void CreateLine(Territory a, Territory b)
        {
            var line = new GameObject($"Link {a.Name} - {b.Name}").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, _layout.GetPosition(a.Id));
            line.SetPosition(1, _layout.GetPosition(b.Id));
            line.startWidth = _lineWidth;
            line.endWidth = _lineWidth;
            line.startColor = _lineColor;
            line.endColor = _lineColor;
            line.sharedMaterial = _lineMaterial;
            line.sortingOrder = _lineSortingOrder;
        }
    }
}

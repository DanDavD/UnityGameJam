using GameJam.Core;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Where each territory sits on the altar-shaped board and how big each kind is drawn.
    /// Positions are indexed by <see cref="Board"/> territory id so they can be moved in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "BoardLayout", menuName = "GodBoard/Board Layout")]
    public sealed class BoardLayout : ScriptableObject
    {
        [Tooltip("Local position of each territory, indexed by Board territory id (0-12).")]
        [SerializeField]
        private Vector2[] _positions = new Vector2[0];

        [Header("Size per kind (world units)")]
        [SerializeField, Min(0.1f)]
        private float _capitalSize = 1.7f;

        [SerializeField, Min(0.1f)]
        private float _villageSize = 1.3f;

        [SerializeField, Min(0.1f)]
        private float _templeSize = 1.6f;

        [SerializeField, Min(0.1f)]
        private float _altarSize = 2.1f;

        public int Count => _positions.Length;

        public Vector2 GetPosition(int territoryId) => _positions[territoryId];

        public float GetSize(TerritoryKind kind) =>
            kind switch
            {
                TerritoryKind.Capital => _capitalSize,
                TerritoryKind.Village => _villageSize,
                TerritoryKind.Temple => _templeSize,
                _ => _altarSize,
            };
    }
}

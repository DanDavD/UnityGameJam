using System;
using GameJam.Core;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>How one god's nation is drawn on the board. Values mirror Docs/design-system.md.</summary>
    [Serializable]
    public struct GodStyle
    {
        [SerializeField]
        private God _god;

        [Tooltip("Nation color: territory borders.")]
        [SerializeField]
        private Color _color;

        [Tooltip("Nation tint: territory fills.")]
        [SerializeField]
        private Color _tint;

        [SerializeField]
        private Sprite _icon;

        public God God => _god;
        public Color Color => _color;
        public Color Tint => _tint;
        public Sprite Icon => _icon;
    }
}

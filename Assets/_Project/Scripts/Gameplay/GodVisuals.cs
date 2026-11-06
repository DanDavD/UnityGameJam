using System;
using GameJam.Core;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>Colors and icons of the four nations, plus how unowned territories look.</summary>
    [CreateAssetMenu(fileName = "GodVisuals", menuName = "GodBoard/God Visuals")]
    public sealed class GodVisuals : ScriptableObject
    {
        [SerializeField]
        private GodStyle[] _gods = new GodStyle[0];

        [Header("Unowned territories")]
        [Tooltip("Fill of territories nobody owns (--paper).")]
        [SerializeField]
        private Color _unownedFill = Color.white;

        [Tooltip("Border of territories nobody owns (--ink).")]
        [SerializeField]
        private Color _unownedBorder = Color.black;

        public Color UnownedFill => _unownedFill;
        public Color UnownedBorder => _unownedBorder;

        public GodStyle Get(God god)
        {
            foreach (GodStyle style in _gods)
            {
                if (style.God == god)
                    return style;
            }
            throw new ArgumentException($"{name} has no style for {god}.", nameof(god));
        }
    }
}

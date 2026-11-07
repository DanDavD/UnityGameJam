using System.Collections;
using GameJam.Core;
using TMPro;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// One territory on the board, drawn like a painted wooden token: offset shadow, ink outline,
    /// owner border and tint, owner icon (never color alone), troop count and highlight ring.
    /// Reads its <see cref="Territory"/>; never changes it.
    /// </summary>
    public sealed class TerritoryView : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField]
        private SpriteRenderer _shadow;

        [Tooltip(
            "--ink outline around the owner border, so every nation color stands out from the board."
        )]
        [SerializeField]
        private SpriteRenderer _outline;

        [SerializeField]
        private SpriteRenderer _border;

        [SerializeField]
        private SpriteRenderer _fill;

        [Tooltip("Parent of the icon and troop count.")]
        [SerializeField]
        private Transform _content;

        [SerializeField]
        private SpriteRenderer _icon;

        [SerializeField]
        private SpriteRenderer _highlight;

        [SerializeField]
        private TextMeshPro _troops;

        [SerializeField]
        private Collider2D _collider;

        [Header("Shape per kind")]
        [SerializeField]
        private Sprite _capitalShape;

        [SerializeField]
        private Sprite _villageShape;

        [SerializeField]
        private Sprite _templeShape;

        [SerializeField]
        private Sprite _altarShape;

        [Tooltip("Shown on empty temples, so the player spots the goal before anyone owns one.")]
        [SerializeField]
        private Sprite _emptyTempleIcon;

        [Tooltip(
            "World size of the icon and troop count, the same on every shape so counts read alike."
        )]
        [SerializeField, Min(0.1f)]
        private float _contentSize = 1.2f;

        // Selected and target differ in ring shape, thickness, motion and lift, not only color,
        // so they stay distinct in grayscale.
        [Header("Highlight")]
        [Tooltip("Thick solid ring around the territory the attack comes from.")]
        [SerializeField]
        private Sprite _selectedRing;

        [Tooltip("Thin dashed ring around legal targets.")]
        [SerializeField]
        private Sprite _targetRing;

        [Tooltip("--ink")]
        [SerializeField]
        private Color _selectedColor = Color.black;

        [Tooltip("--wood")]
        [SerializeField]
        private Color _targetColor = Color.yellow;

        [Tooltip("The selected territory is drawn slightly bigger, as if lifted off the board.")]
        [SerializeField, Min(1f)]
        private float _selectedScale = 1.1f;

        [Tooltip("Shadow offset at rest, in the piece's local units.")]
        [SerializeField]
        private Vector2 _shadowOffset = new Vector2(0.03f, -0.06f);

        [Tooltip("Shadow offset of the selected (lifted) piece.")]
        [SerializeField]
        private Vector2 _liftedShadowOffset = new Vector2(0.06f, -0.12f);

        [Header("Target pulse")]
        [Tooltip("Seconds per gentle breath of the target ring.")]
        [SerializeField, Min(0.1f)]
        private float _pulsePeriod = 1.4f;

        [SerializeField, Range(0f, 1f)]
        private float _pulseMinAlpha = 0.5f;

        [Tooltip("Extra ring scale at the top of the breath.")]
        [SerializeField, Min(0f)]
        private float _pulseGrow = 0.05f;

        [Header("Hop when troops or owner change")]
        [SerializeField, Min(0.01f)]
        private float _hopDuration = 0.2f;

        [SerializeField, Min(1f)]
        private float _hopScale = 1.15f;

        [Header("Troop loss (-1) floating text")]
        [SerializeField, Min(0.01f)]
        private float _lossDuration = 1f;

        [Tooltip("How far the -1 rises, in the content's local units.")]
        [SerializeField, Min(0f)]
        private float _lossRise = 0.6f;

        [Tooltip(
            "Where the -1 starts above the troop count, in the content's local units, so it never covers the number."
        )]
        [SerializeField, Min(0f)]
        private float _lossStartOffset = 0.5f;

        private GodVisuals _visuals;
        private TextMeshPro _lossText;
        private Coroutine _lossFloat;
        private float _size;
        private float _restScale;
        private int _shownTroops;
        private God? _shownOwner;
        private Coroutine _hop;
        private Vector3 _ringRestScale;
        private bool _pulsing;

        public Territory Territory { get; private set; }
        public Collider2D Collider => _collider;

        private void Awake()
        {
            _ringRestScale = _highlight.transform.localScale;
        }

        // Only legal targets pulse; every target uses the same clock so they breathe together.
        private void Update()
        {
            if (!_pulsing)
                return;

            float breath = 0.5f + 0.5f * Mathf.Cos(Time.time * (2f * Mathf.PI / _pulsePeriod));
            Color color = _targetColor;
            color.a = Mathf.Lerp(_pulseMinAlpha, 1f, breath);
            _highlight.color = color;
            _highlight.transform.localScale = _ringRestScale * (1f + _pulseGrow * breath);
        }

        public void Bind(Territory territory, GodVisuals visuals, float size)
        {
            Territory = territory;
            _visuals = visuals;
            _size = size;
            name = territory.Name;

            Sprite shape = ShapeFor(territory.Kind);
            _shadow.sprite = shape;
            _outline.sprite = shape;
            _border.sprite = shape;
            _fill.sprite = shape;
            _content.localScale = Vector3.one * (_contentSize / size);

            SetHighlight(TerritoryHighlight.None);
            Draw();
        }

        /// <summary>Redraws from the territory and hops if its troops or owner changed.</summary>
        public void Refresh()
        {
            bool changed = Territory.Troops != _shownTroops || Territory.Owner != _shownOwner;
            Draw();
            if (!changed || !isActiveAndEnabled)
                return;

            if (_hop != null)
                StopCoroutine(_hop);
            _hop = StartCoroutine(Hop());
        }

        /// <summary>Floats a "-1" above the troop count, so a combat shows which side paid for it.</summary>
        public void ShowTroopLoss()
        {
            if (!isActiveAndEnabled)
                return;

            // Placeholder: a copy of the troop count, created on first use. Final look comes with the art pass.
            if (_lossText == null)
            {
                _lossText = Instantiate(_troops, _content);
                _lossText.name = "Troop Loss";
                _lossText.SetText("-1");
            }

            if (_lossFloat != null)
                StopCoroutine(_lossFloat);
            _lossFloat = StartCoroutine(FloatLoss());
        }

        public void SetHighlight(TerritoryHighlight highlight)
        {
            bool selected = highlight == TerritoryHighlight.Selected;
            _highlight.enabled = highlight != TerritoryHighlight.None;
            _highlight.sprite = selected ? _selectedRing : _targetRing;
            _highlight.color = selected ? _selectedColor : _targetColor;
            _highlight.transform.localScale = _ringRestScale;
            _pulsing = highlight == TerritoryHighlight.Target;

            // Lifted: a bit bigger with a longer shadow, like a token picked up off the table.
            _shadow.transform.localPosition = selected ? _liftedShadowOffset : _shadowOffset;
            _restScale = _size * (selected ? _selectedScale : 1f);
            if (_hop == null)
                transform.localScale = Vector3.one * _restScale;
        }

        private void Draw()
        {
            God? owner = Territory.Owner;
            _shownOwner = owner;
            _shownTroops = Territory.Troops;

            if (owner.HasValue)
            {
                GodStyle style = _visuals.Get(owner.Value);
                _fill.color = style.Tint;
                _border.color = style.Color;
                _icon.sprite = style.Icon;
            }
            else
            {
                _fill.color = _visuals.UnownedFill;
                _border.color = _visuals.UnownedBorder;
                _icon.sprite = Territory.Kind == TerritoryKind.Temple ? _emptyTempleIcon : null;
            }

            // Unowned territories always have 0 troops and show no count: no number reads as "free to take".
            _troops.enabled = _shownTroops > 0;
            _troops.SetText("{0}", _shownTroops);
        }

        private IEnumerator Hop()
        {
            float elapsed = 0f;
            while (elapsed < _hopDuration)
            {
                elapsed += Time.deltaTime;
                float bump = Mathf.Sin(Mathf.Clamp01(elapsed / _hopDuration) * Mathf.PI);
                transform.localScale = Vector3.one * (_restScale * (1f + (_hopScale - 1f) * bump));
                yield return null;
            }
            transform.localScale = Vector3.one * _restScale;
            _hop = null;
        }

        private IEnumerator FloatLoss()
        {
            Vector3 start = _troops.transform.localPosition + Vector3.up * _lossStartOffset;
            Color color = _troops.color;
            _lossText.enabled = true;
            float elapsed = 0f;
            while (elapsed < _lossDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _lossDuration);
                _lossText.transform.localPosition = start + Vector3.up * (_lossRise * t);
                color.a = 1f - t;
                _lossText.color = color;
                yield return null;
            }
            _lossText.enabled = false;
            _lossFloat = null;
        }

        private Sprite ShapeFor(TerritoryKind kind) =>
            kind switch
            {
                TerritoryKind.Capital => _capitalShape,
                TerritoryKind.Village => _villageShape,
                TerritoryKind.Temple => _templeShape,
                _ => _altarShape,
            };
    }
}

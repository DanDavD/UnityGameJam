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
        // The middle row of the board counts as the top half, so its tags hang below like the top row's
        // and never meet the bottom row's tags above.
        private const float TagSideTolerance = 0.5f;

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

        [Tooltip(
            "The Road rings breathe less than target rings: they are the call to act, so they stay bright."
        )]
        [SerializeField, Range(0f, 1f)]
        private float _roadPulseMinAlpha = 0.8f;

        [Tooltip(
            "--paper: the pale edge around a Road ring. It separates the ring from the board and, for the dark god, from the plain ink selection ring."
        )]
        [SerializeField]
        private Color _roadHaloColor = new Color(0.984f, 0.965f, 0.925f);

        [Tooltip("Size of the Road ring's pale edge relative to the ring.")]
        [SerializeField, Range(1f, 1.3f)]
        private float _roadHaloScale = 1.1f;

        [Tooltip("Size of the ROAD tag relative to the troop count.")]
        [SerializeField, Range(0.2f, 1f)]
        private float _roadTagScale = 0.75f;

        [Tooltip("The word on a territory that can march, so the Road ring is never the only cue.")]
        [SerializeField]
        private string _roadTagText = "ROAD";

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

        [Header("Hover (only on pieces the player can use right now)")]
        [Tooltip(
            "Scale on top of the rest scale while hovered, like a token nudged toward the hand."
        )]
        [SerializeField, Min(1f)]
        private float _hoverScale = 1.08f;

        [Tooltip("Seconds to grow or settle back: --duration-fast.")]
        [SerializeField, Min(0.01f)]
        private float _hoverDuration = 0.12f;

        [Tooltip(
            "--wood: the ink outline warms to wood while hovered, a soft glow that says 'clickable'."
        )]
        [SerializeField]
        private Color _hoverOutlineColor = new Color(0.545f, 0.369f, 0.235f);

        [Header("Hop when troops are gained or the owner changes")]
        [SerializeField, Min(0.01f)]
        private float _hopDuration = 0.2f;

        [SerializeField, Min(1f)]
        private float _hopScale = 1.15f;

        [Header("Capture ring")]
        [Tooltip("Seconds the ring in the new owner's color takes to expand and fade.")]
        [SerializeField, Min(0.01f)]
        private float _captureRingDuration = 0.35f;

        [Tooltip("Extra ring scale at the end of the expansion.")]
        [SerializeField, Min(0f)]
        private float _captureRingGrow = 0.45f;

        [Tooltip("Drawn above the piece and its troop count.")]
        [SerializeField]
        private int _effectSortingOrder = 6;

        [Header("Troop change (-2, +1) floating text and Wrath preview")]
        [SerializeField, Min(0.01f)]
        private float _lossDuration = 1f;

        [Tooltip("Share of the duration the -1 stays fully opaque before it fades.")]
        // Below 1, so the fade that follows always has time to run.
        [SerializeField, Range(0f, 0.95f)]
        private float _lossHold = 0.4f;

        [Tooltip(
            "Size of the -1 relative to the troop count, so the number itself stays the focus."
        )]
        [SerializeField, Range(0.3f, 1f)]
        private float _lossScale = 0.85f;

        [Tooltip("--ink, like the count: a loss is never shown in a nation color.")]
        [SerializeField]
        private Color _lossColor = Color.gray;

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
        private TextMeshPro _previewText;
        private Coroutine _lossFloat;

        // When the current "-1" / "+1" starts to fade; a capture ring waits for it.
        private float _lossHoldEnd;
        private float _size;
        private float _restScale;
        private int _shownTroops;
        private God? _shownOwner;
        private Coroutine _hop;
        private SpriteRenderer _captureRing;
        private Coroutine _captureRingRoutine;
        private Vector3 _ringRestScale;
        private bool _pulsing;

        private Color _outlineBaseColor;

        // The ring color the current highlight breathes in (wood for targets, the acting god's own color for the Road).
        private Color _pulseColor;
        private float _pulseAlphaFloor;
        private TextMeshPro _roadTag;
        private SpriteRenderer _roadHalo;

        // Whether the pointer is on this piece and it is usable; _hover eases 0 to 1 toward it.
        private bool _hovered;
        private float _hover;

        public Territory Territory { get; private set; }
        public Collider2D Collider => _collider;

        // Ease-out (cubic), so the piece reacts at once and settles softly.
        private float HoverFactor
        {
            get
            {
                float eased = 1f - (1f - _hover) * (1f - _hover) * (1f - _hover);
                return 1f + (_hoverScale - 1f) * eased;
            }
        }

        private void Awake()
        {
            _ringRestScale = _highlight.transform.localScale;
            _outlineBaseColor = _outline.color;
        }

        private void Update()
        {
            if (_pulsing)
                Pulse();

            float target = _hovered ? 1f : 0f;
            if (_hover == target)
                return;
            _hover = Mathf.MoveTowards(_hover, target, Time.deltaTime / _hoverDuration);
            _outline.color = Color.Lerp(_outlineBaseColor, _hoverOutlineColor, _hover);
            if (_hop == null)
                transform.localScale = Vector3.one * (_restScale * HoverFactor);
        }

        /// <summary>
        /// Grows the piece slightly and warms its outline while the pointer is on it. The caller
        /// only sets it on pieces a click would use right now, so unusable pieces never react.
        /// </summary>
        public void SetHovered(bool hovered) => _hovered = hovered;

        // Only legal targets and Road rings pulse; they all use the same clock so they breathe together.
        private void Pulse()
        {
            float breath = 0.5f + 0.5f * Mathf.Cos(Time.time * (2f * Mathf.PI / _pulsePeriod));
            Color color = _pulseColor;
            color.a = Mathf.Lerp(_pulseAlphaFloor, 1f, breath);
            _highlight.color = color;
            _highlight.transform.localScale = _ringRestScale * (1f + _pulseGrow * breath);
            FollowRing(_roadHalo, _roadHaloScale);
        }

        // A copy of the ring (the pale edge) swells and shrinks with it.
        private void FollowRing(SpriteRenderer copy, float scale)
        {
            if (copy != null)
                copy.transform.localScale = _highlight.transform.localScale * scale;
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

        /// <summary>
        /// Redraws from the territory. It hops when it gains troops or changes owner; a loss shows
        /// the "-1" instead, so only one thing moves on a piece at a time.
        /// </summary>
        public void Refresh()
        {
            bool gained = Territory.Troops > _shownTroops || Territory.Owner != _shownOwner;
            Draw();
            if (!gained || !isActiveAndEnabled)
                return;

            if (_hop != null)
                StopCoroutine(_hop);
            _hop = StartCoroutine(Hop());
        }

        /// <summary>
        /// Floats "-2" or "+1" above the troop count, so every change shows who paid or gained and
        /// how much. Gains use the same ink as losses: the sign carries the meaning, never a color.
        /// </summary>
        public void ShowTroopChange(int delta)
        {
            if (delta == 0 || !isActiveAndEnabled)
                return;

            // A copy of the troop count (same font and layout), created on first use: most
            // territories never change by more than a placed troop, so the prefab does not carry one each.
            if (_lossText == null)
                _lossText = CopyTroopText("Troop Change");
            // TMP formats the number without allocating a string.
            _lossText.SetText(delta < 0 ? "-{0}" : "+{0}", Mathf.Abs(delta));

            if (_lossFloat != null)
                StopCoroutine(_lossFloat);
            _lossHoldEnd = Time.time + _lossDuration * _lossHold;
            _lossFloat = StartCoroutine(FloatLoss());
        }

        /// <summary>
        /// Redraws and floats the difference from what the piece showed, for changes that were held
        /// back while the Destiny Die rolled (bad omen, Storm, Sun at noon).
        /// </summary>
        public void RefreshShowingChange()
        {
            int delta = Territory.Troops - _shownTroops;
            Refresh();
            ShowTroopChange(delta);
        }

        /// <summary>
        /// A still "-3" above the troop count while the human aims a Wrath, so the cost of each target
        /// is read before the click. 0 hides it.
        /// </summary>
        public void SetPreview(int delta)
        {
            if (delta == 0)
            {
                if (_previewText != null)
                    _previewText.enabled = false;
                return;
            }

            if (_previewText == null)
            {
                _previewText = CopyTroopText("Wrath Preview");
                _previewText.color = _lossColor;
                _previewText.transform.localPosition =
                    _troops.transform.localPosition + Vector3.up * _lossStartOffset;
            }
            _previewText.SetText(delta < 0 ? "-{0}" : "+{0}", Mathf.Abs(delta));
            _previewText.enabled = true;
        }

        private TextMeshPro CopyTroopText(string copyName)
        {
            TextMeshPro copy = Instantiate(_troops, _content);
            copy.name = copyName;
            copy.transform.localScale = _troops.transform.localScale * _lossScale;
            copy.sortingOrder = _effectSortingOrder;
            copy.enabled = false;
            return copy;
        }

        /// <summary>
        /// A ring in the new owner's color expands and fades: this territory just changed hands.
        /// If a "-1" is still being read on this piece, the ring waits for its hold to end, so only
        /// one strong animation plays at a time.
        /// </summary>
        public void ShowCaptureRing()
        {
            if (!isActiveAndEnabled || !Territory.Owner.HasValue)
                return;

            // A copy of the highlight ring, created on first use, so the selection ring stays untouched.
            if (_captureRing == null)
            {
                _captureRing = Instantiate(_highlight, _highlight.transform.parent);
                _captureRing.name = "Capture Ring";
                _captureRing.sprite = _selectedRing;
                _captureRing.sortingOrder = _effectSortingOrder;
            }

            if (_captureRingRoutine != null)
                StopCoroutine(_captureRingRoutine);
            _captureRingRoutine = StartCoroutine(
                ExpandRing(_visuals.Get(Territory.Owner.Value).Color)
            );
        }

        public void SetHighlight(TerritoryHighlight highlight)
        {
            bool selected = highlight == TerritoryHighlight.Selected;
            bool road =
                highlight == TerritoryHighlight.RoadOrigin
                || highlight == TerritoryHighlight.RoadDestination;
            // The Road is the loudest call to act, so its rings are the thick ones, breathing, in the
            // acting god's own color (every Road territory is the acting god's); the attack's
            // targets keep the thin dashed wood ring.
            _highlight.enabled = highlight != TerritoryHighlight.None;
            _highlight.sprite = selected || road ? _selectedRing : _targetRing;
            _pulseColor =
                road && Territory.Owner.HasValue
                    ? _visuals.Get(Territory.Owner.Value).Color
                    : _targetColor;
            _pulseAlphaFloor = road ? _roadPulseMinAlpha : _pulseMinAlpha;
            _highlight.color = selected ? _selectedColor : _pulseColor;
            _highlight.transform.localScale = _ringRestScale;
            _pulsing = highlight == TerritoryHighlight.Target || road;

            ShowRoadHalo(road);
            ShowRoadTag(highlight == TerritoryHighlight.RoadOrigin);

            // Lifted: a bit bigger with a longer shadow, like a token picked up off the table.
            _shadow.transform.localPosition = selected ? _liftedShadowOffset : _shadowOffset;
            _restScale = _size * (selected ? _selectedScale : 1f);
            if (_hop == null)
                transform.localScale = Vector3.one * (_restScale * HoverFactor);
        }

        // A pale paper edge just outside the Road ring: with the thick ring it makes a double outline,
        // so even the dark god's black ring never passes for the plain ink selection ring, and a gold
        // or sky-blue ring stays crisp on the tan board. A copy of the ring, created on first use.
        private void ShowRoadHalo(bool visible)
        {
            if (!visible)
            {
                if (_roadHalo != null)
                    _roadHalo.enabled = false;
                return;
            }

            if (_roadHalo == null)
            {
                _roadHalo = Instantiate(_highlight, _highlight.transform.parent);
                _roadHalo.name = "Road Halo";
                _roadHalo.color = _roadHaloColor;
                _roadHalo.sortingOrder = _highlight.sortingOrder - 1;
            }
            _roadHalo.sprite = _selectedRing;
            _roadHalo.enabled = true;
            FollowRing(_roadHalo, _roadHaloScale);
        }

        // A small "ROAD" word just outside the ring, in ink like the troop count: the cue that is not a color.
        // It sits on the side facing the board's middle (below the top half, above the bottom half),
        // so it never lands on the wooden frame. Created on first use, since only a few pieces march.
        private void ShowRoadTag(bool visible)
        {
            if (!visible)
            {
                if (_roadTag != null)
                    _roadTag.enabled = false;
                return;
            }

            if (_roadTag == null)
            {
                _roadTag = CopyTroopText("Road Tag");
                _roadTag.transform.localScale = _troops.transform.localScale * _roadTagScale;
                _roadTag.color = _selectedColor;
                _roadTag.SetText(_roadTagText);
            }

            Bounds ring = _highlight.bounds;
            float halfHeight = _roadTag.preferredHeight * _roadTag.transform.lossyScale.y * 0.5f;
            bool inTopHalf = transform.localPosition.y > -TagSideTolerance;
            float y = inTopHalf ? ring.min.y - halfHeight : ring.max.y + halfHeight;
            _roadTag.transform.position = new Vector3(
                ring.center.x,
                y,
                _roadTag.transform.position.z
            );
            _roadTag.enabled = true;
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
                transform.localScale =
                    Vector3.one * (_restScale * HoverFactor * (1f + (_hopScale - 1f) * bump));
                yield return null;
            }
            transform.localScale = Vector3.one * (_restScale * HoverFactor);
            _hop = null;
        }

        // Rises with an ease-out (fast, then settling) and stays opaque for the hold, so it is read
        // before it fades.
        private IEnumerator FloatLoss()
        {
            Vector3 start = _troops.transform.localPosition + Vector3.up * _lossStartOffset;
            Color color = _lossColor;
            _lossText.enabled = true;
            float elapsed = 0f;
            while (elapsed < _lossDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _lossDuration);
                float rise = 1f - (1f - t) * (1f - t) * (1f - t);
                _lossText.transform.localPosition = start + Vector3.up * (_lossRise * rise);
                color.a = 1f - Mathf.Clamp01((t - _lossHold) / (1f - _lossHold));
                _lossText.color = color;
                yield return null;
            }
            _lossText.enabled = false;
            _lossFloat = null;
        }

        private IEnumerator ExpandRing(Color color)
        {
            Vector3 restScale = _ringRestScale;
            // Hidden while waiting, in case a previous ring was cut off mid-expansion.
            _captureRing.enabled = false;
            while (Time.time < _lossHoldEnd)
                yield return null;
            _captureRing.enabled = true;
            float elapsed = 0f;
            while (elapsed < _captureRingDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _captureRingDuration);
                _captureRing.transform.localScale = restScale * (1f + _captureRingGrow * t);
                color.a = 1f - t;
                _captureRing.color = color;
                yield return null;
            }
            _captureRing.enabled = false;
            _captureRingRoutine = null;
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

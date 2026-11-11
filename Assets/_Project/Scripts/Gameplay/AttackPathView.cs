using System.Collections;
using System.Collections.Generic;
using GameJam.Core;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// The march line of an attack: a row of dots that grows from the attacking territory toward its
    /// target, led by an arrowhead, in the attacker's nation color with an outline (ink, or paper for
    /// the dark god). It stays lit softly while the dice roll and fades when the combat resolves, so the
    /// eye follows cause to effect: this army, that land. Dots and arrowhead are pooled sprites placed
    /// once per attack; nothing allocates while it animates.
    /// </summary>
    public sealed class AttackPathView : MonoBehaviour
    {
        [SerializeField]
        private BoardView _board;

        [Tooltip("Round dot (circle).")]
        [SerializeField]
        private Sprite _dot;

        [Tooltip("Diamond, drawn long and thin as the arrowhead.")]
        [SerializeField]
        private Sprite _head;

        [Tooltip("Same as the pieces (Sprite-Lit-Default), so the path sits in the same light.")]
        [SerializeField]
        private Material _spriteMaterial;

        [Tooltip("--ink: the outline of the path.")]
        [SerializeField]
        private Color _inkColor = Color.black;

        [Tooltip(
            "--paper: the outline when the nation color is dark (Morrígan), so it stays visible."
        )]
        [SerializeField]
        private Color _paperColor = Color.white;

        [Tooltip("Above the pieces and the capture ring (6), below the god effects (8).")]
        [SerializeField]
        private int _sortingOrder = 7;

        [Header("Shape (world units)")]
        [SerializeField, Min(0.1f)]
        private float _dotSpacing = 0.55f;

        [SerializeField, Min(0.05f)]
        private float _dotSize = 0.34f;

        [Tooltip("Extra size of the outline around each dot and the arrowhead.")]
        [SerializeField, Min(0f)]
        private float _outline = 0.14f;

        [SerializeField, Min(0.1f)]
        private float _headLength = 1f;

        [SerializeField, Min(0.1f)]
        private float _headWidth = 0.72f;

        [Tooltip("Gap between a piece's edge (or its ring) and where the path starts or ends.")]
        [SerializeField, Min(0f)]
        private float _edgeGap = 0.15f;

        [Tooltip("Shortest path drawn, for links between pieces that almost touch.")]
        [SerializeField, Min(0.1f)]
        private float _minLength = 0.5f;

        [Header("Motion (seconds)")]
        [Tooltip("How long a path with no dice (an empty territory) stays lit before it fades.")]
        [SerializeField, Min(0f)]
        private float _lonePathHoldSeconds = 0.3f;

        [Tooltip("Opacity while it waits for the combat, with a gentle breath.")]
        [SerializeField, Range(0.2f, 1f)]
        private float _holdAlpha = 0.85f;

        [SerializeField, Range(0f, 0.5f)]
        private float _holdBreath = 0.15f;

        [SerializeField, Min(0.1f)]
        private float _breathPeriod = 1.4f;

        private readonly List<SpriteRenderer> _dotOutlines = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> _dotCores = new List<SpriteRenderer>();
        private SpriteRenderer _headOutline;
        private SpriteRenderer _headCore;
        private Coroutine _routine;

        // Per path, set in Play and read by the animation.
        private Vector3 _start;
        private Vector3 _direction;
        private float _length;
        private float _dotUnit = 1f;
        private Vector2 _headUnit = Vector2.one;
        private float _drawn;
        private float _headLen;
        private float _headWid;
        private int _dotCount;
        private Color _core;
        private Color _edge;

        /// <summary>
        /// Draws the march from <paramref name="from"/> to <paramref name="to"/> in the attacker's
        /// color. It stays until <see cref="Fade"/>; with <paramref name="fadeByItself"/> (an attack with
        /// no combat to wait for) it fades after a short hold.
        /// </summary>
        /// <param name="growSeconds">How long the line takes to reach the target (already paced).</param>
        public void Play(
            Territory from,
            Territory to,
            God attacker,
            float growSeconds,
            bool fadeByItself
        )
        {
            if (_dot == null || _head == null || _board == null)
                return;
            Clear();
            // The sprites' own sizes, read once per path instead of per dot per frame.
            _dotUnit = _dot.bounds.size.x;
            _headUnit = _head.bounds.size;
            _drawn = 0f;
            Vector3 a = _board.PositionOf(from);
            Vector3 b = _board.PositionOf(to);
            Vector3 span = b - a;
            float distance = span.magnitude;
            _direction = span / distance;
            float startInset = _board.EdgeDistance(from, _direction) + _edgeGap;
            float endInset = _board.EdgeDistance(to, -_direction) + _edgeGap;
            _start = a + _direction * startInset;
            _length = Mathf.Max(_minLength, distance - startInset - endInset);

            // A short link (Capital to Village) leaves little room: the arrowhead shrinks to fit.
            float fit = Mathf.Min(1f, _length * 0.6f / _headLength);
            _headLen = _headLength * fit;
            _headWid = _headWidth * fit;

            _core = _board.ColorOf(attacker);
            _edge = _core.grayscale < 0.2f ? _paperColor : _inkColor;
            // The last dots would sit under the arrowhead, so the row stops short of it.
            _dotCount = Mathf.Max(
                0,
                Mathf.FloorToInt((_length - _headLen * 0.6f) / _dotSpacing) + 1
            );
            EnsurePool(_dotCount);
            PlaceSprites();
            _routine = StartCoroutine(Animate(Mathf.Max(0.01f, growSeconds), fadeByItself));
        }

        /// <summary>The combat resolved: the path softly goes out.</summary>
        public void Fade(float seconds)
        {
            if (_routine == null)
                return;
            StopCoroutine(_routine);
            _routine = StartCoroutine(FadeOut(Mathf.Max(0.01f, seconds)));
        }

        /// <summary>Removes the path at once (a new turn, the end of the match).</summary>
        public void Clear()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            HideAll();
        }

        private void OnDisable()
        {
            _routine = null;
            HideAll();
        }

        private IEnumerator Animate(float grow, bool fadeByItself)
        {
            // Ease-out: the line shoots off the territory and settles on the target.
            for (float elapsed = 0f; elapsed < grow; elapsed += Time.deltaTime)
            {
                float t = elapsed / grow;
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                Draw(eased * _length, 1f);
                yield return null;
            }
            Draw(_length, 1f);

            if (fadeByItself)
            {
                for (float held = 0f; held < _lonePathHoldSeconds; held += Time.deltaTime)
                    yield return null;
                yield return FadeOut(Mathf.Max(0.01f, grow));
                yield break;
            }

            // Lit softly while the dice roll, with a slow breath.
            while (true)
            {
                float phase = Time.time * 2f * Mathf.PI / _breathPeriod;
                float alpha = _holdAlpha - _holdBreath * (0.5f - 0.5f * Mathf.Cos(phase));
                Draw(_length, alpha);
                yield return null;
            }
        }

        private IEnumerator FadeOut(float seconds)
        {
            // Fades from wherever the hold left it.
            float startAlpha = _headCore != null ? _headCore.color.a : _holdAlpha;
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
            {
                // From the distance reached, so a fade during the grow does not snap to full length.
                Draw(_drawn, startAlpha * (1f - elapsed / seconds));
                yield return null;
            }
            _routine = null;
            HideAll();
        }

        // Places the dots and the arrowhead along the line, hidden: Draw reveals them as it grows.
        private void PlaceSprites()
        {
            for (int i = 0; i < _dotCount; i++)
            {
                Vector3 position = _start + _direction * (i * _dotSpacing);
                PlaceSprite(_dotOutlines[i], position, 0f);
                PlaceSprite(_dotCores[i], position, 0f);
            }
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            _headOutline.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            _headCore.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static void PlaceSprite(SpriteRenderer sprite, Vector3 position, float scale)
        {
            sprite.transform.position = position;
            sprite.transform.localScale = new Vector3(scale, scale, 1f);
            sprite.gameObject.SetActive(true);
        }

        // distance: how far along the line the head has reached. Each dot pops when the head passes it.
        private void Draw(float distance, float alpha)
        {
            Color core = _core;
            core.a = alpha;
            Color edge = _edge;
            edge.a = alpha;
            for (int i = 0; i < _dotCount; i++)
            {
                float pop = Mathf.Clamp01((distance - i * _dotSpacing) / (_dotSpacing * 0.5f));
                // Sizes are in world units; the sprite's own size is divided out.
                float size = _dotSize * pop / _dotUnit;
                SetSprite(_dotOutlines[i], edge, size + _outline * pop / _dotUnit);
                SetSprite(_dotCores[i], core, size);
            }

            // The arrowhead rides the head of the line; it appears with the first stretch.
            float arrow = Mathf.Clamp01(distance / _dotSpacing);
            Vector3 position = _start + _direction * distance;
            _drawn = distance;
            Vector2 unit = _headUnit;
            _headOutline.transform.position = position;
            _headCore.transform.position = position;
            _headOutline.color = edge;
            _headCore.color = core;
            _headOutline.transform.localScale = new Vector3(
                (_headLen + _outline) * arrow / unit.x,
                (_headWid + _outline) * arrow / unit.y,
                1f
            );
            _headCore.transform.localScale = new Vector3(
                _headLen * arrow / unit.x,
                _headWid * arrow / unit.y,
                1f
            );
        }

        private static void SetSprite(SpriteRenderer sprite, Color color, float size)
        {
            sprite.color = color;
            sprite.transform.localScale = new Vector3(size, size, 1f);
        }

        private void HideAll()
        {
            for (int i = 0; i < _dotOutlines.Count; i++)
            {
                _dotOutlines[i].gameObject.SetActive(false);
                _dotCores[i].gameObject.SetActive(false);
            }
            if (_headOutline != null)
            {
                _headOutline.gameObject.SetActive(false);
                _headCore.gameObject.SetActive(false);
            }
        }

        // The pool only grows, to the longest path seen; the board has a few fixed links, so it stops
        // growing after the first attacks.
        private void EnsurePool(int dots)
        {
            while (_dotCores.Count < dots)
            {
                _dotOutlines.Add(NewSprite(_dot, "Path Dot Outline", 0));
                _dotCores.Add(NewSprite(_dot, "Path Dot", 1));
            }
            if (_headOutline == null)
            {
                _headOutline = NewSprite(_head, "Path Head Outline", 2);
                _headCore = NewSprite(_head, "Path Head", 3);
            }
            _headOutline.gameObject.SetActive(true);
            _headCore.gameObject.SetActive(true);
        }

        private SpriteRenderer NewSprite(Sprite sprite, string spriteName, int orderOffset)
        {
            var renderer = new GameObject(spriteName).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(transform, false);
            renderer.sharedMaterial = _spriteMaterial;
            renderer.sprite = sprite;
            renderer.sortingOrder = _sortingOrder + orderOffset;
            renderer.gameObject.SetActive(false);
            return renderer;
        }
    }
}

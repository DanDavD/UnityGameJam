using System.Collections;
using System.Collections.Generic;
using GameJam.Core;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// The god powers drawn on the board: a zigzag bolt for Zeus, a hammer blow and shockwave for
    /// Thor, sun rays for Ra, crows and feathers for Morrígan. Each god has its own shape as well as
    /// its color, so a power is never told by color alone. Built from the placeholder sprites and two
    /// LineRenderers, pooled, so nothing is downloaded and the web build stays cheap.
    /// It only displays: <see cref="MatchPresenter"/> decides when. Lives as a child of the Board at
    /// its origin, so the <see cref="BoardLayout"/> positions are its local positions.
    /// Every effect lasts about one and a half to two seconds, slow enough to follow (playtest 2);
    /// <c>pace</c> below 1 shortens it (fast bots).
    /// </summary>
    public sealed class GodVfx : MonoBehaviour
    {
        // Effects are short and end on their own; every sprite starts from these and is tinted.
        [Header("Board and colors")]
        [SerializeField]
        private BoardLayout _layout;

        [SerializeField]
        private GodVisuals _visuals;

        [Tooltip("--ink: the outline of the bolt, like the outline of the pieces.")]
        [SerializeField]
        private Color _inkColor = Color.black;

        [Header("Shapes")]
        [SerializeField]
        private Sprite _circle;

        [Tooltip("Thin ring: shockwaves, impact and ember pulses.")]
        [SerializeField]
        private Sprite _ring;

        [Tooltip("Rounded square, drawn thin and long as a sun ray.")]
        [SerializeField]
        private Sprite _ray;

        [Tooltip("Diamond, drawn thin as a feather or an ember spark.")]
        [SerializeField]
        private Sprite _feather;

        [Tooltip("Same as the pieces (Sprite-Lit-Default), so effects sit in the same light.")]
        [SerializeField]
        private Material _spriteMaterial;

        [Tooltip("Same as the board's connection lines (Sprite-Unlit-Default).")]
        [SerializeField]
        private Material _lineMaterial;

        [Tooltip("Above the pieces, their troop counts and the capture ring (6).")]
        [SerializeField]
        private int _sortingOrder = 8;

        [Header("Zeus: Thunderbolt and Storm")]
        [Tooltip("Height the bolt falls from, in world units.")]
        [SerializeField, Min(0.5f)]
        private float _boltHeight = 6f;

        [SerializeField, Min(0.01f)]
        private float _boltWidth = 0.14f;

        [Tooltip("Ink outline width added around the bolt's color.")]
        [SerializeField, Min(0f)]
        private float _boltOutline = 0.1f;

        [Tooltip("Sideways zigzag of each bolt segment, in world units.")]
        [SerializeField, Min(0f)]
        private float _boltJitter = 0.45f;

        [SerializeField, Range(2, 16)]
        private int _boltSegments = 7;

        [SerializeField, Min(0.05f)]
        private float _boltDuration = 1f;

        [Tooltip("Storm bolts are this much smaller than the Thunderbolt.")]
        [SerializeField, Range(0.1f, 1f)]
        private float _stormBoltScale = 0.4f;

        [Tooltip("Seconds between Storm strikes, so they read as a rolling storm.")]
        [SerializeField, Min(0f)]
        private float _stormStagger = 0.15f;

        [Tooltip(
            "Longest time from the first Storm strike to the last; many struck territories share it."
        )]
        [SerializeField, Min(0f)]
        private float _stormSpread = 0.5f;

        [Tooltip("Seconds the struck troops take to fly to Zeus's home.")]
        [SerializeField, Min(0.05f)]
        private float _sparkFlight = 0.8f;

        [Tooltip("World size of a flying Storm troop (a small bolt icon).")]
        [SerializeField, Min(0.05f)]
        private float _sparkSize = 0.75f;

        [Header("Thor: Mjölnir and Ragnarök")]
        [SerializeField, Min(0.02f)]
        private float _hammerDrop = 0.22f;

        [Tooltip("Seconds the shockwave takes to expand and fade.")]
        [SerializeField, Min(0.05f)]
        private float _shockwaveDuration = 0.8f;

        [Tooltip("Shockwave size at the end, relative to the piece.")]
        [SerializeField, Min(1f)]
        private float _shockwaveGrow = 2.4f;

        [Tooltip("Seconds of one ember pulse; it pulses twice.")]
        [SerializeField, Min(0.05f)]
        private float _emberPulse = 0.6f;

        [SerializeField, Range(0, 8)]
        private int _emberSparks = 4;

        [Header("Ra: Sun at noon and Eye of Ra")]
        [SerializeField, Range(4, 16)]
        private int _rayCount = 8;

        [SerializeField, Min(0.05f)]
        private float _sunDuration = 1.7f;

        [Tooltip("Ray thickness and length, in world units.")]
        [SerializeField]
        private Vector2 _raySize = new Vector2(0.08f, 0.38f);

        [SerializeField, Min(0.05f)]
        private float _eyeDuration = 1.3f;

        [Header("Morrígan: Night of crows and Harvest")]
        [SerializeField, Min(0.05f)]
        private float _crowFlight = 1.5f;

        [SerializeField, Min(0.05f)]
        private float _crowSize = 0.6f;

        [SerializeField, Range(1, 5)]
        private int _maxCrows = 3;

        [Tooltip("Seconds between crows leaving.")]
        [SerializeField, Min(0f)]
        private float _crowStagger = 0.2f;

        [Tooltip("Thickness and length of a feather, in world units.")]
        [SerializeField]
        private Vector2 _featherSize = new Vector2(0.1f, 0.28f);

        [SerializeField, Min(0.05f)]
        private float _featherLife = 0.9f;

        [Tooltip("How far from the board's edge the crows of Night of crows come in.")]
        [SerializeField, Min(0f)]
        private float _crowArrivalDistance = 3f;

        private readonly List<SpriteRenderer> _sprites = new List<SpriteRenderer>();
        private readonly List<LineRenderer> _lines = new List<LineRenderer>();

        // Ray sets reused between Ra effects, so a burst allocates nothing once warmed up. A stack,
        // not one scratch array, because Sun at noon bursts on every Ra territory at the same time.
        private readonly Stack<SpriteRenderer[]> _raySets = new Stack<SpriteRenderer[]>();
        private int _running;

        /// <summary>True while any effect is on screen, so the presenter can keep one strong animation at a time.</summary>
        public bool IsPlaying => _running > 0;

        // Effects never outlive the board: a disabled object stops its coroutines, so tidy up.
        private void OnDisable()
        {
            _running = 0;
            foreach (SpriteRenderer sprite in _sprites)
                sprite.gameObject.SetActive(false);
            foreach (LineRenderer line in _lines)
                line.gameObject.SetActive(false);
        }

        /// <summary>Zeus's Thunderbolt: a zigzag bolt strikes the target and an impact ring spreads.</summary>
        public void PlayThunderbolt(Territory target, float pace = 1f)
        {
            Vector3 at = PositionOf(target);
            Color zeus = ColorOf(God.Zeus);
            Play(Bolt(at, _boltHeight, 1f, 0f, pace));
            Play(
                Ring(
                    at,
                    SizeOf(target),
                    _shockwaveGrow * 0.75f,
                    zeus,
                    _boltDuration * 0.2f,
                    _shockwaveDuration,
                    pace
                )
            );
        }

        /// <summary>
        /// Zeus's Storm: a small bolt on every struck territory, then one bolt icon per struck troop
        /// flies to <paramref name="home"/>, where the troops join him.
        /// </summary>
        public void PlayStorm(IReadOnlyList<Territory> struck, Territory home, float pace = 1f)
        {
            if (struck.Count == 0)
                return;
            // A long list stays within the same total time.
            float stagger = Mathf.Min(_stormStagger, _stormSpread / struck.Count);
            Vector3 homeAt = home != null ? PositionOf(home) : Vector3.zero;
            Sprite bolt = _visuals.Get(God.Zeus).Icon;
            Color zeus = ColorOf(God.Zeus);
            for (int i = 0; i < struck.Count; i++)
            {
                Vector3 at = PositionOf(struck[i]);
                float delay = i * stagger;
                Play(Bolt(at, _boltHeight * _stormBoltScale, _stormBoltScale + 0.2f, delay, pace));
                if (home != null)
                    Play(
                        Fly(
                            bolt,
                            zeus,
                            at,
                            homeAt,
                            _sparkSize,
                            delay + _boltDuration * 0.4f,
                            _sparkFlight,
                            0.6f,
                            pace
                        )
                    );
            }
            if (home != null)
            {
                float arrival = (struck.Count - 1) * stagger + _boltDuration * 0.4f + _sparkFlight;
                Play(
                    Ring(homeAt, SizeOf(home), 1.5f, zeus, arrival, _shockwaveDuration * 0.7f, pace)
                );
            }
        }

        /// <summary>Thor's Mjölnir: the hammer drops on the target and a shockwave rolls out.</summary>
        public void PlayMjolnir(Territory target, float pace = 1f)
        {
            Vector3 at = PositionOf(target);
            float size = SizeOf(target);
            Color thor = ColorOf(God.Thor);
            Play(Hammer(at, size, thor, pace));
            Play(Ring(at, size, _shockwaveGrow, thor, _hammerDrop, _shockwaveDuration, pace));
            Play(
                Ring(
                    at,
                    size * 0.8f,
                    _shockwaveGrow * 0.8f,
                    _inkColor,
                    _hammerDrop + 0.06f,
                    _shockwaveDuration,
                    pace
                )
            );
        }

        /// <summary>
        /// Ragnarök: a red-orange ember pulse (two quick rings and rising sparks) on each piece; used
        /// when the Day starts on all of Thor's pieces, and on the winning piece of a 2-troop blow.
        /// </summary>
        public void PlayEmbers(IReadOnlyList<Territory> territories, float pace = 1f)
        {
            for (int i = 0; i < territories.Count; i++)
                PlayEmbers(territories[i], pace);
        }

        public void PlayEmbers(Territory territory, float pace = 1f)
        {
            Vector3 at = PositionOf(territory);
            float size = SizeOf(territory);
            Color thor = ColorOf(God.Thor);
            Play(Ring(at, size * 1.05f, 1.3f, thor, 0f, _emberPulse, pace));
            Play(Ring(at, size * 1.05f, 1.3f, thor, _emberPulse * 0.8f, _emberPulse, pace));
            for (int i = 0; i < _emberSparks; i++)
            {
                Vector3 start = at + new Vector3(Random.Range(-0.4f, 0.4f) * size, size * 0.2f, 0f);
                Vector3 end = start + new Vector3(Random.Range(-0.2f, 0.2f), 0.7f, 0f);
                Play(
                    Drift(
                        _feather,
                        thor,
                        start,
                        end,
                        _featherSize * 0.8f,
                        i * _emberPulse * 0.25f,
                        _emberPulse * 2f,
                        45f,
                        pace
                    )
                );
            }
        }

        /// <summary>Ra's Sun at noon: golden rays and a glow burst from every Ra territory at once.</summary>
        public void PlaySunAtNoon(IReadOnlyList<Territory> lit, float pace = 1f)
        {
            Color ra = ColorOf(God.Ra);
            for (int i = 0; i < lit.Count; i++)
            {
                Vector3 at = PositionOf(lit[i]);
                float size = SizeOf(lit[i]);
                Play(Glow(at, size, ra, _sunDuration * 0.7f, pace));
                Play(Rays(at, size, ra, 0.55f, 1f, _sunDuration, pace));
            }
        }

        /// <summary>The Eye of Ra: a sun disc flares over the converted territory, with a ray burst.</summary>
        public void PlayEyeOfRa(Territory target, float pace = 1f)
        {
            Vector3 at = PositionOf(target);
            float size = SizeOf(target);
            Color ra = ColorOf(God.Ra);
            Play(SunFlash(at, size, ra, pace));
            Play(Rays(at, size, ra, 0.6f, 1.3f, _eyeDuration, pace));
            Play(Ring(at, size, 1.8f, ra, 0f, _eyeDuration * 0.6f, pace));
        }

        /// <summary>
        /// Night of crows and Harvest: crows fly from the territory that lost troops to the one that
        /// gains them, dropping feathers. When both are the same (a Harvest that captured), the crows
        /// circle in from beside it.
        /// </summary>
        public void PlayCrows(Territory from, Territory to, int troops, float pace = 1f)
        {
            Vector3 end = PositionOf(to);
            Vector3 start = from == to ? end + new Vector3(-1.4f, 1.2f, 0f) : PositionOf(from);
            CrowFlock(start, end, troops, pace);
        }

        /// <summary>Night of crows begins: crows fly in from beyond the board to Morrígan's nest.</summary>
        public void PlayNightOfCrows(Territory nest, float pace = 1f)
        {
            if (nest == null)
                return;
            Vector3 end = PositionOf(nest);
            // From outside the board, on the side of the nest (her corner), so they come "from the night".
            Vector3 outward = end.sqrMagnitude > 0.01f ? end.normalized : Vector3.up;
            CrowFlock(end + outward * _crowArrivalDistance, end, _maxCrows, pace);
        }

        private void CrowFlock(Vector3 start, Vector3 end, int troops, float pace)
        {
            // Never a lone bird: two at least, so it reads as a flock of crows and not a stray icon.
            int crows = Mathf.Clamp(troops + 1, 2, _maxCrows);
            Sprite crow = _visuals.Get(God.Morrigan).Icon;
            Color color = ColorOf(God.Morrigan);
            Vector3 path = end - start;
            Vector3 side = new Vector3(-path.y, path.x, 0f).normalized;
            for (int i = 0; i < crows; i++)
            {
                // Spread the birds on alternating sides of the straight line.
                float bend = (i % 2 == 0 ? 1f : -1f) * (0.6f + 0.4f * i);
                Vector3 jitter = new Vector3(
                    Random.Range(-0.2f, 0.2f),
                    Random.Range(-0.2f, 0.2f),
                    0f
                );
                Play(Crow(crow, color, start + jitter, end, side * bend, i * _crowStagger, pace));
            }
        }

        // Runs an effect and counts it, so IsPlaying stays true until the last one ends.
        private void Play(IEnumerator effect)
        {
            if (!isActiveAndEnabled)
                return;
            StartCoroutine(Track(effect));
        }

        private IEnumerator Track(IEnumerator effect)
        {
            _running++;
            yield return effect;
            _running = Mathf.Max(0, _running - 1);
        }

        private static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
                yield return null;
        }

        // ---- Zeus ----

        private IEnumerator Bolt(
            Vector3 target,
            float height,
            float widthScale,
            float delay,
            float pace
        )
        {
            yield return Wait(delay * pace);
            LineRenderer outline = TakeLine(_sortingOrder);
            LineRenderer core = TakeLine(_sortingOrder + 1);
            ShapeBolt(outline, core, target, height, widthScale);

            Color zeus = ColorOf(God.Zeus);
            float duration = _boltDuration * pace;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                // Strike at full strength, one flicker, then fade: how lightning reads on a table.
                float alpha =
                    t < 0.15f ? 1f
                    : t < 0.25f ? 0.2f
                    : 1f - (t - 0.25f) / 0.75f;
                SetLineColor(outline, _inkColor, alpha);
                SetLineColor(core, zeus, alpha);
                yield return null;
            }
            Release(outline);
            Release(core);
        }

        // A zigzag from above down to the target, wide at the strike point and thin at the top.
        private void ShapeBolt(
            LineRenderer outline,
            LineRenderer core,
            Vector3 target,
            float height,
            float widthScale
        )
        {
            int points = _boltSegments + 1;
            outline.positionCount = points;
            core.positionCount = points;
            Vector3 top = target + new Vector3(Random.Range(-0.15f, 0.15f) * height, height, 0f);
            float jitter = _boltJitter * Mathf.Clamp01(height / _boltHeight * 2f);
            for (int i = 0; i < points; i++)
            {
                float t = (float)i / _boltSegments;
                Vector3 point = Vector3.Lerp(top, target, t);
                bool end = i == 0 || i == points - 1;
                if (!end)
                    point.x += (i % 2 == 0 ? 1f : -1f) * Random.Range(0.5f, 1f) * jitter;
                outline.SetPosition(i, point);
                core.SetPosition(i, point);
            }
            float width = _boltWidth * widthScale;
            core.startWidth = width * 0.4f;
            core.endWidth = width;
            outline.startWidth = (width + _boltOutline) * 0.4f;
            outline.endWidth = width + _boltOutline * widthScale;
        }

        // ---- Thor ----

        private IEnumerator Hammer(Vector3 target, float size, Color color, float pace)
        {
            SpriteRenderer hammer = TakeSprite(_visuals.Get(God.Thor).Icon, color, 2);
            Transform hammerTransform = hammer.transform;
            Vector3 raised = target + new Vector3(0.3f, 1.2f, 0f);
            // Smaller than the piece, so it stamps the token instead of hiding it.
            float rest = size * 0.6f;

            // Swings down with an ease-in, so it lands hard.
            float drop = _hammerDrop * pace;
            for (float t = 0f; t < 1f; t += Time.deltaTime / drop)
            {
                float eased = t * t;
                hammerTransform.localPosition = Vector3.Lerp(raised, target, eased);
                hammerTransform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(-45f, 0f, eased)
                );
                hammerTransform.localScale = Vector3.one * Mathf.Lerp(rest * 1.3f, rest, eased);
                yield return null;
            }
            hammerTransform.localPosition = target;
            hammerTransform.localRotation = Quaternion.identity;

            float fade = _shockwaveDuration * 0.7f * pace;
            for (float t = 0f; t < 1f; t += Time.deltaTime / fade)
            {
                SetSpriteColor(hammer, color, 1f - t);
                yield return null;
            }
            Release(hammer);
        }

        // ---- Ra ----

        private IEnumerator Rays(
            Vector3 center,
            float size,
            Color color,
            float startRadius,
            float endRadius,
            float duration,
            float pace
        )
        {
            SpriteRenderer[] rays = TakeRaySet();
            int count = rays.Length;
            for (int i = 0; i < count; i++)
                rays[i] = TakeSprite(_ray, color, 0);

            float spin = 360f / count * 0.5f;
            duration *= pace;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float eased = 1f - (1f - t) * (1f - t);
                float radius = size * Mathf.Lerp(startRadius, endRadius, eased);
                // In fast, out slow: the rays flare, hold a beat, then fade.
                float alpha = t < 0.15f ? t / 0.15f : 1f - Mathf.Clamp01((t - 0.5f) / 0.5f);
                float length = _raySize.y * Mathf.Lerp(0.6f, 1f, eased);
                for (int i = 0; i < count; i++)
                {
                    float angle = i * 360f / count + spin * eased;
                    Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
                    Transform ray = rays[i].transform;
                    ray.localRotation = rotation;
                    ray.localPosition = center + rotation * (Vector3.up * (radius + length * 0.5f));
                    ray.localScale = new Vector3(_raySize.x, length, 1f);
                    SetSpriteColor(rays[i], color, alpha);
                }
                yield return null;
            }
            for (int i = 0; i < count; i++)
            {
                Release(rays[i]);
                rays[i] = null;
            }
            _raySets.Push(rays);
        }

        // A set from an earlier burst, unless the ray count was changed in the Inspector since.
        private SpriteRenderer[] TakeRaySet()
        {
            while (_raySets.Count > 0)
            {
                SpriteRenderer[] set = _raySets.Pop();
                if (set.Length == _rayCount)
                    return set;
            }
            return new SpriteRenderer[_rayCount];
        }

        // A soft disc of light under the rays, like noon sun on the piece.
        private IEnumerator Glow(
            Vector3 center,
            float size,
            Color color,
            float duration,
            float pace
        )
        {
            SpriteRenderer glow = TakeSprite(_circle, color, -1);
            glow.transform.localPosition = center;
            duration *= pace;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                glow.transform.localScale = Vector3.one * (size * Mathf.Lerp(0.9f, 1.4f, t));
                SetSpriteColor(glow, color, 0.45f * (1f - t));
                yield return null;
            }
            Release(glow);
        }

        private IEnumerator SunFlash(Vector3 center, float size, Color color, float pace)
        {
            SpriteRenderer sun = TakeSprite(_visuals.Get(God.Ra).Icon, color, 2);
            sun.transform.localPosition = center;
            float duration = _eyeDuration * pace;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float eased = 1f - (1f - t) * (1f - t) * (1f - t);
                sun.transform.localScale = Vector3.one * (size * Mathf.Lerp(0.5f, 1.5f, eased));
                sun.transform.localRotation = Quaternion.Euler(0f, 0f, -30f * eased);
                SetSpriteColor(sun, color, t < 0.3f ? 1f : 1f - (t - 0.3f) / 0.7f);
                yield return null;
            }
            Release(sun);
        }

        // ---- Morrígan ----

        private IEnumerator Crow(
            Sprite sprite,
            Color color,
            Vector3 start,
            Vector3 end,
            Vector3 bend,
            float delay,
            float pace
        )
        {
            yield return Wait(delay * pace);
            SpriteRenderer crow = TakeSprite(sprite, color, 2);
            // The icon faces right; it flies beak first.
            crow.flipX = end.x < start.x;
            Vector3 control = (start + end) * 0.5f + bend + Vector3.up * 0.5f;
            float duration = _crowFlight * pace;
            bool firstFeather = false;
            bool secondFeather = false;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float eased = t * t * (3f - 2f * t);
                Vector3 at = Bezier(start, control, end, eased);
                crow.transform.localPosition = at;
                // Wings beat: a quick squash on the vertical axis.
                float flap = 1f - 0.35f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 6f));
                crow.transform.localScale = new Vector3(_crowSize, _crowSize * flap, 1f);
                float alpha =
                    t < 0.1f ? t / 0.1f
                    : t > 0.8f ? (1f - t) / 0.2f
                    : 1f;
                SetSpriteColor(crow, color, alpha);

                if (!firstFeather && t > 0.3f)
                {
                    firstFeather = true;
                    DropFeather(color, at, pace);
                }
                if (!secondFeather && t > 0.6f)
                {
                    secondFeather = true;
                    DropFeather(color, at, pace);
                }
                yield return null;
            }
            Release(crow);
        }

        private void DropFeather(Color color, Vector3 at, float pace)
        {
            Vector3 end = at + new Vector3(Random.Range(-0.25f, 0.25f), -0.35f, 0f);
            Play(
                Drift(
                    _feather,
                    color,
                    at,
                    end,
                    _featherSize,
                    0f,
                    _featherLife,
                    Random.Range(-120f, 120f),
                    pace
                )
            );
        }

        private static Vector3 Bezier(Vector3 a, Vector3 control, Vector3 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * b;
        }

        // ---- Shared pieces ----

        // A ring that expands from the piece and fades: impact, shockwave, arrival.
        private IEnumerator Ring(
            Vector3 center,
            float size,
            float grow,
            Color color,
            float delay,
            float duration,
            float pace
        )
        {
            yield return Wait(delay * pace);
            SpriteRenderer ring = TakeSprite(_ring, color, 1);
            ring.transform.localPosition = center;
            duration *= pace;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float eased = 1f - (1f - t) * (1f - t);
                ring.transform.localScale = Vector3.one * (size * Mathf.Lerp(1f, grow, eased));
                SetSpriteColor(ring, color, 1f - t);
                yield return null;
            }
            Release(ring);
        }

        // An icon that travels on a gentle arc and shrinks into its destination (Storm troops).
        private IEnumerator Fly(
            Sprite sprite,
            Color color,
            Vector3 start,
            Vector3 end,
            float size,
            float delay,
            float duration,
            float endScale,
            float pace
        )
        {
            yield return Wait(delay * pace);
            SpriteRenderer icon = TakeSprite(sprite, color, 2);
            Vector3 control = (start + end) * 0.5f + Vector3.up * 1.2f;
            duration *= pace;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float eased = t * t * (3f - 2f * t);
                icon.transform.localPosition = Bezier(start, control, end, eased);
                icon.transform.localScale = Vector3.one * (size * Mathf.Lerp(1f, endScale, eased));
                SetSpriteColor(icon, color, t < 0.15f ? t / 0.15f : 1f);
                yield return null;
            }
            Release(icon);
        }

        // A small sprite that drifts, turns and fades: feathers and embers.
        private IEnumerator Drift(
            Sprite sprite,
            Color color,
            Vector3 start,
            Vector3 end,
            Vector2 size,
            float delay,
            float duration,
            float turn,
            float pace
        )
        {
            yield return Wait(delay * pace);
            SpriteRenderer bit = TakeSprite(sprite, color, 0);
            bit.transform.localScale = new Vector3(size.x, size.y, 1f);
            float startAngle = Random.Range(0f, 360f);
            duration *= pace;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                float eased = 1f - (1f - t) * (1f - t);
                Vector3 at = Vector3.Lerp(start, end, eased);
                // A light sway, like something settling through the air.
                at.x += Mathf.Sin(t * Mathf.PI * 2f) * 0.08f;
                bit.transform.localPosition = at;
                bit.transform.localRotation = Quaternion.Euler(0f, 0f, startAngle + turn * t);
                SetSpriteColor(bit, color, 1f - t * t);
                yield return null;
            }
            Release(bit);
        }

        // ---- Pool ----

        private SpriteRenderer TakeSprite(Sprite sprite, Color color, int orderOffset)
        {
            SpriteRenderer renderer = null;
            foreach (SpriteRenderer pooled in _sprites)
            {
                if (!pooled.gameObject.activeSelf)
                {
                    renderer = pooled;
                    break;
                }
            }
            if (renderer == null)
            {
                renderer = new GameObject("Vfx Sprite").AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(transform, false);
                renderer.sharedMaterial = _spriteMaterial;
                _sprites.Add(renderer);
            }
            renderer.gameObject.SetActive(true);
            renderer.sprite = sprite;
            renderer.flipX = false;
            renderer.sortingOrder = _sortingOrder + orderOffset;
            renderer.color = color;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = Vector3.zero;
            return renderer;
        }

        private LineRenderer TakeLine(int sortingOrder)
        {
            LineRenderer line = null;
            foreach (LineRenderer pooled in _lines)
            {
                if (!pooled.gameObject.activeSelf)
                {
                    line = pooled;
                    break;
                }
            }
            if (line == null)
            {
                line = new GameObject("Vfx Bolt").AddComponent<LineRenderer>();
                line.transform.SetParent(transform, false);
                line.useWorldSpace = false;
                line.sharedMaterial = _lineMaterial;
                // Sharp joints: a bolt is all corners.
                line.numCornerVertices = 0;
                line.numCapVertices = 2;
                _lines.Add(line);
            }
            line.gameObject.SetActive(true);
            line.sortingOrder = sortingOrder;
            return line;
        }

        private static void Release(Component component) => component.gameObject.SetActive(false);

        private static void SetSpriteColor(SpriteRenderer renderer, Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }

        private static void SetLineColor(LineRenderer line, Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            line.startColor = color;
            line.endColor = color;
        }

        private Vector3 PositionOf(Territory territory) => _layout.GetPosition(territory.Id);

        private float SizeOf(Territory territory) => _layout.GetSize(territory.Kind);

        private Color ColorOf(God god) => _visuals.Get(god).Color;
    }
}

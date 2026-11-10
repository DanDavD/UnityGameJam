using System;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// One pooled mark that explains a Favor change where the eye can follow it: a star that pops
    /// over a sacred combat die and flies on an arc to its god's Favor stars (which fill as it lands),
    /// or, quieter, a cracked-die mark that sinks and fades over a meter that just lost a star.
    /// Created once by <see cref="HudView"/>; it moves with <c>style.translate</c> from the panel
    /// scheduler, so a frame of motion never allocates or re-runs layout.
    /// </summary>
    public sealed class FavorFlyer
    {
        private const string BumpClass = "favor--bump";
        private const string DimClass = "favor--dim";

        // Share of the star's time spent popping over the die, then flying; the rest is the burst
        // over the meter after it lands.
        private const float PopShare = 0.25f;
        private const float LandShare = 0.85f;

        // Star scale while popping, flying and bursting: big over the die, small as it lands, then
        // a soft flash that fades.
        private const float PopStartScale = 0.4f;
        private const float PopScale = 1.2f;
        private const float LandScale = 0.6f;
        private const float BurstScale = 1.4f;

        // The cracked mark sinks this far (panel pixels) and stays opaque for the first 40%, like the
        // board's "-1".
        private const float CrackSink = 16f;
        private const float CrackHoldShare = 0.4f;

        private readonly VisualElement _parent;
        private readonly VisualElement _element;
        private readonly IVisualElementScheduledItem _tick;

        private bool _isStar;
        private Vector2 _from;
        private Vector2 _to;
        private float _arc;
        private float _startTime;
        private float _duration;
        private VisualElement _target;
        private Action _arrived;

        public FavorFlyer(VisualElement parent, VisualElement element)
        {
            _parent = parent;
            _element = element;
            _element.pickingMode = PickingMode.Ignore;
            _parent.Add(_element);
            // Created once and resumed per flight, so no flight allocates a scheduled item.
            _tick = _element.schedule.Execute(Tick).Every(0);
            _tick.Pause();
        }

        public bool IsBusy { get; private set; }

        /// <summary>
        /// Pops a star over <paramref name="from"/> and flies it to <paramref name="to"/>;
        /// <paramref name="arrived"/> runs as it lands (fill the meter then). False when it cannot fly
        /// (busy, or an element not laid out): the caller shows the change at once.
        /// </summary>
        public bool PlayStar(
            VisualElement from,
            VisualElement to,
            God god,
            float seconds,
            float arcPixels,
            Action arrived
        )
        {
            if (IsBusy || !TryCenter(from, out _from) || !TryCenter(to, out _to))
                return false;
            GodUss.Apply(_element, god);
            _isStar = true;
            _arc = arcPixels;
            Begin(to, seconds, arrived);
            return true;
        }

        /// <summary>A cracked-die mark that sinks over <paramref name="at"/> while the meter dims.</summary>
        public bool PlayCrack(VisualElement at, float seconds)
        {
            if (IsBusy || !TryCenter(at, out _to))
                return false;
            _from = _to;
            _isStar = false;
            Begin(at, seconds, null);
            at.AddToClassList(DimClass);
            return true;
        }

        /// <summary>Stops at once (match over, view disabled), leaving the target as it is now.</summary>
        public void Stop()
        {
            if (!IsBusy)
                return;
            Finish();
        }

        private void Begin(VisualElement target, float seconds, Action arrived)
        {
            IsBusy = true;
            _target = target;
            _arrived = arrived;
            _duration = Mathf.Max(0.01f, seconds);
            _startTime = Time.time;
            Apply(_from, _isStar ? PopStartScale : 1f, 0f);
            _element.style.visibility = Visibility.Visible;
            _tick.Resume();
        }

        private void Tick()
        {
            // Game time, like the dice's coroutines, so the star keeps pace with the rest of the combat.
            float t = Mathf.Clamp01((Time.time - _startTime) / _duration);
            if (_isStar)
                TickStar(t);
            else
                TickCrack(t);
            if (t >= 1f)
                Finish();
        }

        private void TickStar(float t)
        {
            if (t < PopShare)
            {
                float pop = EaseOut(t / PopShare);
                Apply(_from, Mathf.Lerp(PopStartScale, PopScale, pop), pop);
                return;
            }
            if (t < LandShare)
            {
                float u = EaseInOut((t - PopShare) / (LandShare - PopShare));
                // Lifted on an arc (panel y grows downward), so it reads as thrown, not slid.
                Vector2 position = Vector2.Lerp(_from, _to, u);
                position.y -= _arc * Mathf.Sin(u * Mathf.PI);
                Apply(position, Mathf.Lerp(PopScale, LandScale, u), 1f);
                return;
            }

            Land();
            float burst = EaseOut((t - LandShare) / (1f - LandShare));
            Apply(_to, Mathf.Lerp(LandScale, BurstScale, burst), 1f - burst);
        }

        private void TickCrack(float t)
        {
            float fade =
                t < CrackHoldShare ? 1f : 1f - (t - CrackHoldShare) / (1f - CrackHoldShare);
            Apply(_to + new Vector2(0f, CrackSink * EaseOut(t)), 1f, fade);
        }

        // Once per flight: the meter fills and bumps the moment the star reaches it.
        private void Land()
        {
            if (_arrived == null)
                return;
            Action arrived = _arrived;
            _arrived = null;
            _target.AddToClassList(BumpClass);
            arrived();
        }

        private void Finish()
        {
            // A flight cut short still delivers its change.
            Land();
            _tick.Pause();
            _element.style.visibility = Visibility.Hidden;
            _target?.RemoveFromClassList(BumpClass);
            _target?.RemoveFromClassList(DimClass);
            _target = null;
            IsBusy = false;
        }

        // Centered on a panel position (parent space): translate the element's top-left there.
        private void Apply(Vector2 center, float scale, float opacity)
        {
            Vector2 size = _element.layout.size;
            Vector2 offset = center - size * 0.5f - _element.layout.position;
            _element.style.translate = new Translate(offset.x, offset.y);
            _element.style.scale = new Scale(new Vector3(scale, scale, 1f));
            _element.style.opacity = opacity;
        }

        // The center of an element, in the parent's space; false before it has a layout.
        private bool TryCenter(VisualElement element, out Vector2 center)
        {
            center = default;
            if (element == null || element.panel == null)
                return false;
            Rect bounds = element.worldBound;
            if (float.IsNaN(bounds.x) || bounds.width <= 0f || bounds.height <= 0f)
                return false;
            center = _parent.WorldToLocal(bounds.center);
            return true;
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

        private static float EaseInOut(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }
}

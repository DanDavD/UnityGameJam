using System.Collections;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// "YOUR TURN" / "THOR'S TURN" with the god's chip, sliding in at the top center when a turn starts.
    /// The slide itself is a USS transition on the shown class; this view only times how long it stays.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TurnBannerView : MonoBehaviour
    {
        private const string ShownClass = "turn-banner--shown";

        [Tooltip("Seconds the banner stays on screen, slide-in included.")]
        [SerializeField, Min(0f)]
        private float _duration = 1.6f;

        private UIDocument _document;
        private VisualElement _banner;
        private VisualElement _chip;
        private Label _label;

        /// <summary>True while the banner is on screen; the match loop waits for it before the turn plays.</summary>
        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _banner = root.Q<VisualElement>("turn-banner");
            _chip = root.Q<VisualElement>("turn-banner-chip");
            _label = root.Q<Label>("turn-banner-label");
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            IsPlaying = false;
        }

        /// <param name="durationFactor">Scales how long it stays: bot turns pass the fast-bots pace.</param>
        public void Show(God god, string text, float durationFactor)
        {
            StopAllCoroutines();
            StartCoroutine(Play(god, text, _duration * durationFactor));
        }

        private IEnumerator Play(God god, string text, float duration)
        {
            IsPlaying = true;
            GodUss.Apply(_banner, god);
            GodUss.Apply(_chip, god);
            _label.text = text;
            _banner.AddToClassList(ShownClass);
            // Timed by hand, not a cached WaitForSeconds, because the duration changes with the pace.
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
                yield return null;
            // Removing the class plays the exit transition; the next turn does not wait for it.
            _banner.RemoveFromClassList(ShownClass);
            IsPlaying = false;
        }
    }
}

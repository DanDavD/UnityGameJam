using System.Collections;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJam.UI
{
    /// <summary>
    /// The banners that slide in over the board: "YOUR TURN" / "THOR'S TURN" with the god's chip at the
    /// top center when a turn starts, and the Sacred Road popup in the middle when a person's sacred die
    /// opens the road. The slides are USS transitions on the shown class; this view only times how
    /// long each one stays.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TurnBannerView : MonoBehaviour
    {
        private const string ShownClass = "turn-banner--shown";
        private const string RoadShownClass = "road-popup--shown";

        [Tooltip("Seconds the banner stays on screen, slide-in included.")]
        [SerializeField, Min(0f)]
        private float _duration = 2.4f;

        [Tooltip("Seconds the Sacred Road popup stays unless a click closes it first.")]
        [SerializeField, Min(0f)]
        private float _roadPopupDuration = 2.2f;

        private UIDocument _document;
        private VisualElement _banner;
        private VisualElement _chip;
        private Label _label;
        private VisualElement _roadPopup;
        private VisualElement _roadChip;
        private Label _roadTitle;
        private Label _roadText;
        private Coroutine _roadPopupRoutine;

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
            _roadPopup = root.Q<VisualElement>("road-popup");
            _roadChip = root.Q<VisualElement>("road-popup-chip");
            _roadTitle = root.Q<Label>("road-popup-title");
            _roadText = root.Q<Label>("road-popup-text");
            root.Q<Label>("road-popup-close").text = MatchText.RoadPopupClose;
            _roadPopup.RegisterCallback<PointerDownEvent>(HandleRoadPopupPointerDown);
        }

        private void OnDisable()
        {
            _roadPopup.UnregisterCallback<PointerDownEvent>(HandleRoadPopupPointerDown);
            HideRoadPopup();
            StopAllCoroutines();
            IsPlaying = false;
        }

        /// <param name="durationFactor">Scales how long it stays: bot turns pass the fast-bots pace.</param>
        public void Show(God god, string text, float durationFactor)
        {
            // A new turn always starts clean: a popup of the last one never hangs over it.
            HideRoadPopup();
            StopAllCoroutines();
            StartCoroutine(Play(god, text, _duration * durationFactor));
        }

        /// <summary>
        /// Slides the Sacred Road popup in at the center of the screen and closes it after a few
        /// seconds, or on a click on it. It only catches clicks while it is shown, so it never
        /// blocks the board for longer than it takes to read.
        /// </summary>
        public void ShowRoadPopup(God god, string title, string text, float durationFactor)
        {
            HideRoadPopup();
            GodUss.Apply(_roadPopup, god);
            GodUss.Apply(_roadChip, god);
            _roadTitle.text = title;
            _roadText.text = text;
            _roadPopup.pickingMode = PickingMode.Position;
            _roadPopup.AddToClassList(RoadShownClass);
            _roadPopupRoutine = StartCoroutine(HoldRoadPopup(_roadPopupDuration * durationFactor));
        }

        /// <summary>Slides the popup out at once (a click on it, the player's next action, a new turn).</summary>
        public void HideRoadPopup()
        {
            if (_roadPopupRoutine != null)
            {
                StopCoroutine(_roadPopupRoutine);
                _roadPopupRoutine = null;
            }
            _roadPopup.RemoveFromClassList(RoadShownClass);
            _roadPopup.pickingMode = PickingMode.Ignore;
        }

        private void HandleRoadPopupPointerDown(PointerDownEvent evt)
        {
            HideRoadPopup();
            evt.StopPropagation();
        }

        private IEnumerator HoldRoadPopup(float duration)
        {
            // Timed by hand, like the turn banner: the duration changes with the pace.
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
                yield return null;
            _roadPopupRoutine = null;
            HideRoadPopup();
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

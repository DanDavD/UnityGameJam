using System;
using System.Collections;
using GameJam.Core;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace GameJam.UI
{
    /// <summary>
    /// The Destiny Die at each round start: a large die tumbles in the center, lands on the face
    /// Destiny rolled and shows what it does this round. A click anywhere skips it.
    /// A god's face takes that god's color, so the favored god is visible at a glance. A last line
    /// in the human's color says what the face does to them ("You: +2").
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DestinyView : MonoBehaviour
    {
        [Header("Timing (seconds)")]
        [SerializeField, Min(0f)]
        private float _tumbleDuration = 1f;

        [Tooltip("Seconds between random faces while tumbling.")]
        [SerializeField, Min(0.01f)]
        private float _faceInterval = 0.08f;

        [SerializeField, Min(0.01f)]
        private float _landDuration = 0.2f;

        [Tooltip("How long the landed face and its effect stay readable.")]
        [SerializeField, Min(0f)]
        private float _hold = 0.8f;

        [Header("Motion")]
        [Tooltip("Max tilt in degrees while tumbling.")]
        [SerializeField, Min(0f)]
        private float _tumbleAngle = 20f;

        [Tooltip("Peak scale of the landing bounce.")]
        [SerializeField, Min(1f)]
        private float _landOvershoot = 1.25f;

        private UIDocument _document;
        private VisualElement _overlay;
        private Label _title;
        private VisualElement _die;
        private VisualElement _chip;
        private Label _face;
        private Label _effect;
        private Label _you;
        private Label _skip;
        private WaitForSeconds _holdWait;

        /// <summary>True from the first tumble until the hold ends or a click skips it.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Raised when the die lands on its face; not raised when a click skips it.</summary>
        public event Action Landed;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _holdWait = new WaitForSeconds(_hold);
        }

        private void OnEnable()
        {
            VisualElement root = _document.rootVisualElement;
            _overlay = root.Q<VisualElement>("destiny-overlay");
            _title = root.Q<Label>("destiny-title");
            _die = root.Q<VisualElement>("destiny-die");
            _chip = root.Q<VisualElement>("destiny-chip");
            _face = root.Q<Label>("destiny-face");
            _effect = root.Q<Label>("destiny-effect");
            _you = root.Q<Label>("destiny-you");
            _skip = root.Q<Label>("destiny-skip");
            _skip.text = MatchText.SkipHint;
            _overlay.RegisterCallback<PointerDownEvent>(HandlePointerDown);
            Hide();
        }

        private void OnDisable()
        {
            _overlay.UnregisterCallback<PointerDownEvent>(HandlePointerDown);
            Hide();
        }

        /// <param name="youLine">What the face does to the human, shown in <paramref name="human"/>'s color.</param>
        public void Show(int round, int face, string effect, God human, string youLine)
        {
            StopAllCoroutines();
            StartCoroutine(Play(round, face, effect, human, youLine));
        }

        /// <summary>Stops the die at once, for example when the match ends during it.</summary>
        public void Hide()
        {
            StopAllCoroutines();
            IsPlaying = false;
            _overlay.style.display = DisplayStyle.None;
        }

        private IEnumerator Play(int round, int face, string effect, God human, string youLine)
        {
            IsPlaying = true;
            _overlay.style.display = DisplayStyle.Flex;
            _title.text = MatchText.RoundTitle(round);
            _effect.text = effect;
            _you.text = youLine;
            GodUss.Apply(_you, human);
            // Both hidden until the die lands, so the effect never spoils the roll.
            _effect.style.visibility = Visibility.Hidden;
            _you.style.visibility = Visibility.Hidden;
            GodUss.Apply(_die, null);
            GodUss.Apply(_chip, null);

            yield return Tumble();

            // The favored god's color and chip (icon) arrive together, so it is never color alone.
            God? favored = Pantheon.GodOfFace(face);
            _face.text = MatchText.FaceText(face);
            GodUss.Apply(_die, favored);
            GodUss.Apply(_chip, favored);
            yield return Land();

            _effect.style.visibility = Visibility.Visible;
            _you.style.visibility = Visibility.Visible;
            Landed?.Invoke();
            yield return _holdWait;
            Hide();
        }

        // The faces shown while tumbling are cosmetic, so they use Unity's random, not the match's.
        private IEnumerator Tumble()
        {
            float elapsed = 0f;
            float nextFace = 0f;
            while (elapsed < _tumbleDuration)
            {
                if (elapsed >= nextFace)
                {
                    nextFace += _faceInterval;
                    _face.text = MatchText.FaceText(Random.Range(1, Die.Faces + 1));
                    _die.style.rotate = new Rotate(Random.Range(-_tumbleAngle, _tumbleAngle));
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            _die.style.rotate = new Rotate(0f);
        }

        private IEnumerator Land()
        {
            float elapsed = 0f;
            while (elapsed < _landDuration)
            {
                elapsed += Time.deltaTime;
                float bump = Mathf.Sin(Mathf.Clamp01(elapsed / _landDuration) * Mathf.PI);
                float scale = 1f + (_landOvershoot - 1f) * bump;
                _die.style.scale = new Scale(new Vector3(scale, scale, 1f));
                yield return null;
            }
            _die.style.scale = new Scale(Vector3.one);
        }

        // Skipping hides the die at once; the HUD round line still says what Destiny did.
        private void HandlePointerDown(PointerDownEvent evt) => Hide();
    }
}

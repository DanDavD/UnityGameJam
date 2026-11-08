using System.Collections;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// A tiny screen shake, used only when a territory is taken from a rival. The amplitude is in
    /// screen pixels, so it feels the same at any resolution or camera size.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraShake : MonoBehaviour
    {
        [Tooltip("Peak offset in screen pixels (design system: 2-4 px).")]
        [SerializeField, Range(0f, 8f)]
        private float _amplitudePixels = 3f;

        [Tooltip("Seconds; under 150 ms per the design system.")]
        [SerializeField, Min(0.01f)]
        private float _duration = 0.12f;

        private Camera _camera;
        private Vector3 _restPosition;
        private Coroutine _shake;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _restPosition = transform.localPosition;
        }

        // A shake cut short (scene reload, disable) must not leave the camera off center.
        private void OnDisable()
        {
            _shake = null;
            transform.localPosition = _restPosition;
        }

        public void Shake()
        {
            if (!isActiveAndEnabled)
                return;
            if (_shake != null)
                StopCoroutine(_shake);
            _shake = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            // An orthographic camera shows 2 * size world units over the screen height.
            float worldPerPixel = 2f * _camera.orthographicSize / Screen.height;
            float amplitude = _amplitudePixels * worldPerPixel;
            for (float elapsed = 0f; elapsed < _duration; elapsed += Time.deltaTime)
            {
                // Fades out, so it settles at rest instead of snapping back.
                float strength = 1f - elapsed / _duration;
                Vector2 offset = Random.insideUnitCircle * (amplitude * strength);
                transform.localPosition = _restPosition + (Vector3)offset;
                yield return null;
            }
            transform.localPosition = _restPosition;
            _shake = null;
        }
    }
}

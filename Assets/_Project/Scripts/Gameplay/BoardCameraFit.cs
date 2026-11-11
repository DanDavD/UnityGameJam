using GameJam.UI;
using UnityEngine;

namespace GameJam.Gameplay
{
    /// <summary>
    /// Fits the board frame into the free table the HUD leaves (HudView's board area: the screen minus
    /// the left column, the combat panel, the round line and the bottom actions). The camera's size and
    /// position follow that rectangle, so no panel covers the board at any aspect ratio from 4:3 to 21:9.
    /// A fixed orthographic size could not do this: it fits the screen height, so a wider-than-16:9
    /// shape fits and a narrower one (16:10, 3:2, 4:3) pushed the board under the panels.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCameraFit : MonoBehaviour
    {
        [SerializeField]
        private HudView _hud;

        [Tooltip("Moves the shake's rest position along with the camera.")]
        [SerializeField]
        private CameraShake _shake;

        [Tooltip(
            "World size to keep clear on screen: the board frame (17.8 x 10.8) plus its soft shadow."
        )]
        [SerializeField]
        private Vector2 _boardSize = new Vector2(18.2f, 11.2f);

        [Tooltip("Half size of the Table sprite (48 x 32): the camera never shows beyond it.")]
        [SerializeField]
        private Vector2 _tableHalfSize = new Vector2(24f, 16f);

        private Camera _camera;
        private Rect _lastArea;
        private Vector2Int _lastScreen;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        // Runs every frame but only refits when the area or the screen changed (the UI may lay out
        // after this frame's LateUpdate; the next frame catches it).
        private void LateUpdate()
        {
            var screen = new Vector2Int(_camera.pixelWidth, _camera.pixelHeight);
            if (screen.x <= 0 || screen.y <= 0)
                return;
            // No HUD yet (first frame, or disabled): fit the whole screen until its rectangle exists.
            if (_hud == null || !_hud.TryGetBoardArea(out Rect area))
                area = new Rect(0f, 0f, 1f, 1f);
            if (area == _lastArea && screen == _lastScreen)
                return;
            _lastArea = area;
            _lastScreen = screen;
            Fit(area, screen);
        }

        // Works in screen pixels: k is how many fit per world unit, so the board's height and width both
        // fit the area and the tighter one wins. The camera then looks at the point that puts the board
        // in the middle of the area (the area's center need not be the screen's).
        private void Fit(Rect area, Vector2Int screen)
        {
            float areaWidth = area.width * screen.x;
            float areaHeight = area.height * screen.y;
            float pixelsPerUnit = Mathf.Min(areaWidth / _boardSize.x, areaHeight / _boardSize.y);

            float centerX = (area.x + area.width * 0.5f) * screen.x;
            // The panel's origin is top-left, the screen's is bottom-left.
            float centerY = (1f - area.y - area.height * 0.5f) * screen.y;
            float offsetX = (centerX - screen.x * 0.5f) / pixelsPerUnit;
            float offsetY = (centerY - screen.y * 0.5f) / pixelsPerUnit;

            // Never show beyond the table sprite behind the board: cap the size, then redo the offsets.
            float size = screen.y / (2f * pixelsPerUnit);
            float maxSize = Mathf.Min(_tableHalfSize.y, _tableHalfSize.x * screen.y / screen.x);
            if (size > maxSize)
            {
                size = maxSize;
                pixelsPerUnit = screen.y / (2f * size);
                offsetX = (centerX - screen.x * 0.5f) / pixelsPerUnit;
                offsetY = (centerY - screen.y * 0.5f) / pixelsPerUnit;
            }
            _camera.orthographicSize = size;
            Vector3 position = transform.localPosition;
            position.x = -offsetX;
            position.y = -offsetY;
            if (_shake != null)
                _shake.SetRestPosition(position);
            else
                transform.localPosition = position;
        }
    }
}

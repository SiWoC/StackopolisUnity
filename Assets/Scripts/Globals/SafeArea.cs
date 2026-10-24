using UnityEngine;

namespace SiWoC.Stackopolis.Globals
{
    /// <summary>
    /// Fits this RectTransform to <see cref="Screen.safeArea"/>.
    /// Use a full-stretch child under a Screen Space Canvas; put edge UI under it
    /// so controls stay clear of notches and punch-holes.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeArea : MonoBehaviour
    {
        RectTransform rectTransform;
        Rect lastSafeArea;
        Vector2Int lastScreenSize;
        ScreenOrientation lastOrientation;

        void Awake()
        {
            rectTransform = (RectTransform)transform;
            Apply();
        }

        void OnEnable() => Apply();

        void Update()
        {
            if (Screen.safeArea != lastSafeArea
                || Screen.width != lastScreenSize.x
                || Screen.height != lastScreenSize.y
                || Screen.orientation != lastOrientation)
            {
                Apply();
            }
        }

        void Apply()
        {
            Rect safe = Screen.safeArea;
            lastSafeArea = safe;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastOrientation = Screen.orientation;

            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            rectTransform.anchorMin = min;
            rectTransform.anchorMax = max;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}

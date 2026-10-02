using UnityEngine;

namespace GameOfLife.UI
{
    /// <summary>Fits its RectTransform inside the device safe area so content avoids notches; its parent must fill the screen.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform fittedRectTransform;
        private Rect appliedSafeArea;
        private Vector2Int appliedScreenSize;

        /// <summary>Fits to the safe area as soon as the object loads.</summary>
        private void Awake()
        {
            fittedRectTransform = (RectTransform)transform;
            FitToSafeArea();
        }

        /// <summary>Refits when the canvas resizes, which happens if the screen size or safe area changes.</summary>
        private void OnRectTransformDimensionsChange()
        {
            if (fittedRectTransform)
            {
                FitToSafeArea();
            }
        }

        /// <summary>Sets the anchors to the safe area as a fraction of the screen, skipping the work when nothing changed.</summary>
        private void FitToSafeArea()
        {
            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var isUnchanged = safeArea == appliedSafeArea && screenSize == appliedScreenSize;
            var isScreenSizeUnknown = screenSize.x <= 0 || screenSize.y <= 0;
            if (isUnchanged || isScreenSizeUnknown)
            {
                return;
            }

            appliedSafeArea = safeArea;
            appliedScreenSize = screenSize;
            fittedRectTransform.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
            fittedRectTransform.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
            fittedRectTransform.offsetMin = Vector2.zero;
            fittedRectTransform.offsetMax = Vector2.zero;
        }
    }
}

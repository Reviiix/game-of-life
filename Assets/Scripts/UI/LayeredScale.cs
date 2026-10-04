using UnityEngine;

namespace GameOfLife.UI
{
    /// <summary>Multiplies together the scales set by separate animations, such as a screen revealing its items and a button being pressed, so each owns one factor and neither overwrites the other.</summary>
    [DisallowMultipleComponent]
    public sealed class LayeredScale : MonoBehaviour
    {
        private float revealScale = 1f;
        private float pressScale = 1f;

        /// <summary>Returns the LayeredScale on a GameObject, adding one if it has none; call during set-up, not every frame.</summary>
        public static LayeredScale GetOrAdd(GameObject target)
        {
            var layeredScale = target.GetComponent<LayeredScale>();
            return layeredScale ? layeredScale : target.AddComponent<LayeredScale>();
        }

        /// <summary>Sets the factor owned by a screen's show transition.</summary>
        public void SetRevealScale(float scale)
        {
            revealScale = scale;
            ApplyScale();
        }

        /// <summary>Sets the factor owned by a button's press animation.</summary>
        public void SetPressScale(float scale)
        {
            pressScale = scale;
            ApplyScale();
        }

        /// <summary>Writes the combined scale to the transform.</summary>
        private void ApplyScale()
        {
            transform.localScale = Vector3.one * (revealScale * pressScale);
        }
    }
}

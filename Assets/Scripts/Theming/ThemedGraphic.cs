using GameOfLife.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameOfLife.Theming
{
    /// <summary>Colours an Image, RawImage or text with a ThemeColour role and keeps it in step with light and dark mode.</summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class ThemedGraphic : MonoBehaviour
    {
        [SerializeField] private ThemeColour colour = ThemeColour.Ink;
        [Tooltip("Multiplies the palette colour's alpha, for faint versions of a role.")]
        [SerializeField, Range(0f, 1f)] private float opacity = 1f;

        private Graphic themedGraphic;
        private ThemeService themeService;

        /// <summary>Registers with the ThemeService, which paints this graphic straight away.</summary>
        private void OnEnable()
        {
            if (!themedGraphic)
            {
                themedGraphic = GetComponent<Graphic>();
                themeService = ServiceLocator.Get<ThemeService>();
            }

            themeService.Register(this);
        }

        /// <summary>Stops following palette changes while disabled; OnEnable repaints it.</summary>
        private void OnDisable()
        {
            themeService?.Unregister(this);
        }

        /// <summary>Switches this graphic to another role, such as a timer turning to the danger colour.</summary>
        public void SetColour(ThemeColour newColour)
        {
            colour = newColour;
            if (themeService != null)
            {
                ApplyPalette(themeService.Palette);
            }
        }

        /// <summary>Paints the graphic with its role's colour from the palette.</summary>
        public void ApplyPalette(ThemePalette palette)
        {
            var paletteColour = palette.Get(colour);
            paletteColour.a *= opacity;
            themedGraphic.color = paletteColour;
        }
    }
}

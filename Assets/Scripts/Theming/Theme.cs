using DG.Tweening;
using GameOfLife.Motion;
using UnityEngine;

namespace GameOfLife.Theming
{
    /// <summary>Designer-editable look and feel: the light and dark palettes, the shape of the board's tiles and the timing and easing of every animation.</summary>
    [CreateAssetMenu(fileName = "Theme", menuName = "Game of Life/Theme")]
    public sealed class Theme : ScriptableObject
    {
        [Header("Palettes")]
        [SerializeField] private ThemePalette lightPalette = new();
        [SerializeField] private ThemePalette darkPalette = new();

        [Header("Board Tiles")]
        [Tooltip("Gap between tiles, as a fraction of the shorter side of a cell.")]
        [SerializeField, Range(0f, 0.4f)] private float tileGap = 0.14f;
        [Tooltip("How round the tile corners are: 0 is square and 1 is fully round.")]
        [SerializeField, Range(0f, 1f)] private float tileRoundness = 0.45f;

        [Header("Cell Motion")]
        [Tooltip("A cell coming alive or being placed grows from nothing.")]
        [SerializeField] private EasedMotion cellAppear = new(0.32f, Ease.OutBack);
        [Tooltip("A dying cell shrinks away.")]
        [SerializeField] private EasedMotion cellDisappear = new(0.24f, Ease.InBack);
        [Tooltip("An absorbed cell squashes, changes colour and springs back.")]
        [SerializeField] private EasedMotion cellConvert = new(0.6f, Ease.OutElastic, 1f, 0.2f);
        [Tooltip("How small an absorbed cell squashes before springing back.")]
        [SerializeField, Range(0.1f, 1f)] private float convertSquashScale = 0.5f;
        [Tooltip("Share of the absorb animation spent changing colour.")]
        [SerializeField, Range(0.05f, 1f)] private float convertColourShare = 0.3f;
        [Tooltip("Cell animations never last longer than this share of the time between generations, so fast games stay readable.")]
        [SerializeField, Range(0.1f, 1f)] private float cellMotionShareOfGeneration = 0.9f;

        [Header("Interface Motion")]
        [Tooltip("Screens and dialogs fade in while their content pops up to full size.")]
        [SerializeField] private EasedMotion screenShow = new(0.4f, Ease.OutBack, 1.4f);
        [SerializeField, Range(0.5f, 1f)] private float screenShowStartScale = 0.94f;
        [Tooltip("Seconds between each listed item popping in when a screen opens.")]
        [SerializeField, Min(0f)] private float screenStaggerDelay = 0.045f;
        [SerializeField] private EasedMotion screenHide = new(0.14f, Ease.InQuad);
        [Tooltip("A button squeezes while held down.")]
        [SerializeField] private EasedMotion buttonSqueeze = new(0.09f, Ease.OutQuad);
        [SerializeField, Range(0.5f, 1f)] private float buttonPressedScale = 0.92f;
        [Tooltip("A button springs back when released; its action runs once the spring settles.")]
        [SerializeField] private EasedMotion buttonRelease = new(0.45f, Ease.OutElastic, 3f, 0.18f);
        [Tooltip("A switch's knob slides across.")]
        [SerializeField] private EasedMotion switchSlide = new(0.28f, Ease.OutBack);
        [Tooltip("Countdown numbers and changing scores pop.")]
        [SerializeField] private EasedMotion labelPop = new(0.35f, Ease.OutBack, 2.5f);
        [SerializeField, Range(1f, 2f)] private float labelPopScale = 1.3f;
        [Tooltip("Colours blend when dark mode is switched on or off.")]
        [SerializeField] private EasedMotion paletteCrossfade = new(0.35f, Ease.InOutSine);
        [Tooltip("The screen fades in from black when the game starts.")]
        [SerializeField] private EasedMotion startupFade = new(1f, Ease.InOutSine);

        public ThemePalette LightPalette => lightPalette;
        public float TileGap => tileGap;
        public float TileRoundness => tileRoundness;
        public EasedMotion CellAppear => cellAppear;
        public EasedMotion CellDisappear => cellDisappear;
        public EasedMotion CellConvert => cellConvert;
        public float ConvertSquashScale => convertSquashScale;
        public float ConvertColourShare => convertColourShare;
        public float CellMotionShareOfGeneration => cellMotionShareOfGeneration;
        public EasedMotion ScreenShow => screenShow;
        public float ScreenShowStartScale => screenShowStartScale;
        public float ScreenStaggerDelay => screenStaggerDelay;
        public EasedMotion ScreenHide => screenHide;
        public EasedMotion ButtonSqueeze => buttonSqueeze;
        public float ButtonPressedScale => buttonPressedScale;
        public EasedMotion ButtonRelease => buttonRelease;
        public EasedMotion SwitchSlide => switchSlide;
        public EasedMotion LabelPop => labelPop;
        public float LabelPopScale => labelPopScale;
        public EasedMotion PaletteCrossfade => paletteCrossfade;
        public EasedMotion StartupFade => startupFade;

        /// <summary>Returns the light or dark palette.</summary>
        public ThemePalette GetPalette(bool darkMode)
        {
            return darkMode ? darkPalette : lightPalette;
        }
    }
}

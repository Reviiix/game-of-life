# Conway's Game of Life

A portrait mobile (Android and iOS) version of Conway's Game of Life, built with Unity 6000.3 and URP. Tap cells to bring them to life, press play, and watch them evolve.

## Getting started

1. Open the project with Unity **6000.3.9f1** with the Android and/or iOS modules installed.
2. Open `Assets/Scenes/Game.unity`. It's the only scene, and it's already in Build Settings.
3. Press Play. Set the Game view to a portrait resolution, such as 1080×2280.

Most tuning needs no code: select `Assets/Configuration/GameSettings.asset` and edit it in the Inspector.

## Project layout

| Folder | Contents |
|---|---|
| `Art/` | Fonts, sprites (`Sprites/Icons.png`), the grid shader and its material |
| `Configuration/` | `GameSettings.asset`, the single source of tunable values |
| `Prefabs/UI/` | Buttons, dialogs and settings rows (see [Prefabs](#prefabs)) |
| `Rendering/` | URP pipeline, renderer and volume assets |
| `Scenes/` | `Game.unity` |
| `Scripts/` | All game code, compiled into the `GameOfLife` assembly |
| `Libraries/pure-unity-methods/` | Shared utility library. **The game does not use it.** It is Editor-only and opt-in, ready to be extracted |
| `TextMesh Pro/` | TMP resources and shaders |

### Scripts (`GameOfLife.*` namespaces)

| Namespace / folder | Responsibility |
|---|---|
| `Core` | `GameInitialiser` (entry point), `ServiceLocator`, `RandomColour` |
| `Configuration` | `GameSettings` ScriptableObject |
| `Simulation` | `CellGrid`, pure C# Conway rules with no Unity dependencies |
| `Grid` | `GridView` draws the grid; `GridTouchInput` turns taps into cell indices |
| `Gameplay` | `GameController` (the core loop), `CountdownDisplay`, `GamePhase` |
| `UI.Screens` | `ScreenPanel`, `ScreenNavigator`, `SettingsPanel` |
| `UI.Buttons` | `AnimatedButton` and one small subclass per button action |
| `UI` | `SafeAreaFitter`, `VersionLabel` |
| `Effects` | `ScreenFader` (start-up fade from black) |

## Scene hierarchy

```
Systems              GameInitialiser, GameController, ScreenNavigator, EventSystem, MainCamera
GameScreen           Canvas 0: Grid (full screen) + SafeArea/ButtonBar + SafeArea/CountdownLabel
MainMenuScreen       Canvas 1: title, Play / Settings buttons, version label
SettingsScreen       Canvas 3: SettingsDialog prefab variant
InvalidGameScreen    Canvas 4: InvalidGameDialog prefab variant
ScreenFade           Canvas 100: black overlay that fades out at start-up
```

Controls and text sit under a `SafeArea` object so they avoid notches. Full-screen backgrounds and the grid sit outside it.

## How it works

### Start-up sequence

`GameInitialiser` is the only entry point. It runs before every other script (`DefaultExecutionOrder(-1000)`):

1. **Awake:** sets the target frame rate and registers the services: `GameSettings`, `GameController` and `ScreenNavigator`.
2. **Start:** initialises the systems in dependency order: screens, game controller (allocates the simulation and grid texture at maximum size), settings panel, play/pause button.
3. Waits one frame so load hitches don't eat into the fade, then fades in from black.

To add a system, give it an `Initialise(...)` method and call it from `GameInitialiser.InitialiseSystems()` in the right order. Avoid doing setup work in its own `Start()`.

### Services

`ServiceLocator` holds exactly one instance per type. Registering a type twice logs an error, and getting a type that isn't registered throws a clear exception.

- **Register** in `GameInitialiser.RegisterServices()`.
- **Use** with `ServiceLocator.Get<GameController>()`. This is how prefab buttons reach scene systems without scene references.
- **Which style to use:** anything that subscribes to events or needs set-up at start-up gets its dependencies through `Initialise(...)`, called by `GameInitialiser`. Fire-and-forget actions, such as a button press, call `ServiceLocator.Get` when they run.

### Core loop (`GameController`)

`Editing` → (play) → `CountingDown` → `Running` → (pause) → `Editing`

- Taps toggle cells in every phase except `CountingDown`.
- If no cell is alive when the countdown ends, the "populate at least one cell" dialog opens.
- Generations run in a coroutine: advance, wait `delayBetweenGenerations`, repeat. There is no `Update()` anywhere in the game.
- `CellGrid` works out the whole next generation before applying it (true Conway rules), and reports only the cells that changed.

### Grid rendering (`GridView` + `GridCells.shader`)

The whole grid is **one UI quad**:
- A texture holds one pixel per cell, with point filtering.
- The shader draws the lines between cells.
- The grid size and line thickness travel in spare UV channels, so no per-instance material is needed. This is why `GameScreen`'s Canvas has **TexCoord1 and TexCoord2** enabled under Additional Shader Channels.

A generation updates only the changed pixels and uploads the texture once. No UI meshes are rebuilt. In Edit mode the grid shows a preview using `GameSettings` values, and nothing is saved into the scene.

### Screens

Each screen root has a `ScreenPanel`. Hiding a screen disables its Canvas and GraphicRaycaster, so hidden screens cost nothing to draw or hit-test. Disabling the Canvas is cheaper than deactivating the GameObject. `ScreenNavigator` owns which screens show at start-up and how they relate (Settings replaces the main menu, and closing it brings the menu back).

## Prefabs

| Prefab | Base | Notes |
|---|---|---|
| `Buttons/IconButton` | — | Image + Button. Change shared icon-button styling here |
| `Buttons/MenuButton`, `PlayPauseButton`, `ResetButton` | IconButton | Each adds its behaviour component and icon |
| `Buttons/TextButton` | — | TMP text + Button in the title font |
| `Buttons/PlayButton`, `SettingsButton` | TextButton | Each adds its behaviour, text and tilt |
| `Dialogs/Dialog` | — | Canvas, scaler, safe area, background and a working close button |
| `Dialogs/SettingsDialog`, `InvalidGameDialog` | Dialog | Content plus `SettingsPanel` for settings |
| `Settings/SliderSetting`, `ToggleSetting` | — | Label + control rows; instances override the label text |
| `Settings/Slider` | — | Nested inside SliderSetting |

Edit a **base** to change every button or dialog at once. Edit a **variant** for one specific button or dialog.

## Common tasks

- **Change a default, a colour, a timing or a grid limit:** edit `GameSettings.asset`.
- **Add a button:** create a variant of `IconButton` or `TextButton`. Write a subclass of `AnimatedButton` that implements `OnPressed()`, and get the services it needs from `ServiceLocator`. Add that component to the variant.
- **Add a dialog:** create a variant of `Dialog`, add it to the scene with a sort order above the screens it covers, and expose it from `ScreenNavigator`. The close button already works.
- **Add a setting:** drop a `SliderSetting` or `ToggleSetting` into `SettingsDialog/.../SettingsList`. Add a serialized field and a listener in `SettingsPanel`, and a default in `GameSettings`.
- **Change the rules:** edit `CellGrid.WillBeAliveNextGeneration`.

## Coding standards

Follow these for all new work:

1. Reach systems through `ServiceLocator`; one instance per service type.
2. Initialise from `GameInitialiser` in an explicit order. No hidden setup in `Start()`.
3. Cache references and precompute at start-up. A longer start-up is fine if it saves work at runtime.
4. Pool and reuse: allocate buffers once at their maximum size, and never allocate per frame or per generation.
5. Use prefabs, and variants for anything that shares a base.
6. Use self-documenting names. Every class and method gets a **one-line** `/// <summary>`; other comments are rare.
7. Keep everything `private` by default. Use `[SerializeField]` only for values a designer should change; read-only access goes through properties.
8. Put code in the `GameOfLife.<Area>` namespace that matches its folder.
9. Use `StringBuilder` (or precomputed strings) when building strings.
10. Avoid `Update()`. Use events, or coroutines that run only while needed, but never at the cost of clarity or stability.
11. Cull hidden UI by disabling Canvas + GraphicRaycaster. Turn off `raycastTarget` on graphics that don't take input.
12. Import sprites at their native size (max size ≥ source), with no mipmaps for UI, and use ASTC 4×4 on mobile.
13. Delete unused code and assets instead of leaving them in the project.

## Mobile settings already applied

- Portrait only; Android ARMv7 + ARM64 (IL2CPP); accelerometer off; target frame rate from `GameSettings` (60).
- URP: HDR, MSAA, shadows, additional lights and LOD cross-fade off. The camera renders no layers (the UI is overlay).
- Title font baked to a static atlas, so no glyphs are generated at runtime.

## Before shipping

- Set the company name, product name and bundle identifier in Player Settings. They're still Unity's defaults.
- Add app icons and splash settings.

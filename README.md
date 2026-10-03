# Conway's Game of Life

A portrait mobile (Android and iOS) version of Conway's Game of Life, built with Unity 6000.3 and URP. There are two modes on the main menu:

- **Classic:** tap cells to bring them to life, press play, and watch them evolve.
- **Versus:** you (blue) against the app (red). Take turns placing squares on your own half, then a timed match runs. Newborn cells take their parents' majority colour. When clusters of different colours touch, the bigger cluster converts the smaller one. Whoever has the most squares when time runs out wins.

## Getting started

1. Open the project with Unity **6000.3.9f1** with the Android and/or iOS modules installed.
2. Open `Assets/Scenes/Game.unity`. It's the only scene, and it's already in Build Settings.
3. Press Play. Set the Game view to a portrait resolution, such as 1080×2280.

Most tuning needs no code: select `Assets/Configuration/GameSettings.asset` and edit it in the Inspector.

## Project layout

| Folder | Contents |
|---|---|
| `Art/` | Fonts, sprites (`Sprites/Icons.png`), audio (`Audio/SoundEffects`, `Audio/Music`), the grid shader and its material |
| `Configuration/` | `GameSettings.asset` (tunable values) and `SoundLibrary.asset` (clips, volumes and repeat limits) |
| `Prefabs/UI/` | Buttons, dialogs and settings rows (see [Prefabs](#prefabs)) |
| `Rendering/` | URP pipeline, renderer and volume assets |
| `Scenes/` | `Game.unity` |
| `Scripts/` | All game code, compiled into the `GameOfLife` assembly |
| `../Tools/Audio/` | `generate_sounds.py`, which synthesises every sound and the music loop. Edit it and run `python3 Tools/Audio/generate_sounds.py` to regenerate |
| `Tests/EditMode/` | NUnit tests for the pure C# engine (Window → General → Test Runner) |
| `Libraries/pure-unity-methods/` | Shared utility library. **The game does not use it.** It is Editor-only and opt-in, ready to be extracted |
| `TextMesh Pro/` | TMP resources and shaders |

### Scripts (`GameOfLife.*` namespaces)

| Namespace / folder | Responsibility |
|---|---|
| `Core` | `GameInitialiser` (entry point), `ServiceLocator`, `RandomColour` |
| `Configuration` | `GameSettings` ScriptableObject |
| `Simulation` | `CellGrid` (two-colour Conway rules), `CellOwner`, `ClusterAbsorber`. Pure C#, no Unity |
| `Opponents` | The app's placement AI: `RandomOpponentStrategy` (Easy), `StrategicOpponentStrategy` (Hard). Pure C# |
| `Versus` | `VersusMatch`: the rules of one match (turns, pools, regions, early end, outcome). Pure C# |
| `Grid` | `GridView` draws the grid; `GridTouchInput` turns taps into cell indices |
| `Gameplay` | `GameModeDirector` (keeps one mode active), `GameOptions` (the player's Settings choices), `CountdownDisplay` |
| `Gameplay.Modes` | `GameModeBase`, `ClassicGameMode`, `VersusGameMode` (timing, turns, pause, result) |
| `UI.Versus` | `VersusHud` (score, timer, status, setup shading), `MatchResultPanel` |
| `UI.Screens` | `ScreenPanel`, `ScreenNavigator`, `SettingsPanel`, `SliderSetting` |
| `UI.Buttons` | `AnimatedButton` and one small subclass per button action |
| `UI` | `SafeAreaFitter`, `VersionLabel` |
| `Effects` | `ScreenFader` (start-up fade from black) |
| `Purchasing` | `PurchaseManager` (Unity IAP v5 store connection, buying and restoring the ad pass), `AdPassOwnership` (saved entitlement) |
| `UI.Store` | `AdPassButton`, `RestorePurchasesButton`, `PurchaseFeedback` |
| `Audio` | `AudioManager` (music plus a pool of reused effect sources), `SoundLibrary`, `SoundEffect`, `SoundEffectSettings` |

## Scene hierarchy

```
Systems              GameInitialiser, GameModeDirector (ClassicMode, VersusMode), ScreenNavigator, EventSystem, MainCamera
GameScreen           Canvas 0: Grid + OpponentHalfShade (full screen), SafeArea/ButtonBar, CountdownLabel, VersusHud
MainMenuScreen       Canvas 1: title, Classic / Versus / Settings buttons, version label
SettingsScreen       Canvas 3: SettingsDialog prefab variant (scrolls)
InvalidGameScreen    Canvas 4: InvalidGameDialog prefab variant
MatchResultScreen    Canvas 5: MatchResultDialog prefab variant
MessageScreen        Canvas 6: MessageDialog prefab variant (purchase results)
ScreenFade           Canvas 100: black overlay that fades out at start-up
```

Controls and text sit under a `SafeArea` object so they avoid notches. Full-screen backgrounds and the Classic grid sit outside it. In Versus the grid moves into `SafeArea/VersusBoardArea`, between the HUD and the button bar, so the player can reach every cell. It moves back when Versus exits. The camera clears to white so the bars look the same in both layouts.

## How it works

### Start-up sequence

`GameInitialiser` is the only entry point. It runs before every other script (`DefaultExecutionOrder(-1000)`):

1. **Awake:** sets the target frame rate, creates `GameOptions`, and registers the services: `GameSettings`, `GameModeDirector` and `ScreenNavigator`. `GameOptions` is passed to the systems that need it through `Initialise`.
2. **Start:** initialises the systems in dependency order:
   - screens
   - the game mode director (allocates the board and grid texture, and builds both modes and both AI opponents)
   - the settings panel
   - the play/pause button

   Classic mode starts behind the main menu.
3. Waits one frame so load hitches don't eat into the fade, then fades in from black.

To add a system, give it an `Initialise(...)` method and call it from `GameInitialiser.InitialiseSystems()` in the right order. Avoid doing setup work in its own `Start()`.

### Services

`ServiceLocator` holds exactly one instance per type. Registering a type twice logs an error, and getting a type that isn't registered throws a clear exception.

- **Register** in `GameInitialiser.RegisterServices()`.
- **Use** with `ServiceLocator.Get<GameModeDirector>()`. This is how prefab buttons reach scene systems without scene references.
- **Which style to use:** anything that subscribes to events or needs set-up at start-up gets its dependencies through `Initialise(...)`, called by `GameInitialiser`. Fire-and-forget actions, such as a button press, call `ServiceLocator.Get` when they run.

### Game modes (`GameModeDirector`)

The director owns the single `CellGrid` and keeps exactly one `GameModeBase` active (its `modes` list in the Inspector). Taps, the play/pause and reset buttons, and option changes all go to the active mode.
- **The mode buttons:** pressing CLASSIC or VERSUS on the menu resumes that mode if it's already active, or starts it fresh.
- **The main menu:** opening it pauses a Versus match. Classic keeps running under it. Play/pause and reset presses that finish after the menu has opened are ignored.
- **Colours:** each mode chooses its own cell colours through `GetCellColour`.
- **Repainting:** `RepaintChangedCells()` redraws only the cells in `CellGrid.ChangedCellIndices`. That list accumulates until it's cleared.

**Classic** (`ClassicGameMode`): `Editing` → (play) → `CountingDown` → `Running` → (pause) → `Editing`.
- Taps toggle cells in every phase except `CountingDown`.
- Pressing play on an empty board opens the "populate at least one cell" dialog straight away, without a countdown.

**Versus** (`VersusGameMode`, with the rules in `VersusMatch`). Its phases (`VersusGamePhase`) are `Setup` → `CountingDown` → `Running` ⇄ `Paused` → `Finished`, plus `WaitingToStart` if setup finishes while the menu is open.
- **Setup:** you and the app take turns placing on your own halves (yours is the top). The app's half is shaded.
- **Setup changes:** changing the square pools or match length while still in setup restarts setup. Difficulty applies from the app's next move.
- **Match:** a countdown, then one match clock coroutine counts the time down and triggers generations and the app's moves. Remaining times are kept exactly across pauses.
- **App moves:** the app spends its in-match squares at jittered intervals. Each one must touch a living cell, so a square never appears out of nowhere. `VersusMatch.CanPlace` enforces this whatever strategy is used. Your own squares can go on any empty cell. Its move search runs on a worker thread against a board snapshot (`OpponentMoveSearch`), so Hard never stalls a frame.
- **After the match:** Play, or choosing VERSUS from the menu, starts a new match.
- **Absorption** (`ClusterAbsorber`) is resolved after every placement and every generation.
- **Early end:** the match ends early if a side has no squares left on the board and none left to place.

There is no `Update()` anywhere in the game. The only per-frame work is the Versus match clock, which runs only while a match is running.

### Audio (`AudioManager`)

`AudioManager` is a registered service, initialised first by `GameInitialiser`.
- **Music:** one looping source streams `puzzle_loop` and fades in. The MUSIC toggle pauses and resumes it.
- **Effects:** play through a small pool of AudioSources (`SoundLibrary.simultaneousSoundEffects`), reused round-robin, so overlapping sounds never allocate.
- **Saved settings:** the MUSIC and SOUND EFFECTS toggles are saved on the device with `PlayerPrefs`. Other Settings last for the session.
- **Repeat limits:** each effect's `minimumSecondsBetweenPlays` stops rapid events, such as fast generations or slider drags, piling up.
- **Where sounds are triggered:**
  - Buttons click in `AnimatedButton` and `CloseScreenButton`; toggles click in `SettingsPanel`; sliders tick in `SliderSetting`.
  - Placed cells call `PlayNoteForRow`, which retunes `cell_placed` to a C-major pentatonic note rising towards the top row.
  - Generations tick only when a cell changed.
  - Absorptions sound bright when you gain cells and lower when the app does (`VersusMatch.LastAbsorbingSide`).
- **Adding a sound:** add a `SoundEffect` value, give it an entry in `SoundLibrary.asset`, and call `Play`.
- **Import settings:** effects are mono 22.05 kHz, Decompress On Load. Music is streaming with Load In Background. Both use Vorbis on every platform, because Unity 6000.3 offers no AAC option for iOS audio.

### Ad pass (`PurchaseManager` + `AdPassOwnership`)

The £0.99 ad pass is a **non-consumable** product. Its ID, `ad_pass`, is set in `GameSettings`.
- **Start-up:** `PurchaseManager` connects through `UnityIAPServices.StoreController()`, then fetches the product and any existing purchases. Owning the pass on record re-grants it, for example after a reinstall.
- **Buying:** NO ADS on the main menu calls `BuyAdPass`. When the store reports the purchase as pending, `AdPassOwnership.Grant()` saves it with `PlayerPrefs`, and only then is `ConfirmPurchase` called. That way a purchase is never confirmed without being saved, and the store re-delivers any unconfirmed purchase on the next launch.
- **Other outcomes:**
  - Deferred purchases (Ask to Buy) grant nothing until approved.
  - A cancelled purchase shows no message.
  - "Already owned" is treated as a restore: purchases are fetched and the player is thanked once the pass is confirmed.
  - An approved Ask to Buy is thanked when it arrives.
- **Connection:** the store retries lost connections with back-off, and reconnects when the player returns to the app. NO ADS is disabled while disconnected.
  - Apple refunds revoke it.
- **Restore:** RESTORE PURCHASES appears only on iOS, where Apple requires it. Android re-grants the pass automatically at every launch, and the Editor's fake store cannot restore.
- **UI:** the button shows the store's localised price, greys out until the store is ready or while a purchase is in progress, and disappears once the pass is owned. `PurchaseFeedback` reports each result in the message dialog.
- **Ads:** whatever shows ads must check `ServiceLocator.Get<AdPassOwnership>().IsOwned` and listen to `OwnershipChanged`.

**Before release:**
- Create a non-consumable product with ID `ad_pass` at £0.99 in both App Store Connect and Google Play Console.
- Test with sandbox / licence-tester accounts.
- Optionally add Google Play receipt validation: generate the tangle with **Services > In-App Purchasing > Receipt Validation Obfuscator** and validate in `OnPurchasePending` before granting.
- In the Editor, Unity's fake store approves purchases from a small dialog and always shows a $0.01 price.

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
| `Buttons/ClassicButton`, `VersusButton`, `SettingsButton` | TextButton | Each adds its behaviour, text and tilt. The mode buttons set `StartGameModeButton.mode` |
| `Dialogs/Dialog` | — | Canvas, scaler, safe area, background and a working close button |
| `Dialogs/SettingsDialog`, `InvalidGameDialog`, `MatchResultDialog` | Dialog | Content, plus `SettingsPanel` for settings and `MatchResultPanel` for results |
| `Settings/SliderSetting`, `ToggleSetting` | — | Label + control rows. `SliderSetting` shows its value; set `valueFormat` per instance |
| `Settings/Slider` | — | Nested inside SliderSetting |

Edit a **base** to change every button or dialog at once. Edit a **variant** for one specific button or dialog.

## Common tasks

- **Change a default, a colour, a timing or a grid limit:** edit `GameSettings.asset`.
- **Add a button:** create a variant of `IconButton` or `TextButton`. Write a subclass of `AnimatedButton` that implements `OnPressed()`, and get the services it needs from `ServiceLocator`. Add that component to the variant.
- **Add a dialog:** create a variant of `Dialog`, add it to the scene with a sort order above the screens it covers, and expose it from `ScreenNavigator`. The close button already works.
- **Add a setting:** drop a `SliderSetting` or `ToggleSetting` into `SettingsDialog/.../SettingsList`. Add a serialized field and a listener in `SettingsPanel`, and a default in `GameSettings`.
- **Change the Conway rules:** edit `CellGrid`. Survival and birth are in its rule method; newborn colour is the parents' majority.
- **Change absorption:** edit `ClusterAbsorber`.
- **Tune the app's Hard play:** edit the weights and candidate cap constants in `StrategicOpponentStrategy`. Its turn delay, colours, pools and match length are in `GameSettings`.
- **Add a game mode:**
  1. Subclass `GameModeBase` and return the new type from `ModeType`.
  2. Add a value to `GameModeType`.
  3. Add the component under `Systems/GameModeDirector` and drag it into the director's `modes` list.
  4. Make a `TextButton` variant with `StartGameModeButton` set to the new mode.
- **Run the tests:** open Window → General → Test Runner → EditMode → Run All. The engine is plain C#, so the tests need no scene.

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

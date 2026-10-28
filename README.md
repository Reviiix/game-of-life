# Conway's Game of Life

A portrait mobile (Android and iOS) version of Conway's Game of Life, built with Unity 6000.3 and URP. Cells are rounded tiles that pop, shrink and bounce as they change, the whole interface follows one minimal light or dark theme, and absorptions burst into particles. There are two modes on the main menu:

- **Classic:** tap cells to bring them to life, press play, and watch them evolve. The colour button picks ink, blue, red or yellow; colours that meet behave as in Versus, so newborns take their parents' colour and bigger clusters absorb smaller ones.
- **Versus:** you (blue) against the app (red). You both place squares on your own halves at the same time against the setup clock (30 seconds by default), then a timed match runs. Newborn cells take their parents' majority colour. When clusters of different colours touch, the bigger cluster converts the smaller one. Whoever has the most squares when time runs out wins.

## Getting started

1. Open the project with Unity **6000.3.9f1** with the Android and/or iOS modules installed.
2. Open `Assets/Scenes/Game.unity`. It's the only scene, and it's already in Build Settings.
3. Press Play. Set the Game view to a portrait resolution, such as 1080×2280.

Most tuning needs no code:
- `Assets/Configuration/GameSettings.asset` holds grid limits, starting options, timings and the store product.
- `Assets/Configuration/Theme.asset` holds the light and dark palettes, the tile shape, and the duration and easing of every animation.

## Project layout

| Folder | Contents |
|---|---|
| `Art/` | Fonts (`Fonts/SpaceGrotesk`: the regular and bold TTFs, their TextMesh Pro assets and the font licence, `OFL.txt`), the interface sprite sheet (`Sprites/Interface.png`), audio (`Audio/SoundEffects`, `Audio/Music`), the grid shader and the grid and particle materials |
| `Configuration/` | `GameSettings.asset` (tunable values), `Theme.asset` (palettes, tile shape, animation), `SoundLibrary.asset` (clips, volumes and repeat limits) and `HowToPlayGuide.asset` (the how-to-play steps and their demo boards) |
| `Plugins/Demigiant/DOTween/` | DOTween 1.2.705 (free) for every animation. The game uses only the core `DOTween.dll`; every optional module is switched off, and DOTween's own helper script compiles into its `DOTween.Modules` assembly. Its settings live in `Resources/DOTweenSettings.asset` beside it |
| `Prefabs/UI/` | Buttons, dialogs and settings rows (see [Prefabs](#prefabs)) |
| `Prefabs/Effects/` | `AbsorptionEffect`, the absorption particles both modes use |
| `Rendering/` | URP pipeline, renderer and volume assets |
| `Scenes/` | `Game.unity` |
| `Scripts/` | All game code, compiled into the `GameOfLife` assembly |
| `../Tools/Audio/` | `generate_sounds.py`, which synthesises every sound and the music loop. Edit it and run `python3 Tools/Audio/generate_sounds.py` to regenerate |
| `../Tools/Art/` | `generate_ui_sprites.py`, which draws the rounded panel, circle, particle tile and icons into `Interface.png`. Run `python3 Tools/Art/generate_ui_sprites.py` to regenerate; the slicing lives in the texture's import settings |
| `Tests/EditMode/` | NUnit tests for the pure C# engine (Window → General → Test Runner) |
| `Libraries/pure-unity-methods/` | Shared utility library. **The game does not use it.** It is Editor-only and opt-in, ready to be extracted |
| `TextMesh Pro/` | TMP resources and shaders |

### Scripts (`GameOfLife.*` namespaces)

| Namespace / folder | Responsibility |
|---|---|
| `Core` | `GameInitialiser` (entry point), `ServiceLocator`, `RandomColour` |
| `Configuration` | `GameSettings` ScriptableObject |
| `Simulation` | `CellGrid` (Conway's rules in up to four colours), `CellOwner`, `ClusterAbsorber`. Pure C#, no Unity |
| `Opponents` | The app's placement AI: `RandomOpponentStrategy` (Easy), `SurvivalOpponentStrategy` (Medium and Hard, tuned by `OpponentSkill`) with its helpers `PlacementSurvey` and `BlockSites`, and `SetupSavingJudge` (whether to end setup early too). Pure C# |
| `Versus` | `VersusMatch`: the rules of one match (setup, pools, regions, early end, running out of setup time, outcome). `OpponentMovePacing`: when the app places its next square. Pure C# |
| `Grid` | `GridView` draws the board, `CellAnimator` animates changing cells, `CellPaint` names what a cell is drawn as, `ClassicCellColours` lists Classic's colours and how each is drawn, `FreshCellTracker` counts down the generations a cell placed mid-game stays lighter, `GridTouchInput` turns taps, presses and drags into cell indices, `PatternGhostView` draws a pattern's ghost with a second `GridView` |
| `Theming` | `Theme` ScriptableObject, `ThemePalette`, `ThemeColour` roles, `ThemeService` (current palette and dark-mode blend), `ThemedGraphic` |
| `Motion` | `EasedMotion` (a duration plus a DOTween ease, editable in the Inspector), `ReplayableTween` (a tween built once and replayed) and `FrameTicker` (a per-frame callback on DOTween's update, built once) |
| `Gameplay` | `GameModeDirector` (keeps one mode active), `PatternPlacer` (choosing and positioning a catalogue pattern), `GameOptions` (the player's Settings choices), `CountdownDisplay` |
| `Gameplay.Modes` | `GameModeBase`, `ClassicGameMode`, `VersusGameMode` (setup and match clocks, pause, result) |
| `Patterns` | The pattern catalogue: `PatternCatalogue` asset type, `CellPattern` and `PatternOrientation` (every rotation and mirror built once), `PatternPose`, `PlacementBounds`, `PatternUnavailableReason`, `IPatternPlacementRules`, `PatternPlacementSession` (the positioning rules) and `RandomPlacementPlanner` (chooses the cells for RANDOM CELLS) and `PulsatingFormationPicker` (the pattern behind the main menu at launch). Pure C# apart from the asset type |
| `UI.Patterns` | `PatternsPanel` (the catalogue dialog), `PatternRow`, `PatternThumbnails`, `PlacementBar`, `PlacementHintLabel` |
| `HowToPlay` | The guide's content: `HowToPlayGuide` asset type, `GuideStep`, `GuideDemo`, `GuideDemoStep`, `DemoStepKind`, and `DemoFrameBuilder`, which plays a demo through the real engine into frames. Pure C# apart from the asset type |
| `UI.HowToPlay` | `HowToPlayPanel` (the guide dialog and its tabs), `GuideStepView`, `DemoBoardView` (a looping mini board drawn by a `GridView`), `FirstRunGuide` (opens the guide on the first launch) |
| `UI.Versus` | `VersusHud` (score, timer, status, setup shading), `MatchResultPanel`, `EarlyStartPanel` |
| `UI.Screens` | `ScreenPanel` (show and hide transitions), `ScreenNavigator`, `SettingsPanel`, `SliderSetting`, `ToggleSwitch` |
| `UI.Buttons` | `AnimatedButton` and one small subclass per button action; `ActionButton` raises an event so a dialog's owner decides what it does |
| `UI` | `SafeAreaFitter`, `VersionLabel`, `LabelPop`, `LayeredScale` |
| `Effects` | `ScreenFader` (start-up fade from black), `AbsorptionEffect` (absorption particles in both modes) |
| `Ads` | `AdManager` (LevelPlay interstitials and when they show), `AdSchedule` (the timing rules, plain C#), `LevelPlayInitialiser`, `AdSettings`, `IAdBreaks` |
| `Purchasing` | `PurchaseManager` (Unity IAP v5 store connection, buying and restoring the ad pass), `AdPassOwnership` (saved entitlement) |
| `UI.Store` | `AdPassButton`, `RestorePurchasesButton`, `PurchaseFeedback` |
| `Audio` | `AudioManager` (music plus a pool of reused effect sources), `SoundLibrary`, `SoundEffect`, `SoundEffectSettings` |

## Scene hierarchy

```
Systems              GameInitialiser, GameModeDirector (ClassicMode, VersusMode, PatternPlacer), ScreenNavigator, AdManager (LevelPlayInitialiser + AdManager), EventSystem, MainCamera
GameScreen           Canvas 0 (Screen Space - Camera): Grid (with AbsorptionEffect and PatternGhost) + OpponentHalfShade, SafeArea/GameControls (ButtonBar dock, PlacementBar, PlacementHint, CountdownLabel, VersusHud)
MainMenuScreen       Canvas 1: title, Classic / Versus / How To Play / Settings / ad pass buttons, version label
PatternsScreen       Canvas 2: PatternsDialog prefab variant (the scrollable pattern catalogue)
SettingsScreen       Canvas 3: SettingsDialog prefab variant (scrolls)
InvalidGameScreen    Canvas 4: InvalidGameDialog prefab variant
MatchResultScreen    Canvas 5: MatchResultDialog prefab variant
MessageScreen        Canvas 6: MessageDialog prefab variant (purchase results, Versus setup warnings)
EarlyStartScreen     Canvas 7: EarlyStartDialog prefab variant (confirm starting Versus early)
HowToPlayScreen      Canvas 8: HowToPlayDialog prefab variant (the rules guide, with Classic and Versus tabs)
ScreenFade           Canvas 100: black overlay that fades out at start-up
```

Controls and text sit under a `SafeArea` object so they avoid notches. Full-screen backgrounds and the Classic grid sit outside it. In Versus the grid moves into `SafeArea/VersusBoardArea`, between the HUD and the button bar, so the player can reach every cell. It moves back when Versus exits.

`GameScreen` renders through `MainCamera` (Screen Space - Camera, UI layer only) so the absorption particles, which are ordinary particle systems, can draw over the board. The camera clears to the theme's background colour. Anything added under `GameScreen` must be on the **UI layer**, especially an object with its own nested Canvas, or the camera culls it. Every other screen is a Screen Space - Overlay canvas, so menus and dialogs always draw above the board and its particles.

## How it works

### Start-up sequence

`GameInitialiser` is the only entry point. It runs before every other script (`DefaultExecutionOrder(-1000)`):

1. **Awake:** sets the target frame rate, initialises DOTween (`DOTween.Init` with safe mode and preallocated capacity), creates `GameOptions` and the `ThemeService`, and registers the services, including `GameSettings` and `ThemeService`. `GameOptions` is passed to the systems that need it through `Initialise`.
2. **Start:** initialises the systems in dependency order:
   - audio and purchases
   - screens (each `ScreenPanel` builds its transition tweens)
   - the game mode director (allocates the board, the grid texture and the cell animator, prepares the pattern placer, which builds the catalogue patterns, the ghost and the catalogue rows, and builds both modes and the AI opponents)
   - the how-to-play guide (builds every step card and plays each demo once into frames), and on the first launch holds ads back
   - the ad manager (starts LevelPlay unless the ad pass is owned)
   - the settings panel (which initialises each settings switch)
   - the play/pause and patterns buttons and the store buttons

   Classic mode starts behind the main menu, running a pulsating pattern (see Menu background below). On the very first launch the guide then opens over it (`FirstRunGuide`).
3. Waits one frame so load hitches don't eat into the fade, then fades in from black with the Theme's `startupFade`.

To add a system, give it an `Initialise(...)` method and call it from `GameInitialiser.InitialiseSystems()` in the right order. Avoid doing setup work in its own `Start()`.

### Services

`ServiceLocator` holds exactly one instance per type. Registering a type twice logs an error, and getting a type that isn't registered throws a clear exception.

- **Register** in `GameInitialiser.RegisterServices()`.
- **Use** with `ServiceLocator.Get<GameModeDirector>()`. This is how prefab buttons reach scene systems without scene references.
- **Which style to use:** anything that subscribes to scene-system events or needs ordered set-up gets its dependencies through `Initialise(...)`, called by `GameInitialiser` (or by the system that owns it, as `SettingsPanel` initialises each `ToggleSwitch`). Fire-and-forget actions, such as a button press, call `ServiceLocator.Get` when they run.
- **View components** that only need the theme (`ThemedGraphic`, `AnimatedButton`) get the `ThemeService` from `ServiceLocator` in `OnEnable`/`Awake`. That is safe because `GameInitialiser` registers it in its `Awake`, which runs first (execution order -1000).

### Game modes (`GameModeDirector`)

The director owns the single `CellGrid` and keeps exactly one `GameModeBase` active (its `modes` list in the Inspector). Taps, the play/pause and reset buttons, and option changes all go to the active mode.
- **The mode buttons:** pressing CLASSIC or VERSUS on the menu resumes that mode if it's already active, or starts it fresh.
- **The main menu:** opening it pauses a Versus match. Classic keeps running under it. Play/pause and reset presses that finish after the menu has opened are ignored. The menu's backdrop is the `Background` role at 80% opacity (its `ThemedGraphic` on `MainMenuScreen`), so the board shows through.
- **Menu background:** at launch the director asks Classic to run a catalogue PULSATE pattern behind the menu (`ClassicGameMode.StartMenuShowcase`, phase `Showcase`). `PulsatingFormationPicker` lists every Pulsate pattern that, centred across the board as drawn or turned a quarter, with its middle at `menuFormationVerticalCentre` of the board's height (0.37 in `GameSettings`, the gap between the title and the buttons), changes and returns exactly to its start within 30 generations, so it pulsates forever there; it picks one at random, avoiding the one shown last launch (`PlayerPrefs` key `MenuFormation.Last`) when another fits. It runs in the chosen colour at the evolution interval, silently, and is not a game: it counts as neither simulating nor placing patterns, so ads and the play button ignore it. Choosing CLASSIC clears it for an empty board, choosing VERSUS stops it, and a grid-size change while it shows picks again for the new size. Nothing runs if no Pulsate pattern fits the board.
- **The bottom bar:** MENU, SETTINGS, PLAY, PATTERNS, the colour button (Classic only), RESET. Settings opened from the bar covers the game and returns to it (`GameModeDirector.OpenSettings`, `ScreenNavigator.OpenSettingsMenuOverGame`, `GameModeBase.OnSettingsClosed`): Versus pauses as for the main menu, a finished match is not restarted, and presses that finish while it is open are ignored. Grid-size changes still restart the mode.
- **Patterns:** PATTERNS opens the catalogue (`PatternCatalogue.asset`: 25 patterns under PERSIST, PULSATE, MOVE and GROW) whenever the mode can place one (`IsPatternPlacementAvailable`); rows that fit neither the board nor, in Versus, the player's half or remaining squares are greyed with the reason. Choosing one swaps the bar for the placement controls: tap the board to drop a ghost, drag to move it, ROTATE and FLIP, tick to place, cross to cancel. `PatternPlacer` runs the pure `PatternPlacementSession`, which centres, clamps and validates through `IPatternPlacementRules`, implemented by every `GameModeBase`. Classic merges the pattern with living cells in `Editing` and `Running`. Versus allows patterns only during setup, while you can still place: every cell must be empty and inside your half, each costs one setup square and absorption resolves once (`VersusMatch.TryPlaceSetupPattern`). The open catalogue tells the mode (`GameModeBase.OnPatternCatalogueOpened` and `OnPatternCatalogueClosed`, through `PatternsPanel.Closed`), and Versus holds its setup clock and the app while it is open. While the catalogue or a ghost is up, the other bar buttons are unreachable or ignored. Classic keeps running beneath a ghost and the merge applies to whatever is alive when you place; in Versus setup the app keeps placing on its own half, which never empties a cell on yours. When a mode stops taking patterns (`PatternPlacementAvailabilityChanged` to false, as when Versus setup time runs out), the director drops any ghost; `PatternPlacer.Cancel` ignores calls made while a placement is being committed.
- **Random cells:** the RANDOM card at the top of the catalogue holds one row, RANDOM CELLS, which places at once with no ghost. `RandomPlacementPlanner` chooses empty cells inside the mode's `PlacementBounds`: in Versus every setup square left (`PlacementBounds.HasSquareLimit`), or as many as the half has empty cells for, and in Classic `randomPlacementBoardShare` of the board (30%), skipping living cells. When the band has no empty cell the row is greyed with NO EMPTY CELLS LEFT (`RandomPlacementPlanner.HasEmptyCell`). For every `randomPlacementCellsPerPatternChance` cells (10) it gets one chance, taken with `randomPlacementPatternChance` (0.4), to drop a whole catalogue pattern in a random pose where no living or planned cell touches it, and keeps single cells off the cells around it while it can, so the pattern keeps its shape. The rest are scattered singles. The plan goes through the mode's `TryPlacePattern` as one move, so Classic places it in the chosen colour and Versus charges a square per cell. The three values are in `GameSettings`; the planner allocates nothing per plan and takes its `System.Random` from `PatternPlacer`, so tests can seed it.
- **Colours:** each mode says how to draw a cell in `PaintCell`: a `CellPaint` role (`Ink`, `Player`, `Opponent`, `Accent` or `Empty`), whose colour comes from the theme, or a custom colour for random colours.
- **Fresh cells:** a cell placed while the simulation runs (Classic's `Running`, Versus's running match, by you or the app, tapped or from a pattern) is drawn as a lighter variant of its colour for `freshCellGenerations` generations (3, in `GameSettings`), then blends back to its own colour on the third generation after it was placed. Cells placed while editing or in Versus setup start in their own colour. A fresh cell that dies or is absorbed stops being fresh at once, so a later cell in its place starts plain. `GameModeBase` keeps one `FreshCellTracker`: modes call `MarkPlacedCellFresh` before `RepaintChangedCells`, pass `IsCellFresh` to `GridView.PaintCell`, and call `SettleFreshCells` after each generation's repaint. How light is `Theme.freshCellBackgroundBlend`; set the generations to 0 to turn it off.
- **Absorption particles:** the director owns the board's `AbsorptionEffect` and hands it to every mode through `GameModeContext`.
- **Repainting:** `RepaintChangedCells()` redraws only the cells in `CellGrid.ChangedCellIndices`, animating each change. That list accumulates until it's cleared.

**Classic** (`ClassicGameMode`): `Editing` → (play) → `CountingDown` → `Running` → (pause) → `Editing`, plus `Showcase` for the pattern behind the menu at launch.
- **Colours:** the colour button (`CellColourButton`, a paint drop beside PATTERNS, shown only in Classic) steps through ink, blue, red and yellow, acting on every tap rather than after its spring (`AnimatedButton.ActsImmediately`). `ClassicCellColours` maps them to the owners `Player`, `Third`, `Opponent` and `Fourth` and to the paints `Ink`, `Player`, `Opponent` and `Accent`. The choice lasts for the session; reset keeps it.
- **Taps** bring an empty cell to life in the chosen colour and remove a living cell whatever its colour, in every phase except `CountingDown`. A tap never repaints a living cell, because a single repainted cell beside a bigger cluster of its old colour would be absorbed straight back. Patterns drop in the chosen colour, over any living cells, and the ghost previews it (`GameModeBase.PlacementPaint`).
- **Mixing colours:** after every tap, pattern and generation, Classic resolves absorptions with its own `ClusterAbsorber`, exactly as Versus does, and bursts particles and plays the absorb sound for any converted cells. A board of one colour skips the work (`CellGrid.HasLivingCellsOfMoreThanOneOwner`), so single-colour play costs what it did before.
- **Random colours** (Settings) re-tint only ink cells, each with a fresh random colour; the other colours always show as themselves. A random tint can look like blue, red or yellow, so the setting is best left off when mixing colours.
- Pressing play on an empty board opens the "populate at least one cell" dialog straight away, without a countdown.

**Versus** (`VersusGameMode`, with the rules in `VersusMatch`). Its phases (`VersusGamePhase`) are `Setup` → `CountingDown` → `Running` ⇄ `Paused` → `Finished`, plus `WaitingToStart` if setup finishes while the menu is open.
- **Setup:** a timed stage with no turns. You and the app place on your own halves (yours is the top) at the same time, and the app's half is shaded. The HUD timer counts the setup time down (SETUP TIME in Settings: 30 seconds by default, from `minimumSetupSeconds` to `maximumSetupSeconds` and `startingSetupSeconds` in `GameSettings`), turning red in its last ten seconds. One setup clock coroutine (`VersusGameMode.RunSetupClock`) counts it down and triggers the app's setup squares; it holds while the board is covered (menu or settings), while the early-start dialog waits, and while the pattern catalogue is open. Setup ends as soon as neither side can place any more (`VersusMatch.IsSetupComplete`), or when the time runs out (`VersusMatch.RunOutOfSetupTime`), when both sides lose any setup squares they have not placed. The countdown then starts.
- **The app's setup squares:** `OpponentMovePacing` spreads them evenly across the setup time left, jittered by 70-130%, so the app finishes before time runs out. Once you have nothing left to place (all placed, half full or started early), it places the rest at `opponentTurnDelay` (0.35 seconds, in `GameSettings`) so you never wait on it; the status line then reads THE APP KEEPS PLACING.
- **Starting early:** once you have placed at least half your setup squares (rounded up), the status line says so and pressing play asks you to confirm (`EarlyStartDialog`). Starting early keeps half your remaining setup squares (rounded down) for the match; the rest are lost. Pressing play with fewer than half placed explains how many more you need (the message dialog). The rules live in `VersusMatch.CanEndSetupEarly` and `EndSetupEarly`.
- **The app's answer:** when you start early, `SetupSavingJudge` plays a copy of the board `opponentSetupLookAheadGenerations` generations ahead (in `GameSettings`), with absorptions. If the app would still be level with or ahead of you, it saves its squares too, under the same rule, once it has placed at least half; otherwise it keeps placing its setup squares until it is done or the time runs out.
- **Setup changes:** changing the square pools, setup time or match length while still in setup restarts setup. Difficulty applies from the app's next move.
- **Patterns in setup:** see Patterns above; a pattern counts as that many placed squares towards starting early.
- **Difficulty** (the OPPONENT slider in Settings: Easy, Medium, Hard). The harder the app, the more of its squares go where they are likely to stay alive:
  - **Easy** places on a random allowed cell.
  - **Medium** scores every allowed cell and plays the best, with a lot of noise. It builds 2x2 blocks (the simplest still life) one square at a time with one empty cell around each, and once its half has no room for another block it puts spare setup squares away from its blocks; in the match it picks cells whose square survives the next generation, rescues lonely cells, triggers its own births and avoids overcrowding (one exact formula, `PlacementSurvey.NextGenerationOwnGain`); and it never joins where a bigger touching enemy cluster would absorb it, preferring moves that absorb smaller ones.
  - **Hard** does the same with two empty cells around each block (one when its half gets crowded) and a quarter of the noise, and plays its best 8 match moves 5 generations ahead on a scratch board (fewer on crowded boards, to bound the cost), choosing the one that leaves it the most cells and keeps the placed square its own.
  - Measured on the default board over 400 simulated matches per tier against a Medium player, each ending three seconds after a side is wiped out as in the game (`VersusMatchSimulator`), the share of the app's squares still alive and its own 5 generations later is about 12% (Easy), 53% (Medium) and 89% (Hard), and the win rate about 24%, 48% and 74%. `OpponentDifficultyOrderingTests` checks this ordering.
- **Match:** a countdown, then one match clock coroutine counts the time down and triggers generations and the app's moves. Remaining times are kept exactly across pauses.
- **App moves:** the app spends its in-match squares at jittered intervals across the match time left (`OpponentMovePacing`). Each one must touch a living cell, so a square never appears out of nowhere. `VersusMatch.CanPlace` enforces this whatever strategy is used. Your own squares can go on any empty cell. Its move search runs on a worker thread against a board snapshot (`OpponentMoveSearch`), so Hard never stalls a frame; if the chosen cell was taken while it searched, Easy's random choice is placed instead.
- **After the match:** Play, or choosing VERSUS from the menu, starts a new match.
- **Absorption** (`ClusterAbsorber`) is resolved after every placement and every generation. `VersusMatch.LastConvertedCellIndices` lists the cells that changed side, and `AbsorptionEffect` bursts particles from each one in its new owner's colour.
- **Early end:** once a side has no squares left on the board (`VersusMatch.HasSideWithNoCells`), whatever squares it still has to place, the status line reads YOU'RE WIPED OUT, APP WIPED OUT or BOTH WIPED OUT with a countdown, and the match ends `emptySideEndSeconds` (3, in `GameSettings`) of match time later. Each side has its own wait, started when that side is emptied: if the side has a living square again before its wait runs out (a placed square that is not absorbed), its wait is dropped, and losing it again starts a full new one. With both sides empty, the sooner wait ends the match, and the countdown never shows more seconds than the match clock has left. `EmptySideCountdown` holds the timing and runs on the match clock, so pausing or opening the menu pauses it.

There is no `Update()` anywhere in the game. The Versus match clock runs only while a match runs, and the board's cell animation (`FrameTicker`) only while cells are animating. DOTween's own update runs every frame, because the replayable tweens stay paused in its list rather than being killed; for a paused tween it does only a cheap check. The tweens are not linked to GameObjects, so DOTween does no per-frame link checks; each owner kills its tweens in `OnDestroy` instead.

### Audio (`AudioManager`)

`AudioManager` is a registered service, initialised first by `GameInitialiser`.
- **Music:** one looping source streams `puzzle_loop` and fades in. The MUSIC toggle pauses and resumes it.
- **Effects:** play through a small pool of AudioSources (`SoundLibrary.simultaneousSoundEffects`), reused round-robin, so overlapping sounds never allocate.
- **Saved settings:** the MUSIC, SOUND EFFECTS and DARK MODE toggles are saved on the device with `PlayerPrefs` (see `GameOptions`). Other Settings last for the session.
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
- **Ads:** `AdManager` checks `AdPassOwnership.IsOwned` and listens to `OwnershipChanged`: buying the pass destroys the loaded ad and stops every ad at once, and a refund turns ads back on.

**Before release:**
- Create a non-consumable product with ID `ad_pass` at £0.99 in both App Store Connect and Google Play Console.
- Test with sandbox / licence-tester accounts.
- Optionally add Google Play receipt validation: generate the tangle with **Services > In-App Purchasing > Receipt Validation Obfuscator** and validate in `OnPurchasePending` before granting.
- In the Editor, Unity's fake store approves purchases from a small dialog and always shows a $0.01 price.

### Ads (`AdManager` + `LevelPlayInitialiser`)

Ads are LevelPlay (Ads Mediation 9.5.1) full-screen interstitials, one ad unit per platform. Every setting is in `Configuration/AdSettings.asset`.

- **When ads show** (rules in `AdSchedule`, all in real-time seconds since launch):
  - **At start-up:** once, if an ad loads within `startupWindowSeconds` (10) and no game has started; a slower ad is skipped rather than interrupting.
  - **On the first launch:** the how-to-play guide opens first and every ad is held back (`AdManager.HoldBack`) until it closes. Closing it (`Release`) shows the start-up ad only if one is loaded and still inside its window; otherwise the usual rules carry on.
  - **Before each game, except the session's first:** a game starts when Classic's play is pressed from editing, or when a Versus match's first countdown would begin (not when resuming). Modes call `Context.AdBreaks.RunBeforeGame(start)`; the game starts straight away if no ad is due or ready, otherwise once the ad closes. The ad is skipped if another showed less than `minimumSecondsBetweenAds` (120) ago, so pausing and replaying in Classic doesn't show an ad every time; set it to 0 for an ad before literally every game.
  - **Every `timedIntervalSeconds` (30 minutes) after the last ad:** if a game is being played (counting down or running, `GameModeDirector.IsSimulationActive`), it waits for the next moment play stops: a pause, the menu, editing, setup or the end of a match.
- **Privacy:** non-personalised ads only. `LevelPlayInitialiser` sets `LevelPlayPrivacySettings.SetGDPRConsent(false)` and `SetCCPA(true)` before `LevelPlay.Init`, so no consent dialog or iOS tracking prompt is shown. The Android `AD_ID` permission is declared through `LevelPlay/Resources/LevelPlayMediationSettings.asset` (Ads Mediation → Developer Settings), as Google Play requires for ad SDKs on Android 13+.
- **Loading:** the interstitial is created once LevelPlay initialises, reloaded after every show and retried `loadRetrySeconds` after a failure. A failure to show is treated like a close, so a game is never held up. No ads load while the pass is owned.
- **In the Editor:** with the build target set to Android or iOS, LevelPlay shows a mock ad with a Close button in the Game view. The Device Simulator cannot click that button (it is IMGUI, which the Simulator only feeds through the legacy Input Manager, and the project uses the Input System alone), so there `AdManager` skips the mock ad as if it closed at once and logs that it did (`IsMockAdUnclosable`); switch to the Game view to see ads. Mock ads never fail, so test failures (airplane mode) on a device.

**Before release:**
- Create the app and an interstitial ad unit per platform in the LevelPlay dashboard, and paste the app keys and ad unit IDs into `AdSettings.asset`. The defaults are the test IDs from the Ads Mediation package's sample.
- Validate on a device with the LevelPlay Test Suite, and add networks through **Ads Mediation → Network Manager**.
- iOS: add the SKAdNetwork IDs of your ad networks to Info.plist.

### Grid rendering (`GridView` + `CellAnimator` + `GridCells.shader`)

The whole board is **one UI quad** drawn by one shader, however many cells there are:
- A texture holds one pixel per cell, with point filtering. Its RGB is the tile colour and its alpha is the tile's **size** (alpha 0 is an empty cell, 170 is full size, and the headroom above that is for bouncy overshoot).
- The shader draws every cell as a rounded tile with an anti-aliased signed-distance edge, and a faint tile behind each empty cell when GRID HIGHLIGHTS is on.
- Grid size, cell size, tile gap, corner radius and the empty-tile opacity travel in spare UV channels, so no per-instance material is needed. The vertex colour carries the empty-tile colour, and its alpha is left to the canvas, so a `CanvasGroup` fade (such as the how-to-play page's) fades every tile. This is why `GameScreen`'s Canvas has **TexCoord1 and TexCoord2** enabled under Additional Shader Channels. The pattern ghost is a second `GridView` on `Grid/PatternGhost` (its own nested Canvas, so moving it never rebuilds the board) painted in the active mode's `PlacementPaint` (blue in Versus, the chosen colour in Classic), or `Danger` where it cannot be placed. Where it can be placed its tiles are opaque, blended toward the background by `Theme.ghostTileOpacity`, so the ghost looks faded over empty cells yet still shows over cells of its own colour. Where it cannot, its danger tiles are faded by the same amount through the ghost's `CanvasRenderer` alpha, so the cells blocking it show through; `PaintCellImmediately` recolours it without animating.

**Cell animation.** When a cell changes, `CellAnimator` starts one of four motions from the Theme: *appear* (grows in, OutBack), *disappear* (shrinks away in its old colour, InBack), *convert* (an absorbed cell squashes, changes colour and springs back, OutElastic) or *settle* (a fresh cell blends back to its own colour without changing size, InOutSine; `GridView.SettleCell`). Each motion starts from the size and colour the cell shows at that moment, so a cell changed again mid-animation never jumps. Every frame while any cell is animating, `GridView`'s `FrameTicker` evaluates each animating cell with DOTween's own easing maths (`EasedMotion.Evaluate`), writes its pixel and uploads the texture once. All state, including the ticker, is allocated once at the largest board size. Animations are capped at a share of the time between generations, so fast games stay readable, and the shader keeps every bouncing tile inside its cell on both axes.

In Edit mode the board shows a preview using `GameSettings` and the Theme, and nothing is saved into the scene.

### Theme and dark mode (`Theme` + `ThemeService`)

Every colour in the game comes from `Theme.asset`, which has a light and a dark `ThemePalette`. A palette gives each `ThemeColour` role a colour: `Background`, `Surface` (cards and bars), `SurfaceStrong` (empty tiles, tracks), `Raised` (dialog cards), `Ink` (text, icons, Classic cells, primary buttons), `InkMuted`, `InkInverse` (text on ink), `Player` (your cells in Versus, blue in Classic), `Opponent` (the app's cells, red in Classic), `Accent` (Classic's yellow), `Danger` (reset, last seconds), `Scrim` (behind dialogs) and `Shade` (the app's half during setup).
- **Colouring a graphic:** add `ThemedGraphic` to any Image, RawImage or text and pick a role. It registers with the `ThemeService` while enabled and is repainted whenever the palette changes. Never set a colour in the Inspector or in code instead.
- **Dark mode:** the DARK MODE switch in Settings calls `ThemeService.SetDarkMode`, which blends every themed graphic, the board, the switches and the camera background to the other palette over `paletteCrossfade`. The choice is saved with `PlayerPrefs`.
- **Code that needs a colour** reads `ThemeService.Palette.Get(role)` or listens to `PaletteChanged`, as `GridView` and `ToggleSwitch` do.
- **Adding a role:** add it at the **end** of `ThemeColour` (scenes store roles by number), give it a field in `ThemePalette` and a case in both `ThemePalette.Get` and `ThemePalette.Set`, and set it in both palettes. `ThemeTests` fails if a role is missing a case.
- **Fonts:** Space Grotesk (SIL Open Font License, free to ship; licence in `Art/Fonts/SpaceGrotesk/OFL.txt`) for every text, and TextMesh Pro's default font in `TMP Settings`. `SpaceGrotesk-Bold SDF` is for titles, headers, button labels, pattern names, the Versus scores and timer, the countdown and the match result; `SpaceGrotesk-Regular SDF` is for everything else (body text, messages, descriptions, settings rows and values, the status line, hints, the price and the version). Never use TextMesh Pro's Bold style (faux bold) or auto-size: pick the bold asset instead, and give each text a fixed size that fits its longest content, as the Versus scores do at 46 so a 1,800-cell score fits. Each asset is a 1024 atlas sampled at 56 with padding 6, prefilled with ASCII, Latin-1 and common punctuation and currency signs; it stays dynamic, so a rare character (such as a store price in another currency) is added at run time from the bundled TTF.
- **Shapes:** rounded panels are the nine-sliced `RoundedPanel` sprite; an Image's *Pixels Per Unit Multiplier* sets its corner radius (radius = 96 ÷ multiplier).
- **Tiles:** `tileGap` and `tileRoundness` shape the board's tiles, `ghostTileOpacity` sets how much of its colour the pattern ghost shows over the background, and `freshCellBackgroundBlend` (0.45) how far a fresh cell blends toward the background: lighter in the light palette, softer in the dark one.

### Animation (DOTween)

All motion uses DOTween, configured from the Theme so a designer can change any duration or ease (including overshoot and elastic period) without code:
- **Start-up** (`ScreenFader`) fades in from black with `startupFade`.
- **Buttons** (`AnimatedButton`) squeeze while held and spring back with an elastic bounce, then act. A button that stops being interactable while held still springs back.
- **Screens and dialogs** (`ScreenPanel`) fade in while their `poppedContent` pops to full size and their `staggeredItems` (the title and menu buttons) pop in one after another; they fade out from wherever their fade is when hidden. A `screenShow` duration of 0 shows instantly (the stagger is skipped) and a `screenHide` duration of 0 hides instantly.
- **Shared scales:** when two animations scale the same object (a menu button popping in while it is pressed), each sets its own factor on the object's `LayeredScale`, which multiplies them, so neither overwrites the other.
- **Switches** (`ToggleSwitch`) slide their knob with a bounce and blend their colours.
- **Labels** (`LabelPop`, `CountdownDisplay`) pop for countdown numbers, changing scores and the last ten seconds of a match.
- **Cells** use the same DOTween curves through `CellAnimator` (above).

Tweens are built once (`ReplayableTween`: a paused tween of progress from 0 to 1 that the owner maps onto what it animates) and replayed, so animating never allocates. Each owner kills its tweens in `OnDestroy`; the `ThemeService` crossfade is killed by `GameInitialiser` when the scene unloads.

DOTween runs in safe mode, which catches exceptions thrown inside tween callbacks, including button actions that run when a spring finishes. `GameInitialiser` sets `DOTween.safeModeLogBehaviour` to `Error` so those exceptions still appear in the console.

DOTween is the free Asset Store package. To upgrade it, import the new version over `Plugins/Demigiant`, then open **Tools → Demigiant → DOTween Utility Panel → Setup DOTween**, untick every module and press **Apply**. Leave the assembly definition as it is: the button beside **Setup DOTween...** should read **Remove ASMDEF...**, which means `Modules/DOTween.Modules.asmdef` still exists. If it reads **Create ASMDEF...**, press it to recreate the file.

### Particles (`AbsorptionEffect`)

The `AbsorptionEffect` prefab sits under the board and holds two particle systems: a soft flash per absorbed cell and a few sparks that fly out and spin. Both modes use it, through `GameModeContext.AbsorptionEffect`, and each burst takes the colour the board now paints the cell (`GridView.GetCellColour`), so call `Play` after repainting. Bursts are emitted with `ParticleSystem.Emit` at each converted cell's centre, so nothing is instantiated; the particle systems pool their own particles. Big absorptions burst from at most `maximumCellsPerBurst` evenly spread cells. Particle sizes and speeds are in cells, so they suit any board size. Overlapping bursts are thinned to the particles still free, so a burst is spread out rather than cut short. Both systems use the `ParticleTile` sprite through texture-sheet animation and the `AbsorbParticles` material, and draw at sorting order 10, above the game screen canvas.

### Screens

Each screen root has a `ScreenPanel` and a `CanvasGroup`. Showing a screen plays its transition; hiding one stops it taking taps at once, fades it out, then disables its Canvas, so hidden screens cost nothing to draw or hit-test. Disabling the Canvas is cheaper than deactivating the GameObject. `SetVisible` switches instantly, for start-up. `GameScreen/SafeArea/GameControls` is a screen too: the in-game controls (button bar, Versus HUD, countdown) fade out whenever the main menu opens and back in when a mode is played, so only the board shows behind the menu; Versus also hides its setup shading while the menu is open. `ScreenNavigator` owns which screens show at start-up and how they relate: Settings opens over the main menu, which is hidden once Settings has fully appeared (`Shown`), and closing Settings brings the menu back (`Hidden`), unless it was opened from the game (`OpenSettingsMenuOverGame`), in which case the stored callback resumes the game instead. `PatternsScreen` is a dialog at sort order 2 that reports the chosen pattern from `Hidden`, like `EarlyStartDialog`. `HowToPlayScreen` is a full page at sort order 8 that hides the main menu once it has fully appeared and brings it back when it closes, like Settings. Its demo boards run only while it is open, and only on the shown tab. The main menu stops taking taps as soon as Settings or the guide starts opening (`Showing`), and an `AnimatedButton` acts only if its screen still takes taps when its spring settles. So once a menu button has started a mode or begun opening Settings or the guide, another menu button tapped just before it no longer acts. The store buttons leave the menu taking taps, so a cancelled purchase sheet never leaves it stuck.

## Prefabs

| Prefab | Base | Notes |
|---|---|---|
| `Buttons/IconButton` | — | Themed icon Image + Button. Change shared icon-button styling here |
| `Buttons/MenuButton`, `PlayPauseButton`, `ResetButton`, `PatternsButton`, `GameSettingsButton` | IconButton | Each adds its behaviour component and icon from `Interface.png`; reset uses the danger colour |
| `Buttons/CellColourButton` | IconButton | Classic's colour button: the `IconSwatch` paint drop, tinted with the chosen colour by `CellColourButton`. Sits in the button bar after PATTERNS; Classic shows it and hides it on exit |
| `Buttons/RotateButton`, `FlipButton`, `CancelButton`, `ConfirmButton` | IconButton | The positioning controls; each carries an `ActionButton`, and `PlacementBar` re-raises their presses for `PatternPlacer`. Cancel uses the danger colour |
| `Buttons/TextButton` | — | Rounded ink panel + Button, with a `Label` child in inverse ink. This is the primary style |
| `Buttons/ClassicButton`, `VersusButton` | TextButton | Primary buttons. They set `StartGameModeButton.mode` |
| `Buttons/HowToPlayButton`, `SettingsButton`, `AdPassButton`, `RestorePurchasesButton` | TextButton | Secondary buttons: surface panel with ink text. `AdPassButton` adds its `Price` label |
| `Buttons/TabButton` | TextButton | One tab of a tab row (`TabButton`): ink when selected, strong surface when not |
| `Dialogs/Dialog` | — | Canvas, scaler, full-screen scrim, and a raised rounded `Card` in the safe area holding a working OK button. The card pops in |
| `Dialogs/InvalidGameDialog`, `MatchResultDialog`, `MessageDialog` | Dialog | Add a `Message` to the card, plus `MatchResultPanel` or `MessagePanel` |
| `Dialogs/EarlyStartDialog` | Dialog | `EarlyStartPanel`: confirms starting Versus before every setup square is placed. Adds a START MATCH `ActionButton`; its OK button becomes KEEP PLACING |
| `Dialogs/SettingsDialog` | Dialog | A full page: the card fills the safe area and holds the title, the scrolling list of section cards, and DONE |
| `Dialogs/PatternsDialog` | Dialog | A full page like Settings: the title, the scrolling RANDOM card and category cards, and CLOSE. `PatternsPanel` fills the cards with `PatternRow`s at start-up, the random row first |
| `Dialogs/HowToPlayDialog` | Dialog | A full page: the title, CLASSIC and VERSUS tabs, the scrolling step cards and GOT IT. Its canvas turns on the extra shader channels the board shader needs |
| `HowToPlay/GuideStep` | — | One guide card: a demo board (`DemoBoardView` on a `GridView`, with a shade over the app's half when asked) beside a heading and body. The board hides for text-only steps |
| `Patterns/PatternRow` | — | One tappable catalogue row: thumbnail, name and description, greyed through its `CanvasGroup` with the reason in place of the description |
| `Settings/SliderSetting` | — | Label and value above a full-width slider. Set `valueFormat` per instance |
| `Settings/ToggleSetting` | — | Label and a `ToggleSwitch` |
| `Settings/Slider` | — | Rounded track, ink fill and round handle; nested inside SliderSetting |
| `Effects/AbsorptionEffect` | — | Flash and spark particle systems for absorptions |

Edit a **base** to change every button or dialog at once. Edit a **variant** for one specific button or dialog.

## Common tasks

- **Change a default, a timing or a grid limit:** edit `GameSettings.asset`.
- **Change a colour, the tile shape or an animation:** edit `Theme.asset`. Colours are per palette; each animation is a duration and a DOTween ease.
- **Add a button:** create a variant of `IconButton` or `TextButton`. Write a subclass of `AnimatedButton` that implements `OnPressed()`, and get the services it needs from `ServiceLocator`. Add that component to the variant.
- **Add a dialog:** create a variant of `Dialog`, put its content in `SafeArea/Card` above the OK button, and add it to the scene with a sort order above the screens it covers. Then wire it into `ScreenNavigator`: add a serialized field for it, add it to the screen list in `ScreenNavigator.Initialise` (which gives it its transitions) and call `SetVisible(false)` on it there so it starts hidden. The OK button already closes it.
- **Add a setting:** drop a `SliderSetting` or `ToggleSetting` into the right section card in `SettingsDialog/.../SettingsList` (`BoardCard`, `DisplayCard`, `SoundCard` or `VersusCard`). Add a serialized field and a listener in `SettingsPanel` (toggles also need `SnapToValue` on their switch, which the toggle loop does), and a default in `GameSettings`.
- **Add an icon or shape:** draw it in `Tools/Art/generate_ui_sprites.py`, run the script, then add its rectangle in the `Interface.png` Sprite Editor. The 1024×1024 sheet holds the rounded panel, circle, particle tile and the play, pause, reset, menu, patterns, settings, rotate, flip, tick and cross icons, and the colour button's paint drop.
- **Add a pattern:** add an entry to `PatternCatalogue.asset` (name, one-line description, category, rows of `.` and `O` with the top row first) and its expectation (period, or period and shift, or Grow) to the table in `PatternCatalogueTests`, which proves it on the engine and that it fits the largest board.
- **Edit the how-to-play guide:** edit `HowToPlayGuide.asset`. Each step has a heading, a body and an optional demo. A demo's `start` rows (top row first) name the colours you see, with `.` for empty. With `versusColours` on, `B` (or `O`) is your blue cell and `R` the app's red; otherwise Classic's colours apply: `O` ink, `B` blue, `R` red and `Y` yellow. Leave `start` empty for a text-only step. Each demo step is either `Generation` (advance one generation) or `Place` (rows the size of the board, where `.` leaves a cell alone and `-` empties it). Every demo resolves absorptions after each step, as both modes do, and `shadeAppHalf` shades the bottom half. A broken demo logs an error when the asset is edited, fails `HowToPlayGuideTests`, and shows in the game as a text-only card with an error in the log, so it can never stop start-up. To see the first-launch guide again, delete the `HowToPlay.Seen` PlayerPrefs key (Edit → Clear All PlayerPrefs).
- **Change the Conway rules:** edit `CellGrid`. Survival and birth are in its rule methods. A newborn takes the colour at least two of its three parents share, or, when all three differ, the fourth colour none of them has; Versus only has two colours, so there it is always the majority. A generation decides every cell from the current board before any change lands, because newborns read their parents' colours.
- **Change absorption:** edit `ClusterAbsorber`.
- **Tune the app's difficulty:** change `OpponentSkill.Medium` or `OpponentSkill.Hard` (block clearance, score noise, lookahead candidates and generations); the next-generation, lookahead and crowding weights and the lookahead cost budget are constants in `SurvivalOpponentStrategy`, the absorption penalty is `AbsorbedCellLossWeight` in `PlacementSurvey`, and the finished-block bonus is `CompletionBonus` in `BlockSites`. Run `OpponentDifficultyOrderingTests` afterwards to confirm each tier still keeps more squares alive than the one below. Its quick setup pace, pools, setup time and match length are in `GameSettings`; its colour is the `Opponent` role in each palette of `Theme.asset`.
- **Add a game mode:**
  1. Subclass `GameModeBase` and return the new type from `ModeType`.
  2. Add a value to `GameModeType`.
  3. Add the component under `Systems/GameModeDirector` and drag it into the director's `modes` list.
  4. Make a `TextButton` variant with `StartGameModeButton` set to the new mode.
- **Change when ads show:** edit `AdSettings.asset`; the rules themselves are in `AdSchedule`, covered by `AdScheduleTests`.
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
14. Take every colour from the Theme (`ThemedGraphic` or `ThemeService.Palette`); never hard-code one.
15. Animate with DOTween, with durations and eases in the Theme as `EasedMotion`s. Build tweens once (`ReplayableTween`) and replay them.

## Mobile settings already applied

- Portrait only; Android ARMv7 + ARM64 (IL2CPP); accelerometer off; target frame rate from `GameSettings` (60).
- Input: the Input System package (1.18) only. **Active Input Handling** (Player Settings) is *Input System Package (New)*, so the deprecated Input Manager is off; changing it needs an Editor restart. The `EventSystem` uses `InputSystemUIInputModule` with the package's default UI actions. Game code reads input only through uGUI pointer events (`GridTouchInput`, buttons), so never call `UnityEngine.Input`.
- URP: HDR, MSAA, shadows, additional lights and LOD cross-fade off. The camera renders only the UI layer: the game screen canvas and its particles.
- Font atlases prefilled with every character the game shows, so glyphs are generated at runtime only for rare characters; no text uses auto-size, which re-lays out text every time it changes.

## Before shipping

- Set the company name, product name and bundle identifier in Player Settings. They're still Unity's defaults.
- Add app icons and splash settings.

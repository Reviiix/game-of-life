# Working on Game of Life: handoff for a Claude Code instance

Read this whole file before doing anything. It describes how the previous Claude session worked on this project, what the user expects, and the traps already found. `README.md` is the developer guide to the code itself; read it too, it is accurate and kept up to date.

## The project

- Unity **6000.3.9f1**, URP, portrait mobile (Android and iOS). Path: `/Users/leon/Developer/Leon/game-of-life`.
- Conway's Game of Life with two modes:
  - **Classic:** four cell colours (ink, blue, red, yellow), Versus-style absorption between colours.
  - **Versus:** the player (blue) against an AI (red). Turn-based setup on each half, then a timed match.
- Other systems: pattern catalogue with RANDOM CELLS, how-to-play guide (opens on first launch), LevelPlay ads, an IAP ad pass, DOTween animation, a theme with dark mode.
- Branch `feature/versus-early-start` holds a large amount of **uncommitted** work across many features.

## Hard rules

1. **Never touch another project's Editor.** The user often has other Unity Editors open (for example `/Users/leon/Developer/VentureForth/the-garrison` and `/Users/leon/Developer/Indiespring/ftp-blackjack`), sometimes driven by another Claude. Every `unity command` call must include `--project-path /Users/leon/Developer/Leon/game-of-life`. Subagents you start must be told never to run the `unity` CLI at all.
2. **Never commit or push unless the user asks.** The user has explicitly said no to committing so far.
3. **Follow the coding standards below** for every change.
4. Ask before anything destructive or outward-facing. Reversible work that follows from the request needs no permission.

## Coding standards (the user's, mandatory)

- Systems are reached through `ServiceLocator`, one instance per type. `GameInitialiser` is the only entry point and initialises systems in an explicit order; no hidden setup in `Start()`.
- Cache references and precompute at start-up. Pool and preallocate; no allocation per frame, per generation or per move.
- Use prefabs and prefab variants.
- Self-documenting names. Every class and method gets a **one-line** `/// <summary>`; other comments are rare.
- `private` by default; `[SerializeField]` only for values a designer should tweak; read-only access through properties.
- Namespaces match folders (`GameOfLife.<Area>`).
- `StringBuilder` or precomputed strings; TMP `SetText(format, number)` is allocation-free and preferred for counters.
- Avoid `Update()`; use events, coroutines that run only while needed, or `FrameTicker`.
- Every colour comes from the Theme (`ThemedGraphic`, `ThemeService.Palette`). Never hard-code one.
- Animate with DOTween through `EasedMotion`, `ReplayableTween` and `FrameTicker`.
- Cull hidden UI by disabling Canvas and GraphicRaycaster; `raycastTarget` off on non-interactive graphics.
- Delete unused code and assets. Append new enum values at the end (scenes and prefabs store enums by number).
- Mobile performance first. The core look and feel must stay the same. The project must stay workable by developers without AI help, so keep `README.md` accurate after every change.

## How the previous session worked

### The loop for every request

1. **Understand before editing.** Read the relevant code first (grep and read, or one exploration agent for broad sweeps).
2. **Decide sensible defaults instead of blocking.** The user is often away. Ask only when a wrong guess would waste the work; otherwise choose, build, and list the choices made in the final summary so the user can redirect.
3. **Keep logic pure and testable.** Put rules in plain C# classes (examples: `RandomPlacementPlanner`, `EmptySideCountdown`, `DemoFrameBuilder`, `ClassicCellColours`) and keep MonoBehaviours thin.
4. **Implement, then verify in this order:**
   - Recompile in the Editor; no errors in the console.
   - Run **all** EditMode tests (currently 412, all passing). A change that breaks one is not done.
   - Check the logic in a Mono harness where useful (fast, no Editor needed).
   - **Play-mode check** of the real flow: drive the game with `eval`, capture the game view, and look at the screenshot.
   - Compile player scripts for **Android and iOS**.
5. **Write tests.** A background agent often writes them in parallel while you work on the scene or UI. Its brief: never run the `unity` CLI, write only under `Assets/Tests/EditMode/`, run the tests under Mono, and mutation-check them.
6. **Adversarial review.** Ultracode is normally on, so run a Workflow:
   - Two to four review lenses, each followed by a skeptical verifier told to refute the findings.
   - Fix every CONFIRMED or PLAUSIBLE finding, and run a short fix-verification pass when the fixes touch shared code.
7. **Update `README.md`**, and the how-to-play guide (`Assets/Configuration/HowToPlayGuide.asset`) when game rules change.
8. **Reset the first-launch flag** (`PlayerPrefs` key `HowToPlay.Seen`) and leave the Editor out of Play mode when finished.
9. **Report** briefly (see below).

### Driving the Editor

All commands go through the Unity CLI (`unity command ... --project-path <this project>`). A helper script in the session scratchpad defined these shortcuts:

```bash
u(){ unity command "$@" --caller plugin --skill unity-cli --project-path /Users/leon/Developer/Leon/game-of-life --no-banner --no-pager --timeout 60 2>&1 | grep -v "^<claude-code-hint" | sed "/^$/d"; }
r(){ u eval "$1" | tail -1 | grep -oE '"result":"[^"]*"|[Ee]rror.*'; }
```

Useful commands:

| Command | Use |
|---|---|
| `u recompile`, then poll `u recompile_status` | Recompile. Wait for `"status":"completed"` or `up_to_date`. The output also contains `"failed":false`, so don't match on the word "failed". |
| `u console --level error` | Compile and runtime errors. |
| `u run_tests --mode EditMode --timeout 900 --result-only` | Run every EditMode test. The CLI appends a hint line after the JSON, so parse the first JSON object only. |
| `u run_script --file X.cs --entry Class.Run` | Run a C# editor script for prefab, scene or asset edits (`PrefabUtility.LoadPrefabContents`, `SerializedObject`, then save). |
| `u eval '<C#>'` | Inspect or drive the game in Play mode. Reach private fields with reflection. |
| `u editor_play` / `u editor_stop` | Enter and leave Play mode. Unpause after stopping (`EditorApplication.isPaused = false`). |
| Step frames | `EditorApplication.isPaused = true; for (...) EditorApplication.Step();` time only advances when frames are stepped. |
| `u capture_game_view --width 540 --height 1140 --result-only` | Screenshot (base64 PNG); decode it and read the image. |

Play-mode recipes:

- **First launch:** the how-to-play guide opens first. Close it with `/HowToPlayScreen/SafeArea/Card/CloseButton`.
- **Ads:** the Editor shows a mock LevelPlay ad. Hide it via `Unity.Services.LevelPlay.InterstitialPrefab.HideAd`, and push `AdSchedule.nextTimedAdTime` far into the future.
- **Buttons:** press one with `Button.onClick.Invoke()`, then step about 40 frames, because buttons act after their spring animation.
- **Board taps:** build a `PointerEventData` at the cell's screen position and call `GridTouchInput.OnPointerClick`.
- **Short Versus matches:** set `GameOptions.SetVersusSetupSquares(5)` and `SetVersusMatchSquares(0)`.

Compile check for each platform (Android shown; iOS is the same with `iOS`):

```csharp
var settings = new UnityEditor.Build.Player.ScriptCompilationSettings { target = UnityEditor.BuildTarget.Android, group = UnityEditor.BuildTargetGroup.Android, options = UnityEditor.Build.Player.ScriptCompilationOptions.None };
UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings, "Temp/PlayerScriptCompileCheckAndroid");
```

### Mono harnesses (no Editor)

Pure code can be compiled and run fast with Roslyn from the project root:

```bash
csc -nologo -langversion:latest -out:/tmp/x.exe Assets/Scripts/Simulation/*.cs YourHarness.cs && mono /tmp/x.exe
```

- **NUnit:** `Library/PackageCache/com.unity.ext.nunit@d8c07649098d/net40/unity-custom/nunit.framework.dll`.
- **Unity DLLs:** `/Applications/UnityInstalls/6000.3.9f1/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/`.
- **netstandard facade:** `/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/Facades/netstandard.dll`.
- **Use `csc`, not `mcs`;** `mcs` can't parse the project's C#.
- **zsh doesn't split unquoted variables,** so loops like `set -- $c` misbehave.

Put scratch files in the session scratchpad, never in the project. The scratchpad can be wiped between days.

## Project knowledge and traps

- **Engine:**
  - `CellGrid` decides every cell from the current board, then applies changes, because newborns read their parents' colours.
  - A newborn takes the colour at least two parents share; if all three differ, it takes the fourth colour.
  - `ClusterAbsorber`: a bigger touching cluster of a different colour converts a smaller one; equal sizes leave each other alone.
- **Layers:** anything under `GameScreen` (Screen Space - Camera) must be on the **UI layer (5)**.
- **GridView boards:**
  - They need TexCoord1 and TexCoord2 shader channels on the root canvas.
  - The shader multiplies by vertex alpha, so CanvasGroup fades work.
  - Each board owns a DOTween sequence, and `GameInitialiser` sets the sequence capacity.
- **ScreenPanel events:** `Hidden` fires at the **start** of the hide fade, `Shown` at the **end** of the show. A button acts only if its screen's raycaster is still on when its spring ends.
- **Layout:** a layout group with `childForceExpandHeight` reports flexible height 1; pin it with a LayoutElement.
- **Pattern ghost:** opaque tiles blended toward the background when placeable; see-through danger tiles when blocked.
- **Versus:**
  - The app's match squares must touch a living cell.
  - A match ends 3 s of match time after a side is wiped out (`EmptySideCountdown`, one wait per side), or when the clock runs out.
- **How-to-play guide:** content lives in `HowToPlayGuide.asset`. Demos are rows of colour symbols played through the real engine. Edit text with `SerializedObject` scripts so the YAML stays valid; `HowToPlayGuideTests` checks every demo.
- **Catalogue descriptions:** pattern-row descriptions must fit two lines. Check with TMP `isTextOverflowing` in Play mode.
- **Unrelated console error:** an IAP `RequestFailedException` in the Editor console is a services token issue; ignore it.
- **User's open preference:** don't commit.

## How to communicate with the user

- Say in one line what you are about to do; give brief progress updates during long work.
- The final message stands alone. Lead with the outcome, then the choices you made for them, what was verified (tests, platforms, Play mode), and anything left open.
- Short sentences, plain words, bullets for parallel items. No headers in short messages. No em-dashes. Keep code names out of prose unless the user needs them.
- Use they/them for anyone whose pronouns are unknown.
- Report honestly: if something failed or was skipped, say so first.
- If a background agent or workflow is still running, say so; never predict its result.

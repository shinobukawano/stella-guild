# Stella Guild UI Bootstrap (Unity)

This folder provides a minimal UI foundation and startup page flow for the iOS-first MVP.

## Added scripts

- `Assets/Stella/Scripts/UI/UIComponent.cs`
- `Assets/Stella/Scripts/UI/UIPage.cs`
- `Assets/Stella/Scripts/UI/UIPageType.cs`
- `Assets/Stella/Scripts/UI/UIPageRouter.cs`
- `Assets/Stella/Scripts/UI/TitlePageController.cs`
- `Assets/Stella/Scripts/UI/SimplePageController.cs`
- `Assets/Stella/Scripts/Flow/StartupFlowController.cs`
- `Assets/Stella/Scripts/Design/ColorToken.cs`
- `Assets/Stella/Scripts/Design/StellaColorTokens.cs`
- `Assets/Stella/Scripts/Design/ColorTokenGraphicBinder.cs`
- `Assets/Stella/Scripts/UI/Buttons/StellaButtonVisualStyle.cs`
- `Assets/Stella/Scripts/UI/Buttons/StellaButtonStylePalette.cs`
- `Assets/Stella/Scripts/UI/Buttons/StellaButtonView.cs`
- `Assets/Stella/Scripts/Editor/StellaProjectSetupMenu.cs`

## Scene setup (quick start)

1. Create a scene (for example: `Bootstrap`).
2. Create a `Canvas` and set `Canvas Scaler` to `Scale With Screen Size`.
3. Create page root objects under the canvas:
   - `TitlePage`
   - `HomePage` (optional)
   - `SettingsPage` (optional)
4. Add `CanvasGroup` to each page root.
5. Add `TitlePageController` to `TitlePage`.
6. Add your start button reference to `TitlePageController.startButton`.
7. Add an empty game object `UIRoot` and attach `UIPageRouter`.
8. Register page entries in `UIPageRouter`:
   - `Title` -> `TitlePage`
   - `Home` -> `HomePage` (optional)
   - `Settings` -> `SettingsPage` (optional)
9. Add another empty game object `App` and attach `StartupFlowController`.
10. Assign the `UIPageRouter` reference in `StartupFlowController`.
11. In `TitlePageController.onStartPressed`, add `App -> StartupFlowController.OnStartPressed`.
12. In `TitlePageController.onSettingsPressed`, add `App -> StartupFlowController.OnSettingsPressed` if needed.
13. (Optional) Create `StartupLogo` UI object with `CanvasGroup`, assign logo sprite, and link it to `StartupFlowController.startupLogoCanvasGroup`.

## One-click setup (Editor menu)

If you prefer automated setup, run this menu in Unity:

- `Stella > Setup > Create Bootstrap + Stage01`
- `Stella > Setup > Apply Startup Logo To Bootstrap`

This creates:

- `Assets/Scenes/Bootstrap.unity`
- `Assets/Scenes/Stage01.unity`
- Required hierarchy (`Canvas`, `TitlePage`, `HomePage`, `SettingsPage`, `UIRoot`, `App`, `EventSystem`)
- Serialized references for `TitlePageController`, `UIPageRouter`, and `StartupFlowController`
- Build Settings entries with `Bootstrap` first

Notes:

- Existing `Bootstrap.unity` / `Stage01.unity` will be overwritten if you confirm.
- If a logo sprite exists in `Assets/Stella/UI/Logos/`, startup logo playback is enabled automatically.
- Startup logo background uses `BaseBackground` token color.
- Logo sprite is shown with horizontal padding (safe margin from screen edges).
- Start button is configured to load `Stage01` via fallback (`startDestinationPage = BattleHud`, page not registered).

## Behavior

- On scene start, `StartupFlowController` calls `UIPageRouter.ShowInitialPage()`.
- If a startup logo is configured, it plays first and then transitions to the initial page.
- The initial page is configurable in `UIPageRouter.initialPage` (default: `Title`).
- Pressing Start:
  - Navigates to `startDestinationPage` if that page is registered.
  - Otherwise loads `gameplaySceneName` (default: `Stage01`).

## Notes

- This is intentionally minimal so art/UI swaps from Figma do not require logic rewrites.
- For the next step, split `TitlePage` into reusable prefabs (`TopBar`, `ActionButton`, `NavButton`).

## Design tokens

- Token source: Figma node `8:2334`
- Shared token file: `Assets/Stella/DesignTokens/stella-color-tokens.json`
- Runtime token access: `StellaGuild.Design.StellaColorTokens`
- UI binding component: `StellaGuild.Design.ColorTokenGraphicBinder`

### Color mapping

- `BaseBackground` = `#FEF5E8`
- `SecondaryBackground` = `#C6B198`
- `Point` = `#EACB02`
- `Accent` = `#02216A`
- `Attention` = `#9F1600`
- `TextShadow` = `#28190A`
- `NavigationPrimary` = `#1B2D57`
- `MainButtonPrimary` = `#E1C41A`
- `MainButtonSecondary` = `#C4AF97`

### Usage

1. Add `ColorTokenGraphicBinder` to a UI object with `Image`, `Text`, or TMP text.
2. Select a token from the `token` field.
3. Keep `applyOnEnable` on for runtime updates.
4. Keep `applyInEditor` on if you want immediate preview in the editor.

## Button components

- Source: Figma node `8:2335`
- Main script: `StellaGuild.UI.Buttons.StellaButtonView`

### Supported styles

- `ActionRed` (purchase/use type)
- `NavigationBlue` (back/tab move type)
- `MainPrimary` (main round button)
- `MainSecondary` (sub round button)

### Setup

1. Create button hierarchy with separate face and shadow graphics.
2. Attach `StellaButtonView` to the root button object.
3. Assign:
   - `faceGraphic`
   - `shadowGraphic`
   - `labelGraphic` (optional)
   - `faceTransform`
4. Pick `style`.
5. Tune `releasedLocalPosition` and `pressedLocalPosition` to match your prefab depth.

## Startup logo

- Source: Figma node `8:2336`
- Recommended import path: `Assets/Stella/UI/Logos/stella-guild-logo.png`

### Setup

1. Export the logo from Figma as PNG with transparent background.
2. In Unity, import as `Sprite (2D and UI)`.
3. Create `StartupLogo` under your startup canvas (full-screen center layout).
4. Add `CanvasGroup` to `StartupLogo`.
5. Assign `StartupFlowController.startupLogoCanvasGroup`.
6. Tune:
   - `startupLogoDurationSeconds`
   - `startupLogoFadeSeconds`
   - `skipStartupLogo` (if you want to disable startup logo temporarily)

## Designer handoff

- Checklist document: `Assets/Stella/Docs/DesignerHandoffChecklist.md`

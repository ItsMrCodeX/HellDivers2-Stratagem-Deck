# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added

- **Custom search keyboard** for the Setup page: full-screen overlay with results on top (1/4) and an in-app QWERTY keyboard at the bottom (3/4), including an A-Z sidebar filter that filters by the first letter of the displayed name combined with live typing
- **Search Keyboard setting** in Settings (`use_custom_keyboard`, custom by default) to switch between the custom keyboard and the system keyboard
- **Input pad** as a `Pad` tab in the bottom bar: swipe up anywhere on the Game screen (at least half the screen height) or tap the tab to open a live D-pad that sends single arrow inputs to the server (`type: "key"`, serialized queue); swipe down, ✕ or Android back returns to Game; a right-aligned hint in the Game top status row announces the swipe
- **Ctrl hold toggle** in the input pad: when on, the server holds Ctrl down while the pad is open (each arrow tap sends just the arrow) and releases it when toggled off, when leaving the pad, when the app sleeps, or after 60s of inactivity; when off, arrows are sent raw so Ctrl can be held on the PC keyboard; the choice is persisted (`ctrl_inputs` preference)

### Changed

- Assigning a stratagem now falls back to the first empty slot when no slot is selected and the auto-advance wraps to the first empty slot when the following slots are full (applies to both the setup grid and the custom keyboard)
- **Icon loading runs off the main thread**: SVGs are decoded in parallel on up to 4 worker threads and all icons are bound in a single UI pass, removing the frame drops caused by one main-thread update per icon; the platform image loader (Glide on Android) is warmed up once with a hidden image so its first-use initialization no longer stalls the first search frame; loading starts with Setup instead of at app startup, and decoded PNGs stay cached in `AppDataDirectory/icon_cache` and are reused across launches (cache is only removed when the app is uninstalled or its data is cleared)
- `Stratagem.IconSource` now raises `PropertyChanged`, so icons that finish loading after binding appear in grids, slots and the custom keyboard results

### Fixed

- Icon cache writes are now atomic (temp file + move) and validated before reuse, so a partial/corrupt PNG is re-decoded instead of rendering a broken icon
- Server TUI no longer cycles the server IP when arrows are injected with `SendInput`: arrow navigation is suppressed for a short window (`TerminalLayout.SuppressInput`)

## [Released 1.2.2] 

### Added

- **Search bar** in Setup page to filter stratagems by name or short name
- **Short names** for all stratagems (e.g. "GL-21 Grenade Launcher" → "Grenade Launcher"); extracted from weapon code prefix
- **Icon disk cache**: SVG icons are decoded to PNG once and cached to `AppDataDirectory/icon_cache/` for faster subsequent loads
- **Debounced loadout save**: `PreferencesService.SaveLoadout` accumulates rapid changes and flushes after 400ms of inactivity
- `AutomationProperties.Name` on slots, buttons and category chips
- `Focused` visual state on Button style for keyboard navigation
- `try/catch` error handling in all page `OnAppearing`/`OnLoaded` handlers
- Log messages in `UdpDiscoveryService` simplified for readability

### Changed

- `stratagems.json` restructured: each entry is now `{ "keys": "...", "shortName": "..." }` instead of a flat string
- Icons use `ImageSource.FromFile(cachePath)` instead of `ImageSource.FromStream(() => ...)` to fix recycled cell glitch
- All display labels changed from `{Binding Name}` to `{Binding DisplayName}` to show short names
- Font size increased from 7 to 9 in stratagem grid items for readability
- Status messages shortened across all ViewModels ("Searching for server..." → "Scanning...", etc.)

### Fixed

- `IconName` property removed (dead code with no-op `Replace("-", "-")`)
- Unnecessary re-assignment `Slots[idx] = slot;` removed from `SetupViewModel.AssignToSlot`
- Direct `Preferences.Default.Remove("saved_loadout")` replaced with `_session.SaveLoadout()`

## [Released 1.2.1] 

### Added

- Server TUI split-pane layout with left pane for QR, PIN and IP and right pane for scrollable log
- DiscoveryBroadcaster activated on startup, broadcasts now appear in the server log
- IP cycling through `←`/`→` keys when server detects multiple network interfaces
- Input guard that blocks injected arrow keys from SendInput while a stratagem is running
- Spectre.Console dependency for FigletText PIN rendering

### Changed

- Server console output rewritten with `TerminalLayout` using cursor positioning and color coded log messages per category
- `CommandListener.OnStatusChanged` signature from `Action<string>` to `Action<LogCategory, string>`
- Mobile app palette to dark theme with Canvas, Surface, Hairline, Body, Mute, Error and Success tokens
- OpenSans fonts replaced with Nunito across all weights
- Buttons changed to pill shape with CornerRadius set to 999 and cards to RoundRectangle 12
- All Shadow styles removed
- Category chips now use grayscale backgrounds instead of bright category colors
- Slot selection stroke in white when selected and hairline color when unselected
- Connection indicator in green when connected and red when disconnected
- Android colors.xml synced with the new palette
- `FillAndExpand` replaced with `Fill` due to .NET 10 deprecation

### Fixed

- QR half-block rendering now decodes double-wide modules correctly instead of treating each character as a pixel
- ZXing scanner now explicitly targets `BarcodeFormat.QrCode` and camera lifecycle is managed through `OnAppearing` and `OnDisappearing`
- Server IP cycling no longer triggered by injected arrows during stratagem execution
- Discovery serialization mismatch where mobile expected `PcName` but server sent `pc` in the JSON payload
- UDP discovery now uses a single client for both sending and receiving so server responses reach the right port
- Discovery listener timeout replaced with CancellationTokenSource based timeout since ReceiveAsync ignores socket timeouts
- Discovery scan runs continuously in a loop with 5s intervals instead of a single pass
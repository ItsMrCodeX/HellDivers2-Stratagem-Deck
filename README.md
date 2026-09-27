# HD2 Stratagem Deck

A companion app for Helldivers 2 that lets you launch stratagems from your phone straight to your PC. It comes in two pieces, a small Windows server that turns incoming commands into keyboard input, and a mobile app built with .NET MAUI where you build loadouts, search stratagems with an in-app keyboard and send live arrow inputs while playing.

## What it does

- **Desktop server, StratagemDeck.Server**: listens for UDP commands on port 12345 and simulates keyboard input through the Windows SendInput API. It runs full stratagem sequences and single arrow inputs, and shows everything that happens in a split console with the QR code, the current PIN and a live log.
- **Mobile app, StratagemDeck.Mobile**: four tabs, Game, Pad, Setup and Settings, so you can build a loadout, fire stratagems with a tap or send live inputs from your phone.
- **Custom search keyboard**: opening the search field on Setup brings up the app's own QWERTY board with the results on top and an A-Z strip on the side. It can also be swapped for the system keyboard in the Settings menu.
- **Live input pad**: a D-pad arranged like a keyboard's arrow cluster. The Ctrl toggle keeps Ctrl held down for you while you tap arrows, or you can send raw arrows and hold Ctrl on your PC instead.
- **QR pairing**: the server displays a QR with its IP and PIN, and the app connects the moment you scan it.
- **Network scan**: the app can also look for servers on your local network by itself.

## Technologies

| Component | Stack |
|---|---|
| Mobile app | .NET 10 MAUI for Android and iOS |
| Desktop server | .NET 9 console app for Windows |
| Communication | UDP |
| QR generation | QRCoder |
| QR scanning | ZXing.Net.Maui.Controls |
| Data | JSON from an embedded stratagems.json |
| Icon rendering | SkiaSharp with Svg.Skia, decoded once and cached as PNG |

## Prerequisites

- .NET 10 SDK for the MAUI app
- .NET 9 runtime, which ships with the SDK, for the server
- MAUI workload: `dotnet workload install maui`
- Android SDK 21 or newer to build the APK
- Windows 10 or newer to run the server, since it relies on the SendInput API

## How to run

### 1. Clone

```bash
git clone https://github.com/ItsMrCodeX/HellDivers2-Stratagem-Deck
cd HellDivers2-Stratagem-Deck
```

### 2. Restore dependencies

```bash
dotnet restore StratagemDeck.slnx
```

### 3. Run the server on your PC

```bash
dotnet run --project StratagemDeck.Server
```

The console shows your local IP addresses, a four digit PIN and an ASCII QR that contains `IP:PIN`. Keep that window open while you play.

### 4. Build and deploy the mobile app

**Android:**

```bash
dotnet build StratagemDeck.Mobile -f net10.0-android -c Release
# APK: StratagemDeck.Mobile/bin/Release/net10.0-android/
```

**Windows, handy for testing:**

```bash
dotnet build StratagemDeck.Mobile -f net10.0-windows10.0.19041.0
dotnet run --project StratagemDeck.Mobile -f net10.0-windows10.0.19041.0
```

### 5. Connect

**QR code:** open the app, go to Settings and tap Scan QR Code, then point the camera at the QR in the server console. The app connects on its own.

**Manual:** go to Settings, open Manual Entry, type the server IP and PIN and hit Connect.

### 6. Play

- **Setup tab**: assign four stratagems plus any mission stratagems you want to keep handy. Tap the search field to open the custom keyboard, type with the keys or narrow the list with the A-Z filter. Picking a result assigns it and moves the selection to the next empty slot.
- **Game tab**: tap any stratagem to send it, including the ones in the mission strip at the bottom.
- **Pad tab**: send arrow inputs as you tap the D-pad. Leave the Ctrl toggle on and the server holds Ctrl while you enter a code, or turn it off if you prefer to hold Ctrl on your PC keyboard.
- Swipe up anywhere on the Game screen to jump straight to the Pad, and swipe down, tap ✕ or press back to return.
- **Save** keeps your loadout for the next session, **Clear** resets it.

## Project structure

```
HellDivers2-Stratagem-Deck/
├── StratagemDeck.slnx
├── StratagemDeck.Server/              # .NET 9 console server for Windows
│   ├── Program.cs                     # entry point, QR display and log wiring
│   ├── Native/KeyInjector.cs          # SendInput P/Invoke, sequences and held keys
│   └── Services/
│       ├── CommandListener.cs         # UDP listener and serialized input queue
│       ├── PinManager.cs              # 4-digit PIN
│       └── TerminalLayout.cs          # split-pane console UI
├── StratagemDeck.Mobile/              # .NET 10 MAUI app
│   ├── Pages/                         # Game, Pad, Setup, Settings and QrScan
│   ├── Controls/                      # custom keyboard and input pad views
│   ├── ViewModels/
│   ├── Services/                      # UDP, discovery, session, preferences, swipe watcher
│   └── Resources/Raw/
│       ├── stratagems.json            # 115 stratagems with keys and short names
│       └── icons/                     # SVG icons
├── README.md
└── .gitignore
```

## Notes

- The server and your phone need to be on the same local network.
- Windows Firewall may block UDP port 12345, so allow it on first run.
- For the best QR scanning, maximize the console window and use good lighting.
- Stratagem data is embedded in the app. Update stratagems.json and the icons folder when the game adds new ones.
- Icons are decoded once and stored in the app data folder, so they only disappear when you uninstall the app or clear its data.
- While the Ctrl toggle is on, the server releases Ctrl when you close the pad, send the app to the background or go 60 seconds without inputs.

# ReadBack 🎙️

<p align="center">
  <img src="assets/readback-overview.jpg" alt="ReadBack Overview & Architecture" width="100%" />
</p>

> **Instant Screen & Document Text-to-Speech for Windows**  
> Built with modern **C# & .NET 10**, Microsoft Edge Neural Voices, offline Windows SAPI fallback, and a sleek **Win+H style floating HUD**.

---

## 🌟 What is ReadBack?

ReadBack turns any text on your screen into natural, human-sounding speech instantly. While reading from the Windows clipboard (`Ctrl+Alt+C`) is the primary out-of-the-box modality, ReadBack's core engine is built around a pluggable text source architecture (`ITextSource`) designed for universal screen reading—including active selections, paragraph click-to-read actions, web pages, and Word/PDF documents.

Highlight any text, press **`Ctrl + Alt + C`**, and listen.

> 📖 **Looking for full documentation?** Check out the [ReadBack User & Customization Guide](docs/USER_GUIDE.md).

---

## ✨ Key Features

- ⚡ **Instant Streaming Playback**: Chunks text by sentence and paragraph. Audio playback starts within milliseconds while upcoming chunks synthesize seamlessly in the background.
- 🪟 **Windows 11 Floating Glass HUD (`Win+H` Style)**:
  - Default **Windows 11 Acrylic Glass** aesthetic with native DWM blur, subtle specular border, and ambient drop shadow.
  - **Modular Theming Engine**: Switch instantly between *Windows 11 Glass*, *Windows 11 Mica (Dark)*, *Windows 11 Light Glass*, and *High Contrast Accessibility*.
  - Live progress display (`2 / 5`), current sentence preview, animated 3-bar soundwave visualizer, and drag grip.
  - Mini media controls: **Previous Paragraph**, **Play/Pause**, **Next Paragraph**, **Stop**, and **Hide**.
- 🗣️ **Default Storyteller Voice: Microsoft Christopher (Natural Online)**:
  - Ultra-realistic, expressive human narration with zero robotic artifacts.
  - Zero API keys, subscriptions, or credit cards required.
  - One-click voice picker for other natural voices: `Jenny`, `Guy`, `Aria`, `Eric`, `Emma`, `Sonia`, and `Ryan`.
- 📥 **Integrated Windows Voice Downloader**:
  - Direct shortcut from the tray menu into Windows Speech Settings (`ms-settings:speech`) to download dozens of free offline Microsoft voice packages.
- 🛡️ **Offline SAPI Fallback**: If offline or internet drops, automatically and gracefully falls back to local Windows voices (`David`, `Zira`).
- 🧠 **Extensible Modular Architecture**:
  - Register custom TTS engines (`ITTSEngine`), custom HUD themes (`IHudTheme`), or narration cleanup filters (`INarrationFilter`) with clean single-interface contracts.
- ⌨️ **Native Global Hotkeys**:
  - `Ctrl + Alt + C`: Narrate clipboard (or toggle).
  - `Ctrl + Alt + X` or `Esc`: Stop narration.
  - `Ctrl + Alt + Space`: Pause / Resume.
  - `Ctrl + Alt + Right`: Skip to next paragraph.
  - `Ctrl + Alt + Left`: Jump to previous paragraph.
- 🔔 **System Tray Integration**: Quietly runs in the Windows notification tray with a categorized right-click menu for voices, speeds (0.85x to 1.5x), themes, and settings.
- 🚀 **Zero Script Wrappers**: Pure compiled Windows binary (`ReadBack.exe`). No Python runtime, no `.bat`, no `.vbs`, no console popups.

---

## ⌨️ Global Shortcuts

| Shortcut | Action |
| :--- | :--- |
| **`Ctrl + Alt + C`** | Narrate current clipboard text |
| **`Ctrl + Alt + Space`** | Pause / Resume narration |
| **`Ctrl + Alt + Right`** | Skip to next paragraph / sentence |
| **`Ctrl + Alt + Left`** | Previous paragraph / sentence |
| **`Ctrl + Alt + X`** or **`Esc`** | Immediately stop narration |

---

## 💻 Command Line & Script Helpers

ReadBack can be used directly from PowerShell or Command Prompt:

```powershell
# 1. Narrate whatever is currently in the clipboard:
.\Read-Clipboard.ps1
# or:
.\speak_clipboard.bat
# or directly:
dotnet run --project src/ReadBack.App/ReadBack.App.csproj -c Release -- speak

# 2. Speak custom text directly from console:
dotnet run --project src/ReadBack.App/ReadBack.App.csproj -c Release -- text "Hello from ReadBack!"

# 3. List all natural and offline voices in the terminal:
dotnet run --project src/ReadBack.App/ReadBack.App.csproj -c Release -- voices
```

---

## 🛠️ Building & Running (.NET 10)

### Prerequisites
- Windows 10 or 11 (64-bit)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Build & Run from Source
```powershell
# Build entire solution
dotnet build ReadBack.slnx -c Release

# Run ReadBack application
dotnet run --project src/ReadBack.App/ReadBack.App.csproj
```

### 2. Run Tests
```powershell
dotnet test ReadBack.slnx
```

### 3. Publish Single-File Executable
```powershell
dotnet publish src/ReadBack.App/ReadBack.App.csproj -c Release -r win-x64 -p:PublishSingleFile=true -o publish/
```
The published folder will contain a standalone `ReadBack.exe` ready to be copied anywhere or placed in your Windows Startup folder (`shell:startup`).

---

## 📂 Project Structure

```
readback/
├── ReadBack.slnx                  # .NET 10 Solution
├── ROADMAP.md                     # Extensibility blueprint & future wishlist
├── README.md                      # Documentation & user guide
├── src/
│   ├── ReadBack.Core/             # Pure Domain & Business Logic
│   │   ├── Models/                # SpeechChunk, PlaybackState, AppSettings, VoiceInfo
│   │   ├── Filters/               # Extensible narration pipeline & regex filters
│   │   ├── Chunking/              # Sentence & paragraph chunker
│   │   ├── TTS/                   # Edge Neural WebSocket client & SAPI fallback
│   │   └── Services/              # Playback controller & settings management
│   └── ReadBack.App/              # Modern WPF Windows Application
│       ├── Services/              # Native Win32 Hotkeys, Clipboard, Audio, Tray
│       ├── ViewModels/            # HudViewModel & RelayCommand
│       ├── Views/                 # MiniFloatingHud (Win+H style capsule)
│       └── Assets/                # icon.ico
└── tests/
    └── ReadBack.Tests/            # Unit tests for filters, chunking, and extensibility
```

---

## 📄 License
This project is licensed under the **Apache License 2.0**. See the [LICENSE](LICENSE) and [NOTICE](NOTICE) files for details.

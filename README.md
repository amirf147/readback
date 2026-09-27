# ReadBack 🎙️

> **Instant Clipboard Text-to-Speech for Windows**  
> Built with modern **C# & .NET 10**, Microsoft Edge Neural Voices, offline Windows SAPI fallback, and a sleek **Win+H style floating HUD**.

---

## 🌟 What is ReadBack?

ReadBack turns any text on your screen into natural, human-sounding speech instantly. Whether you are consuming AI chat responses (Gemini, ChatGPT), technical articles, documentation, or emails, ReadBack eliminates reading fatigue with zero friction.

Highlight any text, press **`Ctrl + Alt + C`**, and listen.

---

## ✨ Key Features

- ⚡ **Instant Streaming Playback**: Chunks text by sentence and paragraph. Audio playback starts within milliseconds while upcoming chunks synthesize seamlessly in the background.
- 🎛️ **Windows 11 Floating HUD (`Win+H` Style)**:
  - Sleek, rounded floating pill with dark acrylic aesthetics.
  - Live progress display (`2 / 5`), current sentence preview, and animated wave indicator.
  - Mini media controls: **Previous Paragraph**, **Play/Pause**, **Next Paragraph**, and **Stop**.
  - Draggable anywhere on screen; auto-shows on narration and auto-hides when done.
- 🧠 **Extensible Narration Pipeline**:
  - Automatically cleans markdown headers, bold, italics, bullets, blockquotes, and tables.
  - Skips and summarizes programming code snippets (e.g., replaces with `"(Omitted python code snippet)"`).
  - Cleans ugly URLs and normalizes LaTeX math symbols.
  - Easily extensible: add custom filter plugins with a single interface (`INarrationFilter`).
- 🗣️ **Microsoft Edge Neural Voices**: Free, ultra-realistic human voices (`Jenny`, `Guy`, `Aria`, `Christopher`, `Eric`, `Sonia`, etc.) with zero API keys required.
- 🛡️ **Offline SAPI Fallback**: If offline or internet drops, automatically falls back to local Windows voices (`David`, `Zira`).
- ⌨️ **Native Global Hotkeys**:
  - `Ctrl + Alt + C`: Narrate clipboard (or toggle).
  - `Ctrl + Alt + X` or `Esc`: Stop narration.
  - `Ctrl + Alt + Space`: Pause / Resume.
  - `Ctrl + Alt + Right`: Skip to next paragraph.
  - `Ctrl + Alt + Left`: Jump to previous paragraph.
- 🔔 **System Tray Integration**: Quietly runs in the Windows notification tray with a right-click menu for voices, speeds (0.85x to 1.5x), and settings.
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
MIT License.

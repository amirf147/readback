# ReadBack 🎙️ User & Customization Guide

Welcome to **ReadBack**, your high-performance clipboard text-to-speech companion for Windows. This guide explains how to use ReadBack, customize voices and themes, download additional Windows voices, and extend the application.

---

## 🚀 Quick Start

1. **Launch ReadBack**: Run `ReadBack.exe` (or `dotnet run --project src/ReadBack.App/ReadBack.App.csproj`).
2. ReadBack will minimize quietly into your Windows System Tray (notification area near the clock).
3. **Select any text** in your browser, IDE, PDF, or document and press `Ctrl + C` (or simply highlight).
4. **Press `Ctrl + Alt + C`**: ReadBack will instantly begin reading the text aloud with ultra-natural human intonation.
5. A sleek **Windows 11 Glass HUD** will appear at the top of your screen showing live sentence progress and playback controls.

---

## 💻 Command Line & Script Usage

ReadBack includes full CLI mode support, allowing you to use it from PowerShell or Command Prompt directly without needing Python or external runtimes:

### 1. Speak Clipboard from Terminal
```powershell
# Using the PowerShell helper:
.\Read-Clipboard.ps1

# Using the Batch helper:
.\speak_clipboard.bat

# Or using the compiled executable directly:
.\src\ReadBack.App\bin\Release\net10.0-windows\ReadBack.exe speak
```

### 2. Speak Custom Text Directly
```powershell
.\src\ReadBack.App\bin\Release\net10.0-windows\ReadBack.exe text "Hello! This is ReadBack speaking directly from the console."
```

### 3. List All Available Voices
```powershell
.\src\ReadBack.App\bin\Release\net10.0-windows\ReadBack.exe voices
```

---

## 🗣️ Voices & Natural Speech

### Default Voice: Microsoft Christopher (Natural Online)
ReadBack defaults to **Microsoft Christopher Online (Natural)** (`en-US-ChristopherNeural`), providing rich, expressive, storyteller-grade narration.

### Switching Voices
Right-click the ReadBack tray icon near the system clock to access the two categorized voice menus:
- **🌐 Neural Voice Selection (Microsoft Edge Natural)**:
  - ⭐ **Microsoft Christopher (US Natural)** - Expressive, warm storyteller male *(Default & Recommended)*
  - ⚡ **Microsoft Guy (US Natural)** - Articulated fast-reading male
  - ⚡ **Microsoft Jenny (US Natural)** - Clear, brisk female
  - **Microsoft Aria (US Natural)** - Expressive, dynamic female
  - **Microsoft Eric (US Natural)** - Friendly, upbeat male
  - **Microsoft Emma (US Multilingual)** - Modern multilingual female
  - **Microsoft Sonia (UK Natural)** - British natural female
  - **Microsoft Ryan (UK Natural)** - British natural male
  - **More English Voices...** - Nested list of all additional discovered English voices.
  - **⚙️ Configure Voices in settings.json...** - Directly open your configuration file to define a custom voice whitelist (`NeuralVoiceWhitelist`).
- **🖥️ SAPI Voice Selection (Windows Offline)**:
  - ⚡ **Microsoft David Desktop** - Crisp, high-speed clarity offline voice *(Recommended for Turbo mode)*.
  - **Microsoft Zira Desktop** - Standard Windows female offline voice.

### 🛡️ Automatic Offline Fallback & Offline-Only Mode
If your internet connection drops or Microsoft Edge endpoints are temporarily unreachable, ReadBack **never crashes or stops working**—it automatically and seamlessly falls back to your local Windows offline voices. You can also manually toggle **Offline Only (Windows SAPI)** in the tray menu.

---

## 📥 How to Download Free Windows Voices

Windows includes a large library of high-quality offline voices that you can install completely free:

1. Right-click the ReadBack tray icon, open **🖥️ SAPI Voice Selection**, and click **📥 Download More Windows Voices (Settings)...**  
   *(Alternatively, press `Win + I` → **Time & language** → **Speech**)*.
2. In the Windows Settings window, scroll to **Manage voices**.
3. Click **Add voices**.
4. Browse or search for languages/accents (e.g. English - United Kingdom, English - Australia, English - Canada, Spanish, German, French, Japanese).
5. Check the box and click **Add**.
6. Once downloaded, ReadBack automatically discovers and lists them under **🖥️ SAPI Voice Selection**.

---

## 🪟 Heads-Up Display (HUD) & Themes

ReadBack features a floating capsule toolbar inspired by the Windows 11 Dictation toolbar (`Win+H`):

### Native Windows 11 Glass Aesthetics
The default theme uses **Windows 11 Acrylic Glass**:
- Translucent frosted glass backdrop with Desktop Window Manager (DWM) composition.
- Subtle specular border with glowing blue accent badge.
- Animated 3-bar soundwave visualizer that pulses while audio is actively playing.
- **Persistent Display**: When speech finishes or is stopped, the HUD remains on screen so you can review what was read or replay chunks.
- Draggable anywhere: click and drag the grip icon or anywhere on the pill to reposition.

### Changing Themes
Right-click the tray icon and select **🎨 HUD Theme**:
- **🪟 Windows 11 Glass (Default)**: Frosted acrylic glass with vibrant electric blue accents and subtle drop shadow.
- **🌙 Windows 11 Mica (Dark)**: Modern matte dark Fluent design with clean borders.
- **☀️ Windows 11 Light Glass**: Crisp, translucent light acrylic for bright desktops.
- **👁️ High Contrast (Accessibility)**: High-visibility black & yellow styling for accessibility needs.

---

## ⌨️ Keyboard Shortcuts Reference

| Shortcut | Action | Description |
| :--- | :--- | :--- |
| **`Ctrl + Alt + C`** | **Narrate Clipboard** | Reads the current clipboard text. If playing, toggles stop/start. |
| **`Ctrl + Alt + H`** | **Toggle Floating HUD** | Shows or hides the floating heads-up display. |
| **`Ctrl + Alt + Space`** | **Pause / Resume** | Pauses speech at the exact current position and resumes smoothly. |
| **`Ctrl + Alt + Right`** | **Next Paragraph** | Skips forward to the next sentence or paragraph chunk. |
| **`Ctrl + Alt + Left`** | **Previous Paragraph** | Jumps back to repeat the previous sentence or paragraph. |
| **`Ctrl + Alt + X`** | **Stop Narration** | Immediately stops playback while keeping the HUD on screen. |
| **`Esc`** | **Two-Stage Dismiss** | **1st press**: Stops narration (keeps HUD open).<br/>**2nd press**: Closes the HUD display. |

---

## ⚙️ Additional Features & Settings

- **🖱️ Tray Icon Left-Click**: Instantly brings the floating HUD into view and focus; right-click opens the context menu.
- **🚀 Launch at Windows Startup**: Automatically starts ReadBack in the system tray when you log into Windows (toggled with one click from the tray menu).
- **⚡ Reading Speed**: Adjust playback rate from `0.85x` (slower), `1.0x` (normal), `1.15x`, `1.25x`, `1.5x`, `1.75x`, `2.0x` (⚡ Turbo), `2.5x`, up to `3.0x` (⚡ Extreme Limit).
- **Show Floating HUD**: Toggle whether the floating capsule displays during narration.
- **Skip Code Snippets**: Intelligently detects Markdown code blocks (e.g. ````python ... ````) and replaces them with a spoken summary `"(Omitted python code snippet)"` so you don't have to listen to raw syntax.
- **Sound Feedback Chimes**: Plays a gentle Windows chime when speech starts.
- **Offline Only Mode**: Forces ReadBack to exclusively use local Windows SAPI voices without using the internet.
- **⚙️ Open Settings Configuration**: One-click tray menu option to open `%APPDATA%\ReadBack\settings.json` to configure hotkeys, themes, or custom voice whitelists.

---

## 🧩 Developer Extensibility Guide

ReadBack was architected from the ground up to be modular and easily extensible.

### 1. Adding a Custom TTS Engine (`ITTSEngine`)
Implement `ReadBack.Core.TTS.ITTSEngine` and register it with `CompositeTTSEngine`:

```csharp
public class MyCustomTTSEngine : ITTSEngine
{
    public string Name => "My Custom TTS";

    public Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<VoiceInfo>>(new[]
        {
            new VoiceInfo("my-voice-id", "Custom Ultra Voice", "en-US", true, false, "MyCustomProvider")
        });
    }

    public async Task<bool> SynthesizeToFileAsync(string text, string outputPath, string voiceId, string rate, CancellationToken ct = default)
    {
        // Synthesize text to MP3 or WAV at outputPath
        return true;
    }
}

// In App.xaml.cs:
ttsEngine.RegisterEngine(new MyCustomTTSEngine());
```

### 2. Adding Custom HUD Themes (`IHudTheme`)
Implement `ReadBack.App.Themes.IHudTheme` and register it with `HudThemeManager`:

```csharp
public class CyberpunkTheme : IHudTheme
{
    public string Id => "cyberpunk";
    public string DisplayName => "Cyberpunk Neon";
    public Brush BackgroundBrush => new SolidColorBrush(Color.FromRgb(15, 10, 30));
    public Brush BorderBrush => new SolidColorBrush(Color.FromRgb(255, 0, 128));
    public Thickness BorderThickness => new(1.5);
    public CornerRadius CornerRadius => new(18);
    // ... define other palette brushes
    public int BackdropType => 3; // Acrylic
    public bool IsDarkMode => true;
}

// In App.xaml.cs:
themeManager.RegisterTheme(new CyberpunkTheme());
```

### 3. Adding Custom Text Filters (`INarrationFilter`)
Implement `ReadBack.Core.Filters.INarrationFilter` and register it with `NarrationPipeline`:

```csharp
public class EmojiStripperFilter : INarrationFilter
{
    public string Name => "Emoji Stripper";
    public int Order => 5;
    public bool IsEnabled { get; set; } = true;

    public string Process(string text, FilterContext context)
    {
        // Strip or clean emojis before sending to speech synthesis
        return Regex.Replace(text, @"\p{Cs}", "");
    }
}

// In App.xaml.cs:
pipeline.RegisterFilter(new EmojiStripperFilter());
```

---

## ❓ Frequently Asked Questions

#### Q: How do I make ReadBack start automatically with Windows?
Simply right-click the ReadBack icon in your system tray and click **🚀 Launch at Windows Startup**. ReadBack will register a startup entry under your user profile (`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`) and automatically launch directly into your system tray when you log in.

#### Q: Why did it temporarily use a robotic voice?
If your computer loses internet connectivity or your firewall blocks outbound WebSocket connections to Microsoft's speech service, ReadBack automatically falls back to your offline Windows SAPI voice so that you are never left without speech. As soon as connectivity returns, natural voices resume.

#### Q: Where are settings saved?
Settings are stored in `%APPDATA%\ReadBack\settings.json`.

---

## 📄 License
ReadBack is licensed under the **Apache License 2.0**. See the `LICENSE` and `NOTICE` files for details.

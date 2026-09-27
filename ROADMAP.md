# ReadBack: Architecture & Future Roadmap 🗺️

This document outlines the modular design principles of **ReadBack** and acts as an actionable wishlist of planned features and extensibility vectors.

---

## 🏛️ Core Architecture Principles

ReadBack is built with a strictly decoupled, event-driven domain architecture targeting **.NET 10**:

```
                       ┌───────────────────────────────┐
                       │   Global Hotkeys (Win32 API)  │
                       │   Ctrl+Alt+C / Esc / Space    │
                       └───────────────┬───────────────┘
                                       │
                                       ▼
┌───────────────────────────┐      ┌───────────────────────────────┐
│ Windows Clipboard Service │─────▶│    IPlaybackController        │
└───────────────────────────┘      └───────────────┬───────────────┘
                                                   │
                ┌──────────────────────────────────┴──────────────────────────────────┐
                ▼                                                                     ▼
┌───────────────────────────────┐                                     ┌───────────────────────────────┐
│  INarrationPipeline (Filters) │                                     │       UI Presentation         │
│  - CodeBlockFilter            │                                     │  - MiniFloatingHud (Win+H)    │
│  - MarkdownFormattingFilter   │                                     │  - TrayIconService (WinForms) │
│  - UrlSimplifierFilter        │                                     └───────────────────────────────┘
│  - LatexMathFilter            │
│  - TableFormattingFilter      │
│  - [Future Plugins/Filters]   │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│  ISpeechChunker               │
│  - Natural pause splitting    │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│  CompositeTTSEngine           │
│  - EdgeTTSClient (Cloud WSS)  │
│  - SapiTTSClient (Offline)    │
│  - [Future: Piper / ONNX]     │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│  WindowsAudioPlayer           │
│  - Streaming MP3 playback     │
└───────────────────────────────┘
```

### 1. Extensible Narration Pipeline (`INarrationFilter`)
Text does not go straight to speech; it traverses an ordered chain of filters. Anyone can register new filters at runtime or compile-time without touching the core engine:

```csharp
public class CustomFilter : INarrationFilter
{
    public string Name => "My Custom Filter";
    public int Order => 25; // Sits between Markdown (20) and URL (30)
    public bool IsEnabled { get; set; } = true;
    public string Process(string text, FilterContext context) => ...;
}
```

### 2. Multi-Provider Speech Engine (`ITTSEngine`)
TTS engines are abstracted behind `ITTSEngine`. Adding local offline AI models (e.g., Piper, Kokoro, Whisper, or cloud OpenAI/ElevenLabs) simply requires implementing `ITTSEngine`.

---

## 🌟 Future Feature Wishlist

### Phase 1: Enhanced Floating HUD ("Win+H" Style)
- [ ] **Interactive Speed Slider**: Quick-adjust reading speed (0.5x to 2.5x) directly from the floating pill.
- [ ] **Voice Selector Flyout**: Click the microphone badge to open a sleek mini popup showing all neural and local voices with instant sample preview.
- [ ] **Word-by-Word Highlighting**: Real-time karaoke-style word highlighting in the HUD as words are spoken.
- [ ] **Pin / Dock Mode**: Option to dock the pill to the screen edge or keep it persistently visible.

### Phase 2: Input Sources & Smart Capture
- [ ] **Auto-Read on Copy Mode**: Toggle to automatically narrate any text copied to clipboard (`Ctrl+C`) without needing `Ctrl+Alt+C`.
- [ ] **OCR Screen Snipping (`Ctrl+Alt+S`)**: Select a rectangular region on screen (e.g., image, protected PDF, remote desktop) and read text aloud using Windows Media OCR.
- [ ] **Active Selection Hook**: Narrate highlighted text directly without overwriting the user's clipboard buffer.

### Phase 3: Advanced Narration Filters
- [ ] **AI Summarization Filter**: Condense long 20-page documents or email threads into a 2-minute bulleted audio summary before reading.
- [ ] **Translation Filter**: On-the-fly translation into any target language prior to synthesis.
- [ ] **Pronunciation & Acronym Dictionary**: Custom user dictionary for domain-specific acronyms, medical terms, and jargon.

### Phase 4: Local AI TTS Backends
- [ ] **Embedded Piper / Kokoro Engine**: Run ultra-natural neural voices 100% offline via ONNX Runtime without needing Microsoft Edge servers.
- [ ] **WAV / MP3 Export**: Save narration directly to an audio podcast file for listening on mobile devices.

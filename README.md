# Interview Overlay

A floating notes panel for Windows that you can position next to (or "attach"
to) a meeting window — Zoom, Teams, Google Meet in Chrome/Edge, or anything
else — so your own notes stay visible while you talk.

**This build is meant to be used openly.** It does not attempt to hide the
overlay from screen sharing or screen recording, and it does not include any
Windows display-affinity / capture-exclusion tricks. If you want to use it
during a video interview, the honest move is to either keep your camera
framed so it's clearly just you referencing notes (same as a printed sheet),
or ask the interviewer if notes are OK — most are fine with it. There's no
technical guarantee of the interviewer never noticing your eyes moving, and
this app makes no attempt to manufacture one.

---

## What it does

- **Floating notes overlay** — borderless, draggable, resizable panel that
  stays on top of other windows.
- **Adjustable opacity** (20%–100%, default 65%) via slider-equivalent
  hotkeys or by editing the profile.
- **Attach to Window** — pick any open window from a list; the overlay
  tracks its position and follows it if it's moved, and hides/restores if
  the target is minimized/restored, and detaches automatically if the
  target closes.
- **Automatic meeting-app hints** — Zoom, Microsoft Teams, and Google Meet
  (Chrome/Edge) are flagged at the top of the "Attach to Window" list based
  on process name / window title, but manual selection always works too,
  since browser tab titles change.
- **Global hotkeys** (work even while Zoom/Teams/Chrome has focus):
  - `Ctrl+Shift+H` — show/hide overlay
  - `Ctrl+Shift+↑ / ↓` — opacity up/down
  - `Ctrl+Shift+= / -` — font size up/down
  - `Ctrl+Shift+D` — detach
  - All rebindable in Settings.
- **Click-through mode** (toggle on the overlay's title bar) — lets clicks
  pass through to the window underneath so you don't accidentally steal
  focus from the meeting app.
- **Interview Profiles** — save separate notes, size, position, opacity,
  and font size per interview.
- **Local-only storage** — notes and settings live in
  `%AppData%\InterviewOverlay\` as JSON. No account, no network calls,
  nothing uploaded anywhere. Auto-save runs every few seconds and a rolling
  backup file protects against a crash mid-save.
- **Export / Import** — JSON, Markdown, or plain text.
- **System tray** — minimize-to-tray with Show/Hide/Attach/Detach/Settings/
  Exit from the tray menu.
- **Start with Windows** (optional, per-user registry Run key — no admin
  rights needed, easy to disable again from Settings or Windows' own
  Startup Apps page).
- **Dark / Light / Custom theme.**
- **Multi-monitor aware** positioning (clamps the overlay to the current
  work area so it can't end up off-screen); **Reset Overlay Position**
  button in Settings as a recovery option.

## What it deliberately does NOT do

- No screen-capture exclusion / "hide from Zoom sharing" feature.
- No audio or video recording of any kind.
- No transcription or analysis of the meeting.
- No network access, telemetry, or accounts.

---

## Requirements to build

- Windows 10/11 (x64)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (17.8+) with the ".NET desktop development" workload,
  **or** just the .NET 8 SDK + command line — both work.

## Build & run (Visual Studio)

1. Open `InterviewOverlay.sln`.
2. Set the solution platform to `x64` (top toolbar).
3. Press F5 to run in debug, or **Build → Publish** for a release build.

## Build & run (command line)

```bash
cd InterviewOverlay
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

## Producing a standalone .exe

```bash
cd InterviewOverlay
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output lands in:

```
InterviewOverlay\bin\Release\net8.0-windows\win-x64\publish\InterviewOverlay.exe
```

That single `.exe` runs on a machine with no .NET installed (self-contained).
Copy it wherever you like — no installer is included in this build, but you
can zip that `publish` folder and hand it to yourself on another machine, or
add an installer later with something like Inno Setup if you want Start Menu
shortcuts / an uninstaller.

## Adding a custom icon (optional)

The app currently uses a default Windows system icon for the tray so it
builds without any extra assets. To brand it:

1. Add a `.ico` file at `InterviewOverlay/Resources/app.ico`.
2. In `InterviewOverlay.csproj`, add back:
   `<ApplicationIcon>Resources\app.ico</ApplicationIcon>`
3. In `App.xaml.cs`, replace the `SystemIcons.Application` line with your
   icon, e.g. `new System.Drawing.Icon("Resources/tray.ico")`.

---

## Where your data lives

```
%AppData%\InterviewOverlay\settings.json   — app settings, hotkeys, theme
%AppData%\InterviewOverlay\notes.json      — all interview profiles/notes
%AppData%\InterviewOverlay\notes.backup.json — rolling backup for crash recovery
```

Nothing else is written or transmitted anywhere.

## Known limitations

- Window tracking is poll-based (checks position ~6–7 times/sec), so there's
  a small lag (under ~200ms) when you drag the target window fast.
- DPI-scaling edge cases on unusual multi-monitor setups with mixed scale
  factors (e.g. 100% + 175%) may need a manual nudge via drag; "Reset
  Overlay Position" in Settings recovers a lost window.
- Meeting-app auto-detection is a convenience, not a guarantee — always
  falls back to manual selection.
- No installer/uninstaller bundled in this build (see above).

## Troubleshooting window attachment

- If your target window doesn't show up in "Attach to Window," click
  **Refresh List** — some windows (e.g. before a meeting fully loads) may
  not register as visible top-level windows yet.
- If the overlay "gets lost" off-screen after unplugging a monitor, open
  **Settings → Reset Overlay Position**.
- Global hotkeys can silently fail to register if another app already owns
  that combo — try a different combination in Settings if a hotkey doesn't
  respond.

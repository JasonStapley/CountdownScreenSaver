# Countdown Screen Saver

A Windows screensaver that counts down to a date you choose. The message sits on a black screen, then jumps to a new place in a new color. Written by Jason Stapley in 2016 and rewritten for .NET 8.

The sentence is the same one the original program painted:

```text
There are 2 weeks and 3 days until Holiday Total days 17
```

Weeks are the whole number of 7-day blocks, and the leftover days are the remainder. A date that has already passed counts backward (the numbers go negative). One of something is singular (`1 week`, `1 day`); every other count, including zero, is plural.

## Features

- Full-screen countdown on every monitor (`/s`, and also when the file is started with no arguments).
- Settings dialog (`/c`) for the message, the date, the font, and how often the text jumps.
- Preview inside the Windows screen saver page (`/p`), drawn in the small monitor window.
- Mouse move, click, wheel, or any key closes every screen. A nudge smaller than about 10 pixels is ignored, so a tiny shake when the saver opens does not dismiss it. The preview window stays up while the pointer moves, because that window is embedded in the settings page.
- Settings are stored in `%AppData%\CountdownScreenSaver\settings.json`.
- If that file is not there yet, customized values are read once from the original `HKCU\CountdownScreenSaver` registry key. The 2016 factory defaults (Microsoft Sans Serif 8.25 pt and a 5 millisecond interval) are treated as “never set” and replaced by Broadway 48 pt Bold, jumping every 3 seconds. A date, message, font, or interval you actually changed is kept. The old program did not store bold or italic; a migrated font is drawn regular until you pick a style again.
- The interval is the timer period in milliseconds, which is what the 2016 program stored. The settings dialog labels the unit. Values under 50 milliseconds are raised to 50 so the saver cannot spin the CPU. The maximum is one hour.
- High-DPI and per-monitor aware. Each screen uses that monitor’s full bounds, including the taskbar. Text is painted in one double-buffered pass.
- If the chosen font is not installed, the saver tries Broadway, then Segoe UI, then Arial.

Fresh settings look like this:

```json
{
  "countdownDate": "2016-11-25T00:00:00",
  "countdownText": "Enter text here",
  "fontName": "Broadway",
  "fontSize": 48,
  "fontStyle": "Bold",
  "intervalMilliseconds": 3000
}
```

`fontStyle` uses .NET font style names, such as `Bold` or `Bold, Italic`.

## Screenshots

Captures are not in the repository yet. After you try the screensaver on Windows, add:

- `docs/screenshots/screensaver.png` — the full-screen countdown: black background, the sentence in a random color.
- `docs/screenshots/settings.png` — the configuration dialog, including the font and the interval in milliseconds.
- `docs/screenshots/preview.png` — the small preview inside Windows display settings.

## Build

You need the .NET 8 SDK.

```bash
dotnet build CountdownScreenSaver.sln -c Release
dotnet test CountdownScreenSaver.sln -c Release --no-build
```

`dotnet test` checks the countdown sentence, the `/s` `/c` `/p` arguments, and the JSON settings file. The window itself has to be tried on Windows.

## Publish a single-file .scr

Publish a self-contained 64-bit executable, then rename it. The file is about 70 MB because the .NET runtime and Windows Forms are inside it, so the PC can run the screensaver without a separate .NET install. The first launch can take a moment while native libraries are unpacked.

```powershell
dotnet publish .\src\CountdownScreenSaver\CountdownScreenSaver.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -o .\artifacts\publish

Rename-Item -Force .\artifacts\publish\CountdownScreenSaver.exe .\artifacts\publish\CountdownScreenSaver.scr
```

From cmd.exe the rename is:

```bat
ren artifacts\publish\CountdownScreenSaver.exe CountdownScreenSaver.scr
```

## Install

1. Build `CountdownScreenSaver.scr` with the commands above, or download it from the GitHub Actions artifact named `CountdownScreenSaver`.
2. If the file came from the internet and Windows will not run it, open Properties and select Unblock.
3. Right-click `CountdownScreenSaver.scr` and choose **Install**.

Windows copies the screensaver into the system folder and opens the Screen Saver dialog. Choose **Countdown Screen Saver**, then **Settings**. You can also copy the `.scr` to `C:\Windows\System32` yourself (this publish is 64-bit) and select it under Settings, Personalization, Lock screen, Screen saver.

The same workflow builds the `.scr` on every pull request and on every push to `master`, and uploads it as an artifact.

## What to try on Windows

The project builds here, but the windows do not. On a Windows machine:

1. Install the `.scr` and set a future date, a short message, a large font, and an interval of `2000` milliseconds.
2. Confirm OK writes `%AppData%\CountdownScreenSaver\settings.json`. Cancel leaves that file unchanged.
3. In the Screen Saver dialog, check the small preview: the sentence should move and change color inside that window, and the dialog should stay open.
4. Run the full-screen preview. The countdown should cover every monitor, including the taskbar. Moving the mouse, clicking, or pressing a key should close all of them together.
5. If you still have customized values under `HKCU\CountdownScreenSaver` and you have not created the JSON file yet, those values should appear in the dialog. If the only saved interval is the old default of `5`, expect it to be raised to `50` milliseconds; set a few seconds (for example `3000`) if the text is flashing.
6. At 150% display scaling, the settings dialog should stay readable and the full-screen text should stay sharp.

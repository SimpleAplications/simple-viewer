# Reiks Viewer

A simple picture and video viewer for Windows, made for private use. Your pictures and videos stay yours: Reiks Viewer shows them only to you, and that is all it does.

**[Download for Windows](https://github.com/reiks-apps/reiks-viewer/releases/latest/download/ReiksViewer-Setup.exe)**
 · [Portable zip](https://github.com/reiks-apps/reiks-viewer/releases/latest/download/ReiksViewer-portable.zip)
 · [Website](https://reiks-apps.github.io/reiks-viewer/)

![Reiks Viewer showing a paused video](docs/img/video.png)

## What it never does

- **Connect to the internet.** No telemetry, crash reports, update checks or online lookups. FFmpeg is restricted to the `file` protocol and to a fixed list of formats, so a crafted file can't make it open a network address.
- **Keep a record of what you open.** No history, recent list, thumbnail cache or library.
- **Look inside your pictures.** No face recognition, tagging or AI. Nothing from your files is ever shared with anyone: it all stays on your PC.
- **Change your files.** It never writes to your media or their folders.
- **Run in the background.** No service, startup entry or tray icon.
- **Ask for anything.** No account, ads, bundled extras or administrator rights.

The only file it writes is your own options, `%APPDATA%\ReiksViewer\settings.json`.

What it can't control: Windows may list files you open from Explorer among its recent items, and diagnostic data and Recall are Windows settings.

## What it does

- **Frame stepping, both ways.** Tap `X` or `Z` to move exactly one frame forward or back; hold to keep going, faster the longer you hold. Every frame is shown complete.
- **Pictures, GIFs and video in one window.** Open one file and move through the rest of the folder. Zoom, pan, rotate and flip.
- **Private on screen too.** Optionally leave the window out of screenshots, recordings and screen sharing; hide file names; press `H` to blank the window at once.
- **Picture controls.** Brightness, contrast, saturation, gamma and warmth, for display only.
- **Your keys, your colour.** Choose the keys for the main actions and the colour of the window.
- **Folder playback.** Move on when a video ends, in order or shuffled, once or on repeat; or repeat one file.

## Keys

| Action | Key |
|---|---|
| Previous / next file | `Left` / `Right`, or `C` / `V` |
| Play / pause | `Space` |
| One frame back / forward (hold to keep going) | `Z` / `X` |
| Jump 5 seconds | `Ctrl` + `Left` / `Right` |
| Zoom | mouse wheel, `+` / `-`, `0` to reset |
| Rotate right | `R` |
| Picture settings | `A` |
| Play the whole folder | `S` |
| Fullscreen | `F11`, `F` or double-click |
| Open a file | `O` |
| Formats and credits | `F1` |
| Hide the picture | `H` |

`Space`, `C`, `V`, `Z`, `X` and `H` can be changed in Options.

## Building from source

You need Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download).

1. Get FFmpeg 8.1 as an LGPL **shared** build for Windows x64, for example
   `ffmpeg-n8.1-latest-win64-lgpl-shared-8.1.zip` from
   [BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds/releases).
2. Copy these files from its `bin` folder into `codecs`:
   `avcodec-62.dll`, `avformat-62.dll`, `avutil-60.dll`, `swresample-6.dll`, `swscale-9.dll`.
3. Build and run:

   ```
   dotnet build ReiksViewer.csproj -c Release
   bin\Release\net10.0-windows\ReiksViewer.exe
   ```

The published releases are built from this repository by GitHub Actions (`.github/workflows/release.yml`), and each file carries a build provenance attestation you can check with `gh attestation verify <file> --repo reiks-apps/reiks-viewer`.

`release.ps1` builds the installer (it needs [Inno Setup 6](https://jrsoftware.org/isinfo.php)) and the portable zip into `dist`.

The picture adjustments use a small pixel shader, `Adjust.fx`. Its compiled form `Adjust.ps` is checked in; to rebuild it, run `fxc /T ps_2_0 /E main /Fo Adjust.ps Adjust.fx` from the Windows SDK.

## Licence and credits

Reiks Viewer's own code is under the [MIT licence](LICENSE).

Pictures and video are decoded by libraries from the [FFmpeg](https://ffmpeg.org) project, under the GNU Lesser General Public License. They are shipped with the releases as separate, unmodified DLLs, and the FFmpeg source for the shipped build is attached to each release. Details are in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt). Reiks Viewer is not affiliated with or endorsed by the FFmpeg project.

The screenshots show the *Sintel* trailer, © Blender Foundation (durian.blender.org), CC BY 3.0; the Carina Nebula "Cosmic Cliffs", NASA, ESA, CSA and STScI; and Blue Marble 2012, NASA Goddard Space Flight Center.

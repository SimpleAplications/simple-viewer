# Security

## Reporting a problem

If you find a security or privacy problem in Reiks Viewer, please report it privately:
open the repository's **Security** tab and choose **Report a vulnerability**.
That sends the report only to the maintainer.

Please don't open a public issue for something that could put users at risk
before it has been fixed.

Useful things to include: what you did, what happened, the version of Reiks Viewer
(press F1 in the app), and a sample file if a specific file triggers the problem.

## What counts

- A file that makes Reiks Viewer crash in a way that could run code.
- Anything that makes Reiks Viewer contact the network, or write outside its own
  options file (`%APPDATA%\ReiksViewer\settings.json`).
- A way the installer or the app could be made to load code that isn't its own.

Problems in FFmpeg itself are best reported to the FFmpeg project
(https://ffmpeg.org/security.html); Reiks Viewer ships FFmpeg unmodified and picks up
fixes by updating to newer FFmpeg releases.

## Supported versions

Only the latest release is maintained. Reiks Viewer does not update itself, so please
check https://github.com/reiks-apps/reiks-viewer/releases for new versions.

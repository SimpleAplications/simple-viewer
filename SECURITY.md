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
- Anything that makes Reiks Viewer contact the network, or, in normal use, write
  anywhere but its own options file (`%APPDATA%\ReiksViewer\settings.json`). The
  developer checks started from the command line (`--selftest` and the like) write
  to the folder they are given; that is intended.
- A way the installer or the app could be made to load code that isn't its own.

Problems in FFmpeg itself are best reported to the FFmpeg project
(https://ffmpeg.org/security.html). Reiks Viewer ships FFmpeg unmodified, so FFmpeg's
fixes reach users when a new version of Reiks Viewer includes a newer FFmpeg.

## What to expect

Reiks Viewer has not had an independent security review. It decodes files with FFmpeg,
a large C library in which security bugs are found and fixed regularly, and it runs
with your normal user rights, without a sandbox. Its privacy measures (no networking in
its own code, FFmpeg limited to local files and a fixed list of formats) reduce what a
hostile file can do; they are not a guarantee. Keep the app up to date, and take care
with files from sources you don't trust.

## Supported versions

Only the latest release is maintained. Reiks Viewer does not update itself, so please
check https://github.com/reiks-apps/reiks-viewer/releases for new versions.

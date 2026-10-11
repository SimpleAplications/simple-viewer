# Security

## Reporting a problem

If you find a security or privacy problem in Simple, please report it privately:
open the repository's **Security** tab and choose **Report a vulnerability**.
That sends the report only to the maintainer.

Please don't open a public issue for something that could put users at risk
before it has been fixed.

Useful things to include: what you did, what happened, the version of Simple
(press F1 in the app), and a sample file if a specific file triggers the problem.

## What counts

- A file that makes Simple crash in a way that could run code.
- Anything that makes Simple contact the network, or write outside its own
  options file (`%APPDATA%\Simple\settings.json`).
- A way the installer or the app could be made to load code that isn't its own.

Problems in FFmpeg itself are best reported to the FFmpeg project
(https://ffmpeg.org/security.html); Simple ships FFmpeg unmodified and picks up
fixes by updating to newer FFmpeg releases.

## Supported versions

Only the latest release is maintained. Simple does not update itself, so please
check https://github.com/SimpleAplications/simple-viewer/releases for new versions.

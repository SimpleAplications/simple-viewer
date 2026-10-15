# Submitting Reiks Viewer to the Microsoft Store

The Store copy is the same program as the installer and the zip. It notices when it runs from
its Store package (`StorePackage.cs`) and then:

- looks for a build of FFmpeg of the user's own in its data folder first, because the Store
  installs apps in a folder nobody may change ("Open codecs folder" in F1 opens that folder);
- opens its Store page from "Check for updates", because the Store installs its updates;
- opens its own page in Settings from "Default app settings".

What the installer does and the Store package does not: the firewall rule option, and taking
over from the app's old name "Simple". File types ("Open with" and Settings' default apps) come
from the package manifest instead.

## Building the package

1. Set `<Version>` in `ReiksViewer.csproj`. The package version is that plus `.0`.
2. Run `release.ps1`. Besides the installer and the zip it writes
   `dist\store\ReiksViewer-<version>.msix`. It needs the Windows SDK (makeappx, makepri).
3. The package is not signed: Microsoft signs it after its review. It is not for the GitHub
   release.

`installer\store\AppxManifest.xml` holds the package identity. `Name`, `Publisher` and
`PublisherDisplayName` must be the values Partner Center shows under
**Product management → Product identity**. The icons are drawn by
`tools\make_store_assets.ps1`.

## Partner Center, first submission

**Pricing and availability**: Free; all markets; public. No free trial.

**Properties**
- Category: Photo & video.
- Privacy policy: `https://reiks-apps.github.io/reiks-viewer/privacy/`
- Website: `https://reiks-apps.github.io/reiks-viewer/`
- Support contact: the Reiks Apps email address, or `https://github.com/reiks-apps/reiks-viewer/issues`
- "This product accesses, collects, or transmits personal information": No.
- Product declarations: leave the defaults. Don't tick the accessibility declaration: the app
  has not been tested against accessibility guidelines.

**Age ratings** (questionnaire): app type "All other app types". No violence, fear, sexual
content, language, drugs, gambling or discrimination. Users cannot interact with each other or
share content. No location sharing. No digital purchases. No unrestricted internet access.

**Packages**: upload `dist\store\ReiksViewer-<version>.msix`. Device family: Windows 10/11
desktop.

**Store listings**: from `store\listing.md`, one listing per language. Screenshots from
`dist\store\screenshots\<language>`, 1600 × 900, in the order of the captions. Store logo
(1:1, 300 × 300): `store\images\logo-300.png`.
- Copyright and trademark info: `© 2026 Reiks Apps`
- Developed by: `Reiks Apps`
- Additional license terms: `Reiks Viewer is free software under the MIT License. It includes
  libraries from the FFmpeg project under the GNU LGPL, version 3 or later; the source code of
  the included build is published with each release at
  https://github.com/reiks-apps/reiks-viewer/releases`

**Submission options**
- Why the app needs the restricted capability `runFullTrust`: `Reiks Viewer is a desktop
  app (WPF, .NET) packaged as MSIX. runFullTrust is what every packaged desktop app of this
  kind declares in order to run.`
- Notes for certification: `Reiks Viewer is a picture and video viewer. No account or
  sign-in is needed. To try it, press O (or the folder button at the bottom left) and open
  any picture or video, or drag a file onto the window. The arrow keys move through the
  other files in that folder. F1 lists the supported formats. Ctrl+P opens printing. The
  app makes no network connections of its own.`

## Every later version

Set the new version, run `release.ps1`, and in Partner Center start a new submission:
replace the package with the new `.msix` and fill in "What's new in this version" for each
listing. Publish the same version on GitHub as well, so both stay at the same version.

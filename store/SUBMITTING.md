# Publishing Reiks Viewer: GitHub and the Microsoft Store

Every version goes out twice, at the same version number: as the installer and zip on GitHub,
and as a package in the Microsoft Store. Both are built from the same commit.

## What differs in the Store copy

The Store copy is the same program as the installer and the zip. It notices when it runs from
its Store package (`StorePackage.cs`) and then:

- looks for a build of FFmpeg of the user's own in its data folder first, because the Store
  installs apps in a folder nobody may change ("Open codecs folder" in F1 opens that folder).
  The FFmpeg licence requires that users can swap in their own FFmpeg;
- opens its Store page from "Check for updates", because the Store installs its updates;
- opens its own page in Settings from "Default app settings";
- keeps its options in the package's own data folder; Windows redirects them there.

What the installer does and the Store package does not: the firewall rule option, and taking
over from the app's old name "Simple". File types ("Open with" and Settings' default apps) come
from the package manifest instead.

`ReiksViewer.exe --storecheck <folder>` writes what the app sees of its package, and a picture
of the F1 window.

## The package identity

Fixed for the life of the app; `installer\store\AppxManifest.xml` must carry exactly these:

- Name: `ReiksApps.Reiksviewer` (lowercase v)
- Publisher: `CN=54F64895-82FA-479D-9B7B-BE225219235C`
- PublisherDisplayName: `Reiks Apps`
- Store ID: `9N4D3NC940GD`; Store page `https://apps.microsoft.com/detail/9N4D3NC940GD`

The package's DisplayName must be a name reserved in Partner Center, with the same capitals:
"Reiks Viewer".

## The order of a release

1. Set `<Version>` in `ReiksViewer.csproj`. Write `release-notes\v<version>.md`. Run
   `python tools\build_site.py` (the website names the version and links to its files).
2. Run `release.ps1`. It writes the installer, the zip and their checksums into `dist\`, and
   the Store package into `dist\store\ReiksViewer-<version>.msix` (needs the Windows SDK:
   makeappx and makepri). The package is not signed: Microsoft signs it after its review.
   `release.ps1` clears `dist\store` each time, so nothing else is kept there.
3. Commit. Publish on GitHub in this order: push the tag `v<version>`, wait until GitHub has
   built the release (about five minutes; the workflow builds the installer and the zip, not
   the Store package), then push `main`, which updates the website. The other way round, the
   website's download button leads nowhere until the release exists.
4. Submit the package in Partner Center (below). If the submission needs anything that is only
   on the website (the privacy policy), the website has to be live first: Microsoft's testers
   open those links.

For 1.5.0, the first submission, certification passed within a day of submitting.

## Partner Center, step by step

Partner Center: Apps and games → Reiks Viewer. A submission's sections appear after
**Application overview → Start submission** (or the existing draft).

**Pricing and availability**
- Base price: choose any currency, then **Free** as the retail price. The "Default" market
  group holds all markets. No free trial, no sale pricing; organizational licensing as it is.
- Visibility, on the same page: **Private audience** with a group holding only the developer's
  own account, for a first submission or a risky change; **Public audience** otherwise.

**Properties**
- Category: Photo & video. No subcategory.
- "Does this product access, collect or transmit personal information?" **Yes**: it opens the
  user's pictures, which can hold personal information, even though nothing leaves the
  computer. Then the privacy policy URL: `https://reiks-apps.github.io/reiks-viewer/privacy/`
  (the link next to the question is Microsoft's App Developer Agreement, not a template).
- Website: `https://reiks-apps.github.io/reiks-viewer/`
- Support contact: the Reiks Apps email address, or `https://github.com/reiks-apps/reiks-viewer/issues`
- **Display mode: PC and HoloLens both unticked.** Ticking PC means "a Windows Mixed Reality
  app" and then demands headset requirements.
- Product declarations: untick "Windows can include this product's data in automatic backups to
  OneDrive" (the options file would go to the cloud) and "record and broadcast clips" (games
  only). Leave accessibility unticked (not tested against the guidelines), generative AI
  unticked (none in the app), install to other drives ticked.
- System requirements: nothing as Minimum (it warns users, and we have not tested low-end PCs);
  Keyboard and Mouse as Recommended.

**Age ratings** (IARC questionnaire): "All Other App Types", then No to every question:
downloaded content, user content sharing, online content (the app shows the user's own files,
not content from a service), age-restricted products, location, digital purchases, cash
rewards, web browser, news or education; No to ratings boards and physical media. Result:
3+ / Everyone. Microsoft gives IARC the publisher name and the account's email address.

**Packages**: drag `dist\store\ReiksViewer-<version>.msix` in. Expected: a warning that
`runFullTrust` needs approval (answered under Submission options). Device family: only
Windows 10/11 Desktop; untick "Let Microsoft decide ... future device families".

**Store listings**: the languages come from the package (`<Resources>` in the manifest): seven.
Each needs at least a description and one screenshot. Quickest:
1. Take the screenshots: `bash tools\store_screenshots.sh <media folder>` writes
   `dist\store-screenshots\<language>\` (1600 × 900; the Store wants at least 1366 × 768).
2. **Export listings**, then `python tools\fill_store_listing.py <exported .csv> <folder>`
   fills it from `store\listing.md` and builds a `ReiksViewerListing` folder with the file,
   the screenshots and the logos (300 × 300 and 1080 × 1080, from `tools\make_store_assets.ps1`).
3. **Import listings → Import folder**, choosing the `ReiksViewerListing` folder itself. One
   wrong field refuses the whole import ("View errors for last import" says why).

Each listing's title is chosen from the reserved names. Extra names (one per language, if
wanted) are reserved inside the app, under **Product management → Manage app names**, one at
a time; **New product** would create a separate app instead. Reserved names that are not used
within three months are removed. The Start menu name comes from the package, not the listing.

**Submission options**
- Publishing hold: as soon as it passes certification (or hold it, to publish at a chosen time).
- Restricted capability `runFullTrust`, at most 500 characters:
  `Reiks Viewer is a Win32 desktop app (WPF, .NET 10) packaged as MSIX. Like any packaged
  desktop app, it needs runFullTrust to run as a full-trust process. It uses it to open the
  picture and video files the user chooses, decode them with FFmpeg DLLs shipped in the
  package, print through Windows, and on request keep its window out of screen capture. No
  services, drivers, startup entries or network code.`
- Notes for certification are on their own page: left menu, **Supplemental info → Additional
  Testing Information**; no credentials:
  `Reiks Viewer is a picture and video viewer. No account or sign-in is needed. To try it,
  press O (or the folder button at the bottom left) and open any picture or video, or drag a
  file onto the window. The arrow keys move through the other files in that folder. F1 lists
  the supported formats. Ctrl+P opens printing. The app makes no network connections of its
  own.`

Then **Submit for certification**. The stages are Submission, Pre-processing, Certification and
Publishing; an email comes when it is done.

## After a private submission

Install from the Store with the account in the private group (the Microsoft Store app must be
signed in with it): `ms-windows-store://pdp/?productid=9N4D3NC940GD`. Check that it starts, that
"Open codecs folder" opens a folder under `%LOCALAPPDATA%\Packages\`, that "Check for updates"
opens the Store page, and that pictures and videos list it under "Open with". Then submit again
with Public audience, and add the Store link to the website.

## Every later version

Steps 1–3 of "The order of a release", then a new submission in Partner Center: replace the
package with the new `.msix` and fill in "What's new in this version" for each listing
(`store\listing.md`, then the listing script and an import).

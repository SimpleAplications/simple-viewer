# Builds everything to publish for a release into .\dist:
#   ReiksViewer-Setup-<version>.exe, ReiksViewer-<version>-portable.zip
# Set the version first, in ReiksViewer.csproj (<Version>). Close Reiks Viewer before running.
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$app = Join-Path $root "dist\ReiksViewer"
$iscc = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "Inno Setup 6 was not found." }

if (Test-Path $app) { Remove-Item -LiteralPath $app -Recurse -Force }
dotnet publish (Join-Path $root "ReiksViewer.csproj") -c Release -r win-x64 --self-contained true -o $app -p:DebugType=none -nologo -v q
if ($LASTEXITCODE -ne 0) { throw "Publish failed." }

$version = (Get-Item (Join-Path $app "ReiksViewer.exe")).VersionInfo.ProductVersion -replace '\+.*$', ''
& $iscc /Q (Join-Path $root "installer\ReiksViewer.iss")
if ($LASTEXITCODE -ne 0) { throw "Installer build failed." }

# The Microsoft Store package, in dist\store: it goes to Partner Center, not into the GitHub
# release. Microsoft signs it after its review, so it is left unsigned here. Needs the
# Windows SDK; without it this step is skipped, and the GitHub release build skips it too.
$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.*\x64\makeappx.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName | Select-Object -Last 1
if ($makeappx -and -not $env:GITHUB_ACTIONS) {
    $kit = Split-Path $makeappx.FullName
    $store = Join-Path $root "dist\store"
    $layout = Join-Path $store "layout"
    $icons = Join-Path $store "icons"
    if (Test-Path $store) { Remove-Item -LiteralPath $store -Recurse -Force }
    Copy-Item $app $layout -Recurse
    Copy-Item (Join-Path $root "installer\store\Assets") (Join-Path $layout "Assets") -Recurse
    $manifest = (Get-Content (Join-Path $root "installer\store\AppxManifest.xml") -Raw).Replace("{{version}}", "$version.0")
    [System.IO.File]::WriteAllText((Join-Path $layout "AppxManifest.xml"), $manifest)
    # The index that lets Windows pick the right icon for each size and display scale.
    New-Item -ItemType Directory $icons | Out-Null
    Copy-Item (Join-Path $root "installer\store\Assets") (Join-Path $icons "Assets") -Recurse
    & (Join-Path $kit "makepri.exe") new /pr $icons /cf (Join-Path $root "installer\store\priconfig.xml") /mn (Join-Path $layout "AppxManifest.xml") /of (Join-Path $layout "resources.pri") /o | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Icon index build failed." }
    & $makeappx.FullName pack /d $layout /p (Join-Path $store "ReiksViewer-$version.msix") /o | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Store package build failed." }
}

$zip = Join-Path $root "dist\ReiksViewer-$version-portable.zip"
Compress-Archive -Path $app -DestinationPath $zip -Force

# Copies without the version in the name: the website's download links point at these,
# so they keep working for every future release. Upload them with the versioned files.
$dist = Join-Path $root "dist"
Copy-Item (Join-Path $dist "ReiksViewer-Setup-$version.exe") (Join-Path $dist "ReiksViewer-Setup.exe") -Force
Copy-Item $zip (Join-Path $dist "ReiksViewer-portable.zip") -Force

# Checksums people can compare their download against.
Get-ChildItem $dist -File | Where-Object Name -ne "SHA256SUMS.txt" | Sort-Object Name | ForEach-Object {
    "{0}  {1}" -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower(), $_.Name
} | Set-Content (Join-Path $dist "SHA256SUMS.txt") -Encoding ascii
Get-ChildItem (Join-Path $root "dist") -File | Select-Object Name, @{ n = "MB"; e = { [math]::Round($_.Length / 1MB, 1) } }

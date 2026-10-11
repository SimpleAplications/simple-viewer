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

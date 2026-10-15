#!/bin/bash
# The Microsoft Store listing screenshots: 1600 x 900, four per language, into
# dist/store-screenshots/<language>/ (outside dist/store, which release.ps1 clears).
#   bash tools/store_screenshots.sh <media folder> [language ...]
# The media folder holds the files the website's screenshots show too (credited in its footer):
# "Sintel trailer.mp4", "Carina Nebula.jpg" and "Blue Marble.jpg". Build the app first.
media="$1"; shift
[ -d "$media" ] || { echo "usage: store_screenshots.sh <media folder> [language ...]"; exit 1; }
cd "$(dirname "$0")/.." || exit 1
APP="bin/Release/net10.0-windows/ReiksViewer.exe"
for lang in ${*:-en es sv fr de pt it}; do
  out="dist/store-screenshots/$lang"; mkdir -p "$out"
  shot() { "$APP" --lang "$lang" --screenshot "$(cygpath -w "$media/$1")" "$(cygpath -w "$out/$2")" "$3" "$4" 1600x900 2>/dev/null; }
  shot "Sintel trailer.mp4" 1-video.png 25.166 none
  shot "Carina Nebula.jpg" 2-image-settings.png 0 picture
  shot "Blue Marble.jpg" 3-print.png 0 print
  shot "Blue Marble.jpg" 4-options.png 0 options
  echo "$lang: $(ls "$out" | tr '\n' ' ')"
done

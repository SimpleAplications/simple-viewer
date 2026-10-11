# Builds the website into docs/: docs/index.html (English), docs/es/, docs/sv/.
# The page layout is in site_template.html; every text is in the table below.
# To add a language: add a column to every entry and a line to LANGUAGES, then run
#   python tools/build_site.py
import io, os

LANGUAGES = [("en", "English", ""), ("es", "Español", "es/"), ("sv", "Svenska", "sv/")]

T = {
"html.title": ("Reiks Viewer - picture and video viewer for Windows", "Reiks Visor - visor de imágenes y vídeos para Windows", "Reiks Visare - bild- och videovisare för Windows"),
"html.description": (
 "A simple, free picture and video viewer for Windows, made for private use. It never connects to the internet, keeps no history, and can hide itself from screenshots and screen sharing.",
 "Un visor de imágenes y vídeos sencillo y gratuito para Windows, pensado para uso privado. Nunca se conecta a internet, no guarda historial y puede ocultarse de las capturas y de la pantalla compartida.",
 "En enkel och gratis bild- och videovisare för Windows, gjord för privat bruk. Den ansluter aldrig till internet, sparar ingen historik och kan dölja sig från skärmbilder och skärmdelning."),
"name": ("Reiks Viewer", "Reiks Visor", "Reiks Visare"),
"icon.alt": ("Reiks Viewer icon", "Icono de Reiks Visor", "Ikon för Reiks Visare"),
"tagline": (
 "Your pictures and videos stay yours. Reiks Viewer shows them only to you, and that is all it does.",
 "Tus imágenes y vídeos siguen siendo tuyos. Reiks Visor te los muestra solo a ti, y eso es todo lo que hace.",
 "Dina bilder och videor förblir dina. Reiks Visare visar dem bara för dig, och det är allt den gör."),
"download": ("Download for Windows", "Descargar para Windows", "Ladda ner för Windows"),
"under": (
 "Free &middot; open source &middot; never connects to the internet &middot; Windows 10 or 11, 64-bit",
 "Gratis &middot; código abierto &middot; nunca se conecta a internet &middot; Windows 10 u 11, 64 bits",
 "Gratis &middot; öppen källkod &middot; ansluter aldrig till internet &middot; Windows 10 eller 11, 64-bitars"),
"portable": ("Portable zip (no install)", "Zip portátil (sin instalar)", "Portabel zip (ingen installation)"),
"checksums": ("Checksums", "Sumas de comprobación", "Kontrollsummor"),
"allversions": ("All versions", "Todas las versiones", "Alla versioner"),
"shot.video.alt": (
 "Reiks Viewer showing a paused video, with the time shown to the millisecond and buttons to step one frame back or forward",
 "Reiks Visor mostrando un vídeo en pausa, con el tiempo al milisegundo y botones para avanzar o retroceder un fotograma",
 "Reiks Visare visar en pausad video, med tiden angiven i millisekunder och knappar för att stega en bildruta bakåt eller framåt"),

"never.h": ("What Reiks Viewer never does", "Lo que Reiks Visor nunca hace", "Det Reiks Visare aldrig gör"),
"never.lead": (
 "Most viewers and players quietly do more than show your files. Reiks Viewer is built so that it can&rsquo;t.",
 "La mayoría de visores y reproductores hacen, sin decirlo, algo más que mostrar tus archivos. Reiks Visor está hecho para que no pueda.",
 "De flesta visare och spelare gör i tysthet mer än att visa dina filer. Reiks Visare är byggd så att den inte kan det."),
"never.1h": ("Connect to the internet", "Conectarse a internet", "Ansluta till internet"),
"never.1": (
 "No telemetry, no usage statistics, no crash reports, no update checks, no online lookups. Its decoders are restricted to local files, so not even a specially made file can make it reach out.",
 "Sin telemetría, sin estadísticas de uso, sin informes de errores, sin comprobación de actualizaciones, sin consultas en línea. Sus decodificadores están limitados a archivos locales, así que ni un archivo preparado a propósito puede hacer que se conecte.",
 "Ingen telemetri, ingen användningsstatistik, inga kraschrapporter, inga uppdateringskontroller, inga uppslagningar på nätet. Dess avkodare är begränsade till lokala filer, så inte ens en särskilt utformad fil kan få den att ansluta."),
"never.2h": ("Keep a record of what you open", "Guardar registro de lo que abres", "Spara vad du öppnar"),
"never.2": (
 "No history, no recent list, no thumbnail cache, no library or index of your folders.",
 "Sin historial, sin lista de recientes, sin caché de miniaturas, sin biblioteca ni índice de tus carpetas.",
 "Ingen historik, ingen lista över senaste filer, ingen miniatyrcache, inget bibliotek eller index över dina mappar."),
"never.3h": ("Look inside your pictures", "Mirar dentro de tus imágenes", "Titta in i dina bilder"),
"never.3": (
 "No face recognition, no tagging, no AI, no &ldquo;enhancing&rdquo; in the cloud. Nothing from your files is ever shared with anyone: it all stays on your PC.",
 "Sin reconocimiento facial, sin etiquetado, sin IA, sin &laquo;mejoras&raquo; en la nube. Nada de tus archivos se comparte jamás con nadie: todo se queda en tu PC.",
 "Ingen ansiktsigenkänning, ingen taggning, ingen AI, ingen &rdquo;förbättring&rdquo; i molnet. Ingenting från dina filer delas någonsin med någon: allt stannar på din dator."),
"never.4h": ("Change your files", "Modificar tus archivos", "Ändra dina filer"),
"never.4": (
 "It never writes to your pictures, videos or their folders: no ratings, no tags, no hidden side files.",
 "Nunca escribe en tus imágenes, vídeos ni en sus carpetas: sin valoraciones, sin etiquetas, sin archivos ocultos al lado.",
 "Den skriver aldrig till dina bilder, videor eller deras mappar: inga betyg, inga taggar, inga dolda sidofiler."),
"never.5h": ("Run in the background", "Ejecutarse en segundo plano", "Köra i bakgrunden"),
"never.5": (
 "No service, no startup entry, no tray icon. When you close it, it is gone.",
 "Sin servicio, sin inicio automático, sin icono en la bandeja. Cuando lo cierras, desaparece.",
 "Ingen tjänst, ingen autostart, ingen ikon i meddelandefältet. När du stänger den är den borta."),
"never.6h": ("Ask for anything", "Pedirte nada", "Be om någonting"),
"never.6": (
 "No account, no sign-in, no ads, no bundled extras, no administrator rights.",
 "Sin cuenta, sin inicio de sesión, sin anuncios, sin extras incluidos, sin permisos de administrador.",
 "Inget konto, ingen inloggning, ingen reklam, inga medföljande extraprogram, inga administratörsrättigheter."),

"does.h": ("What it does", "Lo que hace", "Vad den gör"),
"does.1h": ("One frame at a time, both ways", "Fotograma a fotograma, en ambos sentidos", "En bildruta i taget, åt båda håll"),
"does.1": (
 "Tap <kbd>X</kbd> or <kbd>Z</kbd> to move exactly one frame forward or back. Hold to keep going, faster the longer you hold. Every frame is shown complete, with no smearing after a jump.",
 "Pulsa <kbd>X</kbd> o <kbd>Z</kbd> para avanzar o retroceder exactamente un fotograma. Mantén pulsado para continuar, más rápido cuanto más tiempo. Cada fotograma se muestra completo, sin imagen corrupta tras un salto.",
 "Tryck på <kbd>X</kbd> eller <kbd>Z</kbd> för att gå exakt en bildruta framåt eller bakåt. Håll nere för att fortsätta, snabbare ju längre du håller. Varje bildruta visas hel, utan utsmetad bild efter ett hopp."),
"does.2h": ("Pictures, GIFs and video together", "Imágenes, GIF y vídeo juntos", "Bilder, GIF:ar och video tillsammans"),
"does.2": (
 "Open one file and move through everything else in the folder with the arrow keys. Zoom with the mouse wheel, drag to pan, rotate and flip.",
 "Abre un archivo y recorre todo lo demás de la carpeta con las flechas. Amplía con la rueda del ratón, arrastra para desplazarte, gira y voltea.",
 "Öppna en fil och bläddra genom allt annat i mappen med piltangenterna. Zooma med mushjulet, dra för att panorera, rotera och spegla."),
"does.3h": ("Private on screen too", "Privado también en pantalla", "Privat även på skärmen"),
"does.3": (
 "Leave the window out of screenshots, recordings and screen sharing: they show whatever is behind it instead. Hide file names from the title bar. Press <kbd>H</kbd> to blank the window at once.",
 "Deja la ventana fuera de capturas, grabaciones y pantalla compartida: en su lugar se ve lo que hay detrás. Oculta los nombres de archivo de la barra de título. Pulsa <kbd>H</kbd> para dejar la ventana en blanco al instante.",
 "Håll fönstret utanför skärmbilder, inspelningar och skärmdelning: de visar det som finns bakom i stället. Dölj filnamn i namnlisten. Tryck på <kbd>H</kbd> för att tömma fönstret direkt."),
"does.4h": ("Picture controls", "Ajustes de imagen", "Bildreglage"),
"does.4": (
 "Brightness, contrast, saturation, gamma and warmth for anything on screen, including paused video. Display only: your files are never changed.",
 "Brillo, contraste, saturación, gamma y calidez para cualquier cosa en pantalla, incluido el vídeo en pausa. Solo afecta a la visualización: tus archivos nunca se modifican.",
 "Ljusstyrka, kontrast, mättnad, gamma och värme för allt på skärmen, även pausad video. Påverkar bara visningen: dina filer ändras aldrig."),
"does.5h": ("Your keys, your colour", "Tus teclas, tu color", "Dina tangenter, din färg"),
"does.5": (
 "Choose the keys for play, next, previous and frame stepping, and pick the window colour. Text switches between white and black to stay readable.",
 "Elige las teclas para reproducir, siguiente, anterior y avance por fotogramas, y el color de la ventana. El texto cambia entre blanco y negro para seguir siendo legible.",
 "Välj tangenter för spela, nästa, föregående och bildrutestegning, och välj fönstrets färg. Texten växlar mellan vitt och svart för att förbli läsbar."),
"does.6h": ("Play a whole folder", "Reproducir toda una carpeta", "Spela en hel mapp"),
"does.6": (
 "Let it move to the next file when a video ends, in order or shuffled, once or on repeat. Or repeat a single video.",
 "Deja que pase al archivo siguiente cuando termine un vídeo, en orden o al azar, una vez o en bucle. O repite un solo vídeo.",
 "Låt den gå vidare till nästa fil när en video tar slut, i ordning eller blandat, en gång eller om och om igen. Eller upprepa en enda video."),

"shot.picture.alt": (
 "The Picture panel with sliders for brightness, contrast, saturation, gamma and warmth",
 "El panel Imagen con controles de brillo, contraste, saturación, gamma y calidez",
 "Panelen Bild med reglage för ljusstyrka, kontrast, mättnad, gamma och värme"),
"shot.picture.cap": ("Picture controls apply instantly and reset with one click.", "Los ajustes de imagen se aplican al instante y se restablecen con un clic.", "Bildreglagen verkar direkt och återställs med ett klick."),
"shot.options.alt": (
 "The Options panel with playback choices, key assignments and window colour",
 "El panel Opciones con la reproducción, la asignación de teclas y el color de la ventana",
 "Panelen Alternativ med uppspelningsval, tangenttilldelning och fönsterfärg"),
"shot.options.cap": ("Options: playback, keys and window colour.", "Opciones: reproducción, teclas y color de la ventana.", "Alternativ: uppspelning, tangenter och fönsterfärg."),
"shot.note": ("", "Las capturas muestran la aplicación en inglés; también está disponible en español.", "Skärmbilderna visar appen på engelska; den finns också på svenska."),

"formats.h": ("Formats", "Formatos", "Format"),
"formats.pictures": ("Pictures", "Imágenes", "Bilder"),
"formats.animations": ("Animations", "Animaciones", "Animationer"),
"formats.animations.v": ("GIF, animated PNG, animated WebP", "GIF, PNG animado, WebP animado", "GIF, animerad PNG, animerad WebP"),
"formats.video": ("Video", "Vídeo", "Video"),
"formats.video.v": (
 "MP4, MOV, MKV, WebM, AVI, WMV, FLV, MPEG, TS and more, with H.264, H.265, VP9, AV1 and other codecs",
 "MP4, MOV, MKV, WebM, AVI, WMV, FLV, MPEG, TS y más, con H.264, H.265, VP9, AV1 y otros códecs",
 "MP4, MOV, MKV, WebM, AVI, WMV, FLV, MPEG, TS med flera, med H.264, H.265, VP9, AV1 och andra codecar"),
"formats.decoding": ("Decoding", "Decodificación", "Avkodning"),
"formats.decoding.v": (
 "On the graphics card where it supports the format, otherwise on the processor",
 "En la tarjeta gráfica cuando admite el formato; si no, en el procesador",
 "På grafikkortet när det stöder formatet, annars på processorn"),
"formats.languages": ("Languages", "Idiomas", "Språk"),
"formats.languages.v": ("English, Spanish, Swedish, French, German, Portuguese, Italian", "Inglés, español, sueco, francés, alemán, portugués, italiano", "Engelska, spanska, svenska, franska, tyska, portugisiska, italienska"),

"verify.h": ("Don&rsquo;t take our word for it", "No hace falta que nos creas", "Lita inte bara på vårt ord"),
"verify.1": (
 "<strong>Block it and see.</strong> Deny Reiks Viewer all network access in Windows Firewall, or tick that option in the installer when installing for all users. Everything keeps working, because it never needed a connection.",
 "<strong>Bloquéalo y compruébalo.</strong> Deniega a Reiks Visor todo acceso a la red en el Firewall de Windows, o marca esa opción en el instalador al instalar para todos los usuarios. Todo sigue funcionando, porque nunca necesitó conexión.",
 "<strong>Blockera den och se själv.</strong> Neka Reiks Visare all nätverksåtkomst i Windows-brandväggen, eller kryssa i det alternativet i installationsprogrammet när du installerar för alla användare. Allt fungerar som förut, eftersom den aldrig behövde någon anslutning."),
"verify.2": (
 "<strong>Read the code.</strong> Reiks Viewer is <a href=\"https://github.com/reiks-apps/reiks-viewer\">open source</a>. The downloads are built from that code on GitHub&rsquo;s own servers, in public, with a signed record of how each file was made.",
 "<strong>Lee el código.</strong> Reiks Visor es de <a href=\"https://github.com/reiks-apps/reiks-viewer\">código abierto</a>. Las descargas se compilan a partir de ese código en los servidores de GitHub, en público, con un registro firmado de cómo se creó cada archivo.",
 "<strong>Läs koden.</strong> Reiks Visare har <a href=\"https://github.com/reiks-apps/reiks-viewer\">öppen källkod</a>. Nedladdningarna byggs från den koden på GitHubs egna servrar, öppet, med ett signerat intyg om hur varje fil skapades."),
"verify.3": (
 "<strong>Check your download.</strong> Every release lists the SHA-256 checksum of each file.",
 "<strong>Comprueba tu descarga.</strong> Cada versión publica la suma SHA-256 de cada archivo.",
 "<strong>Kontrollera din nedladdning.</strong> Varje version anger SHA-256-kontrollsumman för varje fil."),

"limits.h": ("What Reiks Viewer can&rsquo;t control", "Lo que Reiks Visor no puede controlar", "Det Reiks Visare inte kan styra över"),
"limits.1": (
 "Windows itself keeps some records that no app can switch off for you. When you open a file by double-clicking it in Explorer, Windows may add it to its own recent files list (Settings &rarr; Personalization &rarr; Start &rarr; &ldquo;Show recently opened items&rdquo;). Windows&rsquo; diagnostic data and Recall are also Windows settings, under Privacy &amp; security. Reiks Viewer&rsquo;s own Open dialog does not add to the recent list, and its &ldquo;hide from screenshots&rdquo; option keeps its window out of screen captures.",
 "El propio Windows guarda algunos registros que ninguna aplicación puede desactivar por ti. Cuando abres un archivo con doble clic en el Explorador, Windows puede añadirlo a su lista de archivos recientes (Configuración &rarr; Personalización &rarr; Inicio &rarr; &laquo;Mostrar elementos abiertos recientemente&raquo;). Los datos de diagnóstico de Windows y Recall también son ajustes de Windows, en Privacidad y seguridad. El cuadro Abrir de Reiks Visor no añade nada a la lista de recientes, y su opción de ocultarse en las capturas mantiene la ventana fuera de ellas.",
 "Windows sparar själv vissa uppgifter som ingen app kan stänga av åt dig. När du öppnar en fil genom att dubbelklicka på den i Utforskaren kan Windows lägga till den i sin egen lista över senaste filer (Inställningar &rarr; Anpassning &rarr; Start &rarr; &rdquo;Visa nyligen öppnade objekt&rdquo;). Windows diagnostikdata och Recall är också Windows-inställningar, under Sekretess och säkerhet. Reiks Visares egen Öppna-dialog lägger inte till något i listan, och alternativet att dölja sig från skärmbilder håller fönstret utanför dem."),
"limits.2": (
 "<strong>The one file Reiks Viewer writes</strong> is your options: your keys, window colour, language, playback and privacy choices, in <code>%APPDATA%\\ReiksViewer\\settings.json</code>. It contains nothing about your files, and the uninstaller removes it.",
 "<strong>El único archivo que escribe Reiks Visor</strong> son tus opciones: tus teclas, el color de la ventana, el idioma y tus preferencias de reproducción y privacidad, en <code>%APPDATA%\\ReiksViewer\\settings.json</code>. No contiene nada sobre tus archivos, y el desinstalador lo elimina.",
 "<strong>Den enda fil Reiks Visare skriver</strong> är dina alternativ: dina tangenter, fönsterfärg, språk samt uppspelnings- och integritetsval, i <code>%APPDATA%\\ReiksViewer\\settings.json</code>. Den innehåller ingenting om dina filer, och avinstallationen tar bort den."),

"install.h": ("Installing", "Instalación", "Installation"),
"install.1": (
 "<strong>Windows may warn you the first time.</strong> The installer is not digitally signed yet, so Windows can show &ldquo;Windows protected your PC&rdquo;. Choose <em>More info</em>, then <em>Run anyway</em>.",
 "<strong>Windows puede avisarte la primera vez.</strong> El instalador aún no está firmado digitalmente, así que Windows puede mostrar &laquo;Windows protegió su PC&raquo;. Elige <em>Más información</em> y luego <em>Ejecutar de todas formas</em>.",
 "<strong>Windows kan varna dig första gången.</strong> Installationsprogrammet är ännu inte digitalt signerat, så Windows kan visa &rdquo;Windows skyddade datorn&rdquo;. Välj <em>Mer information</em> och sedan <em>Kör ändå</em>."),
"install.2": (
 "To check that your download is genuine, compare its SHA-256 checksum with <a href=\"https://github.com/reiks-apps/reiks-viewer/releases/latest/download/SHA256SUMS.txt\">SHA256SUMS.txt</a>. In PowerShell: <code>Get-FileHash ReiksViewer-Setup.exe</code>",
 "Para comprobar que tu descarga es auténtica, compara su suma SHA-256 con <a href=\"https://github.com/reiks-apps/reiks-viewer/releases/latest/download/SHA256SUMS.txt\">SHA256SUMS.txt</a>. En PowerShell: <code>Get-FileHash ReiksViewer-Setup.exe</code>",
 "För att kontrollera att din nedladdning är äkta, jämför dess SHA-256-kontrollsumma med <a href=\"https://github.com/reiks-apps/reiks-viewer/releases/latest/download/SHA256SUMS.txt\">SHA256SUMS.txt</a>. I PowerShell: <code>Get-FileHash ReiksViewer-Setup.exe</code>"),
"install.3": (
 "The installer needs no administrator rights and can add Reiks Viewer to Explorer&rsquo;s &ldquo;Open with&rdquo; list. To update, run a newer installer over the old one; &ldquo;Check for updates&rdquo; in the app (F1) opens the download page in your browser, and the app itself stays offline.",
 "El instalador no necesita permisos de administrador y puede añadir Reiks Visor a la lista &laquo;Abrir con&raquo; del Explorador. Para actualizar, ejecuta un instalador más reciente sobre el anterior; &laquo;Buscar actualizaciones&raquo; en la aplicación (F1) abre la página de descarga en tu navegador, y la aplicación en sí sigue sin conexión.",
 "Installationsprogrammet kräver inga administratörsrättigheter och kan lägga till Reiks Visare i Utforskarens lista &rdquo;Öppna med&rdquo;. För att uppdatera kör du ett nyare installationsprogram över det gamla; &rdquo;Sök efter uppdateringar&rdquo; i appen (F1) öppnar nedladdningssidan i din webbläsare, och själva appen förblir offline."),

"oss.h": ("Open source inside", "Código abierto por dentro", "Öppen källkod inuti"),
"oss.1": (
 "Reiks Viewer decodes pictures and video with libraries from the <a href=\"https://ffmpeg.org\">FFmpeg</a> project, licensed under the <a href=\"https://www.gnu.org/licenses/lgpl-3.0.html\">GNU Lesser General Public License</a>. FFmpeg is included as separate, unmodified DLL files that you may replace with your own build. The FFmpeg source code for the included build is published with every <a href=\"https://github.com/reiks-apps/reiks-viewer/releases/latest\">release</a>, and the full notices are in THIRD-PARTY-NOTICES.txt inside the app&rsquo;s folder.",
 "Reiks Visor decodifica imágenes y vídeo con bibliotecas del proyecto <a href=\"https://ffmpeg.org\">FFmpeg</a>, con licencia <a href=\"https://www.gnu.org/licenses/lgpl-3.0.html\">GNU Lesser General Public License</a>. FFmpeg se incluye como archivos DLL independientes y sin modificar, que puedes sustituir por tu propia compilación. El código fuente de FFmpeg de la compilación incluida se publica con cada <a href=\"https://github.com/reiks-apps/reiks-viewer/releases/latest\">versión</a>, y los avisos completos están en THIRD-PARTY-NOTICES.txt, dentro de la carpeta de la aplicación.",
 "Reiks Visare avkodar bilder och video med bibliotek från <a href=\"https://ffmpeg.org\">FFmpeg</a>-projektet, licensierade under <a href=\"https://www.gnu.org/licenses/lgpl-3.0.html\">GNU Lesser General Public License</a>. FFmpeg ingår som separata, oförändrade DLL-filer som du får ersätta med ditt eget bygge. FFmpeg-källkoden för det medföljande bygget publiceras med varje <a href=\"https://github.com/reiks-apps/reiks-viewer/releases/latest\">version</a>, och de fullständiga upplysningarna finns i THIRD-PARTY-NOTICES.txt i appens mapp."),
"oss.2": (
 "Reiks Viewer is not affiliated with or endorsed by the FFmpeg project. FFmpeg is a trademark of Fabrice Bellard.",
 "Reiks Visor no está afiliado al proyecto FFmpeg ni cuenta con su respaldo. FFmpeg es una marca de Fabrice Bellard.",
 "Reiks Visare är inte knuten till eller godkänd av FFmpeg-projektet. FFmpeg är ett varumärke som tillhör Fabrice Bellard."),

"footer.credits": (
 "Screenshots show the <em>Sintel</em> trailer, &copy; Blender Foundation (durian.blender.org), CC BY 3.0; the Carina Nebula &ldquo;Cosmic Cliffs&rdquo;, NASA, ESA, CSA and STScI; and Blue Marble 2012, NASA Goddard Space Flight Center.",
 "Las capturas muestran el tráiler de <em>Sintel</em>, &copy; Blender Foundation (durian.blender.org), CC BY 3.0; la nebulosa de Carina &laquo;Cosmic Cliffs&raquo;, NASA, ESA, CSA y STScI; y Blue Marble 2012, NASA Goddard Space Flight Center.",
 "Skärmbilderna visar trailern till <em>Sintel</em>, &copy; Blender Foundation (durian.blender.org), CC BY 3.0; Carinanebulosan &rdquo;Cosmic Cliffs&rdquo;, NASA, ESA, CSA och STScI; samt Blue Marble 2012, NASA Goddard Space Flight Center."),
"footer.warranty": ("Reiks Viewer is provided as is, without warranty.", "Reiks Visor se ofrece tal cual, sin garantía.", "Reiks Visare tillhandahålls i befintligt skick, utan garanti."),
}

here = os.path.dirname(os.path.abspath(__file__))
docs = os.path.join(here, "..", "docs")
template = io.open(os.path.join(here, "site_template.html"), encoding="utf-8").read()

for index, (code, _, folder) in enumerate(LANGUAGES):
    page = template
    up = "../" if folder else ""
    switcher = " &middot; ".join(
        f'<strong>{name}</strong>' if other == code else f'<a href="{up}{other_folder}" hreflang="{other}" lang="{other}">{name}</a>'
        for other, name, other_folder in LANGUAGES)
    alternates = "\n".join(
        f'<link rel="alternate" hreflang="{other}" href="https://reiks-apps.github.io/reiks-viewer/{other_folder}">'
        for other, _, other_folder in LANGUAGES)
    page = page.replace("{{lang}}", code).replace("{{up}}", up).replace("{{switcher}}", switcher).replace("{{alternates}}", alternates)
    for key, values in T.items():
        assert len(values) == len(LANGUAGES), key
        assert "{{" + key + "}}" in template, "unused text: " + key
        page = page.replace("{{" + key + "}}", values[index])
    assert "{{" not in page, page[page.index("{{"):][:60]
    target = os.path.join(docs, folder)
    os.makedirs(target, exist_ok=True)
    io.open(os.path.join(target, "index.html"), "w", encoding="utf-8", newline="\n").write(page)
    print("wrote", os.path.join("docs", folder, "index.html"))

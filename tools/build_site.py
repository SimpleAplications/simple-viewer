# Builds the website into docs/: docs/index.html (English), docs/es/, docs/sv/.
# The page layout is in site_template.html; every text is in the table below.
# To add a language: add a column to every entry and a line to LANGUAGES, then run
#   python tools/build_site.py
import io, os, re

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
"github.title": ("Source code on GitHub", "Código fuente en GitHub", "Källkod på GitHub"),
"version": ("Version {v}", "Versión {v}", "Version {v}"),
"whatsnew": ("What&rsquo;s new", "Novedades", "Nyheter"),
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
 "Some viewers and players do more than show your files: they check for updates, collect usage statistics, keep a history or build a library. Reiks Viewer does none of that.",
 "Algunos visores y reproductores hacen algo más que mostrar tus archivos: buscan actualizaciones, recogen estadísticas de uso, guardan un historial o crean una biblioteca. Reiks Visor no hace nada de eso.",
 "Vissa visare och spelare gör mer än att visa dina filer: de söker efter uppdateringar, samlar användningsstatistik, sparar historik eller bygger ett bibliotek. Reiks Visare gör inget av det."),
"never.1h": ("Connect to the internet", "Conectarse a internet", "Ansluta till internet"),
"never.1": (
 "No telemetry, no usage statistics, no crash reports, no update checks, no online lookups. Its own code has no networking at all. Its decoders are limited to local files and refuse playlist and streaming formats, to keep a specially made file from pointing it at a web address.",
 "Sin telemetría, sin estadísticas de uso, sin informes de errores, sin comprobación de actualizaciones, sin consultas en línea. Su propio código no tiene ninguna función de red. Sus decodificadores están limitados a archivos locales y rechazan los formatos de lista de reproducción y de streaming, para evitar que un archivo preparado a propósito lo dirija a una dirección web.",
 "Ingen telemetri, ingen användningsstatistik, inga kraschrapporter, inga uppdateringskontroller, inga uppslagningar på nätet. Dess egen kod har inga nätverksfunktioner alls. Dess avkodare är begränsade till lokala filer och avvisar spellistor och strömningsformat, för att en särskilt utformad fil inte ska kunna styra den till en webbadress."),
"never.2h": ("Keep a record of what you open", "Guardar registro de lo que abres", "Spara vad du öppnar"),
"never.2": (
 "No history, no recent list, no thumbnail cache, no library or index of your folders.",
 "Sin historial, sin lista de recientes, sin caché de miniaturas, sin biblioteca ni índice de tus carpetas.",
 "Ingen historik, ingen lista över senaste filer, ingen miniatyrcache, inget bibliotek eller index över dina mappar."),
"never.3h": ("Look inside your pictures", "Mirar dentro de tus imágenes", "Titta in i dina bilder"),
"never.3": (
 "No face recognition, no tagging, no AI, no &ldquo;enhancing&rdquo; in the cloud. Reiks Viewer sends nothing from your files to anyone.",
 "Sin reconocimiento facial, sin etiquetado, sin IA, sin &laquo;mejoras&raquo; en la nube. Reiks Visor no envía nada de tus archivos a nadie.",
 "Ingen ansiktsigenkänning, ingen taggning, ingen AI, ingen &rdquo;förbättring&rdquo; i molnet. Reiks Visare skickar ingenting från dina filer till någon."),
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
 "No account, no sign-in, no ads, no bundled extras. Installing it for yourself needs no administrator rights.",
 "Sin cuenta, sin inicio de sesión, sin anuncios, sin extras incluidos. Instalarlo solo para ti no requiere permisos de administrador.",
 "Inget konto, ingen inloggning, ingen reklam, inga medföljande extraprogram. Att installera den bara för dig själv kräver inga administratörsrättigheter."),

"does.h": ("What it does", "Lo que hace", "Vad den gör"),
"does.1h": ("One frame at a time, both ways", "Fotograma a fotograma, en ambos sentidos", "En bildruta i taget, åt båda håll"),
"does.1": (
 "Tap <kbd>X</kbd> or <kbd>Z</kbd> to move exactly one frame forward or back. Hold to keep going, faster the longer you hold. Each frame is fully decoded before it is shown, so a jump does not leave a smeared picture.",
 "Pulsa <kbd>X</kbd> o <kbd>Z</kbd> para avanzar o retroceder exactamente un fotograma. Mantén pulsado para continuar, más rápido cuanto más tiempo. Cada fotograma se decodifica por completo antes de mostrarse, así que un salto no deja la imagen corrupta.",
 "Tryck på <kbd>X</kbd> eller <kbd>Z</kbd> för att gå exakt en bildruta framåt eller bakåt. Håll nere för att fortsätta, snabbare ju längre du håller. Varje bildruta avkodas helt innan den visas, så ett hopp lämnar ingen utsmetad bild."),
"does.2h": ("Pictures, GIFs and video together", "Imágenes, GIF y vídeo juntos", "Bilder, GIF:ar och video tillsammans"),
"does.2": (
 "Open one file and move through everything else in the folder with the arrow keys. Zoom with the mouse wheel, drag to pan, rotate and flip. Print a picture, or the frame of a paused video, with <kbd>Ctrl</kbd>+<kbd>P</kbd>.",
 "Abre un archivo y recorre todo lo demás de la carpeta con las flechas. Amplía con la rueda del ratón, arrastra para desplazarte, gira y voltea. Imprime una imagen, o el fotograma de un vídeo en pausa, con <kbd>Ctrl</kbd>+<kbd>P</kbd>.",
 "Öppna en fil och bläddra genom allt annat i mappen med piltangenterna. Zooma med mushjulet, dra för att panorera, rotera och spegla. Skriv ut en bild, eller bildrutan i en pausad video, med <kbd>Ctrl</kbd>+<kbd>P</kbd>."),
"does.3h": ("Private on screen too", "Privado también en pantalla", "Privat även på skärmen"),
"does.3": (
 "Have Windows leave the window out of screenshots, recordings and screen sharing, which then show whatever is behind it. Hide file names from the title bar and taskbar. Press <kbd>H</kbd> to blank the window at once.",
 "Haz que Windows deje la ventana fuera de capturas, grabaciones y pantalla compartida, que muestran entonces lo que hay detrás. Oculta los nombres de archivo de la barra de título y la barra de tareas. Pulsa <kbd>H</kbd> para dejar la ventana en blanco al instante.",
 "Låt Windows hålla fönstret utanför skärmbilder, inspelningar och skärmdelning, som då visar det som finns bakom. Dölj filnamn i namnlisten och aktivitetsfältet. Tryck på <kbd>H</kbd> för att tömma fönstret direkt."),
"does.4h": ("Image settings", "Ajustes de imagen", "Bildinställningar"),
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
 "The Image settings menu with sliders for brightness, contrast, saturation, gamma and warmth, and buttons to rotate and flip",
 "El menú Ajustes de imagen con controles de brillo, contraste, saturación, gamma y calidez, y botones para girar y voltear",
 "Menyn Bildinställningar med reglage för ljusstyrka, kontrast, mättnad, gamma och värme samt knappar för att rotera och spegla"),
"shot.picture.cap": ("Image settings: light, colour, rotate and flip. They apply at once and reset with one click.", "Ajustes de imagen: luz, color, girar y voltear. Se aplican al instante y se restablecen con un clic.", "Bildinställningar: ljus, färg, rotera och spegla. De verkar direkt och återställs med ett klick."),
"shot.options.alt": (
 "The Options menu with key assignments, privacy options, window colour and language",
 "El menú Opciones con la asignación de teclas, la privacidad, el color de la ventana y el idioma",
 "Menyn Alternativ med tangenttilldelning, integritetsval, fönsterfärg och språk"),
"shot.options.cap": ("Options: keys, privacy, window colour and language.", "Opciones: teclas, privacidad, color de la ventana e idioma.", "Alternativ: tangenter, integritet, fönsterfärg och språk."),
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
"formats.note": (
 "Whether a particular file opens depends on what is inside it. HEIC photos taken with an iPhone have had little testing so far. If a file does not open, please <a href=\"https://github.com/reiks-apps/reiks-viewer/issues\">report it</a>.",
 "Que un archivo concreto se abra depende de su contenido. Las fotos HEIC hechas con un iPhone se han probado poco hasta ahora. Si un archivo no se abre, <a href=\"https://github.com/reiks-apps/reiks-viewer/issues\">avísanos</a>.",
 "Om en viss fil går att öppna beror på vad den innehåller. HEIC-bilder tagna med en iPhone är ännu lite testade. Om en fil inte går att öppna får du gärna <a href=\"https://github.com/reiks-apps/reiks-viewer/issues\">rapportera det</a>."),

"verify.h": ("Don&rsquo;t take our word for it", "No hace falta que nos creas", "Lita inte bara på vårt ord"),
"verify.1": (
 "<strong>Block it and see.</strong> Deny Reiks Viewer all network access in Windows Firewall; when you install for all users, the installer offers to add that rule for you. Everything keeps working, because it never needed a connection.",
 "<strong>Bloquéalo y compruébalo.</strong> Deniega a Reiks Visor todo acceso a la red en el Firewall de Windows; si instalas para todos los usuarios, el instalador te ofrece añadir esa regla. Todo sigue funcionando, porque nunca necesitó conexión.",
 "<strong>Blockera den och se själv.</strong> Neka Reiks Visare all nätverksåtkomst i Windows-brandväggen; när du installerar för alla användare erbjuder installationsprogrammet att lägga till den regeln åt dig. Allt fungerar som förut, eftersom den aldrig behövde någon anslutning."),
"verify.2": (
 "<strong>Read the code.</strong> Reiks Viewer is <a href=\"https://github.com/reiks-apps/reiks-viewer\">open source</a>. The installer and the zip are built from that code on GitHub&rsquo;s own servers, in public. GitHub signs a statement for each of them saying which code it was built from, so anyone can check that a download really came from the published code. The FFmpeg libraries inside are not built there: they are an unmodified public build, identified by its checksum in the notices.",
 "<strong>Lee el código.</strong> Reiks Visor es de <a href=\"https://github.com/reiks-apps/reiks-viewer\">código abierto</a>. El instalador y el zip se compilan a partir de ese código en los servidores de GitHub, en público. GitHub firma para cada uno una declaración que indica a partir de qué código se compiló, así cualquiera puede comprobar que una descarga procede realmente del código publicado. Las bibliotecas FFmpeg que incluye no se compilan allí: son una compilación pública sin modificar, identificada por su suma de comprobación en los avisos.",
 "<strong>Läs koden.</strong> Reiks Visare har <a href=\"https://github.com/reiks-apps/reiks-viewer\">öppen källkod</a>. Installationsprogrammet och zip-filen byggs från den koden på GitHubs egna servrar, öppet. GitHub signerar för var och en ett intyg som anger vilken kod den byggdes från, så att vem som helst kan kontrollera att en nedladdning verkligen kommer från den publicerade koden. FFmpeg-biblioteken som följer med byggs inte där: de är ett oförändrat offentligt bygge, angivet med sin kontrollsumma i upplysningarna."),
"verify.3": (
 "<strong>Check your download.</strong> Every release lists the SHA-256 checksum of each file.",
 "<strong>Comprueba tu descarga.</strong> Cada versión publica la suma SHA-256 de cada archivo.",
 "<strong>Kontrollera din nedladdning.</strong> Varje version anger SHA-256-kontrollsumman för varje fil."),

"limits.h": ("What Reiks Viewer can&rsquo;t control", "Lo que Reiks Visor no puede controlar", "Det Reiks Visare inte kan styra över"),
"limits.1": (
 "Windows keeps some records of its own, and Reiks Viewer has no say over them. When you open a file by double-clicking it in Explorer, Windows may add it to its recent files list (Settings &rarr; Personalization &rarr; Start &rarr; &ldquo;Show recently opened items&rdquo;). Reiks Viewer asks Windows to keep files opened through its own Open dialog off that list, but Windows may still remember which folder that dialog was last in. If Reiks Viewer ever crashes, Windows Error Reporting may record the crash, as it does for any app. Windows&rsquo; diagnostic data and Recall are Windows settings too, under Privacy &amp; security.",
 "Windows guarda algunos registros por su cuenta, y Reiks Visor no tiene control sobre ellos. Cuando abres un archivo con doble clic en el Explorador, Windows puede añadirlo a su lista de archivos recientes (Configuración &rarr; Personalización &rarr; Inicio &rarr; &laquo;Mostrar elementos abiertos recientemente&raquo;). Reiks Visor pide a Windows que no añada a esa lista los archivos abiertos desde su propio cuadro Abrir, pero Windows puede seguir recordando en qué carpeta estuvo ese cuadro por última vez. Si Reiks Visor llegara a fallar, el Informe de errores de Windows puede registrar el fallo, como con cualquier aplicación. Los datos de diagnóstico de Windows y Recall también son ajustes de Windows, en Privacidad y seguridad.",
 "Windows sparar vissa uppgifter på egen hand, och dem kan Reiks Visare inte påverka. När du öppnar en fil genom att dubbelklicka på den i Utforskaren kan Windows lägga till den i sin lista över senaste filer (Inställningar &rarr; Anpassning &rarr; Start &rarr; &rdquo;Visa nyligen öppnade objekt&rdquo;). Reiks Visare ber Windows att inte lägga till filer som öppnas via dess egen Öppna-dialog i den listan, men Windows kan ändå komma ihåg vilken mapp dialogen senast visade. Om Reiks Visare skulle krascha kan Windows felrapportering registrera kraschen, precis som för andra appar. Windows diagnostikdata och Recall är också Windows-inställningar, under Sekretess och säkerhet."),
"limits.3": (
 "<strong>&ldquo;Hide from screenshots&rdquo; relies on a Windows feature.</strong> Windows applies it to the usual ways of capturing a screen: Print Screen, the Snipping Tool, screen recorders and screen sharing in meeting apps. It cannot stop a camera pointed at your screen or software made to get around it, and we have not tested it against Windows Recall.",
 "<strong>&laquo;Ocultar en capturas&raquo; se apoya en una función de Windows.</strong> Windows la aplica a las formas habituales de capturar la pantalla: Impr Pant, la Herramienta Recortes, los grabadores de pantalla y la pantalla compartida de las aplicaciones de reuniones. No puede impedir que una cámara apunte a tu pantalla ni detener un programa hecho para saltársela, y no la hemos probado con Windows Recall.",
 "<strong>&rdquo;Dölj från skärmbilder&rdquo; bygger på en funktion i Windows.</strong> Windows tillämpar den på de vanliga sätten att fånga skärmen: Print Screen, Skärmklippverktyget, skärminspelare och skärmdelning i mötesappar. Den kan inte hindra en kamera som riktas mot skärmen eller program som är gjorda för att kringgå den, och vi har inte testat den mot Windows Recall."),
"limits.4": (
 "If you open a file from a network drive or a cloud-synced folder, Windows or your sync program carries that data over the network, as with any app. Printing goes through Windows&rsquo; own print system: Windows and the printer&rsquo;s driver talk to the printer, over your network if it is a network printer, and they may keep a record of the job. And like every media player, Reiks Viewer has to decode whatever a file contains: a deliberately damaged file could make it crash or misbehave, so keep it up to date and take care with files from sources you don&rsquo;t trust.",
 "Si abres un archivo desde una unidad de red o una carpeta sincronizada con la nube, es Windows o tu programa de sincronización quien mueve esos datos por la red, como con cualquier aplicación. La impresión pasa por el sistema de impresión de Windows: son Windows y el controlador de la impresora quienes se comunican con ella, por tu red si es una impresora de red, y pueden guardar un registro del trabajo. Y, como todo reproductor, Reiks Visor tiene que decodificar lo que contenga un archivo: un archivo dañado a propósito podría hacer que falle o se comporte de forma inesperada, así que mantenlo actualizado y ten cuidado con los archivos de origen dudoso.",
 "Om du öppnar en fil från en nätverksenhet eller en molnsynkad mapp är det Windows eller ditt synkprogram som flyttar datan över nätverket, precis som för andra appar. Utskrifter går via Windows eget utskriftssystem: det är Windows och skrivarens drivrutin som talar med skrivaren, över ditt nätverk om det är en nätverksskrivare, och de kan spara uppgifter om utskriften. Och som alla mediespelare måste Reiks Visare avkoda det en fil innehåller: en avsiktligt skadad fil kan få den att krascha eller bete sig oväntat, så håll den uppdaterad och var försiktig med filer från källor du inte litar på."),
"limits.2": (
 "<strong>In normal use, the only file Reiks Viewer writes</strong> is your options: your keys, window colour, language, playback and privacy choices, in <code>%APPDATA%\\ReiksViewer\\settings.json</code>. It contains nothing about your files. Uninstalling removes it; if you use the portable zip, delete that folder yourself.",
 "<strong>En el uso normal, el único archivo que escribe Reiks Visor</strong> son tus opciones: tus teclas, el color de la ventana, el idioma y tus preferencias de reproducción y privacidad, en <code>%APPDATA%\\ReiksViewer\\settings.json</code>. No contiene nada sobre tus archivos. Al desinstalar se elimina; si usas el zip portátil, borra tú esa carpeta.",
 "<strong>Vid normal användning är den enda fil Reiks Visare skriver</strong> dina alternativ: dina tangenter, fönsterfärg, språk samt uppspelnings- och integritetsval, i <code>%APPDATA%\\ReiksViewer\\settings.json</code>. Den innehåller ingenting om dina filer. Avinstallationen tar bort den; om du använder den portabla zip-filen tar du själv bort den mappen."),

"install.h": ("Installing", "Instalación", "Installation"),
"install.1": (
 "<strong>Windows may warn you the first time.</strong> The installer is not digitally signed yet, so Windows can show &ldquo;Windows protected your PC&rdquo;. Choose <em>More info</em>, then <em>Run anyway</em>.",
 "<strong>Windows puede avisarte la primera vez.</strong> El instalador aún no está firmado digitalmente, así que Windows puede mostrar &laquo;Windows protegió su PC&raquo;. Elige <em>Más información</em> y luego <em>Ejecutar de todas formas</em>.",
 "<strong>Windows kan varna dig första gången.</strong> Installationsprogrammet är ännu inte digitalt signerat, så Windows kan visa &rdquo;Windows skyddade datorn&rdquo;. Välj <em>Mer information</em> och sedan <em>Kör ändå</em>."),
"install.2": (
 "To check that your download is complete and matches the published file, compare its SHA-256 checksum with <a href=\"https://github.com/reiks-apps/reiks-viewer/releases/download/v{v}/SHA256SUMS.txt\">SHA256SUMS.txt</a>. In PowerShell: <code>Get-FileHash ReiksViewer-Setup-{v}.exe</code>",
 "Para comprobar que tu descarga está completa y coincide con el archivo publicado, compara su suma SHA-256 con <a href=\"https://github.com/reiks-apps/reiks-viewer/releases/download/v{v}/SHA256SUMS.txt\">SHA256SUMS.txt</a>. En PowerShell: <code>Get-FileHash ReiksViewer-Setup-{v}.exe</code>",
 "För att kontrollera att din nedladdning är hel och stämmer med den publicerade filen, jämför dess SHA-256-kontrollsumma med <a href=\"https://github.com/reiks-apps/reiks-viewer/releases/download/v{v}/SHA256SUMS.txt\">SHA256SUMS.txt</a>. I PowerShell: <code>Get-FileHash ReiksViewer-Setup-{v}.exe</code>"),
"install.3": (
 "Installing for yourself needs no administrator rights; installing for all users does. The installer can add Reiks Viewer to Explorer&rsquo;s &ldquo;Open with&rdquo; list. To update, run a newer installer over the old one; &ldquo;Check for updates&rdquo; in the app (F1) opens the download page in your browser, and the app itself stays offline.",
 "Instalar solo para ti no requiere permisos de administrador; instalar para todos los usuarios, sí. El instalador puede añadir Reiks Visor a la lista &laquo;Abrir con&raquo; del Explorador. Para actualizar, ejecuta un instalador más reciente sobre el anterior; &laquo;Buscar actualizaciones&raquo; en la aplicación (F1) abre la página de descarga en tu navegador, y la aplicación en sí sigue sin conexión.",
 "Att installera bara för dig själv kräver inga administratörsrättigheter; att installera för alla användare gör det. Installationsprogrammet kan lägga till Reiks Visare i Utforskarens lista &rdquo;Öppna med&rdquo;. För att uppdatera kör du ett nyare installationsprogram över det gamla; &rdquo;Sök efter uppdateringar&rdquo; i appen (F1) öppnar nedladdningssidan i din webbläsare, och själva appen förblir offline."),
"install.4": (
 "Reiks Viewer is tested on Windows 11. It should also run on Windows 10, but that has not been tested yet.",
 "Reiks Visor está probado en Windows 11. También debería funcionar en Windows 10, pero eso aún no se ha probado.",
 "Reiks Visare är testad på Windows 11. Den bör även fungera på Windows 10, men det har ännu inte testats."),

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
 "Screenshots show the <em>Sintel</em> trailer, &copy; Blender Foundation (durian.blender.org), CC BY 3.0; the Carina Nebula &ldquo;Cosmic Cliffs&rdquo;, NASA, ESA, CSA and STScI; and Blue Marble 2012, NASA Goddard Space Flight Center. Their use here does not imply any endorsement.",
 "Las capturas muestran el tráiler de <em>Sintel</em>, &copy; Blender Foundation (durian.blender.org), CC BY 3.0; la nebulosa de Carina &laquo;Cosmic Cliffs&raquo;, NASA, ESA, CSA y STScI; y Blue Marble 2012, NASA Goddard Space Flight Center. Su uso aquí no implica ningún respaldo.",
 "Skärmbilderna visar trailern till <em>Sintel</em>, &copy; Blender Foundation (durian.blender.org), CC BY 3.0; Carinanebulosan &rdquo;Cosmic Cliffs&rdquo;, NASA, ESA, CSA och STScI; samt Blue Marble 2012, NASA Goddard Space Flight Center. Att de används här innebär inte att upphovsmännen står bakom Reiks Visare."),
"footer.marks": (
 "The GitHub name and logo are trademarks of GitHub, Inc., used here only to link to this project&rsquo;s page there.",
 "El nombre y el logotipo de GitHub son marcas de GitHub, Inc., y se usan aquí solo para enlazar a la página de este proyecto.",
 "Namnet GitHub och dess logotyp är varumärken som tillhör GitHub, Inc. och används här endast för att länka till projektets sida där."),
"footer.warranty": ("Reiks Viewer is provided as is, without warranty.", "Reiks Visor se ofrece tal cual, sin garantía.", "Reiks Visare tillhandahålls i befintligt skick, utan garanti."),
}

here = os.path.dirname(os.path.abspath(__file__))
docs = os.path.join(here, "..", "docs")
template = io.open(os.path.join(here, "site_template.html"), encoding="utf-8").read()
# The version comes from the project file, so it is set in one place. The page links to that
# version's own files: publish the release before the website, or the links lead nowhere.
project = io.open(os.path.join(here, "..", "ReiksViewer.csproj"), encoding="utf-8").read()
version = re.search(r"<Version>([^<]+)</Version>", project).group(1)

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
    page = page.replace("{v}", version)
    target = os.path.join(docs, folder)
    os.makedirs(target, exist_ok=True)
    io.open(os.path.join(target, "index.html"), "w", encoding="utf-8", newline="\n").write(page)
    print("wrote", os.path.join("docs", folder, "index.html"))

# Writes the language files (lang/<code>.json) that the app embeds.
# To add a language: add a column to every entry below, add it to LANGUAGES here and
# to L.Languages in Localization.cs, then run:  python lang/make_languages.py
import io, json, os

LANGUAGES = ["en", "es", "sv", "fr", "de", "pt", "it"]

T = {
"app.name": ["Reiks Viewer", "Reiks Visor", "Reiks Visare", "Reiks Visionneuse", "Reiks Betrachter", "Reiks Visualizador", "Reiks Visualizzatore"],

"hint.drop": [
 "Drop a picture, GIF or video here, or press O to open one.",
 "Suelta aquí una imagen, un GIF o un vídeo, o pulsa O para abrir uno.",
 "Släpp en bild, GIF eller video här, eller tryck på O för att öppna en.",
 "Déposez ici une image, un GIF ou une vidéo, ou appuyez sur O pour en ouvrir.",
 "Bild, GIF oder Video hierher ziehen oder O drücken, um eine Datei zu öffnen.",
 "Largue aqui uma imagem, um GIF ou um vídeo, ou prima O para abrir.",
 "Trascina qui un'immagine, una GIF o un video, oppure premi O per aprirne uno."],

"tip.open": ["Open (O)", "Abrir (O)", "Öppna (O)", "Ouvrir (O)", "Öffnen (O)", "Abrir (O)", "Apri (O)"],
"tip.prev": ["Previous file (Left, {0})", "Archivo anterior (Izquierda, {0})", "Föregående fil (Vänster, {0})", "Fichier précédent (Gauche, {0})", "Vorherige Datei (Links, {0})", "Ficheiro anterior (Esquerda, {0})", "File precedente (Sinistra, {0})"],
"tip.next": ["Next file (Right, {0})", "Archivo siguiente (Derecha, {0})", "Nästa fil (Höger, {0})", "Fichier suivant (Droite, {0})", "Nächste Datei (Rechts, {0})", "Ficheiro seguinte (Direita, {0})", "File successivo (Destra, {0})"],
"tip.play": ["Play / pause ({0})", "Reproducir / pausar ({0})", "Spela / pausa ({0})", "Lecture / pause ({0})", "Wiedergabe / Pause ({0})", "Reproduzir / pausar ({0})", "Riproduci / pausa ({0})"],
"tip.back": ["Play backward ({0})", "Reproducir hacia atrás ({0})", "Spela bakåt ({0})", "Lecture arrière ({0})", "Rückwärts abspielen ({0})", "Reproduzir para trás ({0})", "Riproduci all'indietro ({0})"],
"tip.forward": ["Play forward ({0})", "Reproducir hacia delante ({0})", "Spela framåt ({0})", "Lecture avant ({0})", "Vorwärts abspielen ({0})", "Reproduzir para a frente ({0})", "Riproduci in avanti ({0})"],
"tip.mute": ["Mute (M)", "Silenciar (M)", "Ljud av (M)", "Muet (M)", "Stumm (M)", "Silenciar (M)", "Muto (M)"],
"tip.picture": ["Picture settings (A)", "Ajustes de imagen (A)", "Bildinställningar (A)", "Réglages de l'image (A)", "Bildeinstellungen (A)", "Ajustes de imagem (A)", "Regolazioni immagine (A)"],
"tip.rotate": ["Rotate and flip (R rotates)", "Girar y voltear (R gira)", "Rotera och spegla (R roterar)", "Pivoter et retourner (R pivote)", "Drehen und spiegeln (R dreht)", "Rodar e inverter (R roda)", "Ruota e capovolgi (R ruota)"],
"tip.options": ["Options", "Opciones", "Alternativ", "Options", "Optionen", "Opções", "Opzioni"],
"tip.formats": ["Formats and credits (F1)", "Formatos y créditos (F1)", "Format och tack (F1)", "Formats et crédits (F1)", "Formate und Danksagungen (F1)", "Formatos e créditos (F1)", "Formati e riconoscimenti (F1)"],
"tip.folder": ["Play the whole folder (S)", "Reproducir toda la carpeta (S)", "Spela hela mappen (S)", "Lire tout le dossier (S)", "Ganzen Ordner abspielen (S)", "Reproduzir a pasta inteira (S)", "Riproduci tutta la cartella (S)"],
"tip.fullscreen": ["Fullscreen (F11, double-click)", "Pantalla completa (F11, doble clic)", "Helskärm (F11, dubbelklick)", "Plein écran (F11, double-clic)", "Vollbild (F11, Doppelklick)", "Ecrã inteiro (F11, duplo clique)", "Schermo intero (F11, doppio clic)"],
"tip.minimize": ["Minimize", "Minimizar", "Minimera", "Réduire", "Minimieren", "Minimizar", "Riduci a icona"],
"tip.maximize": ["Maximize", "Maximizar", "Maximera", "Agrandir", "Maximieren", "Maximizar", "Ingrandisci"],
"tip.restore": ["Restore", "Restaurar", "Återställ", "Restaurer", "Wiederherstellen", "Restaurar", "Ripristina"],
"tip.close": ["Close", "Cerrar", "Stäng", "Fermer", "Schließen", "Fechar", "Chiudi"],

"msg.nofiles": ["No pictures or videos found in that folder.", "No se han encontrado imágenes ni vídeos en esa carpeta.", "Inga bilder eller videor hittades i den mappen.", "Aucune image ni vidéo trouvée dans ce dossier.", "In diesem Ordner wurden keine Bilder oder Videos gefunden.", "Não foram encontradas imagens nem vídeos nessa pasta.", "Nessuna immagine o video trovato in quella cartella."],
"msg.cantopen": ["Can't open {0}", "No se puede abrir {0}", "Det går inte att öppna {0}", "Impossible d'ouvrir {0}", "{0} kann nicht geöffnet werden", "Não é possível abrir {0}", "Impossibile aprire {0}"],
"msg.cantplay": ["Can't play {0}", "No se puede reproducir {0}", "Det går inte att spela {0}", "Impossible de lire {0}", "{0} kann nicht abgespielt werden", "Não é possível reproduzir {0}", "Impossibile riprodurre {0}"],
"msg.noffmpeg": ["Video needs the FFmpeg libraries.", "El vídeo necesita las bibliotecas FFmpeg.", "Video kräver FFmpeg-biblioteken.", "La vidéo nécessite les bibliothèques FFmpeg.", "Für Videos werden die FFmpeg-Bibliotheken benötigt.", "O vídeo precisa das bibliotecas FFmpeg.", "I video richiedono le librerie FFmpeg."],
"msg.pressf1": ["Press F1 for details.", "Pulsa F1 para ver los detalles.", "Tryck på F1 för mer information.", "Appuyez sur F1 pour les détails.", "F1 drücken für Details.", "Prima F1 para ver os detalhes.", "Premi F1 per i dettagli."],
"name.thisfile": ["this file", "este archivo", "den här filen", "ce fichier", "diese Datei", "este ficheiro", "questo file"],

"dlg.all": ["Pictures and videos", "Imágenes y vídeos", "Bilder och videor", "Images et vidéos", "Bilder und Videos", "Imagens e vídeos", "Immagini e video"],
"dlg.pictures": ["Pictures", "Imágenes", "Bilder", "Images", "Bilder", "Imagens", "Immagini"],
"dlg.videos": ["Videos", "Vídeos", "Videor", "Vidéos", "Videos", "Vídeos", "Video"],
"dlg.allfiles": ["All files", "Todos los archivos", "Alla filer", "Tous les fichiers", "Alle Dateien", "Todos os ficheiros", "Tutti i file"],

"status.zoom": ["zoom {0}%", "zoom {0} %", "zoom {0} %", "zoom {0} %", "Zoom {0} %", "zoom {0}%", "zoom {0}%"],
"status.repeat": ["repeating file", "repitiendo archivo", "upprepar filen", "fichier en boucle", "Datei wird wiederholt", "a repetir o ficheiro", "ripetizione del file"],
"status.folder": ["playing folder", "reproduciendo carpeta", "spelar mappen", "lecture du dossier", "Ordner wird abgespielt", "a reproduzir a pasta", "riproduzione della cartella"],
"status.shuffled": ["playing folder, shuffled", "reproduciendo carpeta, aleatorio", "spelar mappen, blandat", "lecture du dossier, aléatoire", "Ordner wird abgespielt, zufällig", "a reproduzir a pasta, aleatório", "riproduzione della cartella, casuale"],

"panel.picture": ["Picture", "Imagen", "Bild", "Image", "Bild", "Imagem", "Immagine"],
"adj.brightness": ["Brightness", "Brillo", "Ljusstyrka", "Luminosité", "Helligkeit", "Brilho", "Luminosità"],
"adj.contrast": ["Contrast", "Contraste", "Kontrast", "Contraste", "Kontrast", "Contraste", "Contrasto"],
"adj.saturation": ["Saturation", "Saturación", "Mättnad", "Saturation", "Sättigung", "Saturação", "Saturazione"],
"adj.gamma": ["Gamma", "Gamma", "Gamma", "Gamma", "Gamma", "Gama", "Gamma"],
"adj.warmth": ["Warmth", "Calidez", "Värme", "Chaleur", "Wärme", "Calor", "Calore"],
"btn.reset": ["Reset", "Restablecer", "Återställ", "Réinitialiser", "Zurücksetzen", "Repor", "Ripristina"],

"panel.rotate": ["Rotate and flip", "Girar y voltear", "Rotera och spegla", "Pivoter et retourner", "Drehen und spiegeln", "Rodar e inverter", "Ruota e capovolgi"],
"rot.left": ["Rotate left", "Girar a la izquierda", "Rotera vänster", "Pivoter à gauche", "Nach links drehen", "Rodar à esquerda", "Ruota a sinistra"],
"rot.right": ["Rotate right", "Girar a la derecha", "Rotera höger", "Pivoter à droite", "Nach rechts drehen", "Rodar à direita", "Ruota a destra"],
"flip.h": ["Flip horizontally", "Voltear horizontal", "Spegla vågrätt", "Retourner horizontalement", "Horizontal spiegeln", "Inverter na horizontal", "Capovolgi in orizzontale"],
"flip.v": ["Flip vertically", "Voltear vertical", "Spegla lodrätt", "Retourner verticalement", "Vertikal spiegeln", "Inverter na vertical", "Capovolgi in verticale"],

"panel.playback": ["Playback", "Reproducción", "Uppspelning", "Lecture", "Wiedergabe", "Reprodução", "Riproduzione"],
"play.folder": ["Play the whole folder", "Reproducir toda la carpeta", "Spela hela mappen", "Lire tout le dossier", "Ganzen Ordner abspielen", "Reproduzir a pasta inteira", "Riproduci tutta la cartella"],
"play.folder.tip": [
 "Opens the next file when a video ends, or after 4 seconds on a picture (S)",
 "Abre el archivo siguiente cuando termina un vídeo, o tras 4 segundos en una imagen (S)",
 "Öppnar nästa fil när en video tar slut, eller efter 4 sekunder på en bild (S)",
 "Ouvre le fichier suivant à la fin d'une vidéo, ou après 4 secondes sur une image (S)",
 "Öffnet die nächste Datei, wenn ein Video endet, oder nach 4 Sekunden bei einem Bild (S)",
 "Abre o ficheiro seguinte quando um vídeo termina, ou após 4 segundos numa imagem (S)",
 "Apre il file successivo quando un video finisce, o dopo 4 secondi su un'immagine (S)"],
"play.random": ["In random order", "En orden aleatorio", "I slumpmässig ordning", "Dans un ordre aléatoire", "In zufälliger Reihenfolge", "Por ordem aleatória", "In ordine casuale"],
"play.again": ["Start again after the last file", "Volver a empezar tras el último archivo", "Börja om efter sista filen", "Recommencer après le dernier fichier", "Nach der letzten Datei von vorn beginnen", "Recomeçar depois do último ficheiro", "Ricomincia dopo l'ultimo file"],
"play.repeat": ["Repeat the current file", "Repetir el archivo actual", "Upprepa den aktuella filen", "Répéter le fichier en cours", "Aktuelle Datei wiederholen", "Repetir o ficheiro atual", "Ripeti il file corrente"],
"play.repeat.tip": [
 "Plays the same video again when it ends, and stays on this file",
 "Vuelve a reproducir el mismo vídeo cuando termina, y se queda en este archivo",
 "Spelar samma video igen när den tar slut, och stannar kvar på den här filen",
 "Relit la même vidéo quand elle se termine, et reste sur ce fichier",
 "Spielt dasselbe Video erneut ab, wenn es endet, und bleibt bei dieser Datei",
 "Volta a reproduzir o mesmo vídeo quando termina, e fica neste ficheiro",
 "Riproduce di nuovo lo stesso video quando finisce, e resta su questo file"],

"panel.keys": ["Keys", "Teclas", "Tangenter", "Touches", "Tasten", "Teclas", "Tasti"],
"key.playpause": ["Play / pause", "Reproducir / pausar", "Spela / pausa", "Lecture / pause", "Wiedergabe / Pause", "Reproduzir / pausar", "Riproduci / pausa"],
"key.next": ["Next file", "Archivo siguiente", "Nästa fil", "Fichier suivant", "Nächste Datei", "Ficheiro seguinte", "File successivo"],
"key.prev": ["Previous file", "Archivo anterior", "Föregående fil", "Fichier précédent", "Vorherige Datei", "Ficheiro anterior", "File precedente"],
"key.forward": ["Play forward", "Reproducir hacia delante", "Spela framåt", "Lecture avant", "Vorwärts abspielen", "Reproduzir para a frente", "Riproduci in avanti"],
"key.backward": ["Play backward", "Reproducir hacia atrás", "Spela bakåt", "Lecture arrière", "Rückwärts abspielen", "Reproduzir para trás", "Riproduci all'indietro"],
"key.hide": ["Hide the picture", "Ocultar la imagen", "Dölj bilden", "Masquer l'image", "Bild ausblenden", "Ocultar a imagem", "Nascondi l'immagine"],
"keys.hint": [
 "Click a key, then press the new one. Esc cancels. The arrow keys always move between files.",
 "Haz clic en una tecla y pulsa la nueva. Esc cancela. Las flechas siempre cambian de archivo.",
 "Klicka på en tangent och tryck sedan på den nya. Esc avbryter. Piltangenterna byter alltid fil.",
 "Cliquez sur une touche, puis appuyez sur la nouvelle. Échap annule. Les flèches changent toujours de fichier.",
 "Auf eine Taste klicken, dann die neue drücken. Esc bricht ab. Die Pfeiltasten wechseln immer die Datei.",
 "Clique numa tecla e prima a nova. Esc cancela. As setas mudam sempre de ficheiro.",
 "Fai clic su un tasto, poi premi quello nuovo. Esc annulla. Le frecce cambiano sempre file."],
"keys.reset": ["Reset keys", "Restablecer teclas", "Återställ tangenter", "Réinitialiser les touches", "Tasten zurücksetzen", "Repor teclas", "Ripristina tasti"],
"keys.press": ["Press a key…", "Pulsa una tecla…", "Tryck på en tangent…", "Appuyez sur une touche…", "Taste drücken…", "Prima uma tecla…", "Premi un tasto…"],

"panel.privacy": ["Privacy", "Privacidad", "Integritet", "Confidentialité", "Privatsphäre", "Privacidade", "Privacy"],
"priv.capture": ["Hide from screenshots and screen sharing", "Ocultar en capturas y al compartir pantalla", "Dölj från skärmbilder och skärmdelning", "Masquer des captures et du partage d'écran", "Vor Screenshots und Bildschirmfreigabe verbergen", "Ocultar de capturas e da partilha de ecrã", "Nascondi da screenshot e condivisione schermo"],
"priv.capture.tip": [
 "Leaves this window out of screenshots, recordings and screen sharing; they show what is behind it",
 "Deja esta ventana fuera de capturas, grabaciones y pantalla compartida; se ve lo que hay detrás",
 "Utelämnar det här fönstret från skärmbilder, inspelningar och skärmdelning; de visar det som finns bakom",
 "Exclut cette fenêtre des captures, enregistrements et partages d'écran ; on y voit ce qui se trouve derrière",
 "Lässt dieses Fenster in Screenshots, Aufnahmen und Bildschirmfreigaben weg; sie zeigen, was dahinter liegt",
 "Deixa esta janela de fora das capturas, gravações e partilha de ecrã; mostram o que está por trás",
 "Esclude questa finestra da screenshot, registrazioni e condivisione schermo; mostrano ciò che c'è dietro"],
"priv.names": ["Hide file names", "Ocultar nombres de archivo", "Dölj filnamn", "Masquer les noms de fichier", "Dateinamen verbergen", "Ocultar nomes de ficheiro", "Nascondi i nomi dei file"],
"priv.names.tip": [
 "Shows \"{0}\" instead of the file's name in the title bar and taskbar",
 "Muestra «{0}» en lugar del nombre del archivo en la barra de título y la barra de tareas",
 "Visar \"{0}\" i stället för filens namn i namnlisten och aktivitetsfältet",
 "Affiche « {0} » à la place du nom du fichier dans la barre de titre et la barre des tâches",
 "Zeigt „{0}“ statt des Dateinamens in Titelleiste und Taskleiste",
 "Mostra «{0}» em vez do nome do ficheiro na barra de título e na barra de tarefas",
 "Mostra \"{0}\" al posto del nome del file nella barra del titolo e nella barra delle applicazioni"],

"panel.colour": ["Window colour", "Color de la ventana", "Fönsterfärg", "Couleur de la fenêtre", "Fensterfarbe", "Cor da janela", "Colore della finestra"],
"col.red": ["Red", "Rojo", "Röd", "Rouge", "Rot", "Vermelho", "Rosso"],
"col.green": ["Green", "Verde", "Grön", "Vert", "Grün", "Verde", "Verde"],
"col.blue": ["Blue", "Azul", "Blå", "Bleu", "Blau", "Azul", "Blu"],
"panel.language": ["Language", "Idioma", "Språk", "Langue", "Sprache", "Idioma", "Lingua"],
"lang.auto": ["Automatic", "Automático", "Automatiskt", "Automatique", "Automatisch", "Automático", "Automatica"],

"formats.title": ["Formats and credits", "Formatos y créditos", "Format och tack", "Formats et crédits", "Formate und Danksagungen", "Formatos e créditos", "Formati e riconoscimenti"],
"formats.codecs": ["Open codecs folder", "Abrir carpeta de códecs", "Öppna codec-mappen", "Ouvrir le dossier des codecs", "Codec-Ordner öffnen", "Abrir pasta de codecs", "Apri cartella dei codec"],
"formats.defaults": ["Default app settings", "Aplicaciones predeterminadas", "Standardappar", "Applications par défaut", "Standard-Apps", "Aplicações predefinidas", "App predefinite"],
"formats.defaults.tip": [
 "Opens Windows Settings, where you can make {0} the default for pictures and videos",
 "Abre la Configuración de Windows, donde puedes hacer que {0} sea la aplicación predeterminada para imágenes y vídeos",
 "Öppnar Windows-inställningarna, där du kan göra {0} till standard för bilder och videor",
 "Ouvre les Paramètres Windows, où vous pouvez faire de {0} l'application par défaut pour les images et les vidéos",
 "Öffnet die Windows-Einstellungen, wo Sie {0} als Standard für Bilder und Videos festlegen können",
 "Abre as Definições do Windows, onde pode tornar {0} a aplicação predefinida para imagens e vídeos",
 "Apre le Impostazioni di Windows, dove puoi rendere {0} l'app predefinita per immagini e video"],
"formats.updates": ["Check for updates", "Buscar actualizaciones", "Sök efter uppdateringar", "Rechercher des mises à jour", "Nach Updates suchen", "Procurar atualizações", "Cerca aggiornamenti"],
"formats.updates.tip": [
 "Opens the download page in your web browser. {0} itself never connects to the internet.",
 "Abre la página de descarga en tu navegador. {0} nunca se conecta a internet por sí mismo.",
 "Öppnar nedladdningssidan i din webbläsare. {0} ansluter aldrig själv till internet.",
 "Ouvre la page de téléchargement dans votre navigateur. {0} ne se connecte jamais à Internet.",
 "Öffnet die Download-Seite in Ihrem Browser. {0} selbst verbindet sich nie mit dem Internet.",
 "Abre a página de transferência no seu navegador. {0} nunca se liga à internet.",
 "Apre la pagina di download nel browser. {0} non si connette mai a Internet."],

"f.intro": [
 "{0} never connects to the internet and keeps no record of what you open.\n\"Check for updates\" below opens the download page in your web browser.",
 "{0} nunca se conecta a internet y no guarda ningún registro de lo que abres.\n«Buscar actualizaciones», abajo, abre la página de descarga en tu navegador.",
 "{0} ansluter aldrig till internet och sparar ingen historik över vad du öppnar.\n\"Sök efter uppdateringar\" nedan öppnar nedladdningssidan i din webbläsare.",
 "{0} ne se connecte jamais à Internet et ne garde aucune trace de ce que vous ouvrez.\n« Rechercher des mises à jour », ci-dessous, ouvre la page de téléchargement dans votre navigateur.",
 "{0} verbindet sich nie mit dem Internet und speichert nicht, was Sie öffnen.\n„Nach Updates suchen“ unten öffnet die Download-Seite in Ihrem Browser.",
 "{0} nunca se liga à internet e não guarda nenhum registo do que abre.\n«Procurar atualizações», em baixo, abre a página de transferência no seu navegador.",
 "{0} non si connette mai a Internet e non tiene traccia di ciò che apri.\n\"Cerca aggiornamenti\", qui sotto, apre la pagina di download nel browser."],
"f.decoders": ["DECODERS", "DECODIFICADORES", "AVKODARE", "DÉCODEURS", "DECODER", "DESCODIFICADORES", "DECODIFICATORI"],
"f.decoders.body": [
 "Pictures, animations and videos are all decoded by FFmpeg, which is limited to reading local files. Nothing is sent anywhere: the app's own code has no networking.",
 "Las imágenes, animaciones y vídeos se decodifican con FFmpeg, que está limitado a leer archivos locales. No se envía nada a ningún sitio: el código propio de la aplicación no tiene funciones de red.",
 "Bilder, animationer och videor avkodas alla av FFmpeg, som är begränsat till att läsa lokala filer. Ingenting skickas någonstans: appens egen kod har inga nätverksfunktioner.",
 "Les images, animations et vidéos sont toutes décodées par FFmpeg, limité à la lecture de fichiers locaux. Rien n'est envoyé nulle part : le code propre à l'application ne contient aucune fonction réseau.",
 "Bilder, Animationen und Videos werden alle von FFmpeg dekodiert, das auf das Lesen lokaler Dateien beschränkt ist. Nichts wird irgendwohin gesendet: Der eigene Code der App enthält keine Netzwerkfunktionen.",
 "Imagens, animações e vídeos são todos descodificados pelo FFmpeg, que está limitado à leitura de ficheiros locais. Nada é enviado para lado nenhum: o código da própria aplicação não tem funções de rede.",
 "Immagini, animazioni e video sono tutti decodificati da FFmpeg, limitato alla lettura di file locali. Nulla viene inviato altrove: il codice proprio dell'app non contiene funzioni di rete."],
"f.loaded": [
 "FFmpeg {0} is loaded from:\n{1}\nBuild licence: {2}",
 "FFmpeg {0} se ha cargado desde:\n{1}\nLicencia de la compilación: {2}",
 "FFmpeg {0} är inläst från:\n{1}\nByggets licens: {2}",
 "FFmpeg {0} est chargé depuis :\n{1}\nLicence de la compilation : {2}",
 "FFmpeg {0} wurde geladen aus:\n{1}\nLizenz des Builds: {2}",
 "FFmpeg {0} foi carregado de:\n{1}\nLicença da compilação: {2}",
 "FFmpeg {0} è caricato da:\n{1}\nLicenza della build: {2}"],
"f.missing": [
 "FFmpeg was not found, so nothing can be opened.\n({0})\n\nTo add it: download an LGPL \"shared\" build of FFmpeg 8.1 for Windows from one of the build sites linked at https://ffmpeg.org/download.html, and copy the DLL files from its \"bin\" folder into:\n{1}",
 "No se ha encontrado FFmpeg, así que no se puede abrir nada.\n({0})\n\nPara añadirlo: descarga una compilación LGPL «shared» de FFmpeg 8.1 para Windows desde uno de los sitios enlazados en https://ffmpeg.org/download.html y copia los archivos DLL de su carpeta «bin» en:\n{1}",
 "FFmpeg hittades inte, så ingenting kan öppnas.\n({0})\n\nSå lägger du till det: ladda ner ett LGPL-bygge (\"shared\") av FFmpeg 8.1 för Windows från någon av byggsidorna som länkas på https://ffmpeg.org/download.html och kopiera DLL-filerna från dess \"bin\"-mapp till:\n{1}",
 "FFmpeg est introuvable, rien ne peut donc être ouvert.\n({0})\n\nPour l'ajouter : téléchargez une compilation LGPL « shared » de FFmpeg 8.1 pour Windows depuis l'un des sites indiqués sur https://ffmpeg.org/download.html, puis copiez les fichiers DLL de son dossier « bin » dans :\n{1}",
 "FFmpeg wurde nicht gefunden, daher kann nichts geöffnet werden.\n({0})\n\nSo fügen Sie es hinzu: Laden Sie einen LGPL-„shared“-Build von FFmpeg 8.1 für Windows von einer der auf https://ffmpeg.org/download.html verlinkten Seiten herunter und kopieren Sie die DLL-Dateien aus dessen „bin“-Ordner nach:\n{1}",
 "O FFmpeg não foi encontrado, por isso nada pode ser aberto.\n({0})\n\nPara o adicionar: transfira uma compilação LGPL «shared» do FFmpeg 8.1 para Windows a partir de um dos sites indicados em https://ffmpeg.org/download.html e copie os ficheiros DLL da pasta «bin» para:\n{1}",
 "FFmpeg non è stato trovato, quindi non è possibile aprire nulla.\n({0})\n\nPer aggiungerlo: scarica una build LGPL \"shared\" di FFmpeg 8.1 per Windows da uno dei siti indicati su https://ffmpeg.org/download.html e copia i file DLL dalla sua cartella \"bin\" in:\n{1}"],
"f.pictures": ["PICTURES AND ANIMATIONS", "IMÁGENES Y ANIMACIONES", "BILDER OCH ANIMATIONER", "IMAGES ET ANIMATIONS", "BILDER UND ANIMATIONEN", "IMAGENS E ANIMAÇÕES", "IMMAGINI E ANIMAZIONI"],
"f.animated": ["Animated: gif, apng, webp.", "Animadas: gif, apng, webp.", "Animerade: gif, apng, webp.", "Animées : gif, apng, webp.", "Animiert: gif, apng, webp.", "Animadas: gif, apng, webp.", "Animate: gif, apng, webp."],
"f.video": ["VIDEO", "VÍDEO", "VIDEO", "VIDÉO", "VIDEO", "VÍDEO", "VIDEO"],
"f.video.gpu": [
 "Video is decoded on the graphics card when it supports the format, otherwise on the processor. Start the app with --software to always use the processor.",
 "El vídeo se decodifica en la tarjeta gráfica cuando admite el formato; si no, en el procesador. Inicia la aplicación con --software para usar siempre el procesador.",
 "Video avkodas på grafikkortet när det stöder formatet, annars på processorn. Starta appen med --software för att alltid använda processorn.",
 "La vidéo est décodée par la carte graphique lorsqu'elle prend en charge le format, sinon par le processeur. Lancez l'application avec --software pour toujours utiliser le processeur.",
 "Videos werden auf der Grafikkarte dekodiert, wenn sie das Format unterstützt, sonst auf dem Prozessor. Starten Sie die App mit --software, um immer den Prozessor zu verwenden.",
 "O vídeo é descodificado na placa gráfica quando esta suporta o formato; caso contrário, no processador. Inicie a aplicação com --software para usar sempre o processador.",
 "I video sono decodificati dalla scheda grafica quando supporta il formato, altrimenti dal processore. Avvia l'app con --software per usare sempre il processore."],
"f.video.depends": [
 "Whether a given file opens depends on the decoders in the FFmpeg build above.",
 "Que un archivo concreto se abra depende de los decodificadores de la compilación de FFmpeg indicada arriba.",
 "Om en viss fil går att öppna beror på avkodarna i FFmpeg-bygget ovan.",
 "L'ouverture d'un fichier donné dépend des décodeurs de la compilation FFmpeg ci-dessus.",
 "Ob sich eine bestimmte Datei öffnen lässt, hängt von den Decodern im oben genannten FFmpeg-Build ab.",
 "Se um dado ficheiro abre ou não depende dos descodificadores da compilação do FFmpeg indicada acima.",
 "L'apertura di un determinato file dipende dai decodificatori presenti nella build di FFmpeg qui sopra."],
"f.credits": ["CREDITS", "CRÉDITOS", "TACK", "CRÉDITS", "DANKSAGUNGEN", "CRÉDITOS", "RICONOSCIMENTI"],
"f.credits.ffmpeg": [
 "This software uses libraries from the FFmpeg project (https://ffmpeg.org), licensed under the GNU Lesser General Public License. The licence text is in codecs\\FFmpeg-LICENSE.txt, and THIRD-PARTY-NOTICES.txt says where to get the exact source code of the build shipped with this app.",
 "Este software utiliza bibliotecas del proyecto FFmpeg (https://ffmpeg.org), con licencia GNU Lesser General Public License. El texto de la licencia está en codecs\\FFmpeg-LICENSE.txt, y THIRD-PARTY-NOTICES.txt indica dónde obtener el código fuente exacto de la compilación incluida con esta aplicación.",
 "Den här programvaran använder bibliotek från FFmpeg-projektet (https://ffmpeg.org), licensierade under GNU Lesser General Public License. Licenstexten finns i codecs\\FFmpeg-LICENSE.txt, och THIRD-PARTY-NOTICES.txt anger var den exakta källkoden för det medföljande bygget finns.",
 "Ce logiciel utilise des bibliothèques du projet FFmpeg (https://ffmpeg.org), sous licence GNU Lesser General Public License. Le texte de la licence se trouve dans codecs\\FFmpeg-LICENSE.txt, et THIRD-PARTY-NOTICES.txt indique où obtenir le code source exact de la compilation fournie avec cette application.",
 "Diese Software verwendet Bibliotheken des FFmpeg-Projekts (https://ffmpeg.org), lizenziert unter der GNU Lesser General Public License. Der Lizenztext steht in codecs\\FFmpeg-LICENSE.txt, und THIRD-PARTY-NOTICES.txt nennt die Bezugsquelle des genauen Quellcodes des mitgelieferten Builds.",
 "Este software utiliza bibliotecas do projeto FFmpeg (https://ffmpeg.org), licenciadas ao abrigo da GNU Lesser General Public License. O texto da licença está em codecs\\FFmpeg-LICENSE.txt, e THIRD-PARTY-NOTICES.txt indica onde obter o código-fonte exato da compilação incluída nesta aplicação.",
 "Questo software utilizza librerie del progetto FFmpeg (https://ffmpeg.org), concesse in licenza secondo la GNU Lesser General Public License. Il testo della licenza si trova in codecs\\FFmpeg-LICENSE.txt, e THIRD-PARTY-NOTICES.txt indica dove ottenere il codice sorgente esatto della build fornita con questa app."],
"f.credits.replace": [
 "FFmpeg is used as separate DLL files and is not modified. You may replace the DLLs in the codecs folder with your own build of FFmpeg 8.x.",
 "FFmpeg se utiliza como archivos DLL independientes y no se ha modificado. Puedes sustituir los DLL de la carpeta codecs por tu propia compilación de FFmpeg 8.x.",
 "FFmpeg används som separata DLL-filer och är inte ändrat. Du får ersätta DLL-filerna i mappen codecs med ditt eget bygge av FFmpeg 8.x.",
 "FFmpeg est utilisé sous forme de fichiers DLL distincts et n'est pas modifié. Vous pouvez remplacer les DLL du dossier codecs par votre propre compilation de FFmpeg 8.x.",
 "FFmpeg wird in Form separater DLL-Dateien verwendet und ist unverändert. Sie dürfen die DLLs im Ordner codecs durch Ihren eigenen Build von FFmpeg 8.x ersetzen.",
 "O FFmpeg é utilizado como ficheiros DLL separados e não foi modificado. Pode substituir os DLL da pasta codecs pela sua própria compilação do FFmpeg 8.x.",
 "FFmpeg è usato come file DLL separati e non è stato modificato. Puoi sostituire i DLL nella cartella codecs con una tua build di FFmpeg 8.x."],
"f.credits.trademark": [
 "FFmpeg is a trademark of Fabrice Bellard, originator of the FFmpeg project. This app is not affiliated with or endorsed by the FFmpeg project.",
 "FFmpeg es una marca de Fabrice Bellard, creador del proyecto FFmpeg. Esta aplicación no está afiliada al proyecto FFmpeg ni cuenta con su respaldo.",
 "FFmpeg är ett varumärke som tillhör Fabrice Bellard, upphovsman till FFmpeg-projektet. Den här appen är inte knuten till eller godkänd av FFmpeg-projektet.",
 "FFmpeg est une marque de Fabrice Bellard, à l'origine du projet FFmpeg. Cette application n'est ni affiliée au projet FFmpeg ni approuvée par lui.",
 "FFmpeg ist eine Marke von Fabrice Bellard, dem Begründer des FFmpeg-Projekts. Diese App steht in keiner Verbindung zum FFmpeg-Projekt und wird von ihm nicht unterstützt.",
 "FFmpeg é uma marca de Fabrice Bellard, criador do projeto FFmpeg. Esta aplicação não é afiliada ao projeto FFmpeg nem aprovada por ele.",
 "FFmpeg è un marchio di Fabrice Bellard, ideatore del progetto FFmpeg. Questa app non è affiliata al progetto FFmpeg né da esso approvata."],

"err.open": ["Can't open the file", "No se puede abrir el archivo", "Det går inte att öppna filen", "Impossible d'ouvrir le fichier", "Die Datei kann nicht geöffnet werden", "Não é possível abrir o ficheiro", "Impossibile aprire il file"],
"err.unsupported": ["This kind of file is not supported.", "Este tipo de archivo no es compatible.", "Den här typen av fil stöds inte.", "Ce type de fichier n'est pas pris en charge.", "Diese Art von Datei wird nicht unterstützt.", "Este tipo de ficheiro não é suportado.", "Questo tipo di file non è supportato."],
"err.novideo": ["No playable video track", "No hay ninguna pista de vídeo reproducible", "Inget spelbart videospår", "Aucune piste vidéo lisible", "Keine abspielbare Videospur", "Nenhuma faixa de vídeo reproduzível", "Nessuna traccia video riproducibile"],
"err.noffmpeg": ["FFmpeg is not available: {0}", "FFmpeg no está disponible: {0}", "FFmpeg är inte tillgängligt: {0}", "FFmpeg n'est pas disponible : {0}", "FFmpeg ist nicht verfügbar: {0}", "O FFmpeg não está disponível: {0}", "FFmpeg non è disponibile: {0}"],
"err.decode": ["The picture could not be decoded.", "No se ha podido decodificar la imagen.", "Bilden kunde inte avkodas.", "L'image n'a pas pu être décodée.", "Das Bild konnte nicht dekodiert werden.", "Não foi possível descodificar a imagem.", "Impossibile decodificare l'immagine."],
}

here = os.path.dirname(os.path.abspath(__file__))
for index, code in enumerate(LANGUAGES):
    table = {}
    for key, values in T.items():
        assert len(values) == len(LANGUAGES), key
        # every translation must keep the same placeholders as the English text
        for n in range(4):
            assert ("{%d}" % n in values[0]) == ("{%d}" % n in values[index]), (key, code, n)
        table[key] = values[index]
    with io.open(os.path.join(here, code + ".json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(table, f, ensure_ascii=False, indent=1)
        f.write("\n")
print("wrote", ", ".join(c + ".json" for c in LANGUAGES), "with", len(T), "texts each")

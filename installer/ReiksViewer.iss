; Inno Setup script for Reiks Viewer. Build everything with ..\release.ps1.
; The installer is written to ..\dist.

#define AppName "Reiks Viewer"
; The same name without a space, for registry keys, file names and the options folder.
#define AppKey "ReiksViewer"
; Taken from the built program, so the version is set in one place: ReiksViewer.csproj.
#define AppVersion GetStringFileInfo("..\dist\ReiksViewer\ReiksViewer.exe", "ProductVersion")
#define AppExe "ReiksViewer.exe"

[Setup]
AppId={{2F4A9C61-8D3E-4B7A-A5C2-91E6D0B3F7A4}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Reiks Apps
AppPublisherURL=https://reiks-apps.github.io/reiks-viewer/
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Installs for the current user without asking for administrator rights;
; the first page offers "for all users" instead.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Shown before installing: the FFmpeg credit and licence information.
InfoBeforeFile=..\THIRD-PARTY-NOTICES.txt
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\{#AppExe}
OutputDir=..\dist
OutputBaseFilename=ReiksViewer-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Tells Explorer to re-read file associations after installing and uninstalling. Without it
; Explorer keeps starting whatever it remembered until the next sign-in.
ChangesAssociations=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "swedish"; MessagesFile: "compiler:Languages\Swedish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "openwith"; Description: "Add Reiks Viewer to the ""Open with"" and ""Default apps"" lists for pictures and videos"; GroupDescription: "Explorer:"
Name: "firewall"; Description: "Block Reiks Viewer from the network with a Windows Firewall rule"; GroupDescription: "Privacy:"; Flags: unchecked; Check: IsAdminInstallMode
Name: "defaults"; Description: "Let me choose Reiks Viewer as the default app for pictures and videos (opens Windows Settings at the end)"; GroupDescription: "Explorer:"; Flags: unchecked

[UninstallRun]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Reiks Viewer (block network)"""; Flags: runhidden; RunOnceId: "RemoveFirewallRule"; Check: IsAdminInstallMode

[UninstallDelete]
; The options file (chosen keys and window colour).
Type: filesandordirs; Name: "{userappdata}\{#AppKey}"

[InstallDelete]
; When updating, clear out the previous version's decoders so that DLLs which a newer
; FFmpeg no longer uses are not left behind.
Type: filesandordirs; Name: "{app}\codecs"

[Files]
Source: "..\dist\ReiksViewer\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Makes Reiks Viewer appear under "Open with" without taking over any file type.
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "{#AppName}"; Flags: uninsdeletekey; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\shell\open\command"; ValueType: string; ValueData: """{app}\{#AppExe}"" ""%1"""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".jpg"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".jpeg"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".jpe"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".jfif"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".png"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".apng"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".bmp"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".gif"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".tif"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".tiff"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".webp"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".ico"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".heic"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".heif"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".avif"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".jxl"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".jp2"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".j2k"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".tga"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".qoi"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".svg"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".mp4"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".m4v"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".mov"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".wmv"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".avi"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".mkv"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".webm"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".mpg"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".mpeg"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".3gp"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".ts"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".mts"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".m2ts"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".flv"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".f4v"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".ogv"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".vob"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".asf"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".gifv"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".mxf"; ValueData: ""; Tasks: openwith
Root: HKA; Subkey: "Software\Classes\Applications\{#AppExe}\SupportedTypes"; ValueType: string; ValueName: ".divx"; ValueData: ""; Tasks: openwith

; --- Default app registration (either task) ---
; Windows only lets the user pick a default app, in Settings. These entries make Reiks Viewer
; one of the apps offered there; nothing is taken over by the installer itself. The task
; "defaults" only adds opening that Settings page at the end.
Root: HKA; Subkey: "Software\Classes\ReiksViewer.Picture"; ValueType: string; ValueData: "Picture"; Flags: uninsdeletekey; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\ReiksViewer.Picture\DefaultIcon"; ValueType: string; ValueData: "{app}\{#AppExe},0"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\ReiksViewer.Picture\shell\open\command"; ValueType: string; ValueData: """{app}\{#AppExe}"" ""%1"""; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\ReiksViewer.Video"; ValueType: string; ValueData: "Video"; Flags: uninsdeletekey; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\ReiksViewer.Video\DefaultIcon"; ValueType: string; ValueData: "{app}\{#AppExe},0"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\ReiksViewer.Video\shell\open\command"; ValueType: string; ValueData: """{app}\{#AppExe}"" ""%1"""; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}"; Flags: uninsdeletekeyifempty; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#AppName}"; Flags: uninsdeletekey; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Picture, GIF and video viewer"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "{#AppKey}"; ValueData: "Software\{#AppKey}\Capabilities"; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpg"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.jpg\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpeg"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.jpeg\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpe"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.jpe\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jfif"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.jfif\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".png"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.png\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".apng"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.apng\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".bmp"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.bmp\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gif"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.gif\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tif"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.tif\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tiff"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.tiff\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".webp"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.webp\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ico"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.ico\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".heic"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.heic\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".heif"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.heif\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".avif"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.avif\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jxl"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.jxl\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jp2"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.jp2\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".j2k"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.j2k\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tga"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.tga\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".qoi"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.qoi\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".svg"; ValueData: "ReiksViewer.Picture"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.svg\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mp4"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.mp4\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".m4v"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.m4v\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mov"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.mov\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".wmv"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.wmv\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".avi"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.avi\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mkv"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.mkv\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".webm"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.webm\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mpg"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.mpg\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mpeg"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.mpeg\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".3gp"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.3gp\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ts"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.ts\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mts"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.mts\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".m2ts"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.m2ts\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".flv"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.flv\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".f4v"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.f4v\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ogv"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.ogv\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".vob"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.vob\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".asf"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.asf\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gifv"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.gifv\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mxf"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.mxf\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\{#AppKey}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".divx"; ValueData: "ReiksViewer.Video"; Tasks: openwith or defaults
Root: HKA; Subkey: "Software\Classes\.divx\OpenWithProgids"; ValueType: string; ValueName: "ReiksViewer.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: openwith or defaults

; --- Coming from Simple, the app's earlier name ---
; Someone who made Simple their default app has that choice stored by Windows under Simple's
; two registration names. Registering those names for Reiks Viewer keeps their choice working.
; Only written when such a choice exists.
Root: HKA; Subkey: "Software\Classes\Simple.Picture"; ValueType: string; ValueData: "Picture"; Flags: uninsdeletekey; Check: HadSimpleDefaults
Root: HKA; Subkey: "Software\Classes\Simple.Picture\DefaultIcon"; ValueType: string; ValueData: "{app}\{#AppExe},0"; Check: HadSimpleDefaults
Root: HKA; Subkey: "Software\Classes\Simple.Picture\shell\open\command"; ValueType: string; ValueData: """{app}\{#AppExe}"" ""%1"""; Check: HadSimpleDefaults
Root: HKA; Subkey: "Software\Classes\Simple.Video"; ValueType: string; ValueData: "Video"; Flags: uninsdeletekey; Check: HadSimpleDefaults
Root: HKA; Subkey: "Software\Classes\Simple.Video\DefaultIcon"; ValueType: string; ValueData: "{app}\{#AppExe},0"; Check: HadSimpleDefaults
Root: HKA; Subkey: "Software\Classes\Simple.Video\shell\open\command"; ValueType: string; ValueData: """{app}\{#AppExe}"" ""%1"""; Check: HadSimpleDefaults

[Run]
; Optional (all-users install only, since it needs administrator rights): a firewall rule so
; that Windows itself keeps the app off the network, whatever the app's code does.
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Reiks Viewer (block network)"" dir=out action=block program=""{app}\{#AppExe}"" enable=yes"; Flags: runhidden; Tasks: firewall
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Reiks Viewer (block network)"" dir=in action=block program=""{app}\{#AppExe}"" enable=yes"; Flags: runhidden; Tasks: firewall
; The Settings page for this app, where the user confirms the defaults.
Filename: "ms-settings:defaultapps?registeredAppUser={#AppKey}"; Description: "Open Windows Settings to make Reiks Viewer the default app"; Flags: shellexec nowait postinstall skipifsilent; Tasks: defaults; Check: not IsAdminInstallMode
Filename: "ms-settings:defaultapps?registeredAppMachine={#AppKey}"; Description: "Open Windows Settings to make Reiks Viewer the default app"; Flags: shellexec nowait postinstall skipifsilent; Tasks: defaults; Check: IsAdminInstallMode
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// True when this user chose Simple (the app's earlier name) as the default for a file type.
function ChoseSimpleFor(Ext: String): Boolean;
var
  Key, Value: String;
begin
  Key := 'Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\' + Ext;
  Result := (RegQueryStringValue(HKCU, Key + '\UserChoiceLatest\ProgId', 'ProgId', Value) or
             RegQueryStringValue(HKCU, Key + '\UserChoice', 'ProgId', Value))
            and ((Value = 'Simple.Picture') or (Value = 'Simple.Video'));
end;

function HadSimpleDefaults: Boolean;
begin
  Result := ChoseSimpleFor('.jpg') or ChoseSimpleFor('.png') or ChoseSimpleFor('.gif') or
            ChoseSimpleFor('.webp') or ChoseSimpleFor('.mp4') or ChoseSimpleFor('.mkv') or
            ChoseSimpleFor('.mov') or ChoseSimpleFor('.avi') or ChoseSimpleFor('.webm');
end;

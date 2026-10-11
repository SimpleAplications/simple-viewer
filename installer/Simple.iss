; Inno Setup script for Simple. Build everything with ..\release.ps1.
; The installer is written to ..\dist.

#define AppName "Simple"
; Taken from the built program, so the version is set in one place: MediaViewer.csproj.
#define AppVersion GetStringFileInfo("..\dist\Simple\Simple.exe", "ProductVersion")
#define AppExe "Simple.exe"

[Setup]
AppId={{6C1B0E5E-3B0F-4D5B-9C36-5A1F0A7B51E0}
AppName={#AppName}
AppVersion={#AppVersion}
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
OutputBaseFilename=Simple-Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "openwith"; Description: "Add Simple to the ""Open with"" list for pictures and videos"; GroupDescription: "Explorer:"
Name: "firewall"; Description: "Block Simple from the network with a Windows Firewall rule"; GroupDescription: "Privacy:"; Flags: unchecked; Check: IsAdminInstallMode
Name: "defaults"; Description: "Let me choose Simple as the default app for pictures and videos (opens Windows Settings at the end)"; GroupDescription: "Explorer:"; Flags: unchecked

[UninstallRun]
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall delete rule name=""Simple (block network)"""; Flags: runhidden; RunOnceId: "RemoveFirewallRule"; Check: IsAdminInstallMode

[UninstallDelete]
; The options file (chosen keys and window colour).
Type: filesandordirs; Name: "{userappdata}\{#AppName}"

[InstallDelete]
; When updating, clear out the previous version's decoders so that DLLs which a newer
; FFmpeg no longer uses are not left behind.
Type: filesandordirs; Name: "{app}\codecs"

[Files]
Source: "..\dist\Simple\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Makes Simple appear under "Open with" without taking over any file type.
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

; --- Default app registration (task "defaults") ---
; Windows only lets the user pick a default app, in Settings. These entries make Simple
; one of the apps offered there; nothing is taken over by the installer itself.
Root: HKA; Subkey: "Software\Classes\Simple.Picture"; ValueType: string; ValueData: "Picture"; Flags: uninsdeletekey; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\Simple.Picture\DefaultIcon"; ValueType: string; ValueData: "{app}\{#AppExe},0"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\Simple.Picture\shell\open\command"; ValueType: string; ValueData: """{app}\{#AppExe}"" ""%1"""; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\Simple.Video"; ValueType: string; ValueData: "Video"; Flags: uninsdeletekey; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\Simple.Video\DefaultIcon"; ValueType: string; ValueData: "{app}\{#AppExe},0"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\Simple.Video\shell\open\command"; ValueType: string; ValueData: """{app}\{#AppExe}"" ""%1"""; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}"; Flags: uninsdeletekeyifempty; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#AppName}"; Flags: uninsdeletekey; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Picture, GIF and video viewer"; Tasks: defaults
Root: HKA; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "{#AppName}"; ValueData: "Software\{#AppName}\Capabilities"; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpg"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.jpg\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpeg"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.jpeg\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jpe"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.jpe\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jfif"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.jfif\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".png"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.png\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".apng"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.apng\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".bmp"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.bmp\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gif"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.gif\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tif"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.tif\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tiff"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.tiff\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".webp"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.webp\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ico"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.ico\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".heic"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.heic\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".heif"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.heif\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".avif"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.avif\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jxl"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.jxl\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".jp2"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.jp2\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".j2k"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.j2k\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".tga"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.tga\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".qoi"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.qoi\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".svg"; ValueData: "Simple.Picture"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.svg\OpenWithProgids"; ValueType: string; ValueName: "Simple.Picture"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mp4"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.mp4\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".m4v"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.m4v\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mov"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.mov\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".wmv"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.wmv\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".avi"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.avi\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mkv"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.mkv\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".webm"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.webm\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mpg"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.mpg\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mpeg"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.mpeg\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".3gp"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.3gp\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ts"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.ts\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mts"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.mts\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".m2ts"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.m2ts\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".flv"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.flv\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".f4v"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.f4v\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ogv"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.ogv\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".vob"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.vob\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".asf"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.asf\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".gifv"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.gifv\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mxf"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.mxf\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults
Root: HKA; Subkey: "Software\{#AppName}\Capabilities\FileAssociations"; ValueType: string; ValueName: ".divx"; ValueData: "Simple.Video"; Tasks: defaults
Root: HKA; Subkey: "Software\Classes\.divx\OpenWithProgids"; ValueType: string; ValueName: "Simple.Video"; ValueData: ""; Flags: uninsdeletevalue; Tasks: defaults

[Run]
; Optional (all-users install only, since it needs administrator rights): a firewall rule so
; that Windows itself keeps the app off the network, whatever the app's code does.
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Simple (block network)"" dir=out action=block program=""{app}\{#AppExe}"" enable=yes"; Flags: runhidden; Tasks: firewall
Filename: "{sys}\netsh.exe"; Parameters: "advfirewall firewall add rule name=""Simple (block network)"" dir=in action=block program=""{app}\{#AppExe}"" enable=yes"; Flags: runhidden; Tasks: firewall
; The Settings page for this app, where the user confirms the defaults.
Filename: "ms-settings:defaultapps?registeredAppUser={#AppName}"; Description: "Open Windows Settings to make Simple the default app"; Flags: shellexec nowait postinstall skipifsilent; Tasks: defaults; Check: not IsAdminInstallMode
Filename: "ms-settings:defaultapps?registeredAppMachine={#AppName}"; Description: "Open Windows Settings to make Simple the default app"; Flags: shellexec nowait postinstall skipifsilent; Tasks: defaults; Check: IsAdminInstallMode
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

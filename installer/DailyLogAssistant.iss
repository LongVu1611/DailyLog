#define AppName "Personal Log Manager"
#define AppVersion "2.0.0"
#define AppExeName "PersonalLogManager.exe"

[Setup]
AppId={{4B35B5F5-13D3-4FB4-976A-B5182C77774A}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Personal Log Manager
DefaultDirName={localappdata}\Programs\Personal Log Manager
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\publish
OutputBaseFilename=PersonalLogManager-Setup-{#AppVersion}
UninstallDisplayIcon={app}\{#AppExeName}
CloseApplications=force
RestartApplications=no
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=2.0.0.0
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Excludes: "*.pdb,DailyLogAssistant.exe"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

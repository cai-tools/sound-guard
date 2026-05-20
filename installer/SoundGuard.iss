#define MyAppName "SoundMonitor"
#ifndef MyAppVersion
  #define MyAppVersion "0.0.1"
#endif
#ifndef SourceDir
  #define SourceDir "..\\dist\\portable-win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\\dist\\installer"
#endif

[Setup]
AppId={{A0A1A5A6-8B20-4F6E-9ED7-6C0F537AF2C0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Developer
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir={#OutputDir}
OutputBaseFilename=SoundMonitor-Setup-v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\SoundMonitor.exe"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\SoundMonitor.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\SoundMonitor.exe"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

; Roomy installer (Inno Setup 6). Per-user install, no admin prompt.
#ifndef AppVersion
  #define AppVersion "0.2.0"
#endif
; Windows file versions must be numeric: "0.2.0" -> "0.2.0.0". CI passes this
; separately so tags like "v0.2.0-ru" still produce a valid version resource.
#ifndef AppVersionNumeric
  #define AppVersionNumeric "0.2.0.0"
#endif

[Setup]
AppId={{6B0F3B2E-6C1A-4C7E-9E3B-52A1F0D7C6A4}
AppName=Roomy
AppVersion={#AppVersion}
AppVerName=Roomy {#AppVersion}
AppPublisher=krezio
AppPublisherURL=https://github.com/krezio/roomy
AppSupportURL=https://github.com/krezio/roomy/issues
AppUpdatesURL=https://github.com/krezio/roomy/releases
AppComments=Unofficial Russian build of Roomy. Original project: https://github.com/krezio/roomy
VersionInfoVersion={#AppVersionNumeric}
VersionInfoCompany=krezio
VersionInfoDescription=Roomy Setup
VersionInfoProductName=Roomy
VersionInfoProductVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\Roomy
DefaultGroupName=Roomy
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=Roomy-Setup
SetupIconFile=..\src\Roomy\Assets\roomy.ico
UninstallDisplayIcon={app}\Roomy.exe
UninstallDisplayName=Roomy
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
CloseApplications=yes
RestartApplications=no
AllowNoIcons=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\dist\publish\Roomy.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\NOTICE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Roomy"; Filename: "{app}\Roomy.exe"; Parameters: "{code:AppLanguageParam}"
Name: "{group}\{cm:UninstallProgram,Roomy}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Roomy"; Filename: "{app}\Roomy.exe"; Parameters: "{code:AppLanguageParam}"; Tasks: desktopicon

[Run]
Filename: "{app}\Roomy.exe"; Parameters: "{code:AppLanguageParam}"; Description: "{cm:LaunchProgram,Roomy}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\Roomy.exe"; Parameters: "--uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "RoomyBackground"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "Roomy"; Flags: dontcreatekey uninsdeletevalue

[Code]
{ Keep the language picked in the wizard for the shortcuts and the first launch,
  so a Russian-speaking user on an English Windows gets the Russian UI. }
function AppLanguageParam(Param: string): string;
begin
  if ActiveLanguage() = 'russian' then
    Result := '--lang ru'
  else
    Result := '--lang en';
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    { The app removes its Run value itself; this is a safety net for a manual uninstall. }
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Roomy');
  end;
end;

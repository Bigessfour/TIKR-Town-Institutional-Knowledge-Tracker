; =============================================================================
; TIKR — Clerk's Vault  |  Windows installer (Inno Setup 6)
; =============================================================================
; Municipal / town-clerk oriented setup for a self-contained .NET Blazor Server
; stack (TIKR.Api + TIKR.Web). Calm UI, Program Files install, ProgramData data.
;
; Prerequisites on the BUILD machine (Windows):
;   1. Inno Setup 6.x  — https://jrsoftware.org/isinfo.php
;   2. Published payload in SourceDir (see MySourceDir below), e.g.:
;        C:\TIKR\TIKR.Api\TIKR.Api.exe
;        C:\TIKR\TIKR.Web\TIKR.Web.exe
;      Or from this repo (after package-thumb-drive / publish):
;        ..\publish\TIKR-Deploy\
;
; Compile:
;   - Double-click this .iss, or
;   - "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\tikr-setup.iss
;
; Output:
;   installer\Output\Setup-TIKR.exe
;
; Security:
;   Do NOT hard-code Syncfusion keys in git. Use license.local.iss (gitignored)
;   or enter the key on the install wizard page.
; =============================================================================

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName "TIKR — Clerk's Vault"
#define MyAppNameShort "TIKR"
#define MyAppPublisher "Town of Wiley (TIKR)"
#define MyAppURL "http://localhost:8080"
#define MyAppHelpURL "http://localhost:8080"
#define MyAppExeName "TIKR.Web.exe"
#define MyStartMenuFolder "TIKR - Clerk's Vault"

; --- Source payload ----------------------------------------------------------
; Default matches a common Dell layout: everything under C:\TIKR
; Override at compile time:
;   ISCC.exe /DMySourceDir="D:\build\TIKR-Deploy" tikr-setup.iss
;
; Expected layout under MySourceDir:
;   TIKR.Api\TIKR.Api.exe
;   TIKR.Web\TIKR.Web.exe
;   (optional appsettings.Production.json already copied by package script)
#ifndef MySourceDir
  #define MySourceDir "C:\TIKR"
#endif

; Optional ports (firewall + runtime defaults)
#ifndef MyApiPort
  #define MyApiPort "5000"
#endif
#ifndef MyWebPort
  #define MyWebPort "8080"
#endif

; Optional pre-baked license (prefer empty + wizard entry)
#ifndef MyAppSyncfusionLicense
  #define MyAppSyncfusionLicense ""
#endif

; Pull private defines if present (Windows build PC only)
#ifexist "license.local.iss"
  #include "license.local.iss"
#endif

[Setup]
; Stable AppId — do not change or Windows treats upgrades as a different product
AppId={{A7C3E9F1-4B2D-4E8A-9C11-8F0E2B1A3D45}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppHelpURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppNameShort}
DefaultGroupName={#MyStartMenuFolder}
DisableProgramGroupPage=yes
; Calm, government-friendly wizard (no custom skin required)
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
MinVersion=10.0
OutputDir=Output
OutputBaseFilename=Setup-TIKR
; Uncomment when you have a town icon:
SetupIconFile=assets\tikr.ico
UninstallDisplayIcon={app}\TIKR.Web\{#MyAppExeName}
LicenseFile=
InfoBeforeFile=
InfoAfterFile=assets\README-Installed.txt
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=TIKR Clerk's Vault Windows installer
VersionInfoProductName={#MyAppName}
VersionInfoCopyright=Copyright (C) Town / TIKR project
UninstallDisplayName={#MyAppName}
; Close running processes so upgrade/uninstall is clean
CloseApplications=yes
RestartApplications=no
; Do not leave the clerk on a console
DisableWelcomePage=no
DisableFinishedPage=no
ShowLanguageDialog=no
; Space for two self-contained .NET apps (~150–250 MB typical)
ExtraDiskSpaceRequired=0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to TIKR — Clerk's Vault Setup
WelcomeLabel2=This will install TIKR on your computer.%n%nTIKR helps town clerks keep institutional knowledge, requirements, and documents in one local place.%n%nIt is recommended that you close other applications before continuing.
FinishedLabelNoIcons=Setup has finished installing TIKR — Clerk's Vault on your computer.%n%nUse the "Start TIKR" shortcut on your Desktop to open the app.
ClickFinish=Click Finish to exit Setup.

[Tasks]
Name: "desktopicon"; Description: "Create a &Desktop shortcut for Start TIKR"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce
Name: "firewall"; Description: "Allow TIKR through Windows Firewall (ports {#MyApiPort} and {#MyWebPort})"; GroupDescription: "Network:"; Flags: checkedonce
Name: "autostart"; Description: "Start TIKR when I sign in to Windows (optional)"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
; --- Published self-contained apps ------------------------------------------
; Exclude bulky PDBs and XML docs from the municipal package
Source: "{#MySourceDir}\TIKR.Api\*"; DestDir: "{app}\TIKR.Api"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Excludes: "*.pdb,*.xml"
Source: "{#MySourceDir}\TIKR.Web\*"; DestDir: "{app}\TIKR.Web"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; \
  Excludes: "*.pdb,*.xml"

; --- Installer helpers (always from this repo's installer\ folder) -----------
Source: "scripts\Start-TIKR-Installed.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\Stop-TIKR-Installed.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\Start-TIKR-Hidden.vbs"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "scripts\Configure-Firewall.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion
Source: "assets\README-Installed.txt"; DestDir: "{app}"; Flags: ignoreversion isreadme

[Dirs]
; Writable data lives under ProgramData (Program Files is read-only for clerks)
Name: "{commonappdata}\TIKR"; Permissions: users-modify
Name: "{commonappdata}\TIKR\documents"; Permissions: users-modify
Name: "{commonappdata}\TIKR\.dpkeys"; Permissions: users-modify

[Icons]
; Start Menu folder: "TIKR - Clerk's Vault"
Name: "{group}\Start TIKR"; \
  Filename: "{app}\scripts\Start-TIKR-Hidden.vbs"; \
  WorkingDir: "{app}"; \
  Comment: "Start TIKR — Clerk's Vault (opens browser)"
Name: "{group}\Stop TIKR"; \
  Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\Stop-TIKR-Installed.ps1"""; \
  WorkingDir: "{app}"; \
  Comment: "Stop TIKR API and Web processes"
Name: "{group}\Open TIKR in browser"; \
  Filename: "http://localhost:{#MyWebPort}/"; \
  Comment: "Open the clerk UI (start TIKR first)"
Name: "{group}\README — Getting Started"; \
  Filename: "{app}\README-Installed.txt"; \
  Comment: "How to start, stop, and back up TIKR"
Name: "{group}\Town help (in-app guide)"; \
  Filename: "http://localhost:{#MyWebPort}/"; \
  Comment: "Opens TIKR; use Settings for the guided tour and user guide"
Name: "{group}\Uninstall TIKR"; \
  Filename: "{uninstallexe}"

; Desktop
Name: "{autodesktop}\Start TIKR"; \
  Filename: "{app}\scripts\Start-TIKR-Hidden.vbs"; \
  WorkingDir: "{app}"; \
  Comment: "Start TIKR — Clerk's Vault"; \
  Tasks: desktopicon

; Optional logon start
Name: "{userstartup}\Start TIKR"; \
  Filename: "{app}\scripts\Start-TIKR-Hidden.vbs"; \
  WorkingDir: "{app}"; \
  Tasks: autostart

[Registry]
; Machine-level Syncfusion license (children inherit for both TIKR.Api / TIKR.Web)
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; \
  ValueType: expandsz; ValueName: "SYNCFUSION_LICENSE_KEY"; \
  ValueData: "{code:GetSyncfusionLicenseKey}"; \
  Flags: preservestringtype uninsdeletevalue; \
  Check: HasSyncfusionLicenseKey

; Document default ports for support staff
Root: HKLM; Subkey: "SOFTWARE\{#MyAppNameShort}"; \
  ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\{#MyAppNameShort}"; \
  ValueType: string; ValueName: "ApiPort"; ValueData: "{#MyApiPort}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\{#MyAppNameShort}"; \
  ValueType: string; ValueName: "WebPort"; ValueData: "{#MyWebPort}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\{#MyAppNameShort}"; \
  ValueType: string; ValueName: "DataPath"; ValueData: "{commonappdata}\TIKR"; Flags: uninsdeletekey

[Run]
; Firewall (optional task) — elevated already because PrivilegesRequired=admin
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\scripts\Configure-Firewall.ps1"" -ApiPort {#MyApiPort} -WebPort {#MyWebPort} -Action Add"; \
  StatusMsg: "Configuring Windows Firewall for TIKR..."; \
  Flags: runhidden waituntilterminated; \
  Tasks: firewall

; Offer to start after install
Filename: "{app}\scripts\Start-TIKR-Hidden.vbs"; \
  Description: "Start TIKR now"; \
  Flags: postinstall nowait skipifsilent shellexec

; Uninstall cleanup runs in [Code] CurUninstallStepChanged (usUninstall)
; so processes are stopped and firewall rules removed BEFORE files are deleted.
; Do not use [UninstallRun] for that — those entries run too late.

[Code]
var
  LicensePage: TInputQueryWizardPage;
  BakedLicense: string;

function GetSyncfusionLicenseKey(Param: string): string;
begin
  { Prefer wizard entry; fall back to compile-time bake }
  if (LicensePage <> nil) and (Trim(LicensePage.Values[0]) <> '') then
    Result := Trim(LicensePage.Values[0])
  else
    Result := Trim(BakedLicense);
end;

function HasSyncfusionLicenseKey: Boolean;
begin
  Result := GetSyncfusionLicenseKey('') <> '';
end;

procedure InitializeWizard;
begin
  BakedLicense := '{#MyAppSyncfusionLicense}';

  LicensePage := CreateInputQueryPage(
    wpSelectTasks,
    'Syncfusion license key',
    'Required for the clerk UI (grids, scheduler, assistant).',
    'Paste your Syncfusion Community / commercial license key below.' + #13#10 +
    'It is stored as a machine environment variable (SYNCFUSION_LICENSE_KEY).' + #13#10 +
    'Leave blank only if the key is already set on this PC.'
  );
  LicensePage.Add('SYNCFUSION_LICENSE_KEY:', False);
  LicensePage.Values[0] := BakedLicense;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = LicensePage.ID then
  begin
    if (Trim(LicensePage.Values[0]) = '') and (Trim(BakedLicense) = '') then
    begin
      if MsgBox(
           'No Syncfusion license key was entered.' + #13#10 + #13#10 +
           'TIKR will install, but Syncfusion components may show a license banner until the key is set.' + #13#10 + #13#10 +
           'Continue without a key?',
           mbConfirmation, MB_YESNO) = IDNO then
        Result := False;
    end;
  end;
end;

; Source payload is validated by the Inno compiler (ISCC) when the [Files]
; section is packed. Missing TIKR.Web.exe / TIKR.Api.exe fails the build —
; no need to re-check MySourceDir on the clerk PC at install time.

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    { Ensure ProgramData tree exists even if [Dirs] was skipped }
    ForceDirectories(ExpandConstant('{commonappdata}\TIKR\documents'));
    ForceDirectories(ExpandConstant('{commonappdata}\TIKR\.dpkeys'));
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  { usUninstall = before files are removed }
  if CurUninstallStep = usUninstall then
  begin
    Exec(
      'powershell.exe',
      '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\scripts\Stop-TIKR-Installed.ps1') + '"',
      ExpandConstant('{app}'),
      SW_HIDE, ewWaitUntilTerminated, ResultCode);

    Exec(
      'powershell.exe',
      '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\scripts\Configure-Firewall.ps1') +
        '" -ApiPort {#MyApiPort} -WebPort {#MyWebPort} -Action Remove',
      ExpandConstant('{app}'),
      SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;

  { Keep clerk data by default — accidental data loss is worse than leftover files }
  if CurUninstallStep = usPostUninstall then
  begin
    MsgBox(
      'TIKR was uninstalled.' + #13#10 + #13#10 +
      'Your data was left in:' + #13#10 +
      '  C:\ProgramData\TIKR' + #13#10 + #13#10 +
      'Delete that folder manually only if you intend to remove all town data.',
      mbInformation, MB_OK);
  end;
end;

;
; nDiscUtils - Advanced utilities for disc management
; Copyright (C) 2018  Lukas Berger
;
; This program is free software; you can redistribute it and/or
; modify it under the terms of the GNU General Public License
; as published by the Free Software Foundation; either version 2
; of the License, or (at your option) any later version.
;
; This program is distributed in the hope that it will be useful,
; but WITHOUT ANY WARRANTY; without even the implied warranty of
; MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
; GNU General Public License for more details.
;
; You should have received a copy of the GNU General Public License
; along with this program; if not, write to the Free Software
; Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
;

#define AppName       "nDiscUtils"
#define AppVersion    "0.4.2" 
#define AppPublisher  "Lukas Berger"
#define AppCopyright  "Copyright (c) 2018 Lukas Berger"
#define AppURL        "https://lukasberger.at/"
#define SourceDir     "D:\Documents\Programme\nDiscUtils"
#define BinaryDir     "D:\Documents\Programme\nDiscUtils\bin"

#define DotNetURL      "https://download.microsoft.com/download/C/3/A/C3A5200B-D33C-47E9-9D70-2F7C65DAAD94/NDP46-KB3045557-x86-x64-AllOS-ENU.exe"
#define DotNetPath     "{tmp}\NDP46-KB3045557-x86-x64-AllOS-ENU.exe"
#define DotNetVersion  "v4.6"

#define DokanURL      "https://github.com/dokan-dev/dokany/releases/download/v1.1.0.2000/DokanSetup_redist.exe"
#define DokanPath     "{tmp}\DokanSetup-v1.1.0.2000-redist.exe"
#define DokanVersion  "1.1.0.2000"

[Setup]
; App Info
AppId={{1CBD5002-56A1-4495-B109-370C6499B071}
AppCopyright={#AppCopyright}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
; Setup Version Info
VersionInfoCompany={#AppPublisher}
VersionInfoCopyright={#AppCopyright}
VersionInfoDescription={#AppName}
VersionInfoProductName={#AppName}
VersionInfoVersion={#AppVersion}
DefaultDirName={pf}\{#AppName}
DefaultGroupName={#AppName}
; Setup Output
OutputDir={#BinaryDir}\Installer
OutputBaseFilename=nDiscUtils-Installer
SetupIconFile={#SourceDir}\nDiscUtils.ico
; Setup Requirements
MinVersion=6.1.7600
PrivilegesRequired=poweruser
; Install Wizard
AllowNoIcons=yes
AlwaysShowComponentsList=yes
AlwaysShowDirOnReadyPage=yes
AlwaysShowGroupOnReadyPage=yes
DisableWelcomePage=no
DisableDirPage=no
LicenseFile={#SourceDir}\LICENSE
SetupLogging=yes
ShowComponentSizes=yes
ShowLanguageDialog=yes
ShowTasksTreeLines=yes
; Uninstall Wizard
UninstallRestartComputer=yes
; Architectures
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64
; Compression
Compression=lzma2/normal
SolidCompression=yes

#define UninsHs_ImageFilesDir AddBackslash(SourceDir) + 'Installer'
#define Use_UninsHs_Default_CustomMessages

#include <idp.iss>
#include "Installer/uninshs.iss"

[Types]
Name: "full";    Description: {cm:TypesFull}
Name: "recom";   Description: {cm:TypesRecom}
Name: "min";     Description: {cm:TypesMin}
Name: "custom";  Description: {cm:TypesCustom};      Flags: IsCustom

[Components]
Name: "netfw";        Description: ".NET Framework {#DotNetVersion}";                     Types: full recom min custom;  Flags: fixed
Name: "dokan";        Description: "Dokan User Mode File System Driver {#DokanVersion}";  Types: full recom custom;
Name: "app";          Description: "nDiscUtils";                                          Types: full recom min custom;  Flags: fixed
Name: "app\native";   Description: {cm:ComponentsAppNative};                              Types: full recom min custom;  Flags: fixed
Name: "app\svc";      Description: {cm:ComponentsAppSvc};                                 Types: full recom;
Name: "app\dbg";      Description: {cm:ComponentsDbg};
Name: "app\dbg\sym";  Description: {cm:ComponentsDbgSym};                                 Types: full;                   Flags: exclusive
Name: "app\dbg\bin";  Description: {cm:ComponentsDbgBin};                                                                Flags: exclusive

[Tasks]
Name: registerprivsvc;  Description: {cm:TasksRegisterPrivSvc};  GroupDescription: {cm:ComponentsAppSvc};      Components: app\svc

[Run]
;
; Requirements
;
Filename: {#DotNetPath};  Parameters: "/passive /norestart";  WorkingDir: "{app}";  Flags: runhidden shellexec waituntilterminated;  Check: not IsRequiredDotNetDetected
Filename: {#DokanPath};   Parameters: "/passive /norestart";  WorkingDir: "{app}";  Flags: runhidden shellexec waituntilterminated;  Check: not IsRequiredDokanDetected

;
; Core
;
Filename: {app}\nDiscUtilsPrivSvc.exe;  Parameters: "--install";    WorkingDir: "{app}";  Verb: "runas";  Flags: runhidden shellexec waituntilterminated;  Tasks: registerprivsvc

[UninstallRun]
Filename: {app}\nDiscUtilsPrivSvc.exe;  Parameters: "--uninstall";  WorkingDir: "{app}";  Verb: "runas";  Flags: runhidden shellexec waituntilterminated;  Tasks: registerprivsvc

[Registry]
Root: HKLM; Subkey: "SOFTWARE\Lukas Berger";             Flags: uninsdeletekeyifempty;  Tasks: registerprivsvc
Root: HKLM; Subkey: "SOFTWARE\Lukas Berger\nDiscUtils";  Flags: uninsdeletekey;         Tasks: registerprivsvc
Root: HKLM; Subkey: "SOFTWARE\Lukas Berger\nDiscUtils";                                 Tasks: registerprivsvc;  ValueType: string;  ValueName: "PrivilegeElevationExecutable";  ValueData: "{app}\nDiscUtils.exe"

[Files]
;
; Common
;
Source: "{#SourceDir}\LICENSE"; DestDir: "{app}";  DestName: "LICENSE";

;
; Requirements
;
Source: "{#DotNetPath}";  DestDir: "{tmp}";  Components: netfw;  Check: not IsRequiredDotNetDetected;  Flags: external;  ExternalSize: 65444688
Source: "{#DokanPath}";   DestDir: "{tmp}";  Components: dokan;  Check: not IsRequiredDokanDetected;   Flags: external;  ExternalSize: 35260120

;
; 64-bit Release Core
;
Source: "{#BinaryDir}\x64\Release\nDiscUtils.exe";         DestDir: "{app}";  DestName: "nDiscUtils.exe";         Check: Is64BitInstallMode;      Components: not app\dbg\bin and app;  Flags: solidbreak
Source: "{#BinaryDir}\x64\Release\nDiscUtils.pdb";         DestDir: "{app}";  DestName: "nDiscUtils.pdb";         Check: Is64BitInstallMode;      Components: not app\dbg\bin and app and app\dbg\sym
Source: "{#BinaryDir}\x64\Release\nDiscUtils.Native.dll";  DestDir: "{app}";  DestName: "nDiscUtils.Native.dll";  Check: Is64BitInstallMode;      Components: not app\dbg\bin and app and app\native
Source: "{#BinaryDir}\x64\Release\nDiscUtils.Native.pdb";  DestDir: "{app}";  DestName: "nDiscUtils.Native.pdb";  Check: Is64BitInstallMode;      Components: not app\dbg\bin and app and app\native and app\dbg\sym
Source: "{#BinaryDir}\x64\Release\nDiscUtilsPrivSvc.exe";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.exe";  Check: Is64BitInstallMode;      Components: not app\dbg\bin and app and app\svc
Source: "{#BinaryDir}\x64\Release\nDiscUtilsPrivSvc.pdb";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.pdb";  Check: Is64BitInstallMode;      Components: not app\dbg\bin and app and app\svc and app\dbg\sym

;
; 32-bit Release Core
;
Source: "{#BinaryDir}\x86\Release\nDiscUtils.exe";         DestDir: "{app}";  DestName: "nDiscUtils.exe";         Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app;  Flags: solidbreak
Source: "{#BinaryDir}\x86\Release\nDiscUtils.pdb";         DestDir: "{app}";  DestName: "nDiscUtils.pdb";         Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app and app\dbg\sym
Source: "{#BinaryDir}\x86\Release\nDiscUtils.Native.dll";  DestDir: "{app}";  DestName: "nDiscUtils.Native.dll";  Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app and app\native
Source: "{#BinaryDir}\x86\Release\nDiscUtils.Native.pdb";  DestDir: "{app}";  DestName: "nDiscUtils.Native.pdb";  Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app and app\native and app\dbg\sym
Source: "{#BinaryDir}\x86\Release\nDiscUtilsPrivSvc.exe";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.exe";  Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app and app\svc
Source: "{#BinaryDir}\x86\Release\nDiscUtilsPrivSvc.pdb";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.pdb";  Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app and app\svc and app\dbg\sym

;
; 64-bit Debug Core
;
Source: "{#BinaryDir}\x64\Debug\nDiscUtils.exe";         DestDir: "{app}";  DestName: "nDiscUtils.exe";         Check: Is64BitInstallMode;      Components: app\dbg\bin and app;  Flags: solidbreak
Source: "{#BinaryDir}\x64\Debug\nDiscUtils.pdb";         DestDir: "{app}";  DestName: "nDiscUtils.pdb";         Check: Is64BitInstallMode;      Components: app\dbg\bin and app
Source: "{#BinaryDir}\x64\Debug\nDiscUtils.Native.dll";  DestDir: "{app}";  DestName: "nDiscUtils.Native.dll";  Check: Is64BitInstallMode;      Components: app\dbg\bin and app and app\native
Source: "{#BinaryDir}\x64\Debug\nDiscUtils.Native.pdb";  DestDir: "{app}";  DestName: "nDiscUtils.Native.pdb";  Check: Is64BitInstallMode;      Components: app\dbg\bin and app and app\native
Source: "{#BinaryDir}\x64\Debug\nDiscUtilsPrivSvc.exe";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.exe";  Check: Is64BitInstallMode;      Components: app\dbg\bin and app and app\svc
Source: "{#BinaryDir}\x64\Debug\nDiscUtilsPrivSvc.pdb";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.pdb";  Check: Is64BitInstallMode;      Components: app\dbg\bin and app and app\svc

;
; 32-bit Debug Core
;
Source: "{#BinaryDir}\x86\Debug\nDiscUtils.exe";         DestDir: "{app}";  DestName: "nDiscUtils.exe";         Check: not Is64BitInstallMode;      Components: app\dbg\bin and app;  Flags: solidbreak
Source: "{#BinaryDir}\x86\Debug\nDiscUtils.pdb";         DestDir: "{app}";  DestName: "nDiscUtils.pdb";         Check: not Is64BitInstallMode;      Components: app\dbg\bin and app
Source: "{#BinaryDir}\x86\Debug\nDiscUtils.Native.dll";  DestDir: "{app}";  DestName: "nDiscUtils.Native.dll";  Check: not Is64BitInstallMode;      Components: app\dbg\bin and app and app\native
Source: "{#BinaryDir}\x86\Debug\nDiscUtils.Native.pdb";  DestDir: "{app}";  DestName: "nDiscUtils.Native.pdb";  Check: not Is64BitInstallMode;      Components: app\dbg\bin and app and app\native
Source: "{#BinaryDir}\x86\Debug\nDiscUtilsPrivSvc.exe";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.exe";  Check: not Is64BitInstallMode;      Components: app\dbg\bin and app and app\svc
Source: "{#BinaryDir}\x86\Debug\nDiscUtilsPrivSvc.pdb";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.pdb";  Check: not Is64BitInstallMode;      Components: app\dbg\bin and app and app\svc

[Icons]
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"

[Languages]
Name: en;  MessagesFile: "compiler:Default.isl"
Name: de;  MessagesFile: "compiler:Languages\German.isl"

[Messages]
en.BeveledLabel=English
de.BeveledLabel=Deutsch

[CustomMessages]
en.ComponentsAppNative  =Native components
en.ComponentsAppSvc     =Privilege Elevation Service   
en.ComponentsDbg        =Debugging
en.ComponentsDbgSym     =Symbols
en.ComponentsDbgBin     =Binariese
en.TypesFull            =Full installation
en.TypesRecom           =Recommended installation
en.TypesMin             =Minimal installation
en.TypesCustom          =Custom installation
en.TasksRegisterPrivSvc =Register service after installation
en.ComponentsDownload   =Additional components (Downloaded dynamically)
de.ComponentsAppNative  =Native Komponenten 
de.ComponentsAppSvc     =Dienst zur Erhöhung der Rechte   
de.ComponentsDbg        =Debugging
de.ComponentsDbgSym     =Symbole
de.ComponentsDbgBin     =Dateien
de.TypesFull            =Vollständige Installation
de.TypesRecom           =Empfohlene Installation
de.TypesMin             =Minimale Installation
de.TypesCustom          =Benutzerdefinierte Installation
de.TasksRegisterPrivSvc =Dienst nach Installation registrieren
de.ComponentsDownload   =Zusätzliche Komponenten (dynamisch Heruntergeladen)

[Code]
//////
// https://stackoverflow.com/a/2099805
/////////////////////////////////////////////////////////////////////

function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

/////////////////////////////////////////////////////////////////////

function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;

/////////////////////////////////////////////////////////////////////

function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
  // Return Values:
  // 1 - uninstall string is empty
  // 2 - error executing the UnInstallString
  // 3 - successfully executed the UnInstallString

  // default return value
  Result := 0;

  // get the uninstall string of the old app
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

/////////////////////////////////////////////////////////////////////

// https://stackoverflow.com/a/4104226
function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1'          .NET Framework 1.1
//    'v2.0'          .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.5'          .NET Framework 4.5
//    'v4.5.1'        .NET Framework 4.5.1
//    'v4.5.2'        .NET Framework 4.5.2
//    'v4.6'          .NET Framework 4.6
//    'v4.6.1'        .NET Framework 4.6.1
//    'v4.6.2'        .NET Framework 4.6.2
//    'v4.7'          .NET Framework 4.7
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
  key, versionKey: string;
  install, release, serviceCount, versionRelease: cardinal;
  success: boolean;
begin
  versionKey := version;
  versionRelease := 0;

  // .NET 1.1 and 2.0 embed release number in version key
  if version = 'v1.1' then begin
    versionKey := 'v1.1.4322';
  end else if version = 'v2.0' then begin
    versionKey := 'v2.0.50727';
  end

  // .NET 4.5 and newer install as update to .NET 4.0 Full
  else if Pos('v4.', version) = 1 then begin
    versionKey := 'v4\Full';
    case version of
      'v4.5':   versionRelease := 378389;
      'v4.5.1': versionRelease := 378675; // 378758 on Windows 8 and older
      'v4.5.2': versionRelease := 379893;
      'v4.6':   versionRelease := 393295; // 393297 on Windows 8.1 and older
      'v4.6.1': versionRelease := 394254; // 394271 before Win10 November Update
      'v4.6.2': versionRelease := 394802; // 394806 before Win10 Anniversary Update
      'v4.7':   versionRelease := 460798; // 460805 before Win10 Creators Update
    end;
  end;

  // installation key group for all .NET versions
  key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + versionKey;

  // .NET 3.0 uses value InstallSuccess in subkey Setup
  if Pos('v3.0', version) = 1 then begin
    success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
  end else begin
    success := RegQueryDWordValue(HKLM, key, 'Install', install);
  end;

  // .NET 4.0 and newer use value Servicing instead of SP
  if Pos('v4', version) = 1 then begin
    success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
  end else begin
    success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
  end;

  // .NET 4.5 and newer use additional value Release
  if versionRelease > 0 then begin
    success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
    success := success and (release >= versionRelease);
  end;

  result := success and (install = 1) and (serviceCount >= service);
end;

/////////////////////////////////////////////////////////////////////

function IsRequiredDotNetDetected(): Boolean;
begin
  result := False; // IsDotNetDetected('{#DotNetVersion}', 0);
end;

/////////////////////////////////////////////////////////////////////

function DownloadDotNet(): Boolean;
begin
  result := (not IsRequiredDotNetDetected)
  if result then begin
    idpAddFile('{#DotNetURL}', ExpandConstant('{#DotNetPath}'));
  end;
end;

/////////////////////////////////////////////////////////////////////

function IsDokanDetected(reqversion: string): Boolean;
var
  registry64Key, registry32Key: string;
  version: string;
  hasRead: Boolean;
begin
  registry32Key := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{65A3A986-3DC3-0101-0000-180119092517}';
  registry64Key := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{65A3A964-3DC3-0101-0000-180119092517}';

  version := '';

  if (RegKeyExists(HKLM, registry32Key)) then begin
    hasRead := RegQueryStringValue(HKLM, registry32Key, 'FullVersion', version);
  end;

  if (RegKeyExists(HKLM, registry64Key)) then begin
    hasRead := RegQueryStringValue(HKLM, registry64Key, 'FullVersion', version);
  end;

  result := hasRead and (Pos(version, reqversion) = 1);
end;

/////////////////////////////////////////////////////////////////////

function IsRequiredDokanDetected(): Boolean;
begin
  result := False; // IsDokanDetected('{#DokanVersion}');
end;

/////////////////////////////////////////////////////////////////////
            
function DownloadDokan(): Boolean;
begin
  result := (not IsRequiredDokanDetected)
  if result then begin
    idpAddFile('{#DokanURL}', ExpandConstant('{#DokanPath}'));
  end;
end;

/////////////////////////////////////////////////////////////////////

procedure InitializeWizard;
begin
  // Wizard
  WizardForm.DiskSpaceLabel.Visible := False;
  WizardForm.ComponentsDiskSpaceLabel.Visible := False;

  // Downloads
  idpDownloadAfter(wpReady);

  // Maintenance
  UninsHs_InitializeWizard();
end;

/////////////////////////////////////////////////////////////////////

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) and (IsUpgrade()) then begin
    UnInstallOldVersion();
  end;
end;

/////////////////////////////////////////////////////////////////////

procedure CurPageChanged(CurPageID: Integer);
var
  dlDotNet, dlDokan: Boolean;
begin
  if CurPageID = wpReady then begin
    // User can navigate to 'Ready to install' page several times, so we
    // need to clear file list to ensure that only needed files are added.
    idpClearFiles;
                 
    dlDotNet := DownloadDotNet;
    dlDokan  := DownloadDokan;

    if (dlDotNet or dlDokan) then begin
      Wizardform.ReadyMemo.Lines.Add('');
      Wizardform.ReadyMemo.Lines.Add(CustomMessage('ComponentsDownload'));

      if (dlDotNet) then begin 
        Wizardform.ReadyMemo.Lines.Add('      .NET Framework {#DotNetVersion}');
      end;
      if (dlDokan) then begin
        Wizardform.ReadyMemo.Lines.Add('      Dokan User Mode File System Driver {#DokanVersion}');
      end;
    end;
  end;
  UninsHs_CurPageChanged(CurPageID);
end;

function ShouldSkipPage(CurPageId: Integer): Boolean;
begin
  Result := False;
  UninsHs_ShouldSkipPage(CurPageId, Result);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  UninsHs_NextButtonClick(CurPageID, Result);
end;

procedure CancelButtonClick(CurPageID: Integer; var Cancel, Confirm: Boolean);
begin
  UninsHs_CancelButtonClick(CurPageID, Cancel, Confirm);
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  UninsHs_InitializeUninstall(Result);
end;

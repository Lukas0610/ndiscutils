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
#include <idp.iss>

#include "Config.iss"

#define AppName       "nDiscUtils"
#define AppVersion    "0.4.2" 
#define AppPublisher  "Lukas Berger"
#define AppCopyright  "Copyright (c) 2018 Lukas Berger"
#define AppURL        "https://lukasberger.at/"

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
UninstallRestartComputer=no

; Architectures
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64

; Compression
Compression=lzma2/normal
SolidCompression=yes

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
Filename: {#DokanPath};   Parameters: "/quiet /norestart";    WorkingDir: "{app}";  Flags: runhidden shellexec waituntilterminated;  Check: not IsRequiredDokanDetected

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

#include "Messages/en.iss"
#include "Messages/de.iss"

[Code]

#include "Code/AutoUninstall.pas"
#include "Code/DetectDokan.pas"
#include "Code/DetectDotNet.pas"

procedure InitializeWizard;
begin
  // Wizard
  WizardForm.DiskSpaceLabel.Visible := False;
  WizardForm.ComponentsDiskSpaceLabel.Visible := False;

  // Downloads
  idpDownloadAfter(wpReady);
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
end;

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
#define AppName "nDiscUtils"
#define AppVersion "0.4.1"
#define AppPublisher "Lukas Berger" 
#define AppURL "https://lukasberger.at/"
#define SourceDir "D:\Projekte\Windows\nDiscUtils\bin"

[Setup]
AppId={{1CBD5002-56A1-4495-B109-370C6499B071}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={pf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
LicenseFile=D:\Projekte\Windows\nDiscUtils\LICENSE
OutputDir=D:\Projekte\Windows\nDiscUtils\bin\Installer
OutputBaseFilename=nDiscUtils-Installer
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
AlwaysShowComponentsList=yes
SetupLogging=yes

[Types]
Name: "full";    Description: "Full installation"
Name: "recom";   Description: "Recommended installation"
Name: "min";     Description: "Minimal installation"
Name: "custom";  Description: "Custom installation";      Flags: IsCustom

[Components]
Name: "app";          Description: "nDiscUtils";                   Types: full recom min custom;  Flags: fixed
Name: "app\native";   Description: "Native Components";            Types: full recom min custom;  Flags: fixed
Name: "app\svc";      Description: "Privilege Elevation Service";  Types: full recom;    
Name: "app\dbg";      Description: "Debugging";
Name: "app\dbg\sym";  Description: "Symbols";                                                     Flags: exclusive
Name: "app\dbg\bin";  Description: "Binaries";                                                    Flags: exclusive
 
[Tasks]
Name: registerprivsvc;  Description: "Register service after installation";  GroupDescription: "Privilege Elevation Service";  Components: app\svc  
 
[Run]
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
Source: "D:\Projekte\Windows\nDiscUtils\LICENSE";  DestDir: "{app}";  DestName: "LICENSE"; 
   
;
; 64-bit Release Core
;
Source: "{#SourceDir}\x64\Release\nDiscUtils.exe";         DestDir: "{app}";  DestName: "nDiscUtils.exe";         Check: Is64BitInstallMode;      Components: not app\dbg\bin and app;  Flags: solidbreak   
Source: "{#SourceDir}\x64\Release\nDiscUtils.pdb";         DestDir: "{app}";  DestName: "nDiscUtils.pdb";         Check: Is64BitInstallMode;      Components: not app\dbg\bin and app and app\dbg\sym
Source: "{#SourceDir}\x64\Release\nDiscUtils.Native.dll";  DestDir: "{app}";  DestName: "nDiscUtils.Native.dll";  Check: Is64BitInstallMode;      Components: not app\dbg\bin and app and app\native 
Source: "{#SourceDir}\x64\Release\nDiscUtils.Native.pdb";  DestDir: "{app}";  DestName: "nDiscUtils.Native.pdb";  Check: Is64BitInstallMode;      Components: not app\dbg\bin and app and app\native and app\dbg\sym  
Source: "{#SourceDir}\x64\Release\nDiscUtilsPrivSvc.exe";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.exe";  Check: Is64BitInstallMode;      Components: not app\dbg\bin and app and app\svc
Source: "{#SourceDir}\x64\Release\nDiscUtilsPrivSvc.pdb";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.pdb";  Check: Is64BitInstallMode;      Components: not app\dbg\bin and app and app\svc and app\dbg\sym  

;
; 32-bit Release Core
; 
Source: "{#SourceDir}\x86\Release\nDiscUtils.exe";         DestDir: "{app}";  DestName: "nDiscUtils.exe";         Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app;  Flags: solidbreak   
Source: "{#SourceDir}\x86\Release\nDiscUtils.pdb";         DestDir: "{app}";  DestName: "nDiscUtils.pdb";         Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app and app\dbg\sym
Source: "{#SourceDir}\x86\Release\nDiscUtils.Native.dll";  DestDir: "{app}";  DestName: "nDiscUtils.Native.dll";  Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app and app\native 
Source: "{#SourceDir}\x86\Release\nDiscUtils.Native.pdb";  DestDir: "{app}";  DestName: "nDiscUtils.Native.pdb";  Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app and app\native and app\dbg\sym   
Source: "{#SourceDir}\x86\Release\nDiscUtilsPrivSvc.exe";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.exe";  Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app and app\svc
Source: "{#SourceDir}\x86\Release\nDiscUtilsPrivSvc.pdb";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.pdb";  Check: not Is64BitInstallMode;      Components: not app\dbg\bin and app and app\svc and app\dbg\sym
   
;
; 64-bit Debug Core
;
Source: "{#SourceDir}\x64\Debug\nDiscUtils.exe";         DestDir: "{app}";  DestName: "nDiscUtils.exe";         Check: Is64BitInstallMode;      Components: app\dbg\bin and app;  Flags: solidbreak   
Source: "{#SourceDir}\x64\Debug\nDiscUtils.pdb";         DestDir: "{app}";  DestName: "nDiscUtils.pdb";         Check: Is64BitInstallMode;      Components: app\dbg\bin and app
Source: "{#SourceDir}\x64\Debug\nDiscUtils.Native.dll";  DestDir: "{app}";  DestName: "nDiscUtils.Native.dll";  Check: Is64BitInstallMode;      Components: app\dbg\bin and app and app\native 
Source: "{#SourceDir}\x64\Debug\nDiscUtils.Native.pdb";  DestDir: "{app}";  DestName: "nDiscUtils.Native.pdb";  Check: Is64BitInstallMode;      Components: app\dbg\bin and app and app\native 
Source: "{#SourceDir}\x64\Debug\nDiscUtilsPrivSvc.exe";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.exe";  Check: Is64BitInstallMode;      Components: app\dbg\bin and app and app\svc
Source: "{#SourceDir}\x64\Debug\nDiscUtilsPrivSvc.pdb";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.pdb";  Check: Is64BitInstallMode;      Components: app\dbg\bin and app and app\svc 

;
; 32-bit Debug Core
; 
Source: "{#SourceDir}\x86\Debug\nDiscUtils.exe";         DestDir: "{app}";  DestName: "nDiscUtils.exe";         Check: not Is64BitInstallMode;      Components: app\dbg\bin and app;  Flags: solidbreak   
Source: "{#SourceDir}\x86\Debug\nDiscUtils.pdb";         DestDir: "{app}";  DestName: "nDiscUtils.pdb";         Check: not Is64BitInstallMode;      Components: app\dbg\bin and app
Source: "{#SourceDir}\x86\Debug\nDiscUtils.Native.dll";  DestDir: "{app}";  DestName: "nDiscUtils.Native.dll";  Check: not Is64BitInstallMode;      Components: app\dbg\bin and app and app\native 
Source: "{#SourceDir}\x86\Debug\nDiscUtils.Native.pdb";  DestDir: "{app}";  DestName: "nDiscUtils.Native.pdb";  Check: not Is64BitInstallMode;      Components: app\dbg\bin and app and app\native
Source: "{#SourceDir}\x86\Debug\nDiscUtilsPrivSvc.exe";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.exe";  Check: not Is64BitInstallMode;      Components: app\dbg\bin and app and app\svc
Source: "{#SourceDir}\x86\Debug\nDiscUtilsPrivSvc.pdb";  DestDir: "{app}";  DestName: "nDiscUtilsPrivSvc.pdb";  Check: not Is64BitInstallMode;      Components: app\dbg\bin and app and app\svc
                          
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Icons]
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"

[Code]


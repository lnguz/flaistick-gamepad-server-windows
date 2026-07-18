#define AppName "FlaiStick Gamepad Server"
#define AppVersion "1.0.0"
#define AppExeName "GamepadServer.exe"
#define FirewallRuleName "GamepadServer UDP 9000"
#define DiscoveryFirewallRuleName "GamepadServer UDP 47998 Discovery"

[Setup]
AppId={{B3B6B6C1-6B7E-4B62-9C7B-3B3F1B6C3E9A}
AppName={#AppName}
AppVersion={#AppVersion}
DefaultDirName={autopf}\FlaiStick Gamepad Server
DefaultGroupName=FlaiStick Gamepad Server
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2
SolidCompression=yes
OutputDir=dist
OutputBaseFilename=FlaiStickGamepadServerSetup
UninstallDisplayIcon={app}\{#AppExeName}
SetupIconFile=assets\flaistick.ico

[Files]
Source: "publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"

[Run]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""try {{ winget install --id ViGEm.ViGEmBus -e --silent --accept-package-agreements --accept-source-agreements | Out-Null }} catch {{}}"""; \
    Flags: runhidden; StatusMsg: "Installing ViGEmBus driver (if needed)..."; Check: NeedsViGEmBus
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""New-NetFirewallRule -DisplayName '{#FirewallRuleName}' -Direction Inbound -Protocol UDP -LocalPort 9000 -Action Allow -Profile Any -ErrorAction SilentlyContinue | Out-Null"""; \
    Flags: runhidden; StatusMsg: "Configuring firewall..."
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""New-NetFirewallRule -DisplayName '{#DiscoveryFirewallRuleName}' -Direction Inbound -Protocol UDP -LocalPort 47998 -Action Allow -Profile Any -ErrorAction SilentlyContinue | Out-Null"""; \
    Flags: runhidden; StatusMsg: "Configuring firewall..."
Filename: "{sys}\schtasks.exe"; \
    Parameters: "/Create /TN ""GamepadServer"" /SC ONLOGON /TR ""\""{app}\{#AppExeName}\"""" /F"; \
    Flags: runhidden; StatusMsg: "Registering startup task..."
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: postinstall nowait skipifsilent runasoriginaluser

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Remove-NetFirewallRule -DisplayName '{#FirewallRuleName}' -ErrorAction SilentlyContinue"""; \
    Flags: runhidden; RunOnceId: "RemoveFirewallRule"
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; \
    Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Remove-NetFirewallRule -DisplayName '{#DiscoveryFirewallRuleName}' -ErrorAction SilentlyContinue"""; \
    Flags: runhidden; RunOnceId: "RemoveDiscoveryFirewallRule"
Filename: "{sys}\reg.exe"; \
    Parameters: "delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v GamepadServer /f"; \
    Flags: runhidden; RunOnceId: "RemoveRunKey"
Filename: "{sys}\schtasks.exe"; \
    Parameters: "/Delete /TN ""GamepadServer"" /F"; \
    Flags: runhidden; RunOnceId: "RemoveScheduledTask"

[Code]
function NeedsViGEmBus(): Boolean;
begin
  Result := not RegKeyExists(HKLM, 'SYSTEM\CurrentControlSet\Services\ViGEmBus');
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and NeedsViGEmBus() then
  begin
    MsgBox('ViGEmBus could not be installed automatically. It is required for virtual controllers to work.' + #13#10#13#10 +
      'Please install it manually:' + #13#10 +
      'winget install ViGEm.ViGEmBus' + #13#10 +
      'or https://github.com/ViGEm/ViGEmBus/releases' + #13#10#13#10 +
      'Restart this PC after installing it, then launch {#AppName} again.',
      mbInformation, MB_OK);
  end;
end;

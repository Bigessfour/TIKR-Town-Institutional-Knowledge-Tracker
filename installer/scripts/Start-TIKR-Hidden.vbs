' Silent launcher: no console flash for the town clerk.
' Double-clicked from Desktop / Start Menu "Start TIKR".
Option Explicit

Dim shell, fso, scriptDir, ps1, cmd
Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

scriptDir = fso.GetParentFolderName(WScript.ScriptFullName)
ps1 = scriptDir & "\Start-TIKR-Installed.ps1"

If Not fso.FileExists(ps1) Then
  MsgBox "Could not find Start-TIKR-Installed.ps1 next to this launcher." & vbCrLf & _
         "Please reinstall TIKR — Clerk's Vault.", vbCritical, "TIKR"
  WScript.Quit 1
End If

' 0 = hidden window; False = do not wait
cmd = "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & ps1 & """ -WindowStyle Hidden"
shell.Run cmd, 0, False

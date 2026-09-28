Set shell = CreateObject("WScript.Shell")
Set files = CreateObject("Scripting.FileSystemObject")
appFolder = files.GetParentFolderName(WScript.ScriptFullName)
appPath = files.BuildPath(appFolder, "publish\win-x64\PersonalLogManager.exe")
If Not files.FileExists(appPath) Then
  MsgBox "The app has not been published yet. Run the publish command from README.md first.", vbInformation, "Personal Log Manager"
  WScript.Quit 1
End If
shell.CurrentDirectory = files.GetParentFolderName(appPath)
shell.Run """" & appPath & """", 1, False

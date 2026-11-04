Option Explicit

Dim fso, shell, scriptPath, toolsDir, repoRoot, invokeScript, logDir, logPath
Dim args, item, command
Set fso = CreateObject("Scripting.FileSystemObject")
Set shell = CreateObject("WScript.Shell")

scriptPath = fso.GetParentFolderName(WScript.ScriptFullName)
toolsDir = fso.GetParentFolderName(scriptPath)
repoRoot = fso.GetParentFolderName(toolsDir)
invokeScript = fso.BuildPath(scriptPath, "Invoke-CodexQueue.ps1")
logDir = shell.ExpandEnvironmentStrings("%LOCALAPPDATA%") & "\RiesgoLavado\CodexQueue"
If Not fso.FolderExists(logDir) Then fso.CreateFolder(logDir)
logPath = fso.BuildPath(logDir, "watcher-output.log")

args = ""
For Each item In WScript.Arguments
    args = args & " " & QuoteArgument(CStr(item))
Next

command = "powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File " & _
          QuoteArgument(invokeScript) & " -RepoRoot " & QuoteArgument(repoRoot) & args & _
          " >> " & QuoteArgument(logPath) & " 2>&1"
shell.Run "cmd.exe /d /s /c " & Chr(34) & command & Chr(34), 0, True

Function QuoteArgument(value)
    QuoteArgument = Chr(34) & Replace(value, Chr(34), Chr(34) & Chr(34)) & Chr(34)
End Function

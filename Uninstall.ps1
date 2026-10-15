$ErrorActionPreference='Stop'
$folder=Join-Path $env:LOCALAPPDATA 'ChromebookKeyboardHelper'
$target=Join-Path $folder 'ChromebookKeys.exe'
$processes=@(Get-Process ChromebookKeys -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $target})
foreach($process in $processes){Stop-Process -Id $process.Id -Force;$process.WaitForExit()}
Remove-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name ChromebookKeyboardHelper -ErrorAction SilentlyContinue
if(Test-Path $folder){Remove-Item $folder -Recurse -Force}
Write-Host 'Removed the helper and its startup entry. Windows language settings were not changed.'

$ErrorActionPreference='Stop'
$source=Join-Path $PSScriptRoot 'ChromebookKeys.exe'
if(!(Test-Path $source)){$source=Join-Path $PSScriptRoot 'dist\ChromebookKeys.exe'}
if(!(Test-Path $source)){throw 'Download ChromebookKeys.exe beside this installer, or run Build.ps1'}
$folder=Join-Path $env:LOCALAPPDATA 'ChromebookKeyboardHelper'
$target=Join-Path $folder 'ChromebookKeys.exe'
New-Item -ItemType Directory -Force $folder | Out-Null
$processes=@(Get-Process ChromebookKeys -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $target})
foreach($process in $processes){Stop-Process -Id $process.Id -Force;$process.WaitForExit()}
if([IO.Path]::GetFullPath($source) -ne [IO.Path]::GetFullPath($target)){Copy-Item $source $target -Force}
$run='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
New-Item $run -Force | Out-Null
New-ItemProperty $run -Name ChromebookKeyboardHelper -Value ('"'+$target+'"') -PropertyType String -Force | Out-Null
Start-Process $target
Write-Host 'Installed for your account. The helper starts at sign-in.'

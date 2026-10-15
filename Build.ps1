$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(!(Test-Path $compiler)){throw 'Windows .NET Framework C# compiler not found'}
$output=Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $output | Out-Null
& $compiler /nologo /target:winexe /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Management.dll `
    ('/out:'+(Join-Path $output 'ChromebookKeys.exe')) (Join-Path $PSScriptRoot 'src\ChromebookKeys.cs')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}

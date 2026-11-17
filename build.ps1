$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs = @('PresentationCore.dll','PresentationFramework.dll','WindowsBase.dll') | ForEach-Object { '/reference:' + (Join-Path $framework ('WPF\' + $_)) }
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 /optimize+ /codepage:65001 /reference:System.Xaml.dll /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll @refs ('/win32icon:' + (Join-Path $PSScriptRoot 'Spider-Codex.ico')) ('/out:' + (Join-Path $PSScriptRoot 'Spider-Shortcut.exe')) (Join-Path $PSScriptRoot 'ShortcutLauncher.cs')
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed' }

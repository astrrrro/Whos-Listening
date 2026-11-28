@echo off
cd /d "%~dp0"
if not exist out mkdir out
set FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set WPF=%FW%\WPF
"%FW%\csc.exe" /nologo /codepage:65001 /target:winexe /win32icon:assets\WhosListening.ico /resource:assets\WhosListening.ico,WhosListening.ico /out:out\WhosListening.exe /recurse:src\*.cs /r:"%WPF%\PresentationFramework.dll" /r:"%WPF%\PresentationCore.dll" /r:"%WPF%\WindowsBase.dll" /r:System.Xaml.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll > out\build.log 2>&1
if errorlevel 1 (
  echo BUILD: ERROR>> out\build.log
  exit /b 1
)
echo BUILD: OK>> out\build.log
exit /b 0

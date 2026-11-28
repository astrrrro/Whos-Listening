@echo off
cd /d "%~dp0"
if not exist out mkdir out
set FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319
set WPF=%FW%\WPF
if exist out\tests.log del out\tests.log
if exist out\WhosListening.Tests.exe del out\WhosListening.Tests.exe
if exist out\test-sources.rsp del out\test-sources.rsp
for /r src %%f in (*.cs) do (
  if /i not "%%f"=="%~dp0src\App\Program.cs" echo "%%f">> out\test-sources.rsp
)
for %%f in (tests\*.cs) do (
  echo "%~dp0%%f">> out\test-sources.rsp
)
"%FW%\csc.exe" /nologo /codepage:65001 /target:exe /resource:assets\WhosListening.ico,WhosListening.ico /out:out\WhosListening.Tests.exe @out\test-sources.rsp /r:"%WPF%\PresentationFramework.dll" /r:"%WPF%\PresentationCore.dll" /r:"%WPF%\WindowsBase.dll" /r:System.Xaml.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:System.Security.dll > out\build.log 2>&1
if errorlevel 1 (
  echo BUILD: ERROR>> out\build.log
  echo RESULTADO: BUILD ERROR> out\tests.log
  exit /b 1
)
echo BUILD: OK>> out\build.log
pushd out
WhosListening.Tests.exe
set RC=%ERRORLEVEL%
popd
exit /b %RC%

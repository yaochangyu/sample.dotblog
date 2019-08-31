@echo off
set batchFolder=%~dp0
set serviceName=ConsoleApp1.exe
set servicePatch=%batchFolder%%serviceName%

echo BatchFolder:%batchFolder%
echo Service:%servicePatch%
echo Uninstall %serviceName%...
echo ---------------------------------------------------
%servicePatch% uninstall
echo ---------------------------------------------------
echo Done.
::pause
@echo off
set batchFolder=%~dp0
set serviceName=ConsoleAppNetFx48
set serviceDisplayName=ConsoleAppNetFx48
set serviceDescription="ด๚ธี"
set serviceLaunchPath=%batchFolder%bin\ConsoleAppNetFx48.exe
set serviceLogonId=.\setup
set serviceLogonPassword=pass@w0rd1~
::set serverName=\\Computer Name
set serverName=
Call SafeStopService %serviceName% %serverName%
Call SafeDeleteService %serviceName% %serverName%
Call SafeCreateService %serviceName% %serviceDisplayName% %serviceDescription% %serviceLaunchPath% %serviceLogonId% %serviceLogonPassword% %serverName%
Call SafeStartService %serviceName% %serverName%


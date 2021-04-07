@echo off
set batchFolder=%~dp0
set serviceName=ConsoleAppNetFx48
set serviceDisplayName=ConsoleAppNetFx48
set serviceDescription="ด๚ธี"
set serviceLaunchPath=%batchFolder%ConsoleAppNetFx48.exe
set serviceLogonId=.\setup
set serviceLogonPassword=password
::set serverName=\\YAO-S658RF
set serverName=""
Call SafeStopService %serviceName% %serverName%
Call SafeDeleteService %serviceName% %serverName%
Call SafeCreateService %serviceName% %serviceDisplayName% %serviceDescription% %serviceLaunchPath% %serviceLogonId% %serviceLogonPassword% %serverName%
::Call SafeStartService %serviceName% %serverName%


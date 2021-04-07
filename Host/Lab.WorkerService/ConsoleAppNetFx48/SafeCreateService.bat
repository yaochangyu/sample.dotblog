@echo off

IF [%1]==[] GOTO usage
IF [%2]==[] GOTO usage
IF [%3]==[] GOTO usage
IF [%4]==[] GOTO usage
IF [%5]==[] GOTO usage

IF NOT "%1"=="" SET serviceName=%1
IF NOT "%2"=="" SET serviceDisplayName=%2
IF NOT "%3"=="" SET serviceDescription=%3
IF NOT "%4"=="" SET serviceLaunchPath=%4
IF NOT "%5"=="" SET serviceLogonId=%5
IF NOT "%6"=="" SET serviceLogonPassword=%6
IF NOT "%7"=="" SET serverName=%7

SC %serverName% query %serviceName%

IF errorlevel 1060 GOTO ServiceNotFound
IF errorlevel 1722 GOTO SystemOffline
IF errorlevel 1001 GOTO DeletingServiceDelay

:ResolveInitialState
SC %serverName% query %serviceName% | FIND "STATE" | FIND "RUNNING"

IF errorlevel 0 IF NOT errorlevel 1 GOTO StopService

SC %serverName% query %serviceName% | FIND "STATE" | FIND "STOPPED"

IF errorlevel 0 IF NOT errorlevel 1 GOTO StoppedService

SC %serverName% query %serviceName% | FIND "STATE" | FIND "PAUSED"

IF errorlevel 0 IF NOT errorlevel 1 GOTO SystemOffline

echo Service State is changing, waiting for service to resolve its state before making changes

sc %serverName% query %serviceName% | Find "STATE"
ping -n 2 127.0.0.1 > NUL
GOTO ResolveInitialState

:StopService
echo Stopping %serviceName% on %serverName%
sc %serverName% stop %serviceName%
GOTO StoppingService

:StoppingServiceDelay
echo Waiting for %serviceName% to stop
ping -n 2 127.0.0.1 > NUL

:StoppingService
SC %serverName% query %serviceName% | FIND "STATE" | FIND "STOPPED"
IF errorlevel 1 GOTO StoppingServiceDelay

:StoppedService
echo %serviceName% on %serverName% is stopped
GOTO DeleteService

:DeleteService
echo Deleting %serviceName% on %serverName%
SC %serverName% delete %serviceName%

:DeletingServiceDelay
echo Waiting for %serviceName% to get deleted
ping -n 2 127.0.0.1 > NUL

:DeletingService
SC %serverName% query %serviceName%
IF NOT errorlevel 1060 GOTO DeletingServiceDelay

:DeletedService
echo %serviceName% on %serverName% is deleted
GOTO CreateService

:SystemOffline
echo Server %serverName% is not accessible or is offline
GOTO End

:ServiceNotFound
echo Service %serviceName% is not installed on Server %serverName%
GOTO CreateService

:CreateService
echo Creating %serviceName% on %serverName%
::SC %serverName% create %serviceName% binpath= "%serviceLaunchPath%" displayname= "THS MSMQ %serviceDisplayName% Agent"
SC %serverName% create %serviceName% binpath= "%serviceLaunchPath%"
SC %serverName% config %serviceName% displayname= "%serviceDisplayName%"
SC %serverName% config %serviceName% obj= %serviceLogonId% password= "%serviceLogonPassword%"
SC %serverName% config %serviceName% start= auto
SC %serverName% description %serviceName% "%serviceDescription%"
::SC "%serverName%" config "%serviceName%" type= share start= auto

:CreatingServiceDelay
echo Waiting for %serviceName% to get created
ping -n 2 127.0.0.1 > NUL

:CreatingService
::SC %serverName% query %serviceName% >NUL
SC %serverName% query %serviceName% | FIND "STATE" | FIND "STOPPED"
IF errorlevel 1 GOTO CreatingServiceDelay

:CreatedService
echo %serviceName% on %serverName% is created
GOTO End

:usage
echo Will cause a local/remote service to START (if not already started).
echo This script will waiting for the service to enter the started state if necessary.
echo.
echo %0 [service name] [system name]
echo Example: %0 MyService server1
echo Example: %0 MyService (for local PC)
echo.

::GOTO:eof
:End
::pause
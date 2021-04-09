@echo off

IF [%1]==[] GOTO usage
IF NOT "%1"=="" SET serviceName=%1
IF NOT "%2"=="" SET serverName=%2

SC %serverName% query %serviceName%
IF errorlevel 1060 GOTO ServiceNotFound
IF errorlevel 1722 GOTO SystemOffline

:ResolveInitialState

SC %serverName% query %serviceName% | FIND "STATE" | FIND "STOPPED"
IF errorlevel 0 IF NOT errorlevel 1 GOTO StartService

SC %serverName% query %serviceName% | FIND "STATE" | FIND "RUNNING"
IF errorlevel 0 IF NOT errorlevel 1 GOTO StartedService

SC %serverName% query %serviceName% | FIND "STATE" | FIND "PAUSED"
IF errorlevel 0 IF NOT errorlevel 1 GOTO SystemOffline
echo Service State is changing, waiting for service to resolve its state before making changes

SC %serverName% query %serviceName% | Find "STATE" >NUL
ping -n 2 127.0.0.1 > NUL

GOTO ResolveInitialState

:StartService
echo Starting %serviceName% on %serverName%
SC %serverName% start %serviceName%

GOTO StartingService

:StartingServiceDelay
echo Waiting for %serviceName% to start
ping -n 2 127.0.0.1 > NUL 

:StartingService
SC %serverName% query %serviceName% | FIND "STATE" | FIND "RUNNING"
IF errorlevel 1 GOTO StartingServiceDelay

:StartedService
echo %serviceName% on %serverName% is started
GOTO End

:SystemOffline
echo Server %serverName% is not accessible or is offline
GOTO End

:ServiceNotFound
echo Service %serviceName% is not installed on Server %serverName%
::exit /b 0
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
@echo off

IF [%1]==[] GOTO usage
IF NOT "%1"=="" SET serviceName=%1
IF NOT "%2"=="" SET serverName=%2

SC %serverName% query %serviceName%
IF errorlevel 1060 GOTO ServiceNotFound
IF errorlevel 1722 GOTO SystemOffline

:ResolveInitialState
SC %serverName% query %serviceName% | FIND "STATE" | FIND "RUNNING"
IF errorlevel 0 IF NOT errorlevel 1 GOTO StopService

SC %serverName% query %serviceName% | FIND "STATE" | FIND "STOPPED"
IF errorlevel 0 IF NOT errorlevel 1 GOTO StoppedService

SC %serverName% query %serviceName% | FIND "STATE" | FIND "PAUSED"
IF errorlevel 0 IF NOT errorlevel 1 GOTO SystemOffline
echo Service State is changing, waiting for service to resolve its state before making changes

SC %serverName% query %serviceName% | Find "STATE"
ping -n 2 127.0.0.1 > NUL
GOTO ResolveInitialState

:StopService
echo Stopping %serviceName% on %serverName%
SC %serverName% stop %serviceName%
GOTO StoppingService

:StoppingServiceDelay
echo Waiting for %serviceName% to stop
ping -n 2 127.0.0.1 > NUL

:StoppingService
SC %serverName% query %serviceName% | FIND "STATE" | FIND "STOPPED"
IF errorlevel 1 GOTO StoppingServiceDelay

:StoppedService
echo %serviceName% on %serverName% is stopped
GOTO End

:SystemOffline
echo Server %serverName% is not accessible or is offline
GOTO End

:ServiceNotFound
echo Service %serviceName% is not installed on Server %serverName%
::exit /b 0
GOTO End

:usage
echo Will cause a local/remote service to STOP (if not already stopped).
echo This script will waiting for the service to enter the stopped state if necessary.
echo.
echo %0 [service name] [system name] {reason}
echo Example: %0 MyService server1 {reason}
echo Example: %0 MyService (for local PC, DO NOT specify reason)
echo.
echo For reason codes, run "sc stop"


::GOTO:eof
:End
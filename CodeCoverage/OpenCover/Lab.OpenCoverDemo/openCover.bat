echo off

SET vsTest="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\Common7\IDE\Extensions\TestPlatform\vstest.console.exe"
set batchFolder=%~dp0
SET openCover="C:\Tools\OpenCover.4.7.922\tools\OpenCover.Console.exe"
SET report="C:\Tools\ReportGenerator.4.1.2\tools\net47\ReportGenerator.exe"
SET reportOutput=%batchFolder%report
SET historyOutput=%batchFolder%history
SET testResultFolder=%batchFolder%TestResults
SET coverFile="%reportOutput%\coverage.xml"
SET testAssembly=%batchFolder%UnitTestProject1\bin\debug\UnitTestProject1.dll ^
%batchFolder%UnitTestProject2\bin\debug\UnitTestProject2.dll

echo batchFolder=%batchFolder%
echo vsTest     =%vsTest%
echo openCover  =%openCover%
echo report     =%report%

if not exist %historyOutput% (mkdir %historyOutput%)

if exist %reportOutput% (rmdir %reportOutput% /s /q)
mkdir %reportOutput%

if exist %testResultFolder% (rmdir %testResultFolder% /s /q)
mkdir %testResultFolder%

::%openCover% -register:user ^
::-target:%vsTest% ^
::-targetargs:"%testAssembly% /logger:trx /InIsolation /EnableCodeCoverage" ^
::-output:%coverFile% ^
::-filter:"+[UnitTestProject3*]*

%openCover% -register:user ^
-target:%vsTest% ^
-targetargs:"%testAssembly% /logger:trx" ^
-targetdir:%batchFolder% ^
-output:%coverFile% ^
-skipautoprops

%report% -reports:%coverFile% ^
-targetdir:%reportOutput% ^
-reporttypes:Cobertura;HTML;Badges ^
-historydir:%historyOutput% ^
-tag:"Build.2019.11.11" ^
-sourcedirs:%batchFolder%

pause
echo off
SET vsTest="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\Common7\IDE\Extensions\TestPlatform\vstest.console.exe"
set batchFolder=%~dp0

set dotCover="C:\Tools\JetBrains.dotCover.CommandLineTools.2018.3.4\dotCover.exe"
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
echo dotCover   =%dotCover%

if not exist %historyOutput% (mkdir %historyOutput%)

if exist %reportOutput% (rmdir %reportOutput% /s /q)
mkdir %reportOutput%

if exist %testResultFolder% (rmdir %testResultFolder% /s /q)
mkdir %testResultFolder%

%dotCover% cover ^
/TargetExecutable=%vsTest% ^
/TargetArguments="%testAssembly% /logger:trx" ^
/Output=%coverFile% ^
/ReportType=DetailedXML ^
/TargetWorkingDir=%batchFolder% ^
/SymbolSearchPaths=%batchFolder%

%report% -reports:%coverFile% ^
-targetdir:%reportOutput% ^
-reporttypes:Cobertura;HTML;Badges ^
-historydir:%historyOutput% ^
-tag:"Build.2019.11.11" ^
-sourcedirs:%batchFolder%\

pause
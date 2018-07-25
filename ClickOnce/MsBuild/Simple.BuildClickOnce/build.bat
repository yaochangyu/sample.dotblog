@echo off
SET MSBUILD="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe"
::SET MSBUILD="C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe"

SET CARWIN="Simple.Winfrom.BuildClickOnce.sln"

%MSBUILD% %CARWIN% /target:publish /p:platform="any cpu" /p:configuration="debug" /p:VisualStudioVersion="14.0" /p:ApplicationVersion=2018.07.19.1 /p:TargetCulture=zh-TW
pause
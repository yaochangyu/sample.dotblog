@echo off
REM SET MSBUILD="C:\Program Files (x86)\MSBuild\14.0\bin\msbuild.exe"
SET Mage="C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7 Tools\mage.exe"
SET UpdateFile=""
%Mage% -Sign Simple.Winfrom.BuildClickOnce.application -CertFile key.pfx -Password pass@w0rd1~

::%Mage% -Update Simple.Winfrom.BuildClickOnce.application -AppManifest "Application Files\Simple.Winfrom.BuildClickOnce_2017_07_27_1\Simple.Winfrom.BuildClickOnce.exe.manifest"



rem %Mage% -Update "Application Files\WindowsFormsApplication1_1_0_0_2\WindowsFormsApplication1.exe.manifest" -fd .



REM %Mage% -Sign WindowsFormsApplication1_1_0_0_2\WindowsFormsApplication1.exe.manifest -CertFile WindowsFormsApplication1_TemporaryKey.pfx -Password 123456



REM %Mage% -Update WindowsFormsApplication1.application -AppManifest WindowsFormsApplication1_1_0_0_2\WindowsFormsApplication1.exe.manifest



REM %Mage% -Sign WindowsFormsApplication1.application -CertFile WindowsFormsApplication1_TemporaryKey.pfx -Password 123456



REM %Mage% -Update WindowsFormsApplication1_1_0_0_2.application -AppManifest WindowsFormsApplication1_1_0_0_2\WindowsFormsApplication1.exe.manifest



REM %Mage% -Sign WindowsFormsApplication1_1_0_0_2.application -CertFile WindowsFormsApplication1_TemporaryKey.pfx -Password 123456
pause
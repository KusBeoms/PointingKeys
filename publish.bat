@echo off
rem Packages PointingKeys as ONE self-contained exe: dist\PointingKeys.exe (the .NET runtime is inside).
setlocal
cd /d "%~dp0"
if exist dist rmdir /s /q dist
dotnet publish src\PointingKeys -c Release -r win-x64 --self-contained true -o dist ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true ^
  -p:DebugType=embedded || exit /b 1
echo.
echo Packaged: %cd%\dist\PointingKeys.exe

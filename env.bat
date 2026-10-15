@echo off
rem Chroma Frontier dev environment (current cmd session only).
rem   Usage:  call env.bat
rem Toolchain is shared with the Gate Defense project (installed by its tools\install-tools.ps1).
rem Override CF_TOOLS before calling to point at a different toolchain folder.

if not defined CF_TOOLS set "CF_TOOLS=C:\Project\Gate Defense\tools"

set "UNITY_VERSION=6000.3.25f1"
set "UNITY_HOME=%CF_TOOLS%\unity\%UNITY_VERSION%\Editor"
set "UNITY_EXE=%UNITY_HOME%\Unity.exe"
set "UNITY_HUB=%CF_TOOLS%\unity-hub\Unity Hub.exe"

rem --- Android (Unity-bundled JDK / SDK / NDK)
set "ANDROID_PLAYER=%UNITY_HOME%\Data\PlaybackEngines\AndroidPlayer"
set "JAVA_HOME=%ANDROID_PLAYER%\OpenJDK"
set "ANDROID_HOME=%ANDROID_PLAYER%\SDK"
set "ANDROID_SDK_ROOT=%ANDROID_HOME%"
set "ANDROID_NDK_ROOT=%ANDROID_PLAYER%\NDK"

set "NODE_HOME=%CF_TOOLS%\node"
set "DOTNET_ROOT=%CF_TOOLS%\dotnet"
set "DOTNET_MULTILEVEL_LOOKUP=0"

set "PATH=%UNITY_HOME%;%JAVA_HOME%\bin;%ANDROID_HOME%\platform-tools;%NODE_HOME%;%DOTNET_ROOT%;%PATH%"

if not exist "%UNITY_EXE%" (echo [env] Unity not found: %UNITY_EXE%) else (echo [env] Chroma Frontier environment ready. Unity %UNITY_VERSION%)

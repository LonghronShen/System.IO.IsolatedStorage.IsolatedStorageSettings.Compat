@REM Copyright (C) Microsoft Corporation. All rights reserved.
@REM Licensed under the MIT license. See LICENSE.txt in the project root for license information.

@if not defined _echo echo off
setlocal enabledelayedexpansion

@REM Determine if MSBuild is already in the PATH
for /f "usebackq delims=" %%I in (`where msbuild.exe 2^>nul`) do (
    "%%I" %*
    exit /b !ERRORLEVEL!
)

@REM Find the latest MSBuild that supports our projects
for /f "usebackq tokens=*" %%i in (`call "%~dp0vswhere.cmd" -latest -prerelease -all -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
  "%%i" %*
  exit /b !errorlevel!
)

echo Could not find msbuild.exe 1>&2
exit /b 2
@echo off

if "%~1" == "" goto :eof

:enteroutput
set /p output=Name of the PAC archive to create, including extension: 

if "%output%" == "" (
    echo Name cannot be blank.
    goto :enteroutput
) else (
    "%~dp0PuyoPac.exe" create "%output%" %*
)

if %errorlevel% neq 0 goto :error

goto :eof

:error
pause

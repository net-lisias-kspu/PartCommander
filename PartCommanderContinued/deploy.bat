
rem @echo off
cd

set H=R:\KSP_1.3.1_dev
set GAMEDIR=PartCommanderContinued
set GAMEDATA="..\GameData\"
set VERSIONFILE=..\%GAMEDIR%.version
echo %H%

copy /Y "%1%2" "%GAMEDATA%\%GAMEDIR%\Plugins"
copy /Y %VERSIONFILE% %GAMEDATA%\%GAMEDIR%

xcopy /y /s /I %GAMEDATA%\%GAMEDIR% "%H%\GameData\%GAMEDIR%"

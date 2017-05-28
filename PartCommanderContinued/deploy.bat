

set H=R:\KSP_1.3.0_dev

copy bin\Debug\PartCommanderContinued.dll ..\GameData\PartCommanderContinued\Plugins


xcopy /y /s /e "..\GameData\PartCommanderContinued" "%H%\GameData\PartCommanderContinued"

#Persistent
SetTimer, MsgBoxCheck, 1000

MsgBoxCheck:

If WinExist("Copied Central Model")
{
	#IfWinActive Revit
   	WinClose
	ExitApp
}

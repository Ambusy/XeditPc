# XeditPc
Clone of the IBM® editor for VM-CMS of the 1980's
--------------------------------------------------


As a sole USER, extract the "bin/release" folder somewhere on your disk (preferable in a Documents sub folder) and execute the XeditPc program. No need to install. Type the HELP command too view all extra commands and features. The program stores data in the HKEY-CURRENT-USER/Software/Ambusy/XeditPc about default screen sizes.


As a DEVELOPER, open the vbproj file, verify (My Project tab) that you have the correct .NET framework or choose one you do have on your pc. Build the project and run it.


XeditPc contains about 90% of the possibilities that the IBM editor offered. Only the fullscreen commands for use on the 3270 terminals, are not implemented. As much as possible I implemented the commands in Rexx, to show how easy it is to create your own commands. In the commands (the .rex files in the bin/release folder) you'll find many little tools to make the life of a COBOL or RPG programmer happy, only I don't know if there still exist any! But they also are an example of how you could create bulk-edits on source. I remember well a project where a database table was extended with 5 columns. Expected time to implement: 3 months. As similar fields in the table existed for each of the new fields, I wrote a command that edited every program and duplicated lines of code, changing only the names, for each occurence of the look alike field. 90% of the programs compiled and executed without further interventions, total time spent: 4 weeks for 250 programs. The hint is: programmer, automate your own work, not only that of your organization. This editor is ideal for that.

Written in Visual Basic. Includes an interpreter for Rexx® to implement the Xedit-commands in Rexx, also written in VB. For more information about the REXX implementation, see my DotNetRexx project on https://github.com/Ambusy/DotNetRexx


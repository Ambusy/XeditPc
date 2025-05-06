# XeditPc
Clone of the IBM® editor for VM-CMS of the 1980's
--------------------------------------------------


As a USER of Xedit, without the need to view the sources of the project, extract the "bin/release" folder somewhere on your disk (preferable in a Documents sub folder) and execute the XeditPc program. No need to install. Needs the Net 8.0 libraries. Type the HELP command to view all extra commands and features. The program stores data in the HKEY-CURRENT-USER/Software/Ambusy/XeditPc about default screen sizes.


As a DEVELOPER, open the vbproj file, verify (My Project tab) that you have the correct .NET framework or choose one you do have on your pc. Build the project and run it. I now use Microsoft Visual Studio Community 2022 (64-bit) - Current Version 17.10.3


XeditPc contains about 95% of the possibilities that the IBM editor offered. 

I implemented the commands as much as possible in Rexx, first of all to show how easy it is to create your own commands and secondly to allow you to add or change things if you feel the need. In the commands (the .rex files in the bin/release folder) you'll find many little tools to make the life of a COBOL or RPG programmer easy, only I don't know if they still exist! But they are also the example of how you could create bulk-edits on source. 

The fullscreen commands for use on the 3270 terminals, are not completly implemented, but creation of screens with textboxes and labels in different colors is possible.

I remember well a project where a DB2-database table was extended with 5 columns and in all programs for the standard maintanance of the table, code had to be added (inits, checks, I/O modules, etc). Forecast time to implement: 3 months. As similar fields in the table existed for each of the new fields, I wrote a command that edited every program and duplicated corresponding lines of code, changing the fieldnames. 90% of the programs compiled and executed without further interventions, total time spent: 4 weeks for 250 programs. 

So programmer: automate your own work, not only that of your organization. This editor is ideal for that.

Written in Visual Basic. Includes an interpreter for Rexx® to implement the Xedit-commands in Rexx, also written in .NET. For more information about the REXX implementation, see my DotNetRexx project on https://github.com/Ambusy/DotNetRexx

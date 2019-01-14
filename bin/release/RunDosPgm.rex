/* Sample Procedure to Run a Program */
trace n
SAY 'Enter the New Directory to change to?'
PULL Path
PARSE VALUE Path WITH Drive ':' Rest
IF Rest = '' THEN
      Rest = "\"
SAY 'Enter the Program Name to Run?'
PULL Program
address cms "cmd /C del tmpname.bat"
call STREAM("tmpname.bat","C","OPEN WRITE")
call LINEOUT("tmpname.bat","@ECHO ON") 
call LINEOUT("tmpname.bat",Drive||":") 
call LINEOUT("tmpname.bat","CD" rest) 
call LINEOUT("tmpname.bat",Program) 
call LINEOUT("tmpname.bat","PAUSE") 
call STREAM("tmpname.bat","C","CLOSE")
address cms "cmd /C tmpname.bat"
EXIT

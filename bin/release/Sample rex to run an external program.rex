/* Sample Procedure to Run a Program */
trace n
SAY 'Enter the New Directory to change to?'
PULL Path
PARSE VALUE Path WITH Drive ':' Rest
IF Rest = '' THEN
      Rest = "\"
SAY 'Enter the Program Name to Run?'
PULL Program
"extract /temp/"
dsn = temp.1||"doIt.bat"
address cms "cmd /C del" dsn
call STREAM(dsn,"C","OPEN WRITE")
call LINEOUT(dsn,"@ECHO ON") 
call LINEOUT(dsn,Drive||":") 
call LINEOUT(dsn,"CD" rest) 
call LINEOUT(dsn,Program) 
call LINEOUT(dsn,"PAUSE") 
call STREAM(dsn,"C","CLOSE")
address cms "cmd /C" dsn
EXIT
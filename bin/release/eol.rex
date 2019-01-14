/* puts cursor after last nonblank char on current line */ 
trace n
parse arg arg1 
if arg1^='' then signal badarg 
'COMMAND EXTRACT /LINE/ZONE/SIZE/CURSOR/' 
if cursor.4<0 | cursor.4>zone.2 then 
   signal badscr 
'COMMAND SET LINEND OFF' 
'COMMAND LOCATE :'cursor.3 
'COMMAND EXTRACT /LENGTH/' 
saverc=rc 
'COMMAND SET MSGMODE OFF' 
":" line.1
'COMMAND CURSOR FILE' cursor.3 (length.1 + 1) 
exit saverc 
BADSCR: 
  'COMMAND EMSG SJC561E Cursor is not on a valid data field.' 
  exit 3 
BADARG: 
  'COMMAND EMSG SJC520E Invalid operand :' arg1 
  exit 5 

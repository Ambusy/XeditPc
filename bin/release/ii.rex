/* REXX */
/* prefix II: repeats itself until a blank line is inserted */ 
trace n
parse arg p l n .
if p = "PREFIX" & l = "LINE" then do
   address "xedit"
   "Extract /Line/Cursor/"
   ":" n
   "Extract /Curline/"
   if curline.3 == " " then do
      "delete 1"
      ":" line.1  
   end
   else  do
      "INPUT  "
      "set pending block ii"
      i = verify(curline.3," ")
      if i<1 then i=1
      "cursor screen" cursor.1 i  
      ":" line.1 + 1  
   end
end
return

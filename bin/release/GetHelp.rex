/* Provide help  */
"SET STAY ON"
'EXTRACT /LINE/CURSOR/LRECL/EXEPATH/'
':' cursor.3
'stack 1'
if rc = 0 then do
   pull line
   j1 = cursor.4
   j2 = cursor.4
   do i=cursor.4 by -1 to 0 until substr(line,i,1) = ' '
      j1 = i 
   end
   do i=cursor.4 to lrecl.1 until substr(line,i,1) = ' ' | substr(line,i,1) = '.'
      j2 = i 
   end
   n = substr(line,j1,j2-j1-1)
   upper n
   "state" exepath.1 || "HELP" || n || ".txt"
   if rc = 0 then "XEDIT" exepath.1 || "HELP " n || ".txt" 
   else "XEDIT" exepath.1 || "HELP.txt" 
end
exit

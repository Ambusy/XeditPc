/* pak een woord op de cursor pos en laat alle regels ervan zien (xref) */
trace n
'EXTRACT /LINE/CURSOR/'
'SET SCOPE ALL'
':' cursor.3
'stack 1'
if rc = 0 then do
   parse pull line
   j1 = cursor.4
   j2 = cursor.4
   do i=cursor.4 by -1 until i = 0 | substr(line,i,1) = ' ' | substr(line,i,1) = '('
      j1 = i 
   end
   do i=cursor.4 until substr(line,i,1) = ' ' ,
                         | substr(line,i,1) = ')' | substr(line,i,1) = '.'
      j2 = i 
   end
   n = substr(line,j1,j2-j1+1)
   'ALL /'n'/'
   'CMSG ALL'
   firarg = n
   'GLOBALV PUTp FIRARG'
end
":"line.1

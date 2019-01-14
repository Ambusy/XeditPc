/* selects a word under the cursor */
trace n
'EXTRACT /CURSOR/CURLINE/FTYPE/'
rl = curline.2
s1 = "qwertyuiopQWERTYUIOPaăsșdfghjklASDFGHJKLzxcvbnmZXCVBNM1234567890_£$@#"
if ftype.1 = "CBL" | ftype.1 = "COB" then s1 = s1 || "-"
"LOCATE :" cursor.3
if rc <> 0 then exit rc
"extract /curline/"
line = curline.3
j1 = cursor.4 
j2 = cursor.4  
do i=cursor.4 by -1 to 1 while pos(substr(line,i,1), s1) > 0
   j1 = i  
end
l = length(line)
do i=cursor.4 by 1 to l while pos(substr(line,i,1), s1) > 0
   j2 = i  
end
if j1 <= j2 then do
  "SELECTSET ON" cursor.3 j1 cursor.3 j2
end
":" rl
exit 

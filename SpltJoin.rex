/* rexx */
/* splits 1 line / joins 2 lines */
trace n
"EXTRACT /CURSOR/LINE/SIZE/LINEND/"
"SET LINEND OFF"
retline = line.1
":" cursor.3
"EXTRACT /CURLINE/"
r = substr(curline.3,cursor.4)
if r <> "" then do
  if cursor.4 > 1 then do
    "REPLACE" substr(curline.3,1,cursor.4-1)
    "INPUT" substr(curline.3,cursor.4)
  end
end
else do
  if cursor.3 < size.1 then do
    "down 1"
    if rc = 0 then do
      l = curline.3
      "EXTRACT /CURLINE/"
      "delete 1"
      "UP 1"
      l = l || curline.3
      "replace" l
    end
  end
end
":"retline
"SET LINEND ON" linend.2
"CURSOR SCREEN" cursor.1 cursor.2
exit

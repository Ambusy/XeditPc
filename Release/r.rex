/* rexx */
trace n
parse arg p l n r .
if p <> "PREFIX" then exit 4
"EXTRACT /CURSOR/"
":" n
"EXTRACT /CURLINE/"
if datatype(r) <> "NUM" then r = 1
do i=1 to r
  "INPUT" curline.3
end
":" cursor.3
exit

/* writes a file from current line */
trace n
parse upper arg p
p = strip(p)
if left(p,1) = '"' then 
   parse var p '"' fn '"' r1 r2 r3 r4
else
   parse var p fn r1 r2 r3 r4
f = 1
n = 999999999
if r1 = "F" then
   f = r2
if r1 = "N" then
   n = r2
if r3 = "F" then
   f = r4
if r3 = "N" then
   n = r4
"EXTRACT /LINE/SIZE/"
if (f - 1 + n) > size.1 then n = size.1 - f + 1
l.0 = n - line.1 + 1
do i=1 to l.0
   "extract /CURLINE/"
   l.i = curline.3
   "DOWN 1"
end
"ERASE" fn
"EXECIO" l.0 "DISKW" fn "( STEM l."
":" line.1
exit                   

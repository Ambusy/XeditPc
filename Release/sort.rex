/* rexx */
TRACE n
parse arg col .
if col = "" then col = 1
"extract /size/line/linend/temp/"
"set linend off"
if size.1 > 1000 then do
   say "SORT is for small files only (uses external DOS-sort), continue (Y/N)?"
   pull a
   if left(a,1) = "N" then exit
end 
tmp = temp.1
inf = tmp || "$s$x$.tmp"
ouf = tmp || "$s$x$2.tmp"
"save" inf 
address dos "SORT /+"||col inf "/O" ouf
"erase" inf
":1"
"delete *"
"top"
"execio * diskr" ouf
do queued()
  parse pull l
  "INPUT" l
end
exit
"erase" inf
"erase" ouf
"top"
"set linend" linend.1
exit

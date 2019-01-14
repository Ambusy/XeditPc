/* rexx locate unicode string*/
trace n
parse arg n 
nn = left(n,1)
if right(n,1) = left(n,1) 
then n = substr(n,2,length(n)-2)
else n = substr(n,2,length(n)-1)
do i=1 to length(n)
  nn = nn || X'00' || substr(n, i,1)
end
nn = nn || left(nn,1)
"locate "||nn 
if rc=0 then do
  "extract /cursor/"
  /*"cursor file" cursor.3 cursor.4*/
end
exit rc

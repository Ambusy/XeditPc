/* rexx , uncommment out C-type source (see CM.REX to comment)*/
trace N
parse arg p l n r .
if p <> "PREFIX" then exit 4
lFrom = n
'EXTRACT /PENDING UC/LINE/'
if pending.0 > 1 then do  
   lTo = pending.1
   do i=lFrom to lTo
      "LOCATE :" i
      "change -// --"
   end
   ":" lTo
   "SET PENDING OFF"
end 
else do
  ":" n
  "SET PENDING BLOCK UC"
end 
":" line.1
exit

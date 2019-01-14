/* rexx , commment out C-type source (see UC.REX to uncomment)*/
trace N
parse arg p l n r .
if p <> "PREFIX" then exit 4
lFrom = n
'EXTRACT /PENDING CM/LINE/'
if pending.0 > 1 then do  
   lTo = pending.1
   do i=lFrom to lTo
      "LOCATE :" i
      "change --// -"
   end
   ":" lTo
   "SET PENDING OFF"
end 
else do
  ":" n
  "SET PENDING BLOCK CM"
end 
":" line.1
exit
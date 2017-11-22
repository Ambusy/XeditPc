/* rexx */
trace n
parse arg p l pn pr .
if p <> "PREFIX" then exit 4
"EXTRACT /PENDING BLOCK XX/LINE/"
if pending.0 = 0 then do
   ':' pn
   'SET PENDING BLOCK XX' 
end
else do
   ':' pending.1   /* last of pair of xx ... xx */
   'SET PENDING OFF'
   n = pending.1 - pn + 1
   ":" pn
   "SET SELECT 1" n
end
":"line.1
exit

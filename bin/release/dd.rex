/* rexx */
trace n
parse arg p l pn pr .
if p <> "PREFIX" then exit 4
"EXTRACT /LINE/"
"EXTRACT /PENDING BLOCK DD/"
if pending.0 = 0 then do
   ':' pn
   'SET PENDING BLOCK DD' 
   ':'line.1
end
else do
   ':' pending.1   /* last of pair of dd ... dd */
   'SET PENDING OFF'
   n = pending.1 - pn + 1
   ":" pn
   "DELETE" n
end
exit

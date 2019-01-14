/* rexx */
trace n
parse arg p l pn pr .
if p <> "PREFIX" then exit 4
"EXTRACT /PENDING BLOCK RR/CURSOR/"
if pending.0 = 0 then do
   ':' pn
   'SET PENDING BLOCK RR'||pr
end
else do
   if pr = "" then pr = pending.5
   ':' pending.1   /* last of pair of rr ... rr */
   'SET PENDING OFF'
   n = pending.1 - pn + 1
   ":" pending.1
   do i=1 to n
      ":" i + pn - 1
      "EXTRACT /CURLINE/"
      ln.i = curline.3
   end
   if datatype(pr) <> "NUM" then pr = 1
   do i=1 to pr
     do j = 1 to n
       "INPUT" ln.j
     end
   end
end
":" cursor.3
exit

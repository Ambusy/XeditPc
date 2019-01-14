/* M MM..MM C CC..CC Move/Copy sourcelines Preceding this line */
trace n
parse arg p l pn pr . 
if p <> "PREFIX" then exit 4
if pn = 0 then do
  "msg not on TOP line "
  exit 4
end
bestFrom = 9999999
BestTo = 9999999
type = ""
'EXTRACT /PENDING BLOCK C/CURSOR/LINE/LINEND/'
"SET LINEND OFF"
if pending.0 > 0 then   
   if pending.1 < bestFrom then do 
      bestFrom = pending.1
      BestTo = pending.1
      type = "C"
   end
'EXTRACT /PENDING BLOCK M/'
if pending.0 > 0 then   
   if pending.1 < bestFrom then do 
      bestFrom = pending.1
      BestTo = pending.1
      type = "M"
   end
'EXTRACT /PENDING CC/'
if pending.0 > 1 then   
  if pending.1 < BestFrom then do
     BestFrom = pending.1
     s = '/PENDING CC :'||(BestFrom+1)||' * /'
     'EXTRACT' s
     if pending.0 > 1 then do  
        BestTo = pending.1
        type = "C"
     end 
  end 
'EXTRACT /PENDING MM/'
if pending.0 > 1 then   
  if pending.1 < BestFrom then do
     BestFrom = pending.1
     s = '/PENDING MM :'||(BestFrom+1)||' * /'
     'EXTRACT' s
     if pending.0 > 1 then do  
        BestTo = pending.1
        type = "M"
     end 
  end 
if BestFrom <> 9999999 then do
  ":" BestFrom
  "SET PENDING OFF"
  if BestFrom <> BestTo then do
    ":" BestTo
    "SET PENDING OFF"
  end
  ":" pn
  "SET PENDING ON BA"
  n = BestTo - BestFrom + 1
  do i=1 to n
    l = i + BestFrom - 1
    ":" l
    "EXTRACT /CURLINE/"
    ln.i = curline.3
  end
  if type = "M" then do
      ":" BestFrom
      "DELETE" n
  end
  'EXTRACT /PENDING BA :0/'
  ":" pending.1
  "SET PENDING OFF"
  ":" pending.1 - 1
  do i=1 to n
    "INPUT" ln.i
  end
end
else do
   ':' pn
   'SET PENDING BLOCK B' 
end
"SET LINEND ON" linend.2
":" line.1  
exit

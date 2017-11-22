/* */
trace n
parse arg pref sc line
if pref <> 'PREFIX' then do
   'MSG Op prefix intypen'
   exit                                  
end                              
if sc = 'CLEAR' then exit                         
'EXTRACT /SCOPE/LINE/PENDING BLOCK PP/'
ll = line.1
'set scope all'              
if pending.0 = 0 then do
   ':' line
   'SET PENDING BLOCK PP'
end
else do
   ':' pending.1
   'SET PENDING OFF'
    if line > pending.1 then do
       frl = pending.1
       tol = line
    end
    else do
       tol = pending.1
       frl = line
    end
   ':' frl
   'SET scope' scope.1
   ppnl = (tol - frl + 1)
   nn = 0
   klaar = 'n'
   do i=1 to ppnl while klaar = 'n'
      'extract /LINE/'
      if line.1 <= tol then do
         nn = nn + 1
         'extract /curline/'
         if rc = 0 then pp.i = curline.3
         else 'MSG Stack fout'
         if length(pp.i)>255 then 'msg Truncated to 255 chars'
         'GLOBALV PUT PP.'||i
         'DOWN 1'
         if rc > 0 then klaar = 'j'
      end
      else klaar = 'j'
   end
   ppnl = nn
   'GLOBALV PUT PPNL'
end
':' ll
'SET scope' scope.1

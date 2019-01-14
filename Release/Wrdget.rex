/* copies the word under the cursor (wrdput pastes it) */
trace n
'EXTRACT /LINE/CURSOR/'
":" cursor.3
'stack 1'
if rc = 0 then do
   parse pull line
   j1 = cursor.4
   j2 = cursor.4
   do i=cursor.4 by -1 until i=0 | pos(substr(line,i,1), ' (') > 0
      j1 = i
   end
   do i=cursor.4 until i > length(line) | pos(substr(line,i,1), ' ).') > 0
      j2 = i
   end
   n = substr(line,j1,j2-j1+1)
   'globalv put N'
   'msg PUT:' N
end
":" line.1
exit   
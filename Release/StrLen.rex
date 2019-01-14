/* counts n° of chars of string between " or ' where the cursor is pointing at */
trace n
'extract /cursor/line/'
':'cursor.3
'extract /curline/'
l=curline.3
cb = 0
do j=cursor.4 by -1 until j<=1 | cb > 0
  if substr(l,j,1) = "'" | substr(l,j,1) ='"' then do
    cb = j
  end
end
if cb = 0 then do
   do j=cursor.4 by 1 until j>=length(l) | cb > 0
     if substr(l,j,1) = "'" | substr(l,j,1) ='"' then do
       cb = j
     end
   end
end
if cb = 0 then do
  'msg No qoutes found'
  ':'line.1
  exit
end
ce = 0
do j=cb+1 by 1 until j>=length(l) | ce > 0
  if substr(l,j,1) = substr(l,cb,1) then do
    ce = j
  end
end
if ce = 0 then do
  ':'line.1
  'msg no ending quote found'
  exit
end
'msg Text is 'ce-cb-1 'chars'
':'line.1

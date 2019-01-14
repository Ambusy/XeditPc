/* LSE type delete optional part */ 
trace n
BracketToken="{}"
BracketOpenL=left(BracketToken,1)
BracketOpenR=right(BracketToken,1)
BracketOptionalToken="[]"
RepeatChars = '...'
RepeatCharsNL = '..;'
'extract /cursor/curline' 
retl=curline.2
Crsrl = cursor.3   
Crsrc = cursor.4 
':'cursor.3
'extract /curline'                                                                          
call getToken 
call moveCursor
":"||retl
"Cursor file" Crsrl Crsrc
exit 
  
err: 
  parse arg a 
  say 'ERROR:' a 
  exit 4
   
MoveCursor: 
":" Crsrl
'extract /curline'                                                                          
if substr(curline.3,crsrc-1,1)<> BracketOpenL then do
  fnd = 0 
  do while fnd = 0
     i= pos(BracketOpenL, substr(curline.3,crsrc))
     if i>0 then do
       fnd=1
       crsrc = crsrc+i
       j=pos(BracketOpenR, substr(curline.3,i))
       f=crsrc-1
       l=j+i-1
       if f>1 then
         if substr(curline.3,f-1,1)=BracketOOpenL then
           if substr(curline.3,l+1,1)=BracketOOpenR then do
             f=f-1
             l=l+1
           end
       "SELECTSET ON" crsrl f crsrl l
     end
     else do
       'down 1'
       if rc = 0 then do
         crsrc = 1
         crsrl = crsrl + 1
         'extract /curline'                                                                          
       end
       else fnd=1
     end
   end
end
return 
 
getToken: 
BracketOOpenL=left(BracketOptionalToken,1)
BracketOOpenR=right(BracketOptionalToken,1)
fil = curline.2
lal = curline.2
n = 1
cr = crsrc
if substr(curline.3,cr,1)= BracketOOpenR then cr = cr - 1
if cr < 0 then do
  "up 1" 
  "extract /curline"
  fil = fil - 1
  cr = length(curline.3)
end 
do while n > 0
  do l=cr to 1 by -1 while n > 0
    if substr(curline.3,l,1)= BracketOOpenL then n = n - 1
    if substr(curline.3,l,1)= BracketOOpenR then n = n + 1
  end
  if n>0 & curline.2 > 0 then do
    fil = fil - 1
    'Up 1'
    'extract /curline'
    cr = length(curline.3)
  end
  else if n>0 then return
end
fip = l+1
if n>0 then return
n = 1
":"lal
"extract /size/curline"                                                                         
cr = crsrc
if substr(curline.3,cr,1)= BracketOOpenL then cr = cr + 1
do while n > 0
  lc = length(curline.3)
  do l=cr to lc while n > 0
    if substr(curline.3,l,1)= BracketOOpenL then n = n + 1
    if substr(curline.3,l,1)= BracketOOpenR then n = n - 1
  end
  if n>0 & curline.2 < size.1 then do
    lal = lal + 1
    'down 1'
    'extract /curline'
    cr = 1
  end
  else if n>0 then return
end
if n>0 then return
lap = l-1
if substr(curline.3,lap+1,length(RepeatChars)) = RepeatChars then do
   lap = lap + length(RepeatChars) 
end
else if substr(curline.3,lap+1,length(RepeatCharsNL)) = RepeatCharsNL then do
   lap = lap + length(RepeatCharsNL) 
end
crsrc=fip
if fil = lal then do
  curline.3 = substr(curline.3,1,fip-1)||substr(curline.3,lap+1)
  if curline.3 = "" then do
    "del 1" 
    "up 1"
    "extract /curline" 
    crsrl = fil - 1
  end 
  else do   
    "R" curline.3
  end 
  return 
end 
if lal-fil>1 then do
  ":"fil+1 
  "del" lal-fil-1
end
":"fil 
"extract /curline" 
fl = curline.3 
"del 1" 
"extract /curline" 
ll = curline.3 
curline.3 = substr(fl,1,fip-1)||substr(ll,lap+1)
crsrl=fil
if curline.3 = "" then do
  "del 1" 
  "up 1"
  "extract /curline" 
  crsrl = fil - 1
end 
else do   
  "R" curline.3
end 
return
  
trSay: 
parse arg aaa
return 
  
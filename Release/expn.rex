/* LSE type goto next token */ 
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
call moveCursor
":"||retl
"Cursor file" Crsrl Crsrc
exit 
  
err: 
  parse arg a 
  say 'ERROR:' a 
  exit 4
   
MoveCursor: 
BracketOOpenL=left(BracketOptionalToken,1)
BracketOOpenR=right(BracketOptionalToken,1)
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
  
trSay: 
parse arg aaa
return 
  
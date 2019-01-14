/* LSE type expand token */ 
signal on novalue
IdentifierChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890$_"
BracketToken="{}"
BracketOptionalToken="[]"
RepeatChars = '...'
RepeatCharsNL = '..;'
'preserve' 
'extract /cursor' 
':'cursor.3
'extract /curline'                                                                          
call getToken 
Crsrl = cursor.3   
Crsrc = cursor.4 
call trsay ("Token = '" || token || "', from="|| tokenst ||", to="||tokenen)                             
call trsay ("TokenR = '" || substr(curline.3,tokenst,tokenenrep-tokenst+1) || "', from="|| tokenst ||", to="||tokenenrep)                             
call trsay ("Rp="|| repeat ||", Ch="||choice ||", Opt="||optional  ||", RpOpt="||repeatoptional)                             
if Optional = 1 then call trsay ("opt: "||substr(curline.3,OptionalSt,OptionalEn-OptionalSt+1)|| "', from="|| OptionalSt ||", to="||OptionalEn)
if Optional = 1 then call ProcOptBrack 
if Repeat > 0 then call ProcRepeat
if Tokenst > 0 
then olp = left(curline.3,TokenSt-1)
else olp = ""
olf = substr(curline.3,TokenEnRep+1)
menu = ""
call subsToken
do while menu = "MENU"
   menu = ""
   call makeChoice 
   call subsToken
end 
call moveCursor
'restore'
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
   
subsToken:  
parse var token t1 (BracketOpenL) t2 (BracketOpenR) t3
if length(t1)>0 | t2 = "" | length(t3)>0 then do 
  "R" olp || token || olf
  Crsrc = length(olp)+1
  return
end 
upper t2
fnd = 0
menu = ""
nx = 0
"extract /ftype/exepath"
tfn = exepath.1 || "\" || ftype.1 || ".LSE"
Y=stream(tfn,'C','CLOSE')
nl = LINES(tfn)
if nl = "ERROR" then do 
  call err(ftype.1||'.lse not found') 
end 
else do i=1 to nl
  x=linein(tfn)   
  if left(x,1)<> "!"then do
    if left(x,3)='__ ' then do
       if fnd=1 then do
         Y=stream(tfn,'C','CLOSE')
         if menu = "MENU" then do
            token = l.1
            do j=2 to nx
              token = token || l.j
            end
         end
         else call subsText
         return
       end
       x=substr(x,4)
       w=pos("__",x)
       w = substr(x,1,w-2)
       upper w
       if w = t2 then do    
         fnd = 1
         j=pos("__",x)
         Menu = substr(x,j+3)
       end
       else
         fnd = 0
     end
     else do
       if fnd = 1 then do
         nx = nx + 1
         l.nx = x
       end
     end
   end
end
say "Enter value for '"||token||"'"
parse pull a
"R" olp || a || olf
Crsrc = length(olp)+1
return
  
SubsText: 
do i=1 to nx 
  lne = l.i
  if i=1 then lne = olp || lne
  if i=nx then lne = lne || olf
  if i=1 then
     "R" lne
  else
     "I" lne
end 
Crsrc = length(olp)+1
if CrsRc < 2 then CrsRc=2
return 
  
getToken: 
token = ""
Optional = 0
Repeat = 0
RepeatOptional = 0
Choice=0
BracketOpenL=left(BracketToken,1)
BracketOpenR=right(BracketToken,1)
BracketOOpenL=left(BracketOptionalToken,1)
BracketOOpenR=right(BracketOptionalToken,1)
i = lastpos(BracketOpenL,substr(curline.3,1,cursor.4-1))
if i>0 then do
   j = pos(BracketOpenR, substr(curline.3,i))
   if j=0 then call err("Missing" BracketOpenR "token in" curline.3)
   if i+j>cursor.4 then do 
     TokenSt = i
     TokenEn = i+j-1
     olp = substr(curline.3,1,i-1)
     olf = substr(curline.3,i+j)
   end
   cursor.4 = TokenSt+1
   if left(olf,length(RepeatChars))= RepeatChars | left(olf,length(RepeatChars))= RepeatCharsNL then do
     if left(olf,length(RepeatChars))= RepeatChars 
     then Repeat = 1
     else Repeat = 2
     olf = substr(olf,1+length(RepeatChars))
     TokenEnRep = TokenEn + length(RepeatChars)
   end
   else TokenEnRep = TokenEn
   OptionalSt = LastPos(BracketOOpenL,olp) 
   if OptionalSt>0 then do
     n = 1
     do l=OptionalSt+1 to length(curline.3) while n > 0
       if substr(curline.3,l,1)= BracketOOpenL then n = n + 1
       if substr(curline.3,l,1)= BracketOOpenR then n = n -1
     end
     if n=0
     then OptionalEn=l-1
     else k=0
   end
   else OptionalEn=0 
   if OptionalSt>0 & OptionalEn>0 & OptionalSt < TokenSt & OptionalEn > TokenEn then do
     Optional = 1
     if substr(curline.3,OptionalEn+1,length(RepeatChars))=RepeatChars | substr(curline.3,OptionalEn+1,length(RepeatChars))=RepeatCharsNL then do
       if substr(curline.3,OptionalEn+1,length(RepeatChars))=RepeatChars 
       then RepeatOptional = 1
       else RepeatOptional = 2
       OptionalEn = OptionalEn + length(RepeatChars)
     end
   end
   Token = substr(curline.3,TokenSt,TokenEn-TokenSt+1)
end 
else do /* no { */ 
  l=length(curline.3)
  do j=cursor.4-1 to 1 by -1
    if pos(substr(curline.3,j,1),IdentifierChars)=0 then do
      i = j
      leave j
    end
  end
  i2=0
  do j=cursor.4 to l           
    if pos(substr(curline.3,j,1),IdentifierChars)=0 then do
      i2 = j
      leave j
    end
  end
  if i2=0 then i2=l + 1
  TokenSt = i
  TokenEn = i2
  TokenEnRep = TokenEn
  Choice = 0
  Optional = 0
  Token = BracketOpenL||substr(curline.3,TokenSt,TokenEn-TokenSt+1)||BracketOpenR
end
return 
  
MakeChoice: 
nt = 0 
tok = strip(token)
ChoiceChars = left(tok,1)
tok = substr(tok,2)
s = ""
i = pos(ChoiceChars,tok) 
do while i > 0 
  nt = nt + 1 
  if left(tok,1)= BracketOpenL
  then t.nt = substr(tok,2,i-2-length(ChoiceChars))
  else t.nt = substr(tok,1,i-length(ChoiceChars))
  tt.nt = substr(tok,1,i-length(ChoiceChars))
  s = s || x2c("0D0A") ||" " || nt DblAmp(t.nt)
  tok = substr(tok,i+1)
  i = pos(ChoiceChars,tok) 
end 
if tok <> "" then do
  nt = nt + 1 
  if left(tok,1)= BracketOpenL
  then t.nt = substr(tok,2,length(tok)-2)
  else t.nt = tok
  tt.nt = tok
  s = s || x2c("0D0A") ||" " || nt DblAmp(t.nt)
end
i = 0
do while i < 1 | i > nt
  say s
  pull i
end
token = tt.i 
call trsay ("Token = '" || token || "', Rp="|| repeat ||", Ch="||choice ||", Opt="||optional  ||", RpOpt="||repeatoptional )                             
return 
  
ProcOptBrack: 
OptPart = substr(curline.3,OptionalSt,OptionalEn-OptionalSt+1)
l1 = optionalSt 
if repeatoptional > 0 then do
  l2 = optionalEn - length(RepeatChars)  
  optins = optpart
end
else do
  l2 = optionalEn
  optins = ""
end
l3 = optionalEn  
OlfAft = length(curline.3)-l3
if RepeatOptional < 2 then do
  curline.3 = substr(curline.3,1,l1-1) || substr(curline.3,l1+1,l2-l1-1) || OptIns || substr(curline.3,l3+1) 
end 
else do 
  curline.3 = substr(curline.3,1,l1-1) || substr(curline.3,l1+1,l2-l1-1)       
  fb = verify(curline.3," ")
  "I" left(" ",fb-1) || OptIns || substr(curline.3,l3+1) 
  "UP"
end 
TokenSt = TokenSt - 1
TokenEn = TokenEn - 1
TokenEnRep = TokenEnRep - 1
Crsrc = Crsrc -1
return
  
DblAmp: 
parse arg DbArg 
dbI = pos("&", DbArg,1) 
do while dbI > 0 
  dbArg = substr(dbArg,1,dbI) || "&" || substr(dbArg,dbI+1) 
  dbI = pos("&",DbArg,dbI+2) 
end
return dbArg
 
ProcRepeat: 
TokPart = substr(curline.3,TokenSt,TokenEnRep-TokenSt+1)
if Repeat = 1 then do
curline.3 = substr(curline.3,1,TokenEnRep) || BracketOOpenL || tokPart || BracketOOpenR || substr(curline.3,TokenEnRep+1) 
end 
else do 
  curline.3 = substr(curline.3,1,TokenEnRep) 
  fb = verify(curline.3," ")
  "I" left(" ",fb-1) || BracketOOpenL || tokPart || BracketOOpenR || substr(curline.3,TokenEnRep+1)  
  "UP"
end 
return
  
trSay: 
parse arg aaa
return 
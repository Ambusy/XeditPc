/* Compare all files in a dir (and it's sub-dirs) with a copy in another dir */
/* ie. Data files copied to a cd, to verify the burning process */ 
/* compare the CD with the original, not the reverse as not all files might have been copied! */
trace n
parse arg inp "," outp
if inp = "" then do
  say "input"
  pull inp
end
inp=strip(inp)
if left(inp,1) = '"' then parse var inp . '"' inp '"' .
if right(inp,1) <> "\" then inp=inp||"\"
 
if outp = "" then do
  say "copy"
  pull outp
end
outp = strip(outp)
if left(outp,1) = '"' then parse var outp . '"' outp '"' .
if right(outp,1) <> "\" then outp=outp||"\"
 
t = VALUE('temp',,'CMD')  
if t = "" then t = VALUE('tmp',,'CMD')  
if t = "" then t = "C:\" 
if right(t,1) <> "\" then t=t||"\"
 
f = t||"compfile.tmp"
fr = t||"compfileresp.tmp"
fo = t||"compfileout.tmp"
ff = t||"compfilef.bat"
"erase" f
"erase" fr
"erase" fo
"erase" ff
queue "n"
"execio 1 diskw" fr
'cms dir "'||inp||'*.*">' || f 
"execio * diskr" f "( stem f."
nc = 1
cm.1 = "@ECHO OFF"
do i=1 to f.0
   ln = f.i
   if word(ln,3) = "<DIR>" then do
     parse var ln . . . dirn
     dirn = strip(dirn)
     if left(dirn,1) <> "." then do
       "macro compfile" inp||dirn "," outp||dirn
     end 
   end
   else do
     if substr(ln,3,1) = "/" & substr(ln,6,1) = "/"then do
        parse var ln . . . filen
        nc=nc+1
        cm.nc="cls"
        nc=nc+1
        cm.nc='echo "' inp|| filen '"'
        nc=nc + 1
        cm.nc = 'comp "'|| inp || filen || '" "'|| outp || filen || '" <' fr ">" fo
        nc=nc + 1
        cm.nc = 'if errorlevel 1 goto ft'i 
        nc=nc + 1
        cm.nc = 'goto ok'i
        nc=nc + 1
        cm.nc = ':ft'i
        nc=nc + 1
        cm.nc = 'cls'
        nc=nc + 1
        cm.nc = 'type' fo
        nc=nc + 1
        cm.nc = 'pause '                                                                                                             c
        nc=nc + 1
        cm.nc = ':ok'i
      end
   end   
end
if nc > 1 then do
   cm.0 = nc
   "execio" nc "diskw" ff "(STEM cm."
   "CMS" ff 
end
"erase" f
"erase" fr
"erase" fo
"erase" ff
exit

/* rexx */
TRACE n; 
parse arg parm
if parm = "" then parm = "1"
nc=0 
keyl = 0
do while parm <> "" 
  parse var parm cl le parm 
  if le = "" then le = 128
  nc = nc + 1 
  scol.nc = cl 
  slen.nc = le 
  keyl = keyl + le
end 
"extract /size/line/linend/temp/Fullfilename/RexxPath/"
"set linend off"
tmp = temp.1
inf = tmp || "$s$x$.tmp"
ouf = tmp || "$s$x$2.tmp"
"erase" inf
"erase" ouf
rc = Stream(inf,"C",'OPEN WRITE')
"top" 
"down 1"  
n = 0
do while rc = 0   
  "extract /curline/"    
  key=""
  do i=1 to nc 
     key = key || substr(curline.3,scol.i,slen.i) 
  end 
  n = n + 1
  rc = Lineout(inf, key || format(n,6) || curline.3)     
  "down 1"
end         
rc = Stream(inf,"C",'CLOSE')
"extract /temp/"
dsn = temp.1||"doIt.bat"
"erase" dsn
call STREAM(dsn,"C","OPEN WRITE")
call LINEOUT(dsn,"@ECHO ON") 
call LINEOUT(dsn,"powershell -NoProfile -ExecutionPolicy Bypass -Command ""Get-Content '"||inf||"' | Sort-Object @{ Expression = { $_.Substring(0,[Math]::Min("||keyl||",$_.Length)) } } | Set-Content '"||ouf||"'"" "
call STREAM(dsn,"C","CLOSE")
address "DOS" "cmd /C" dsn
rc = Stream(ouf,"C",'OPEN READ')
do l=1 to n
   curline = LINEIN(ouf) 
   if l=1 then do
      if left(curline,3) = "ï»¿" then curline = substr(curline,4)
   end
   ln = substr(curline,keyl+1,6) /* line in original text */   
   text=substr(curline,keyl+7)
   ":" l
   "R" text
end
rc = Stream(ouf,"C",'CLOSE')
"top"
exit
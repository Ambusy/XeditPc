/* rexx */
TRACE n
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
"CMS SORT" inf "/O" ouf
rc = Stream(ouf,"C",'OPEN READ')
do l=1 to n
   curline = LINEIN(ouf) 
   ln = substr(curline,keyl+1,6) /* line in original text */   
   text=substr(curline,keyl+7)
   ":" l
   "R" text
end
rc = Stream(ouf,"C",'CLOSE')
"top"
exit
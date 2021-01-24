/* rexx */
TRACE n
parse arg col .
if col = "" then col = 1
"extract /size/line/linend/temp/Fullfilename/"
"set linend off"
if size.1 > 1000 then do
   say "SORT is for small files only (uses external DOS-sort), continue (Y/N)?"
   pull a
   if left(a,1) = "N" then exit
end 
tmp = temp.1
inf = tmp || "$s$x$.tmp"
ouf = tmp || "$s$x$2.tmp"
"erase" inf
"erase" ouf
"top" 
"down 1"  
n = 0
do while rc = 0   
  "extract /curline/"    
  n = n + 1
  "R" format(n,6) || curline.3     
  "down 1"
end         
"Bot"
"up 1"
"I" " " 
"save" inf 
coln = col + 6
"CMS SORT /+"||coln inf "/O" ouf
"XEDIT" ouf
"top"
"down 1"  
do i=1 to n+1 while rc = 0   
  "extract /curline/"    
  ln = substr(curline.3,1,6)     
  if datatype(ln)="NUM" then do  /* empty line on top! */
   "xedit" Fullfilename.1
   ":" ln
   "extract /curline/"    
   tx = substr(curline.3,7)
   "Bot"
   "up 1"
   "I" tx 
   "xedit" ouf  
  end
  "down 1"   
end         
"quit"  /* temp file */
"xedit" Fullfilename.1
"extract /size/"
":1"
"del" size.1 - n   /* keep only inserted lines */
"top"
"set linend" linend.1
exit
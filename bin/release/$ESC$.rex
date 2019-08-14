/* ESC pressed and no messages waiting: */
/* insert abbreviated words */
"EXTRACT /CURSOR/CURLINE/"
cl = curline.2
a.1 =  "P Procedure"
a.2 =  "B Begin"
a.3 =  "E End"
a.0=3
a = ""
do i=1 to a.0 
 a = a || a.i || "0D0A"X
end
say a
pull r
do i=1 to a.0 while r <> ""
  if left(a.i,length(r)) = r then do
   ':'cursor.3 
   "EXTRACT /CURLINE/"
   line = curline.3
   j1 = cursor.4- 1
   a = substr(a.i,length(r)+2)
   l = substr(curline.3,1,j1) || a || substr(curline.3,j1+1)
   'R' l 
   "CURSOR SCREEN" cursor.1 cursor.2 + length(a)
   ":"cl
   r=""
  end
end
exit
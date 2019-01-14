/*      COVerlay text
                   overlays the current line (starting at the column pointer)
                   with the non-blank characters in "text."  Use underscores in
                   "text" to force blanks on the current line.*/
trace n 
parse arg text
text = strip(text)
l=length(text)
"extract /cursor/curline/"
if cursor.3 = curline.2  /* start on curline or continue on cursorpos of curline? */
then sp = cursor.4  
else sp = 1
p = substr(curline.3,sp)
do i=1 to l 
  x = substr(text,i,1)  
  if x <> " " then do
    if x = "_" 
    then p = OVERLAY(" ",p,i)     
    else p = OVERLAY(x,p,i)     
  end
end       
if sp>1 then p=substr(curline.3,1,sp-1) || p        
"REPLACE" p
exit    

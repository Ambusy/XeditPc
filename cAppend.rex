/*  
         CAPPEND [text]
                   appends the specified text to the end of the current line.
                   The column pointer is moved to the first appended character,
                   or the first trailing blank if no text is given.        */
trace n
parse arg text
"EXTRACT /curline/"
l = length(curline.3) 
curline.3 = curline.3 || text 
"replace" curline.3 
"cursor file" curline.2 l+1 
exit 

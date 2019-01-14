/*        CReplace text
                   replaces the characters in the current line (starting at the
                   column pointer) with the "text."  This is similar to
                   COVERLAY, except blanks and underscores have no special
                   significance.*/
trace n 
parse arg text
"extract /cursor/curline/"
if cursor.3 = curline.2  /* start on curline or continue on cursorpos of curline? */
then sp = cursor.4  
else sp = 1
p = OVERLAY(text, curline.3, sp)     
"replace" p
exit    

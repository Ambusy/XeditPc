/*         CINSERT text
                   inserts text immediately before the column pointer in the
                   current line.  */
trace n
parse arg text
"extract /cursor/curline/"
if cursor.3 = curline.2  /* start on curline or continue on cursorpos of curline? */
then sp = cursor.4 - 1
else sp = 0
p = insert(text,curline.3,sp)
"REPLACE" p
exit    

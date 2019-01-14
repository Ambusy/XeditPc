/*         CInsCursor text
                   inserts text on the cursor position */
trace n
parse arg text
"extract /cursor/curline/"
cl = curline.2
":" cursor.3
"extract /curline/"
p = insert(text,curline.3,cursor.4-1)
"REPLACE" p
":" cl
"Cursor file" cursor.3 cursor.4+1
exit    

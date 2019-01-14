/* line command: changes all to lowercase */
trace n
parse arg p l n r .
if p <> "PREFIX" then exit 4
"EXTRACT /LINE/"
":" n
"EXTRACT /CURLINE/"
lower curline.3
"REPLACE" curline.3
":" line.1
exit

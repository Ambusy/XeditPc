/* linecommand: change line to all uppercase */
trace n
parse arg p l n r .
if p <> "PREFIX" then exit 4
"EXTRACT /line/"
":" n
"EXTRACT /CURLINE/"
upper curline.3
"REPLACE" curline.3
":" line.1
exit

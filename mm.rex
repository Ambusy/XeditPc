/* rexx */
parse arg p l n r .
if p <> "PREFIX" then exit 4
"EXTRACT /LINE/"
":" n
"SET PENDING BLOCK MM"
":" line.1
exit

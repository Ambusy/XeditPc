/* rexx */
trace n
parse arg p l n r .
if p <> "PREFIX" then exit 4
"EXTRACT /LINE/"
":" n
"SET PENDING BLOCK CC"
":" line.1
exit

/* rexx */
/* Prefix command: indicates the line to be copied by A or B line command */
/* Command: Abbrev for Change */
trace n
parse arg p l n r .
"EXTRACT /LINE/"        
":" n
"SET PENDING BLOCK C"
":" line.1
exit 

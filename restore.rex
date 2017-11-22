/* REXX */
/* restore all env settings */
call restore("MSGMODE")
call restore("ZONE")
call restore("TRUNC")
call restore("SCOPE")
call restore("RECFM")
call restore("LRECL")
call restore("WRAP")
call restore("HEX")
call restore("VERIFY")
call restore("CASE")
call restore("LINEND")
call restore("AUTOSAVE")
call restore("SHADOW")
exit 
  
restore: 
parse arg w 
"SavGlobal GET PRESERVE" w||".0"
pull n
c = w
do i=1 to n
  "SavGlobal GET PRESERVE" w||"."||i
  parse pull x
  c = c x
end
"SET" c
return  
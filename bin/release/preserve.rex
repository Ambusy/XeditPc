/* REXX */
/* preserve all env settings */
trace n
call save("MSGMODE")
call save("ZONE")
call save("TRUNC")
call save("SCOPE")
call save("RECFM")
call save("LRECL")
call save("WRAP")
call save("HEX")
call save("VERIFY")
call save("CASE")
call save("LINEND")
call save("AUTOSAVE")
call save("SHADOW")
exit 
  
save: 
parse arg w 
"EXTRACT /"||w||"/"
interpret "n ="  w||".0"
"SavGlobal PUT PRESERVE" w||".0" n
do i=1 to n
  interpret "v ="  w||"."||i
  "SavGlobal PUT PRESERVE" w||"."||i v  
end
return  
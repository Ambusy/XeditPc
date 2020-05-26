/* Execute cms (dos) command */ 
trace n
parse arg c
address "DOS" "cmd /C " || c
exit rc

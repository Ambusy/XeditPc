/* Execute cms (dos) command */ 
trace n
parse arg c
address cms "cmd /C " || c
exit rc

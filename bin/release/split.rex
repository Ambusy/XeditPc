/* split current line by character specified in command (text tab becomes character tab)*/
trace n
parse arg char 
if char = "" then exit
if char = "tab" then char = '09'x
"extract /LINE/CURLINE/" 
s = curline.3 
do while s <> "" 
   parse var s s1 (char) s
   "I" s1 
end 
exit 
 
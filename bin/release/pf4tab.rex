/* set cursor on next tab position */
trace n
"extract /cursor/tabs/"
p = cursor.4
if tabs.1 = "OFF" 
then exit 
else do 
   t = tabs.1
   parse var t . t
   do while t <> ""
     parse var t pt t
     if pt > p then do
       "CURSOR FILE" cursor.3 pt
       exit 
     end
   end
end
exit
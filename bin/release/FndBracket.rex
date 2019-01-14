/* Find corresponding CLOSE bracket */
trace n
'EXTRACT /LINE/CURSOR/WRAP/FNAME/'
'SET WRAP OFF'
msg = ''
cl = cursor.3
cp = cursor.4
':' cursor.3
'stack 1'
if rc = 0 then do
   pull line
   sp = substr(line,cursor.4,1)
   spc = ""
   if sp = "(" then spc = ")"
   if sp = "[" then spc = "]"
   if sp = "{" then spc = "}"
   if spc = "" then say "cursor not on a valid bracket open"
   else do
     l = substr(line,cursor.4+1)
     n = 1
     do while n > 0
       do i=1 to length(l) while n > 0
         cp = cp + 1
         s = substr(l,i,1)
         if s = sp then n = n + 1
         else if s = spc then n = n - 1
       end
       if n > 0 then do
         cl = cl + 1
         "down 1"
         if rc = 0 then do 
           "stack 1"
           pull l
           cp = 1
         end
       end
     end
   end
end
'SET WRAP' wrap.1
         ":"line.1
         "CURSOR FILE" cl cp 
if msg <> '' then 'MSG' msg
exit

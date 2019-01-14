/* REXX */
/* stack N lines */
trace n
parse arg target start length 
if start = "" then start = 1
if length = "" then length = 256
do i=1 to target while rc = 0
   "EXTRACT /CURLINE/"
   if rc = 0 then do
      queue substr(curline.3,start,length)
      'down 1'
   end
   else
      exit 4
end
exit

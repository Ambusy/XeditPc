/* VisDbl show only lines with an identical text on position "from" for "len" characters */
trace n 
parse arg from len
"extract /line/wrap/scope/" 
"set wrap off" 
"set scope all"
"top" 
"set select 0 *" /* all invisible */
vs = ""
"top"
"down 1"
do while rc=0 
   "extract /curline/" 
   ns = substr(curline.3,from,len)
   if ns = vs then do
     "up 1"
     "set select 1 2" /* current and previous line become visible */
     "down 2"
   end
   else do
     vs = ns
     "down 1"
   end
end 
"TOP"
"BOT"   /* do show correctly lines not displayed */
"top"
"set wrap" wrap.1
"set display" 1 1
"set scope display"
exit
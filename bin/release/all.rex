/* all /target/ shows all lines with target */
trace n 
parse arg target
if target = "" then do 
  "set scope all"
  "globalv get AllRetLine"
  "locate :" AllRetLine
  AllRetLine = " "
  "globalv put AllRetLine"  
  exit
end 
"extract /line/wrap/scope/" 
"globalv get AllRetLine"
if AllRetLine = " " then do
   AllRetLine = line.1
   "globalv put AllRetLine"  
 end
"set wrap off" 
"set scope all"
"top" 
"set select 0 *" /* all invisible */
selected = 0
"locate" target 
do while rc=0 
   "set select 1 1"
   selected = 1
   "locate" target 
end 
"TOP"
"BOT"
":"line.1 
"set wrap" wrap.1
"set display" selected selected
"set scope display"
if selected = 0 then do 
   "msg Not found" 
   exit 4 
end
exit
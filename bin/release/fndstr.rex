/* simulate f16 in STREU in iSeries 400 */
trace n
"extract /cursor/line/"
'read cmdline'
pull txt
txt=strip(txt)
if txt <> "" then do
  "globalv put txt"
  'top'
end
else "globalv get txt"
'locate /'txt
if rc > 0 then do
  "set cursor screen" cursor.1 cursor.2
  ":" line.1
end 
exit
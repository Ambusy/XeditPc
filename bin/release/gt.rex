/* change > and > in &gt; and &lt; */
trace n
"EXTRACT /SELECTSET/LINE/CURSOR/"
if selectset.1 > -1 then do
   ":" selectset.1
   "C /</&lt;/"  selectset.3 -  selectset.1 + 1 "*" 
   ":" selectset.1
   "C />/&gt;/"  selectset.3 -  selectset.1 + 1 "*" 
   ":" selectset.1
   "C /~/&ne;/"  selectset.3 -  selectset.1 + 1 "*" 
   ":" selectset.1
   "C /_//"  selectset.3 -  selectset.1 + 1 "*" 
   "selectset off"
end
"LOCATE :"line.1
exit

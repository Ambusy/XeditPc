/* splits iedere regel in woorden */
trace n 
"extract /size/" 
do l=size.1 to 1 by -1
   ":" l
   "extract /curline/" 
   if index(curline.3," ")>0 then do
      "del 1"
      "up 1"
      wr.0 = split("WR",curline.3," ") 
      do i=1 to wr.0 
         "I" wr.i
      end
   end
end 
exit 
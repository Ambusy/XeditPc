/* Locate selected text (CTRL-F) */
trace n
parse upper arg a .
"EXTRACT /SELECTSET/"
if selectset.1 > -1 then do
   ":" selectset.1
   "EXTRACT /CURLINE/"
   s = substr(curline.3, selectset.2, selectset.4 - selectset.2 + 1)
   "TOP"
   if a = "ALL" then
      "ALL /"||s
   else do
      "LOCATE /"||s
      "CMSG &/"||s 
   end
   exit rc
end
exit 4

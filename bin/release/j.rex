/* insert "r" lines using prefix command on line "n", with a blank on each line to avoid deletion */
trace n
parse arg p l n r
if p = "PREFIX" & l = "LINE" then do
   "Extract /Line/"
   ":" n
   if r="" then r=1
   do r
     "INPUT  "
   end
  ":" line.1
end
return
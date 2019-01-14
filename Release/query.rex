/* rexx */
trace n
parse upper arg p .
if p = "PF" then do 
   do i = 1 to 12
     if i < 10 
     then j = "0"||i
     else j = i
     "EXTRACT /PF"||j
     if value("PF"||j||".0") > 0
     then say "PF"||i "=" value("pf"||j||".1")
     "EXTRACT /SHIFT-PF"||j
     if value("SHIFT-PF"||j||".0") > 0
     then say "SHIFT-PF"||i "=" value("SHIFT-PF"||j||".1")
     "EXTRACT /CTRL-PF"||j
     if value("CTRL-PF"||j||".0") > 0
     then say "CTRL-PF"||i "=" value("CTRL-PF"||j||".1")
     "EXTRACT /ALT-PF"||j
     if value("ALT-PF"||j||".0") > 0
     then say "ALT-PF"||i "=" value("ALT-PF"||j||".1")
   end
   exit
end
say "Implemented: Q PF"
exit

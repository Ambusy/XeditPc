/*         CLocate column-target    ( :N     or     /Text/  )
                   makes the column specified by the column target the column
                   pointer value.  Searching begins after the column pointer
                   position in the current line.  If SET STREAM ON (the initial
                   setting) is in effect searching continues beyond the current
                   line.  */
trace n
rrc = 0
parse arg targetText
targetText = strip(targetText)
rrc = locateParm(targetText)
exit rrc
 
locateParm:
   parse arg targetText
   if left(targetText,1) = ":" then do
      "EXTRACT /cursor/"
      "CURSOR FILE" cursor.3 substr(targetText,2)
      rrc = rc
   end
   else do
      "EXTRACT /line/cursor"
      curspos = cursor.3 cursor.4
      startPos = cursor.4 
      if cursor.3 <> line.1  /* start on cursorpos 1 of curline if cursor is not on curline, orelse continue on cursorpos of curline */
      then do 
         ":"cursor.3
         ":"line.1
      end
      "EXTRACT /curline/"
      haystack = substr(curline.3,startPos)
      rrc = 4
      target = targetText
      sep = left(target,1)
      target = substr(target,2)
      i = index(target, sep)
      if i > 0 then target = left(target,i-1)
      "EXTRACT /case/"
      if case.2 = "IGNORE" then do
         upper target
         upper haystack 
      end 
      i = index(haystack, target)
      if i > 0 then do                     /* found in text on current line on or after cursor position*/
         "CURSOR FILE" curline.2 (i + startPos - 1) 
         rrc = 0
      end
      else do                              /* try find it on next line(s) */
         "EXTRACT /stream/"
         if stream.1 = "ON" then do
            "LOCATE" targetText
            rrc = rc
            if rrc=0 then do               /*found, now put the cursor on it */
               "Clocate :1" 
               "clocate" targetText  
            end
         end 
         else
            rrc = 4
      end
      "EXTRACT /cursor/"
      if curspos = cursor.3 cursor.4 then do  /* Found it again? I want the NEXT one */
         "CURSOR FILE" cursor.3 (cursor.4 + 1)   
         rrc=locateParm(targetText)
      end
   end
   return rrc
/* wordindex(text,index) return position of the "index"ed word */
wordindex: trace n
  parse arg txt, idx
  inw = 0  /* in a word? */
  nc = 0 /* n* chars scanned */
  nw = 0   /* n° words encountered */
  ps = 1   /* current position scanned*/ 
  lt = length(txt)
  do while ps <= lt     
     nc = nc + 1
     c = substr(txt,ps,1)
     if c = " " & inw = 1 then do /* space after a word */
        inw = 0
     end
     else if c = " " then do /* 2nd, 3rd, ... space */
        nop
     end
     else if inw = 1 then do /* 2nd, 3rd, ... non-blank */  
        nop
     end
     else do /* start of a word */
        inw = 1
        nw = nw + 1
        if nw = idx then return nc
     end
     ps = ps + 1
  end      
  return 0        
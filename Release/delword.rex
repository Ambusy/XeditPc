/* delword (text, index[, n° words]) */
delword:
  trace n
  parse arg txt, idx, lent 
  if lent = "" then do
     idxl = idx
  end
  else do
     idxl = idx + lent - 1
  end
  inw = 0  /* in a word? */
  nw = 0   /* n° words encountered */
  ps = 1   /* current position scanned*/ 
  cp = 1   /* copy char to output? */
  txtn = "" /* new text */
  lt = length(txt)
  do while ps <= lt     
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
        if cp = 0 then cp = 1 /* restart copying */
        inw = 1
        nw = nw + 1
        if nw >= idx & nw <= idxl then do /* delete word and intermediate blanks */
          cp = 0 /* don't copy */
        end
     end
     if cp then txtn = txtn || c
     ps = ps + 1
  end      
 return txtn        
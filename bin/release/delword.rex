/* delword(text, index[, nr of words]) */
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
  nw = 0   /* nr words encountered */
  ps = 1   /* current position scanned*/ 
  cp = 1   /* copy char to output? */
  txtn = "" /* new text */
  lt = length(txt)
  do while ps <= lt     
     c = substr(txt,ps,1)
     if c = " " | inw = 1 then do /* space after a word or next same character*/
        if c = " " & inw = 1 then inw = 0 /* 1st space after text: ends word */
     end
     else do /* c<>" " & inw=0 : start of a word */
        if cp = 0 then cp = 1 /* restart copying */
        inw = 1
        nw = nw + 1
        if nw >= idx & nw <= idxl then do /* daelete word and intermediate blanks */
          cp = 0 /* don't copy */
        end
     end
     if cp then txtn = txtn || c
     ps = ps + 1
  end      
 return txtn        
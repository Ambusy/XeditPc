/* space(text, [n° chars], [pad_char]) return words with exact spacing with indicated spacechar*/
space: trace n  
  parse arg txt, ns, pad
  if ns = "" then ns = 1
  if pad = "" then pad = " "
  pad = left(pad,1)
  txt = strip(txt)
  inw = 0  /* in a word? */
  ps = 1   /* current position scanned*/ 
  cp = 1   /* copy char to output? */
  txtn = "" /* new text */
  lt = length(txt)
  do while ps <= lt     
     c = substr(txt,ps,1)
     if c = " " & inw = 1 then do /* space after a word */
        inw = 0
        cp = 0
        txtn = txtn || copies(pad,ns)
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
     end
     if cp then txtn = txtn || c
     ps = ps + 1
  end      
  return txtn        
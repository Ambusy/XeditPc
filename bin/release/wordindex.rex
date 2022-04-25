/* wp = wordindex(haystack, index) returns the position of the first character in the nth blank-delimited word in string or returns 0*/
wordindex: trace n
  parse arg txt, idx
  inw = 0  /* in a word? */
  nc = 0   /* nr chars scanned */
  nw = 0   /* nr words encountered */
  ps = 1   /* current position scanned*/ 
  lt = length(txt)
  do while ps <= lt     
     nc = nc + 1
     c = substr(txt,ps,1)
     if c = " " | inw = 1 then do /* space after a word or next same character*/
        if c = " " & inw = 1 then inw = 0 /* 1st space after text: ends word */
     end
     else do /* c<>" " & inw=0 : start of a word */
        inw = 1
        nw = nw + 1
        if nw = idx then return nc
     end
     ps = ps + 1
  end      
  return 0        
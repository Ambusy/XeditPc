/* subword(text,[from],[n° words]) */
subword: 
  trace n
  parse arg txt, strt, nr  
  if strt = 1 then  
     t = txt      
  else     
     t = delword(txt,1,strt-1)      
  if nr <> "" then t = delword(t,nr+1,9999)     
  return t      
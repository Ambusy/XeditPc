/* wordlength(text,index) return length of nth word*/
wordlength: trace n
  parse arg txt, idx
  if idx > 1
  then txt = delword(txt, 1, idx-1)
  return length(word(txt,1))        
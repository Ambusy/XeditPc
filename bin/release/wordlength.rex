/* wordlength(text,index) */
wordlength: trace n
  parse arg txt, idx
  if idx > 1
  then txt = delword(txt, 1, idx-1)
  return length(word(txt,1))        
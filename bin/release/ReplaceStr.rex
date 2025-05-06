/* ReplaceStr(string, old, new): returns "string" with all occurrenced of the substring "old" replaced by "new" 
   SET CASE settings are used to search */
  parse arg text, old, new
  l=length(text)
  lo = length(old)
  ln = length(new)
  srcText = text
  "extract /case/"
  if case.2 = "IGNORE" then upper srcText 
  if case.2 = "IGNORE" then upper old 
  newText=""
  i = 1
  do while i <= l
     if substr(srcText,i,lo)=old
     then do 
        newt = new
        i = i + lo - 1
     end
     else newt = substr(text,i,1)
     newtext = newText || newt
     i = i + 1
  end
  return newText
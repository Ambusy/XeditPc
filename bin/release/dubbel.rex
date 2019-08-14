/* eliminate lines that appear twice one below the other */ 
/* from: startposition, default 1
   upto: end position, default length of line. negative: cut tail with given number of chars */
parse arg from upto .
if from != "" then do 
  if datatype(from) != "NUM" then from = "" 
end
if upto != "" then do 
  if datatype(upto) != "NUM" then from = "" 
end
trace n
v = ""
'top' 
'down 1' 
do while rc=0 
  "extract /curline/" 
  l=curline.3
  if upto != "" then do
    if upto > 0 
    then l = left(l,upto)
    else if length(l) >= (0-upto) 
         then l = left(l,length(l) + upto)  
  end
  if from != "" 
  then l = substr(l,from)   
  if l = v
  then 'del 1'
  else do
    v = l
    'down 1'
  end
end
exit 
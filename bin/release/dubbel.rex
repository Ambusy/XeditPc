/* eliminate lines that appear twice one below the other */ 
/* from: startposition, default 1
   upto: end position, default 255 */
parse arg from upto f2 u2 f3 u3 f4 u4 f5 u5 .
if from != "" then do 
  if datatype(from) != "NUM" then from = "" 
end
if upto != "" then do 
  if datatype(upto) != "NUM" then from = "" 
end
if from="" then from=1
if upto="" then upto=255
trace n
v = ""
'top' 
'down 1' 
do while rc=0 
  "extract /curline/" 
  c=curline.3
  l = substr(c,from,upto)
  if f2 <> "" then  l = l || substr(c,f2,u2)
  if f3 <> "" then  l = l || substr(c,f3,u3)
  if f4 <> "" then  l = l || substr(c,f4,u4)
  if f5 <> "" then  l = l || substr(c,f5,u5)
  if l = v
  then 'del 1'
  else do
    v = l
    'down 1'
  end
end
exit 
/* eliminate lines that appear twice */ 
trace n
v = ""
top 
down 1 
do while rc=0 
 "extract /curline/" 
  l=curline.3
  if l = v
  then del 1
  else do
    v = l
    down 1
  end
end
exit 
/* deletes all lines with equal contents */
trace n
parse arg start lengt .
if datatype(start) <> 'NUM' then start=vraag('startpos of compare field:')
if datatype(lengt) <> 'NUM' then lengt=vraag('n° of chars to compare:')
prev = x'00'
"extract /LINE/"
'top'
'down 1'
do while rc = 0
  'extract /curline/'
  deze = curline.3
  if substr(deze,start,lengt) = prev
  then 'delete 1'
  else 'down 1'
  prev = substr(deze,start,lengt)
end
":"line.1
exit

vraag:
 parse arg q
 say q
 parse pull v
 return v

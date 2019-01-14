/* gooit (deels) dubbele regels weg uit een file */
trace n
parse arg start lengt save
if datatype(start) <> 'NUM' then start=vraag('startpositie:')
if datatype(lengt) <> 'NUM' then lengt=vraag('aantal tekens:')
prev = x'00'
'extract /cursor/'
'top'
'down 1'
do while rc = 0
  'extract /curline/'
  deze = curline.3
  if substr(deze,start,lengt) = prev
  then do
    'delete 1'
  end
  else 'down 1'
  prev = substr(deze,start,lengt)
end
":" cursor.3
exit

vraag:
 parse arg q
 say q
 parse pull v
 return v

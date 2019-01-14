/* write all visible lines to a (given) file */
trace n
parse arg ds
if ds = "" then ds = "savvis.txt"
'set scope display'
'extract /shadow/'
'set shadow off'
'top'
'down 1'
do while rc=0
  'extract /curline/'
  queue curline.3 
  'down 1'
end
"execio" queued() "diskw" ds
'set shadow' shadow.1
return

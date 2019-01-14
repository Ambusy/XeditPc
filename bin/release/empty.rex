/* delete all empty lines */
trace n
'top'
'down 1'
do while rc = 0
  'extract /curline/'
  if curline.3 = ''
  then "del 1"
  else "down 1"
end
exit

/* strip trailing spaces from all lines */
trace n
'top'
'down 1'
do while rc = 0
  'extract /curline/'
  deze = strip(curline.3,"T")
  "REPLACE" deze
  if deze = "Bot of file" 
  then rc = 1
  else "DOWN 1"
end
exit


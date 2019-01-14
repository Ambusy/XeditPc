/* save  all visible lines in a file */
trace n
parse arg f
'top'
'down 1'
'stack 99999'
'execio * diskw' f
exit

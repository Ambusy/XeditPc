/*a */
"top"
"down 1"
do while rc = 0
  "stack 1"
  pull l
  l = word(l,words(l))
  "R" l
  "DOWN 1"
END
"top"
exit

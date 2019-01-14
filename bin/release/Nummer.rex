/* add a sort of sequencenr in specified colunms of the visible source*/
/* eg NUMMER 72 8 10 10 to put VM-CMS std. sequence-nrs               */
trace n
parse arg start lengt begnt inc .
"EXTRACT /LINE/SHADOW"
"SET SHADOW OFF"
do while datatype(start) <> 'NUM' ; start=vraag('position:'); end
do while datatype(lengt) <> 'NUM' ; lengt=vraag('n° of chars:'); end
do while datatype(begnt) <> 'NUM' ; begnt=vraag('1st value'); end
do while datatype(inc) <> 'NUM' ; inc=vraag('increment:'); end
'top'
'down 1'
do while rc = 0
   x=right(copies('0',lengt)||begnt,lengt)
   begnt = begnt + inc
   "EXTRACT /CURLINE/"
   l =  curline.3
   l = substr(l,1,start-1) || x || substr(l,start+lengt)
   'REPLACE' l
   'down 1'
end
':'line.1
"SET SHADOW" shadow.1
exit

vraag:
 parse arg q
 say q
 parse pull v
 return v

/* calc length of IBM-cobol structure */
trace n
'set scope display'
'extract /cursor/line/'
grp = '' /* COMP / COMP-3 from 1st level field */
nl = 0
':' cursor.3
'stack 1'
if rc = 0 then do
   pull . 7 slev line 73 .
   call lined
   call grp_ind
   nl = nl + 1
   lv.nl = slev
   occ.nl = 1
   call grp_occ
   len = 0
   klaar = 'n'
   do while klaar = 'n'
      klaar = 'j'
      'down 1'
      if rc = 0 then do
         'stack 1'
         pull . 7 lev line 73 .
         do while (left(lev,1) = '*' | lev > 49 | lev = ' ') & rc = 0
            'down 1'
            if rc = 0 then do
               'stack 1'
               pull . 7 lev line 73 .
             end
          end
      end
      if rc = 0 then do
          if index(line, "REDEFINES")>0 then do
             mlev = lev
             'down 1'
             if rc = 0 then do
                'stack 1'
                pull . 7 lev line 73 .
             end
             do while rc=0 & lev > mlev
               'down 1'
               if rc = 0 then do
                  'stack 1'
                  pull . 7 lev line 73 .
                end
             end
          end
      end
      if rc = 0 then do
         if lev > slev then do
            klaar = 'n'
            line = strip(line)
            do while right(line,1) <> "." & rc = 0
               'down 1'
               'stack 1'
               pull . 7 x 73 .
               line = line strip(x)
            end
            call lined
            do while lv.nl >= lev
               nl = nl - 1
            end
            nl = nl + 1
            lv.nl = lev
            occ.nl = 1
            call grp_occ
            i = nl - 1
            occ.nl = occ.nl * occ.i
            if left(line,4) = 'PIC ' | left(line,8) = 'PICTURE '
            then line = ' ' line
            if index(line,' PIC ')>0 | index(line,' PICTURE ')>0 then do
               if index(line,' PIC ')>0
               then parse value line with . ' PIC ' pic
               if index(line,' PICTURE ')>0
               then parse value line with . ' PICTURE ' pic
               parse value pic with p1 p2 .
               if right(p1,1) = '.' then p1=left(p1,length(p1)-1)
               i=index(p1,'V')
               if i>0 then p1=left(p1,i-1)||substr(p1,i+1)
               i=index(p1,'(')
               do while i > 0
                  parse value p1 with pv '(' pn ')' pz
                  p1 = pv || copies(right(pv,1),pn-1) || pz
                  i=index(p1,'(')
               end
               if left(p1,1) = 'S' then p1=substr(p1,2)
               if index(p2,'PACKED') > 0 then p2 = 'COMP-3'
               else if index(p2,'BINARY') > 0 then p2 = 'COMP  '
               if left(p2,4) = 'COMP' | left(grp,4) = 'COMP' then do
                  if left(p2,4) <> 'COMP' then p2 = grp
                  if index(p2,'-3')>0 then do  /* COMP-3 */
                     l = (length(p1)+2)%2
                  end
                  else do      /* COMP */
                     if length(p1)> 4
                     then l = 4
                     else l = 2
                  end
               end
               else do /* DISPLAY */
                  l = length(p1)
               end
               len = len + l * occ.nl
            end
            else do /* no PIC */
               call grp_ind
            end
         end
      end
   end
end
if len > 0 then 'msg Length =' len
':'line.1
exit

grp_ind:
   grp = ''
   line = strip(line)
   if index(line,' COMP') > 0 then do
      if index(line,'-3') > 0
      then grp = 'COMP-3'
      else grp = 'COMP'
   end
   if index(line,' PACKED') > 0 then do
      grp = 'COMP-3'
   end
   if index(line,' BINARY') > 0 then do
      grp = 'COMP'
   end
   return

grp_occ:
   parse value line with . ' OCCURS ' noc to noc2 .
   if noc <> '' then do
      if to = 'TO' then noc = noc2
      occ.nl = noc
   end
   return

lined:
   line = strip(line)
   if right(line,1) = '.'
   then line = left(line,length(line)-1)
   return            

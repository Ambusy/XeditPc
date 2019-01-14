/*         CLocate column-target    ( :N     or     /Text/  )
                   makes the column specified by the column target the column
                   pointer value.  Searching begins at the column pointer
                   position in the current line.  If SET STREAM ON (the initial
                   setting) is in effect searching continues beyond the current
                   line.  */
trace n
rc = 0
parse arg lp
lp = strip(lp)
if left(lp,1) = ":" then do
   "extract /curline/"
   n = substr(lp,2)
   "CURSOR FILE" curline.2 n
end
else do
  "extract /cursor/stream/curline/"
  if cursor.3 = curline.2  /* start on curline or continue on cursorpos of curline? */
  then sp = cursor.4 + 1
  else sp = 1
  p = substr(curline.3,sp)
  push lp                              /* parse the target  */
  'MACRO PARSE 1 TARGET'               /* With parse macro  */
  if rc<>0 then exit rc
  pull nbl
  if nbl<>1 then exit rc
  pull targstart targlen               /* get the target parms  */
  target=substr(lp,targstart,targlen)  /* Get the actual target and remainder */
  i = index(p, target)
  if i > 0 then do                     /* on current line */
    "CURSOR FILE" curline.2 (i + sp - 1) 
  end
  else do                              /* try find it elsewhere */
    i = stream.1
    upper i
    if i <> "OFF" then
       "LOCATE" lp
    else
       rc = 4
  end
end
exit rc

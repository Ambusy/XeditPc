/*         CDELETE [column target|1]
                   deletes characters from the current line, beginning at the
                   column pointer and deleting up to the column target.  SET
                   STREAM ON (the initial setting) allows text on multiple
                   lines to be deleted. */
trace i
parse arg lp
"Extract /cursor/curline"  
sl = curline.2
sp = cursor.4
if lp = "" then lp = ":1"
"Clocate" lp
if rc = 0 then do
  "Extract /cursor/curline/"  
  nsl = curline.2
  nsp = cursor.4 - 1
  if sl < nsl | sp < nsp then do 
    "selectset on" sl sp nsl nsp
    "MCut"
  end
end
rr = rc
"locate :" sl
exit rc





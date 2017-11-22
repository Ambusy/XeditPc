/* inserts a file at current line. GET file [F firstline] [N number of lines] */
trace n
"EXTRACT /Curline/size/"
if size.1 < curline.2 then ":" size.1
parse upper arg p
p = strip(p)
if left(p,1) = '"' then 
   parse var p '"' fn '"' r1 r2 r3 r4
else
   parse var p fn r1 r2 r3 r4
f = 1
n = 999999999
if r1 = "F" then
   f = r2
if r1 = "N" then
   n = r2
if r3 = "F" then
   f = r4
if r3 = "N" then
   n = r4
"EXTRACT /LINEND/"
"SET LINEND OFF"
InPath = STREAM(fn,'C','QUERY EXIST')
if InPath = '' then do
   "MSG File not found" fn
   exit 4
end
LineNumber = 0
nl = LINES(fn)  
if nl > (f - 1 + n) then nl = f - 1 + n
do i=1 to nl
   Text = LINEIN(fn) 
   LineNumber = LineNumber + 1
   if LineNumber >= f then 
      "INPUT" text  
end
call STREAM(fn,"C","CLOSE") 
"SET LINEND" linend.1
exit                   

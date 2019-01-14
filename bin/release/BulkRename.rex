/* BULK RENAME path begin_file_name extensie old new */
trace n
parse arg path
path = strip(path)
if left(path,1) = '"' then  
   parse var path '"' path '"' fname ext old new .
else if left(path,1) = "'" then  
   parse var path "'" path "'" fname ext old new .
else
   parse var path path fname ext old new .
fn = fname
upper fn
lfn = length(fn)
nn = 0
'cms dir "'|| path||'\*.'||ext||'" >C:\bulkrename.temp'
"xedit c:\bulkrename.temp"
"locate :1"
do while rc = 0
  "extract /curline/"
  parse var curline.3 . . . fnamei 
  fni = left(fnamei,lfn)
  upper fni
  if fni = fn then do
   nn = nn + 1
   nm.nn = fnamei
   p = lastpos(".",fnamei)
   nnr = new + substr(fnamei,lfn+1,p-lfn-1) - old 
   do while length(nnr) < (p-lfn-1)
     nnr = "0" || nnr
   end
   nmn.nn = fname||nnr||right(fnamei,4)
  end
  "down 1"
end
"quit"
"ERASE C:\bulkrename.temp"
"ERASE c:\bulkrename.bat"
do i=1 to nn
  queue 'ren "'|| path || "\" || nm.i || '" "' || nmn.i || '"'
end
"execio * diskw c:\bulkrename.bat 1"
"CMS c:\bulkrename.bat"
"ERASE c:\bulkrename.bat"
exit

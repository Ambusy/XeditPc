/* Rename many files in a map according to certain rules */
/* here any bla bla bla 99999.jpg is renamed in 99999.jpg where 99999 is any number */
say "Folder (e.g. C:\Pictures\video)"
pull folder
if folder = "" then exit
"extract /temp/"   /* get user's TEMP directory */
dsn = temp.1||"file.list"
say "executing command DIR" folder||"\*.jpg >"||dsn
address cmd "cmd /C " "DIR" folder||"\*.jpg >"||dsn
call STREAM(dsn,"C","OPEN READ")
n = lines(dsn)
do i=1 to n
  l = LINEIN(dsn) 
  upper l
  p = pos(".JPG", l)
  if p>0 then do
    if datatype(substr(l,p-5,5)) = "NUM" then do
      parse var l . . . file
      filen = right(file,9)
      if file <> filen then do
        say "executing command REN" '"'||folder||'\'||file||'"' '"'||filen||'"'
        address cmd "cmd /C " "REN" '"'||folder||'\'||file||'"' '"'||filen||'"'
      end
    end
  end
end
call STREAM(dsn,"C","CLOSE")
exit
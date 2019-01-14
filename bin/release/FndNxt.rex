/* find next */
trace n
'EXTRACT /LINE/CURSOR/WRAP/FNAME/'
'SET WRAP OFF'
msg = ''
':' cursor.3
'stack 1'
if rc = 0 then do
   pull line
   j1 = cursor.4
   j2 = cursor.4
   do i=cursor.4 by -1 until substr(line,i,1) = ' '
      j1 = i 
   end
   do i=cursor.4 until substr(line,i,1) = ' ' 
      j2 = i                                  
   end
   n = strip(substr(line,j1,j2-j1+1))
   upper n
   '/'||n
   if rc > 0 then do
      ':' line.1
   end
   else do
      'GLOBALV SELECT' fname.1 'GET NZS'
      if nzs= '' then nzs = 0
      if verify(nzs,'0123456789')>0 then nzs = 0
      nzs = nzs + 1
      nzsl.nzs = line.1
      nzsc.nzs = cursor.3
      nzsp.nzs = cursor.4
      'GLOBALV SELECT' fname.1 'PUTp NZS' nzs
      'GLOBALV SELECT' fname.1 'PUTp NZSL.'||nzs
      'GLOBALV SELECT' fname.1 'PUTp NZSC.'||nzs
      'GLOBALV SELECT' fname.1 'PUTp NZSP.'||nzs
   end
end
'SET WRAP' wrap.1
if msg <> '' then 'MSG' msg
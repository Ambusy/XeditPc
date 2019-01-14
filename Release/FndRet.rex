/* zoek de plaats terug van een perform naampje */
trace n
'EXTRACT /LINE/CURSOR/FNAME/'
'GLOBALV SELECT' fname.1 'GET NZS'
if nzs= '' then nzs = 0
if verify(nzs,'0123456789')>0 then nzs = 0
msg = ''
if nzs = 0 then do
   msg = 'Can''t return'
end
else do
   'GLOBALV SELECT' fname.1 'GET NZSL.'||nzs
   'GLOBALV SELECT' fname.1 'GET NZSC.'||nzs
   'GLOBALV SELECT' fname.1 'GET NZSP.'||nzs
   ':' nzsl.nzs
   'cursor file' nzsc.nzs nzsp.nzs
   nzs = nzs - 1
   'GLOBALV SELECT' fname.1 'PUTp NZS' nzs
end
if msg <> '' then 'MSG' msg
exit

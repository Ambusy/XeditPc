/* bewaar de plaats van de cursor */
trace n
'EXTRACT /LINE/CURSOR/FNAME/'
'GLOBALV SELECT' fname.1 'GET NZS'
if nzs= '' then nzs = 0
if verify(nzs,'0123456789')>0 then nzs = 0
nzs = nzs + 1
nzsl.nzs = line.1
nzsc.nzs = cursor.3
nzsp.nzs = cursor.4
'GLOBALV SELECT' fname.1 'PUTp NZS'  
'GLOBALV SELECT' fname.1 'PUTp NZSL.'||nzs
'GLOBALV SELECT' fname.1 'PUTp NZSC.'||nzs
'GLOBALV SELECT' fname.1 'PUTp NZSP.'||nzs

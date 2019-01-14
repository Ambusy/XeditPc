/* pastes the word copied by WRDGET on the cursor pos */
trace n
'EXTRACT /CURSOR/LINE/'
":"cursor.3
'stack 1'
if rc = 0 then do
   'globalv get N'
   parse pull line
   j1 = cursor.4
   l = substr(line,1,j1-1)||n||substr(line,j1)
   'REPLACE' strip(l,'T')
end
":"LINE.1
exit

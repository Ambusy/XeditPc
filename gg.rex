/* */
trace n
parse arg pref sc line
if pref <> 'PREFIX' then do
   'MSG Op prefif intype'
   exit
end
if sc = 'CLEAR' then exit
'EXTRACT /SCOPE/LINE/'
'SET SCOPE ALL'
':' line
'GLOBALV GET PPNL'
if '0'||ppnl = 0 then do
   'MSG Eerst PP 2 *'
end
do i=1 to ppnl
   'GLOBALV GET PP.'||i
   'INPUT' pp.i ||' '
end
':' line.1
'SET SCOPE' scope.1
'CURSOR FILE' (line+1) 1
exit

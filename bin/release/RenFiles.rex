/* renumber files, like from Foto camera's */
/* eg. IMnnnn.jpg */
/* RenFiles "c:\hp\photo\" IM 1 201    attn: don't forget the closing \ */
/* renames all files starting with IM increasing the number by 200 */
trace n
parse upper arg '"' p '"' f v t .
if p = "" then do
   say "enter path"
   pull p
   if p = "" then p = ".\"
end
if f = "" then do
   say "enter file start letters"
   pull f
end
if v = "" then do
   say "enter file start number"
   pull v
end
if t = "" then do
   say "enter file new start number"
   pull t
end
"erase a.$$$"
'cms dir "' || p || f || '*.*" >a.$$$'
"XEDIT a.$$$"
"TOP"
"DOWN 1"
do while rc = 0
  "extract /curline/"
  parse upper var curline.3 a b c d .
  if left(d,length(f)) = f then do
     parse var d . (f) on "." ft
     fo = p || f || on || "." || ft 
     nn = on + t - v
     if length(nn) < length(on) 
     then nn = right("0000000000"||nn,length(on))
     fn =  f || nn || "." || ft 
     lower fn
     say 'rename "' || fo ||'" "'|| fn || '"'
     'cms rename "' || fo ||'" "'|| fn || '"'
  end
  "DOWN 1"
end
"qquit"
exit

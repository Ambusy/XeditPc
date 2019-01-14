/* REXX */
/* parse arg on stack, and extract locate-string */
trace n
pull a
if a="" then exit 5
j = 0
k = 0
l = length(a)
if l < 2 then exit 5
st = 0
le = -1
do i=1 to l
   if st = 0 & substr(a,i,1) <> " " then do
      st = i
      leave i
   end
end
do i=l to st by -1
   if le = -1 & substr(a,i,1) = substr(a,st,1) then do
      le = i - st + 1
      leave i
   end
end
if st = 0 | le = -1 then exit 5
push st le
push 1
exit

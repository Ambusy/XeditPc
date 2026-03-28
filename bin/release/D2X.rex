/* decimal to hex */
d2x: procedure
parse upper arg x
  tr = "0123456789ABCDEF"
  res = ""
  do while x > 0 
     cf = x // 16
     x = x % 16
     res = substr(tr,cf+1,1) || res
  end
  if length(res) // 2 <> 0 then res = "0" || res
  return res
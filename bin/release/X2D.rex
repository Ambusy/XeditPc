/* hex to decimal */
x2d: 
  parse upper arg x
  tr = "0123456789ABCDEF"
  res = 0
  mul = 1
  do i=length(x) to 1 by -1                     
     dg = substr(x,i,1)                          
     cf = index(tr, dg) - 1                     
     res = res + cf * mul                      
     mul = mul * 16                            
  end
  return res
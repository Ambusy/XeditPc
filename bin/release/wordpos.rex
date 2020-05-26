/* wordpos(needle,haystack) */
wordpos: procedure expose  
   trace n
   parse arg ndl, hst  
   t1 = space(ndl,1)
   nw = words(t1)
   t2 = space(hst,1) 
   do i=1 to words(t2)  
      if subword(t2,1,nw) = t1 then return i 
      t2 = delword(t2,1,1)
   end    
   return 0     
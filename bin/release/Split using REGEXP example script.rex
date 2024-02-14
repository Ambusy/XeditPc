/* splitting a string on a character using regexp */
trace i 
tab='09'X
s = "aaa"|| tab||"bbb"||tab||"ccc"||tab||"ddd"
call split(s,tab) 
say #regexp.0 #regexp.1 #regexp.2 #regexp.3 #regexp.4
exit 
split: 
  parse arg #string, #char 
  #regexp = "[^"||#char||"]*"; 
  #retc = REGEXP("#REGEXP", #string, #regexp); 
  return #retc 
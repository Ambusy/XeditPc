/* saves or retrieves a variable v in environment s */ 
parse arg w s v vl 
if w = 'GET' then do
    "globalv select" s "GET" v   
    x=value(v) 
    push x
    return
end 
interpret v "= '"||vl||"'"
"globalv select" s "PUT" v 
return  
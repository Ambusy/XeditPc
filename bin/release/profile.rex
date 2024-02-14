/* my defaultsettings */
trace n
"SET STAY ON"
"SET LRECL 32670"
"EXTRACT /FTYPE/"
upper ftype.1
if ftype.1 = "EXE" | ftype.1 = "DLL"  | ftype.1 = "JPG"  then do
  "SET RECFM F"
  "SET LRECL 64"
end
if ftype.1 = "WAV"   then do
  "SET RECFM F"
  "SET LRECL 25"
  "SET v 1 25 h 1 25"
end
if ftype.1 = "CBL"   then do
  "SET PF04 FndSect"
  "SET PF05 FndRet"
  "SET PF06 FndDecl"
  "SET PF02 SavePos"
  "SET reserved -2        F1 Help - 2 SavPos - 3 Quit - 4 FndSect - 5 FndRet - 6 FndDecl - 11 SpltJoin - 12 Recall "
  "SET reserved -1        Ctrl-C Copy - X Cut - V Paste - Z Undo "
  "SET VIEW 7 *"
end
if ftype.1 = "RPGI" then do
  "SET PF04 FndNxt"
  "SET PF05 FndRet"
  "SET PF06 FndDecl"
  "SET PF02 SavePos"
  "SET reserved 26       F1 Help - 2 SavPos - 3 Quit - 4 FndNxt - 5 FndRet - 6 FndDecl - 11 SpltJoin - 12 Recall "
  "SET reserved 27        Ctrl-C Copy - X Cut - V Paste - Z Undo "
  "SET VIEW 7 *"
end
"SET PF01 GETHELP"
"SET CTRL-F DoLocate"
"SET CTRL-E macro exp"
"SET CTRL-M macro expi" /* remove optional (include) */
"SET CTRL-D macro expd" /* remove clause */
"SET CTRL-N macro expn" /* select next clause */
"SET CTRL-TAB Macro expn"
"SET CTRL-A SelAll"
"SET CTRL-8 CInsCursor {" 
"SET CTRL-9 CInsCursor }" 
"SET ALT-9 FndBracket" 
"SET CTRL-½ macro CtrlMin"   /* key - on italian keyboard */
"SET CTRL-» macro CtrlPlus"  /* key + on italian keyboard */
exit 

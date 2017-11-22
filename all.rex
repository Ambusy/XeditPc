/*************************************************************/
/*                                                           */
/*                                                           */
/*                                                           */
/*  COPYRIGHT -                                              */
/*              5684-112 (C) COPYRIGHT IBM CORP. 1983, 1991  */
/*              LICENSED MATERIALS - PROPERTY OF IBM         */
/*              SEE COPYRIGHT INSTRUCTIONS, G120-2083        */
/*                                                           */
/*  Syntax:           : meaning:                             */
/*                                                           */
/*   ALL /loc_str/    : shows all lines containing "loc_str" */
/*   ALL |/loc_str/   : also shows lines containing "loc_str"*/
/*   ALL &/loc_str/   : shows lines containing "loc_str"     */
/*                      in those lines visible on the screen */
/*   ALL ^/loc_str/   : shows lines not containing "loc_str" */
/*                      in those lines visible on the screen */
/*                                                           */
/*************************************************************/
/*-----------------------------------------------------------*/
  Trace n
  parse arg targparm                /* Parse the target      */
  name = "ALL"
  'COMMAND EXTRACT /LINE/'
  if targparm = ''  then signal novalue  /* Is this 'RESET'? */

  t = left(strip(targparm),1)
  if t = '|' then call or_all
  if t = '&' then call and_all
  if t = '^' then call not_all
  call all_all
  exit
all_all:
  push targparm                     /* No, parse the target  */
  'MACRO PARSE 1 TARGET'            /* With parse macro      */
  rcparse = rc                      /* Parse error case      */
  extra = targparm                  /*   set up error msg    */
  if rc = 5  then signal invdel
  if rc<>0 then signal badparm
  pull nbl
  if nbl<>1 then signal badparm
  pull targstart targlen            /* get the target parms  */
/*                       Get the actual target and remainder */
  target=targparm
  extra=''
  if extra <> '' then signal badparm
  linex = line.1
  'COMMAND TOP'
  'COMMAND EXTRACT /SCOPE/'
  'COMMAND SET SCOPE ALL'           /* Get the entire file   */
  'COMMAND PRESERVE'
  'COMMAND SET VERIFY OFF'          /* Scrap unwanted output */
  'COMMAND LOCATE' target           /* Is the target there?  */
  saverc=rc
  if rc<>0 then do                  /* No, exit quickly      */
     'COMMAND LOCATE :'line.1
     'COMMAND MSG Not found'
     'COMMAND RESTORE'
     'COMMAND SET SCOPE' scope.1
     exit 2
     end
/* At this point the target has been verified and located at */
/* least once.  Now we loop looking for more.                */
  'COMMAND EXTRACT /LINE/'         /* This is new current    */
  line=line.1                      /* line after processing  */
  'COMMAND TOP'
  'COMMAND SET SELECT 1 *'         /* Reset all select levels*/
  'COMMAND TOP'
  'COMMAND SET MSGMODE OFF'
  'COMMAND SET WRAP OFF'
  'COMMAND LOCATE :'line           /* Return to the first one*/
  'COMMAND SET SELECT 0 1'         /* And place it in the set*/
  'COMMAND LOCATE' target          /* Second search          */
  if rc=0 then do                  /* If found ...           */
     'COMMAND EXTRACT /LINE/'      /* See where we are       */
     if line.1<>line then do       /* If it's not equal, then*/
        do until rc<>0             /* ... loop until no more */
           'COMMAND SET SELECT 0 1'
           'COMMAND LOCATE' target
           end
        end
     end
  'COMMAND SET SCOPE ALL'
  'COMMAND :'linex                 /* Move to the first one  */
  'COMMAND RESTORE'
  'COMMAND SET DISPLAY' 0 0        /* And show chosen lines  */
  'COMMAND SET SCOPE DISPLAY'      /* Force SCOPE DISPLAY    */
  exit
and_all:   
  targparm  = substr(targparm,2)
  push targparm                     /* No, parse the target  */
  'MACRO PARSE 1 TARGET'            /* With parse macro      */
  rcparse = rc                      /* Parse error case      */
  extra = targparm                  /*   set up error msg    */
  if rc = 5  then signal invdel
  if rc<>0 then signal badparm
  pull nbl
  if nbl<>1 then signal badparm
  pull targstart targlen            /* get the target parms  */
/*                       Get the actual target and remainder */
  target=substr(targparm,targstart,targlen)
  extra=delstr(targparm,targstart,targlen)
  'COMMAND TOP'
  'COMMAND EXTRACT /SCOPE/'
  'COMMAND SET SCOPE DISPLAY'       /* Get the visible file  */
  'COMMAND PRESERVE'
  'COMMAND SET VERIFY OFF'          /* Scrap unwanted output */
  'COMMAND LOCATE' target           /* Is the target there?  */
  saverc=rc
  if rc<>0 then do                  /* No, exit quickly      */
     'COMMAND LOCATE :'line.1
     'COMMAND MSG Not found'
     'COMMAND RESTORE'
     'COMMAND SET SCOPE' scope.1
     exit 2
     end
/* At this point the target has been verified and located at */
/* least once.  Now we loop looking for more.                */
  'COMMAND EXTRACT /LINE/'         /* This is new current    */
  line=line.1                      /* line after processing  */
  liner=line.1                      /* return */
  'COMMAND TOP'
  'COMMAND SET MSGMODE OFF'
  'COMMAND SET WRAP OFF'
  'COMMAND LOCATE :1'              /* tof*/
  'COMMAND SET SELECT 2' line-1    /* take not containing out of set */
  'COMMAND LOCATE :'line           /* 1st */
  'COMMAND LOCATE' target          /* Second search          */
  do while rc = 0                  /* ... loop until no more */
     'COMMAND EXTRACT /LINE/'         /* This is new current    */
     line2=line.1                      /* line after processing  */
     'COMMAND LOCATE :'line + 1    /* after contaiing */
     'COMMAND SET SELECT 2' line2 - line - 1
     line = line2
     'COMMAND LOCATE :'line2       /* after 2nd contaiing */
     'COMMAND LOCATE' target
   end
  'COMMAND LOCATE :'line + 1    /* after contaiing */
  'COMMAND SET SELECT 2 *'
  'COMMAND RESTORE'
  'COMMAND LOCATE :'liner
  'COMMAND SET DISPLAY' 0 0        /* And show deleted lines */
  exit
not_all:   
  targparm  = substr(targparm,2)
  push targparm                     /* No, parse the target  */
  'MACRO PARSE 1 TARGET'            /* With parse macro      */
  rcparse = rc                      /* Parse error case      */
  extra = targparm                  /*   set up error msg    */
  if rc = 5  then signal invdel
  if rc<>0 then signal badparm
  pull nbl
  if nbl<>1 then signal badparm
  pull targstart targlen            /* get the target parms  */
/*                       Get the actual target and remainder */
  target=substr(targparm,targstart,targlen)
  extra=delstr(targparm,targstart,targlen)
  'COMMAND TOP'
  'COMMAND EXTRACT /SCOPE/'
  'COMMAND EXTRACT /LINE/'         /* This is new current    */
  liner=line.1                     /* return point  */
  'COMMAND SET SCOPE DISPLAY'       /* Get the visible file  */
  'COMMAND PRESERVE'
  'COMMAND SET VERIFY OFF'          /* Scrap unwanted output */
  'COMMAND LOCATE' target           /* Is the target there?  */
  saverc=rc
  if rc<>0 then do                  /* No, exit quickly      */
     'COMMAND LOCATE :'liner
     'COMMAND RESTORE'
     'COMMAND SET SCOPE' scope.1
     exit 2
   end
/* At this point the target has been verified and located at */
/* least once.  Now we loop looking for more.                */
  'COMMAND TOP'
  'COMMAND EXTRACT /LINE/'         /* This is new current    */
  line=line.1                      /* line after processing  */
  'COMMAND SET MSGMODE OFF'
  'COMMAND SET WRAP OFF'
  'COMMAND LOCATE' target          /* Second search          */
trace n
  do while rc=0                    /* ... loop until no more */
     'COMMAND EXTRACT /LINE/'  
     'COMMAND SET SELECT 2 1'
     'COMMAND LOCATE' target
  end
  'COMMAND LOCATE :'liner           /* Return to the first one*/
  'COMMAND RESTORE'
  'COMMAND SET DISPLAY' 0 0        /* And show selected lines */
  exit
or_all:
trace n
  targparm  = substr(targparm,2)
  push targparm                     /* No, parse the target  */
  'MACRO PARSE 1 TARGET'            /* With parse macro      */
  rcparse = rc                      /* Parse error case      */
  extra = targparm                  /*   set up error msg    */
  if rc = 5  then signal invdel
  if rc<>0 then signal badparm
  pull nbl
  if nbl<>1 then signal badparm
  pull targstart targlen            /* get the target parms  */
/*                       Get the actual target and remainder */
  target= targparm 
  'COMMAND TOP'
  'COMMAND EXTRACT /SCOPE/'
  'COMMAND SET SCOPE ALL'           /* Get the entire file   */
  'COMMAND PRESERVE'
  'COMMAND SET VERIFY OFF'          /* Scrap unwanted output */
  'COMMAND LOCATE' target           /* Is the target there?  */
  saverc=rc
  if rc<>0 then do                  /* No, exit quickly      */
     'COMMAND LOCATE :'line.1
     'COMMAND MSG Not found'
     'COMMAND RESTORE'
     'COMMAND SET SCOPE' scope.1
     exit 2
     end
/* At this point the target has been verified and located at */
/* least once.  Now we loop looking for more.                */
  'COMMAND EXTRACT /LINE/'         /* This is new current    */
  line=line.1                      /* line after processing  */
  'COMMAND TOP'
  'COMMAND SET MSGMODE OFF'
  'COMMAND SET WRAP OFF'
  'COMMAND LOCATE :'line           /* Return to the first one*/
  'COMMAND SET SELECT 0 1'         /* And place it in the set*/
  'COMMAND LOCATE' target          /* Second search          */
  if rc=0 then do                  /* If found ...           */
     'COMMAND EXTRACT /LINE/'      /* See where we are       */
     if line.1<>line then do       /* If it's not equal, then*/
        do until rc<>0             /* ... loop until no more */
           'COMMAND SET SELECT 0 1'
           'COMMAND LOCATE' target
           end
        end
     end
  'COMMAND :'line                  /* Move to the first one  */
  'COMMAND RESTORE'
  'COMMAND SET DISPLAY' 0 0        /* And show chosen lines  */
  'COMMAND SET SCOPE DISPLAY'      /* Force SCOPE DISPLAY    */
  exit
NOVALUE:
  'COMMAND SET SCOPE ALL'          /* Get all the file lines */
  'COMMAND PRESERVE'
  'COMMAND SET VERIFY OFF'
  'COMMAND TOP'
  'COMMAND SET SELECT 0 *'         /* Make them all zero     */
  'COMMAND RESTORE'
  'COMMAND SET DISPLAY 0 0'        /* New display level      */
  'COMMAND LOCATE :'line.1         /* Return to the old line */
  'COMMAND SET SCOPE DISPLAY'      /* Reset scope.           */
  exit
BADPARM:
  'COMMAND emsg Bad argument'
  exit 5
INVDEL:
  'COMMAND EMSG Int. error'
  exit 5

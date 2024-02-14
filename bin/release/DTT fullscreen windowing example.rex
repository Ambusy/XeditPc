/* REXX  FULL SCREEN data entry: fill-in form to call DITTO to copy part of a VSAM file*/
/* note that scripts DTTL and DT are not included, as this is only to show FULLSCREEN as example*/
/*    the invoked DITTO utility exists only in VM/CMS with a VSE/DOS system */
/*                                                                          */
/* create the screen with init_screen                                       */
/* create the labels and textboxes                                          */
/* Literals (= Label): lit row col color text                               */
/* Fields (Textbox)  : vld row col color variablename type max-length fill  */
/*     row can be absolute, relative or same (symbol *)                     */
/*     col can be absolute, relative or adjacent (symbol *)                 */
/*     color = blue red pink green turquoise yellow white default (is white)*/
/*     variablename is the name if the variable that contains               */
/*                  the initial value when "screen" is called               */
/*                  the final value when "screen" has finished              */
/*     type = X of N                                                        */
/*     fill = L or T and a paddingchar. L for leading, T for trailing,      */
/*          the paddingchar will be removed  from the final value           */
/*          the T type paddingchar will also be removed while typing a value*/
/* Cursor_pos contains the initial and final cursor position                */
/* assign initial values and call the screen                                */
/* the variabels will contain the final values, pfkey is the termination key*/
/*                                                                          */
/* on line 247 the @tonen routine shows how to use the original XEDIT cmds  */
/*                                                                          */
call init_screen PFKEY3
call lit ' 1  2 red DITTO copy VSAM to CMS'
call lit ' * 55 green' date()
call lit ' * 70 green' time()
call lit ' 4  2 green Volume:'
call vld ' *  * white vol X 6 T_'
call lit ' +1  2 green Dsn:'
call vld ' *  * white dsn X 44 T_'
call lit ' +1 2 green CMS-File:'
call vld ' *  * white fn X 8 T_'
call vld ' *  +2 white ft X 8 T_'
call lit ' +2 2 green number of records to copy:'
call vld ' *  * white nrecs N 6 L0'
call lit ' +1 2 green number of records to skip:'
call vld ' *  * white skip N 6 L0'
call lit ' +2 2 red PF1 lists all fienames'
call lit ' +1 2 red A name may end in * to list a group of files.'
 
'globalv select dittovgz get vol'
if vol = '' then vol = 'TST...'
old_vol = vol
'globalv select dittovgz get dsn'
if dsn = '' then dsn = ' '
old_dsn = dsn
'globalv select dittovgz get fn'
if fn = '' then fn = ' '
old_ft = ft
'globalv select dittovgz get ft'
if ft = '' then ft = 'DATA'
old_ft = ft
nrecs = 999999
skip = 0
 
call screen
if pfkey = 'PFKEY1' | pos('*',dsn)>0 then do /* create list of satisfying files */
   if pos('*',dsn)>0 then x = dsn
   else x = ''
   'exec dttl' x
   if queued() >0 then do
      pull vol
      pull dsn
      Cursor_pos = '6 12'
      call screen
      call doDitto
   end
end
else do
   call doDitto
end
exit
 
doDitto:
   if pfkey = 'ENTER' then do
      if old_vol <> vol then 'globalv select dittovgz putp vol'
      if old_dsn <> dsn then 'globalv select dittovgz putp dsn'
      if old_fn <> fn then 'globalv select dittovgz putp fn'
      if old_ft <> ft then 'globalv select dittovgz putp ft'
      'EXEC DT' vol dsn fn ft nrecs skip
   end
   return

/* own checks, will be called once for each field */
controle:
if pfkey = 'ENTER' then do
    parse upper arg fld, valu
    if fld = 'FN' & valu = '' then do
       msg = 'Fill'; return 1
    end
    if fld = 'VOL' & valu = '' then do
       msg = 'Fill'; return 1
    end
    if fld = 'DSN' & valu = '' then do
       msg = 'Fill'; return 1
    end
 end
 return 0  /* all ok */

/* ******* Following routines to be included without changes at all *************************** */
init_screen:
  parse arg @endkey  /* pf-key waarop geen controles meer plaatsvinden */
  'VMFclear'
  @nv = 0   /* aantal lit en vld */
  @curg = 'N' /* cursor op 1e veld */
  @curs = '1 1' /*  voor als er geen input-vld is */
  msg = ' '  /*   geen message op scherm */
  return

lit:
  parse arg @l @c @kl @t /* lijn kol kleur tekst */
  @nv = @nv + 1
  @lv.@nv = 'L'
  @lv.@nv.@txt = @t
  @lv.@nv.@leng = length(@t)
  call @scrps @l @c @kl length(@t)
  return

vld:
  parse arg @l @c @kl @v @t @n @f /* lijn kol kleur veldnaam type lengte fillchar */
  @nv = @nv + 1
  @lv.@nv = 'V'
  @lv.@nv.@veld = @v
  @lv.@nv.@type = @t
  @lv.@nv.@leng = @n
  if @f = '' then @f = 'T ' /* default: trailing spaces */
  @lv.@nv.@strip= left(@f,1)
  @lv.@nv.@stripc= substr(@f,2,1)
  call @scrps @l @c @kl @n
  if @curg = 'N' then Cursor_pos = @lv.@nv.@lnr @lv.@nv.@cnr /* cursor */
  @curg = 'J'
  return

@scrps: /* scherm posities afhendelen */
  parse arg @l @c @kl @n /* lijk kol kleur lengte */
  @lv.@nv.@lnr = @l
  @lv.@nv.@cnr = @c
  if @l = '*' then do
     if @nv < 1 then call @erre @nv 'lijn niet eerder opgegeven'
     @x = @nv - 1
     @l = @lv.@x.@lnr
  end
  if left(@l,1) = '+' then do
     if @nv < 1 then call @erre @nv 'lijn niet eerder opgegeven'
     @x = substr(@l,2)
     if datatype(@x)<>"NUM" then call @erre @nv 'lijn fout'
     @y = @nv - 1
     @l = @lv.@y.@lnr + @x
  end
  if @l < 1 | @l > 24 then do /* let op lijn 23 = message */
     call @erre @nv 'lijn niet tussen 1 en 24'
  end
  @lv.@nv.@lnr = @l
  if @c = '*' then do
     if @nv < 1 then call @erre @nv 'kolom niet eerder opgegeven'
     @x = @nv - 1
     @c = @lv.@x.@cnr + @lv.@x.@leng + 1
  end
  if left(@c,1) = '+' then do
     if @nv < 1 then call @erre @nv 'kolom niet eerder opgegeven'
     @x = substr(@c,2)
     if datatype(@x)<>"NUM" then call @erre @nv 'kolom fout'
     @y = @nv - 1
     @c = @lv.@y.@cnr + @lv.@x.@leng + 1 + @x
  end
  if datatype(@n)<>"NUM" then call @erre @nv 'aantal scherm-posities kan niet'
  if @c < 2 | (@c - @n) > 79 then do
     call @erre @nv 'kolom niet tussen 2 en 79'
  end
  @lv.@nv.@cnr = @c
  @lv.@nv.@klr = translate(@kl," ","_")
  return

@erre: /* einde na fout */
  parse arg @i @ft
  say @ft
  say 'regelnr = ' @i
  if @lv.@i = 'L'
  then say '(l,c)=('@lv.@i.@lnr','@lv.@i.@cnr')' @lv.@i.@txt
  else say '(l,c)=('@lv.@i.@lnr','@lv.@i.@cnr')' @lv.@i.@veld
  exit 16

@get_assgn: /* haal waarde van een veld op */
  parse arg @vld
  return value(@vld)

@put_assgn: /* geef veld een waarde (quotes eerst verdubbelen) */
  parse arg @vag @wag
  @iag = index(@wag,"'")
  if @iag > 0 then do
     @yag = ''
     do while @iag > 0
        @yag = @yag || substr(@wag,1,@iag)"'"
        @wag = substr(@wag,@iag+1)
        @iag = index(@wag,"'")
     end
     @wag = @yag||@wag
  end
  interpret @vag"='"@wag"'"
  return

screen: /* toon scherm en handel controles af */
  @error = 1
  do while @error = 1
     @error = 0
     call @tonen
     if pfkey <> @endkey then call @controle
  end
  return

@controle: /* eigen controles */
  @curg = 'N'   /* cursor bij OK op 1e veld */
  do @i = 1 to @nv while @error = 0
    if @lv.@i = 'V' then do
       if @curg = 'N' then @curx = @lv.@i.@lnr @lv.@i.@cnr /* cursor pos zonder          fouten*/
       @curg = 'J'
       if @lv.@i.@type = 'X' then do
          @x = @get_assgn(@lv.@i.@veld)
          upper @x
          call @put_assgn @lv.@i.@veld @x
       end
       if @lv.@i.@type = 'N' then do
          @x = @get_assgn(@lv.@i.@veld)
          if datatype(@x) <> "NUM" then do
             Cursor_pos = @lv.@i.@lnr @lv.@i.@cnr /* cursor op fout */
             msg = 'Niet numeriek'
             @error = 1
          end
       end
    end
  end
  /* user-controles */
  do @i = 1 to @nv while @error = 0
    if @lv.@i = 'V' then do
       @x = @get_assgn(@lv.@i.@veld)
       @x = controle(@lv.@i.@veld,@x)
       if @x = 1 then do
          Cursor_pos = @lv.@i.@lnr @lv.@i.@cnr
          @error = 1
       end
    end
  end
  if @curg = 'N' then Cursor_pos = @curx
  return

@tonen: /* scherm display en oppakken van ingetypte waarden */
  'VSCREEN DEFINE scherm  24 80 0 0'
  'WINDOW DEFINE scherm  24 80 1 1'
  'WINDOW SHOW scherm ON scherm'
  do @i=1 to 24 /* eerste alles protect */
     'VSCREEN WRITE scherm ' @i ' 1 80 (PROT FIELD  '
  end
  do @i = 1 to @nv /* dan de lit en vld erop plaatsen */
    if @lv.@i = 'L' then do
       'VSCREEN WRITE scherm' @lv.@i.@lnr @lv.@i.@cnr-1 0,
        '(' @lv.@i.@klr 'PROT FIELD' @lv.@i.@txt
    end
    else do
       @x = strip(@get_assgn(@lv.@i.@veld),'T') /* fill-char */
       if @lv.@i.@strip = 'T'
       then @x = @x|| copies(@lv.@i.@stripc,@lv.@i.@leng - length(@x))
       else @x = copies(@lv.@i.@stripc,@lv.@i.@leng - length(@x)) || @x
       @y =value("prot_"@lv.@i.@veld) /* veld protected ? */
       if @y = 'J' then @y = 'PROT'; else @y = ''
       'VSCREEN WRITE scherm' @lv.@i.@lnr @lv.@i.@cnr-1 @lv.@i.@leng+1 ,
        '(' @lv.@i.@klr @y 'FIELD' @x
    end
  end
  'VSCREEN CURSOR scherm' Cursor_pos
  'VSCREEN WRITE scherm 23 1 0 (red PROT FIELD' msg
  msg = ' ' /* msg maar een keer */
  'VSCREEN WAITREAD scherm'
  if waitread.0=0 then exit 16
  pfkey = word(waitread.1,1)|| word(waitread.1,2)
  cursorposition = waitread.2
  do @i=3 to waitread.0
     @rk = word(waitread.@i,2) word(waitread.@i,3)
     if word(waitread.@i,1) = 'DATA' then do
        do @j = 1 to @nv
           if @lv.@j = 'V' then do
              if @lv.@j.@lnr @lv.@j.@cnr-1 = @rk then do
                 @x = strip(strip(subword(waitread.@i,4),"T"," "),@lv.@j.@strip,@lv.@j.@stripc)
                 if @lv.@j.@strip = "L" & @lv.@j.@stripc = "0" & @x = "" then @x = "0"  /leave at least one zero */
                 call @put_assgn @lv.@j.@veld @x
              end
           end
        end
     end
  end
  'VSCREEN DELETE scherm'
  'WINDOW DELETE scherm'
  return
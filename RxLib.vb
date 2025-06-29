Imports VB6 = Microsoft.VisualBasic
Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions
#Const CreRexxLogFile = False

Public Class Rexx
    Dim RexxRuns As New Collection
    Private tmpIntCode As New Collection, PrevTmpCode As Integer ' temporary executable code while compiling
    Private IntPp As Integer ' ix in asm codetable
    Private RexxFileName As String = ""
    Friend sayFile As StreamWriter ' new name for each execution
    Private eofRexxFile As Boolean
    Private nErr As Integer
    Private DecimalSepPt As Boolean ' . is decimal separator and not ,
    Private cSymb As Symbols ' current symbol
    Private cChara As Char ' first char after cSymb
    Private prChara As Char ' char preceding cChara
    Private cId As String = "" ' current identifier
    Private DefVars As DefVariable ' current definition variable structure
    Private VariaRuns As VariabelRun ' current execution variable structure
#If DEBUG Then
    Private asrc As String = "", asy As String = "" ' for debugging
#End If
    Private RcComp As Integer ' internal RC
    Private RexxPathElements() As String
    Public MemorySource As String
    Private Itre As New Collection ' iterate names and indexes
    Private ItreX As New Collection
    Private Leave As New Collection ' leave names and indexes
    Private LeaveX As New Collection
    Private NumY As Double ' num. value of last checked number: if numeric, numY contains the float value
    Private SrcLstLin As Integer ' current linenumber
    Private UpCase As Boolean ' next parse wants uppercase?
    Private LwCase As Boolean ' next parse wants lowercase?
    Private iCurrParSeqNr, nDo, iParSeqNr, iMaxParSeqNr As Integer ' numbers to generate unique internal names
    Private GenTemp As Boolean ' generate temp code?
    Private CharAftId, CharBefId As Char ' char before and after current identifier
    Private Streams As New Collection ' open streams
    Private ncChar As String = "" ' added symbol at end-of-line
    Private sSrcPos, sSrcLine As Integer
    Private NrStepsExecuted As Integer = 0
    Private EntriesInIntcode As Integer = 1 ' nr existing entries in IntCode
    Private CultInf As New CultureInfo("en-US", False)
    Public CurrRexxRun As RexxCompData ' storage to correctly execute one rexx program
    Friend Stack As New Collection ' execution currRexxRun.Stack
    Friend Shared SayLine(24) As String ' say buffer for all routines
    Friend Shared lSay, nSay As Integer
    Friend Shared RexxWords As New Collection ' symbols for compiler
    Friend Shared RexxFunctions(48) As String ' builtin functions with parameter definitions
    Friend Shared SymsStr(78) As String ' names of symbols for errors
    Friend Shared SysMessages As New Collection ' text of messages
    Public Shared QStack As New Collection ' Queue/Pull currRexxRun.Stack
    Public Shared CancRexx As Boolean ' User requested end of all active rexx-procs
    Public Shared RexxHandlesSay As Boolean = True ' True: I handle say and pull, false Caller handles them
    Public Shared RexxTrace As Boolean = False ' True: each rexx starts with a Trace ?N
    Friend Shared CallDepth As Integer ' nesting of rexx programs, on 1: init or finish variables / buffers
    Event doCmd(ByVal env As String, ByVal c As String, ByVal e As RexxEvent)
    Event doCancel()
    Event doSay(ByVal s As String)
    Event doPull(ByRef s As String)
    Event doStep()
    Private Const sAtoZcap As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
    Private Const sAtoZlow As String = "abcdefghijklmnopqrstuvwxyz"
    Private Const s0to9 As String = "0123456789"
    Private Const s0to9pm As String = "0123456789+-"
    Private Const sAtoZS As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz@$£#_"
    Private Const sAtoZS_0to9 As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz@$£#_0123456789"
    Private Const sAtoZ As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
    Private Const sAtoZ_0to9 As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
    Private Const ExtRoutineTagstring = "External Routine Parameters: "
    Private Const ExtRoutineParmSep = "0501040203"

    Friend InputBx As New InputBoxDialog()

    Public Enum Symbols ' Rexx symbols
        addresym = 1
        ands
        argsym
        becomes
        bysym
        callsym
        colon
        comclose
        comma
        comopen
        concat
        dosym
        dropsym
        elsesym
        endprog
        endsym
        eql
        eqlstr
        exitsym
        exposesym
        forsym
        geq
        geqstr
        gtr
        gtrstr
        ident
        idiv
        ifsym
        itersym
        itpsym
        leavesym
        leq
        leqstr
        lowersym
        lparen
        lss
        lssstr
        minus
        moddiv
        nbrsym
        neq
        neqstr
        nopsym
        notsym
        ors
        otherwisesym
        parsesym
        period
        plus
        powr
        procsym
        pullsym
        pushsym
        questmk
        queuesym
        retsym
        rparen
        saysym
        selectsym
        semicolon
        signalsym
        slash
        thensym
        times
        tosym
        tracesym
        txtsym
        untilsym
        uppersym
        valuesym
        varsym
        whensym
        whilesym
        withsym
    End Enum
    Enum fct ' internal assembly operation codes
        opr ' operation on stacked value(s)
        '                           code[].a = type of operation
        lod ' load var on currRexxRun.Stack
        lodc ' load compound variable on currRexxRun.Stack
        sto ' store on currRexxRun.Stack code[].a = index in IdName
        '                                code[].l = 0: my level, 1 Next level (params only)
        stoc ' store compound variable from currRexxRun.Stack code[].a = index in IdName
        '                                code[].l = 0: my level, 1 Next level (params only)
        jmp ' jump                       code[].a = idx in interpretcode 
        jbr ' jump to builtin routine    code[].a = idx in interpretcode  
        jcf ' jump if condition false    code[].a = idx in interpretcode 
        lin ' linenrs:              code[].a = Linnr
        drp ' drop                  code[].a = index in IdName
        '                           code[].l = 1 if group-drop
        tra ' trace                 code[].a = 1:N 2:R 3:I
        cll ' call parameter def    code[].a = index in IdName
        '                           code[].l = seqnr of parameter "P l"
        adr ' Address name          code[].a = idx of name
        cal ' call                  code[].a = idx of name to call, substituted by index in interpretcode, after compile
        '                           code[].l = nr of arguments in CALL
        ret ' return
        exi ' exit
        lbl ' label def.            code[].a = level for local vars
        '                           code[].l = ident | procsym
        cle ' call external rex     code[].a = idx of name to call
        arg ' ARG and helpers.      code[].a = nr arguments in ARG
        '                           code[].l = seq nr of the parameter  
        upp ' uppercase             code[].a = 1: UPPER = 2: LOWER(is my extension of rexx)
        parp ' ARG +n               code[].a = +/-number
        '                           code[].l = seq nr in ARG
        parc ' ARG n                code[].a = number
        '                           code[].l = seq nr in ARG
        parv ' ARG variable         code[].a = index in IdName
        '                           code[].l = seq nr in ARG
        parl ' ARG literal          code[].a = index in literalpool
        '                           code[].l = seq nr in ARG
        parh ' ARG (variable)       code[].a = index in IdName
        '                           code[].l = seq nr in ARG
        pul ' pull (like ARG)
        pvr ' parse var (like ARG)
        pvl ' parse value (like ARG)
        stk ' push/queue            code[].l =0:push; =1:queue
        say ' say
        upc ' upper                 code[].l = 1
        exc ' external call
        sig ' signal novalue        code[].a = 1: on 0: off
        jct ' jump if condition true code[].a = idx in interpretcode 
        itp ' interpret
    End Enum
    Enum tpSymbol ' type of identifier
        tpUnknown = -1
        tpProcedure = 1
        tpVariable
        tpConstant
        tpCompoundVar
    End Enum
#If CreRexxLogFile Then
    Dim fn As String = System.IO.Path.GetTempFileName
    Friend logFile As StreamWriter = File.CreateText(fn & ".Rexx.Log.txt") ' new name for each execution
#Else
    Friend logFile As StreamWriter
#End If
    Friend IndentSpace As Integer = 0
    <Conditional("CreRexxLogFile")>
    Friend Sub Logg(ByVal s As String)
        Dim i As Integer
        i = s.IndexOf(" "c)
        If i > 0 Then
            If s.Length() > i + 5 Then
                If s.Substring(i + 1, 5) = "start" Then IndentSpace += 2
            End If
        End If
        logFile.WriteLine(" ".PadRight(IndentSpace) & s)
        Debug.WriteLine(s)
        If i > 0 Then
            If s.Length() > i + 3 Then
                If s.Substring(i + 1, 3) = "end" Then
                    IndentSpace -= 2
                    If IndentSpace < 0 Then IndentSpace = 0
                End If
            End If
        End If
    End Sub
    Function ExistsProgramFile(RexxFileName As String) As String
        Dim fnd As Boolean
        If File.Exists(RexxFilePath + "\" + RexxFileName) Then
            RexxFileName = Path.GetFullPath(RexxFilePath + "\" + RexxFileName)
            Logg("found " + RexxFileName)
            fnd = True
        Else
            RexxPathElements = Split(RexxPath, ";"c)
            For i = 0 To RexxPathElements.Length() - 1
                RexxPathElements(i) = RexxPathElements(i).Trim()
                If RexxPathElements(i) <> "" Then
                    If RexxPathElements(i)(RexxPathElements(i).Length - 1) <> "\" Then
                        RexxPathElements(i) += "\"
                    End If
                    If RexxPathElements(i)(0) = "%" Then
                        Dim parts() As String = Split(RexxPathElements(i), "%"c)
                        If parts.Length > 2 Then
                            Dim s As String = Environment.GetEnvironmentVariable(parts(1))
                            RexxPathElements(i) = s + parts(2)
                        End If
                    End If
                    Logg("try : " + RexxPathElements(i) + RexxFileName)
                    Logg("    : " + Path.GetFullPath(RexxPathElements(i) + RexxFileName))
                    If File.Exists(RexxPathElements(i) + RexxFileName) Then
                        RexxFileName = Path.GetFullPath(RexxPathElements(i) + RexxFileName)
                        Logg("found " + RexxFileName)
                        fnd = True
                        Exit For
                    End If
                End If
            Next
        End If
        If Not fnd Then ' default path
            Logg("foundd " + ExecutablePath & RexxFileName)
            RexxFileName = ExecutablePath & RexxFileName
        End If
        Return RexxFileName
    End Function
    Public Function CompileRexxScript(ByVal Filename As String) As Integer
        Dim k, i, j, cLn As Integer
        Dim asm As New AsmStatement
        Dim np As Integer
        Dim Code As AsmStatement
        Logg("Comp start")
        If RexxWords.Count() = 0 Then
            DecimalSepPt = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator = "."
            Logg("Comp decPt = . : " & CStr(DecimalSepPt))
            FillRexxWords()
        End If
        RcComp = 0
        RexxFileName = Filename
        If Filename <> "In memory source" Then
            If Right(RexxFileName, 4).ToUpper(CultInf) <> ".REX" Then RexxFileName = RexxFileName & ".REX"
            If InStr(RexxFileName, "\") = 0 Then
                Logg("RexxPath = : " & RexxPath)
                RexxFileName = ExistsProgramFile(RexxFileName)
            End If
        End If
        Dim Interpreting As Boolean = False  ' first file
        If Not (CurrRexxRun Is Nothing) Then
            Interpreting = CurrRexxRun.InInterpret
        End If
        Dim minDate As Date = Now()
        If RcComp = 0 And Not Interpreting Then
            CurrRexxRun = Nothing
            For Each rs As RexxCompData In RexxRuns
                If Not rs.InExecution Then
                    If RexxFileName = rs.fileName Then
                        CurrRexxRun = rs
                        Logg("Already compiled and not active" & RexxFileName)
                        If Filename <> "In memory source" Then
                            If System.IO.File.GetLastWriteTime(RexxFileName) > rs.fileTstamp OrElse rs.CompRc <> 0 Then
                                Logg("Recompile " & RexxFileName)
                                RcComp = -2
                            End If
                        Else
                            RcComp = -2
                        End If
                        CurrRexxRun.UseStamp = Now()
                        Exit For
                    Else
                        If minDate > rs.UseStamp Then
                            minDate = rs.UseStamp
                        End If
                    End If
                End If
            Next
            If CurrRexxRun Is Nothing Then
                CurrRexxRun = New RexxCompData
                CurrRexxRun.fileName = RexxFileName
                Try
                    CurrRexxRun.fileTstamp = System.IO.File.GetLastWriteTime(RexxFileName)
                Catch ex As Exception
                    CurrRexxRun.fileTstamp = Now()
                End Try
                RcComp = -1
            End If
        End If
        Dim RcIi As Integer = RcComp
        If RcComp < 0 Or Interpreting Then ' -1: to be compiled. -2: recompile
            CurrRexxRun.SrcLine = 0
            CurrRexxRun.SrcPos = -1
            If Not Interpreting Then
                CurrRexxRun.Source.Clear()
                RcComp = ReadSource(CurrRexxRun.Source, RexxFileName)
            Else
                eofRexxFile = False
                CurrRexxRun.ItpSource.Clear()
                RcComp = ReadSource(CurrRexxRun.ItpSource, RexxFileName)
            End If
            If RcComp <> 0 Then
                If Filename <> "$ESC$" Then
                    SigErrorF(124, "")
                End If
                CurrRexxRun = Nothing
                RcIi = 999 ' stop
            Else
                If RcIi = -1 Then
                    CurrRexxRun.UseStamp = Now()
                    If RexxRuns.Count >= 99 Then
                        For ix As Integer = 1 To RexxRuns.Count()
                            Dim rs = DirectCast(RexxRuns(ix), RexxCompData)
                            If Not rs.InExecution AndAlso minDate = rs.UseStamp Then
                                Logg("Substitute " & rs.fileName & " by " & CurrRexxRun.fileName)
                                RexxRuns.Remove(ix)
                                RexxRuns.Add(CurrRexxRun)
                                If RexxRuns.Count < 99 Then
                                    Exit For
                                End If
                            End If
                        Next
                    Else
                        Logg("Add " & CurrRexxRun.fileName)
                        RexxRuns.Add(CurrRexxRun)
                    End If
                ElseIf Filename <> "In memory source" Then
                    CurrRexxRun.fileTstamp = System.IO.File.GetLastWriteTime(RexxFileName)
                End If
            End If
            CallDepth += 1
            If CallDepth = 1 Then CancRexx = False
            If RcComp = 0 Then
                If Not Interpreting Then ' if not an INTERPRET
                    Logg("Comp file " & RexxFileName)
                    nErr = 0
                    nDo = 0
                    iParSeqNr = 100 ' seq. nr for temp variables to transer parametervalues
                    iMaxParSeqNr = 0
                    iCurrParSeqNr = 0
                    EntriesInIntcode = 1
                    CurrRexxRun.InteractiveTracing = 0
                    ' // create own variables and literals
                    CurrRexxRun.ProcNum = 0 ' Main proc
                    CurrRexxRun.IdExpose = New Collection
                    CurrRexxRun.IdExposeStk.Clear()
                    CurrRexxRun.IdExposeStk.Add(CurrRexxRun.IdExpose)
                    i = StoreLiteral("0")
                    CurrRexxRun.iRes = SourceNameIndexPosition("RESULT", tpSymbol.tpVariable, DefVars)
                    CurrRexxRun.iRc = SourceNameIndexPosition("RC", tpSymbol.tpVariable, DefVars)
                    CurrRexxRun.iSigl = SourceNameIndexPosition("SIGL", tpSymbol.tpVariable, DefVars)
                Else
                    EntriesInIntcode = CurrRexxRun.IntCode.Count()
                    Logg("Comp INTERPRET file " & RexxFileName)
                End If
            End If
            If RcComp = 0 Then
                CurrRexxRun.SrcLine = 1
                If Not Interpreting Then
                    CurrRexxRun.IntCode.Clear()
                End If
                Logg("Comp compile rexx ")
                CurrRexxRun.SrcPos = -1
                eofRexxFile = False
                cChara = " "c
                GetNextSymbol()
                If RexxTrace Then
                    GenerateAsm(fct.lin, 0, 0)
                    GenerateAsm(fct.tra, 1, 3) ' halt for debug
                End If
                While (cSymb <> Symbols.endprog And cSymb <> Symbols.procsym)
                    CompileOneStatement()
                End While
                While (cSymb = Symbols.procsym)
                    CompileOneStatement()
                    While (cSymb <> Symbols.endprog And cSymb <> Symbols.procsym)
                        CompileOneStatement()
                    End While
                End While
            End If
            If RcComp = 0 Then
                Logg("Comp insert JMPs ")
                If CurrRexxRun.IntCode.Count() > 0 And Not Interpreting Then
                    Code = DirectCast(CurrRexxRun.IntCode.Item(CurrRexxRun.IntCode.Count()), AsmStatement)
                    If Code.f <> fct.exi Then ' exit present at end? else generate one
                        GenerateAsm(fct.lod, tpSymbol.tpConstant, 1)
                        GenerateAsm(fct.sto, 0, CurrRexxRun.iRes)
                        GenerateAsm(fct.exi, 0, 1)
                    End If
                End If
                For i = EntriesInIntcode + 1 To CurrRexxRun.IntCode.Count() ' insert call addresses 
                    asm = DirectCast(CurrRexxRun.IntCode.Item(i), AsmStatement)
                    If asm.f = fct.lin Then cLn = asm.l ' report linenumber in case of errors
                    If asm.f = fct.sig Then ' replace labelname by index in currRexxRun.IntCode
                        Dim nrVars = CurrRexxRun.IdName.Count()
                        Dim xi = SourceNameIndexPosition("NOVALUE", tpSymbol.tpProcedure, DefVars) ' Find NOVALUE: label
                        If CurrRexxRun.IdName.Count() <= nrVars Then ' not added now, so this is where to go in case of novalue
                            asm.a = xi
                            DefVars = DirectCast(CurrRexxRun.IdName.Item(asm.a), DefVariable)
                            For j = 1 To CurrRexxRun.IxProcName.Count()
                                If CStr(CurrRexxRun.IxProcName.Item(j)) = DefVars.Id Then
                                    k = CInt(CurrRexxRun.IxProc.Item(j)) - 1
                                    asm.a = k
                                End If
                            Next
                        End If
                    End If
                    If asm.f = fct.sto Then
                        If asm.l > 0 Then
                            DefVars = DirectCast(CurrRexxRun.IdName.Item(asm.l), DefVariable)
                            Dim fndIx As Boolean = False
                            For j = 1 To CurrRexxRun.IxProcName.Count()
                                If CStr(CurrRexxRun.IxProcName.Item(j)) = DefVars.Id Then
                                    k = CInt(CurrRexxRun.IxProc.Item(j)) - 1
                                    Dim asm2 As AsmStatement = DirectCast(CurrRexxRun.IntCode.Item(k), AsmStatement)
                                    If asm2.f = fct.lin Then
                                        asm2 = DirectCast(CurrRexxRun.IntCode.Item(k + 1), AsmStatement)
                                    End If
                                    fndIx = True
                                    If asm2.l = Symbols.procsym Then
                                        asm.l = 1 ' procedure
                                    Else
                                        asm.l = 0 ' label is not a procedure
                                    End If
                                End If
                            Next
                            If Not fndIx Then
                                asm.l = 0 ' builtin or external
                            End If
                        End If
                    End If
                    If asm.f = fct.cal Then ' replace routinename by index in currRexxRun.IntCode
                        If asm.l = -1 Then asm.f = fct.jmp ' signal: jmp, not cal !     XXXXX
                        DefVars = DirectCast(CurrRexxRun.IdName.Item(asm.a), DefVariable)
                        asm.a = 0
                        For j = 1 To CurrRexxRun.IxProcName.Count()
                            If CStr(CurrRexxRun.IxProcName.Item(j)) = DefVars.Id Then
                                k = CInt(CurrRexxRun.IxProc.Item(j)) - 1
                                asm.a = k ' is a user defined procedure
                            End If
                        Next
                        If asm.a = 0 Then ' routine not found yet
                            For j = 1 To UBound(RexxFunctions, 1)
                                If Mid(RexxFunctions(j), 12) = DefVars.Id Then
                                    asm.f = fct.jbr ' is a builtin routine
                                    asm.a = j
                                    np = 0 ' check nr of parameters
                                    While np < 10 And Mid(RexxFunctions(j), 3 + np, 1) <> " "
                                        np = np + 1
                                    End While
                                    If asm.l > np Then
                                        SigErrorF(126, Mid(RexxFunctions(j), 12) & ": " & GetSLin(cLn))
                                    End If
                                    If asm.l < CInt(Mid(RexxFunctions(j), 1, 2)) Then
                                        SigErrorF(125, Mid(RexxFunctions(j), 12) & ": " & GetSLin(cLn))
                                    End If
                                    Exit For
                                End If
                            Next
                        End If
                        If asm.a = 0 Then ' routine not found yet
                            Dim fn As String = ExistsProgramFile(DefVars.Id & ".REX")
                            If File.Exists(fn) Then ' is an external rexx routine
                                asm.a = StoreLiteral(fn)
                                asm.f = fct.cle
                            Else
                                DefVars.Id = "--> " & fn
                            End If
                        End If
                        If (asm.a = 0) Then ' routine not found 
                            SigErrorF(123, (DefVars.Id))
                        End If
                    End If
                Next
            End If
            CallDepth -= 1
            If RcIi <> 999 Then CurrRexxRun.CompRc = RcComp
#If CreRexxLogFile Then
            Logg("Generated code:")
            Dim sts As Integer = 1
            If Interpreting Then
                sts = EntriesInIntcode + 1
            End If
            If Not IsNothing(CurrRexxRun) Then

                For i = sts To CurrRexxRun.IntCode.Count()
                    asm = DirectCast(CurrRexxRun.IntCode.Item(i), AsmStatement)
                    Logg(i & " " & DumpStr(asm))
                Next
                Logg("Literalpool:")
                For i = 1 To CurrRexxRun.TxtValue.Count()
                    Logg(i & " """ & CStr(CurrRexxRun.TxtValue.Item(i)) & """")
                Next
                Logg("Variablepool:") '         
                For i = 1 To CurrRexxRun.IdName.Count()
                    DefVars = DirectCast(CurrRexxRun.IdName.Item(i), DefVariable)
                    Logg(i & " " & CStr(DefVars.Id))
                Next
            End If
#End If
        End If
        Logg("Comp end " & CStr(RcComp))
        Return RcComp
    End Function
    Private Sub CompileOneStatement()
        Dim j0, cx0, i, cx1, j1 As Integer
        Dim vSymb As Symbols
        Dim vId As String
        Dim codeUntil, jmpc, LoopVar As String
        Dim wsto, wsby, wsfor As Boolean
        Dim jf, wstoi, j, wsfori, wsbyi As Integer
        Dim svs As Symbols
        If cSymb = Symbols.semicolon Then
            GetNextSymbol()
        Else
            GenerateLinenumberIndication()
            If (cSymb = Symbols.ident) Then
                vId = cId
                GetNextSymbol()
                If (cSymb = Symbols.colon) Then ' label(s)
                    iParSeqNr = iMaxParSeqNr + 1
                    i = SourceNameIndexPosition(vId, tpSymbol.tpProcedure, DefVars)
                    If DefVars.Kind = tpSymbol.tpVariable And i = CurrRexxRun.IdName.Count() Then
                        DefVars.Kind = tpSymbol.tpProcedure
                    Else
                        If DefVars.Kind <> tpSymbol.tpProcedure Then SigError(105)
                    End If
                    CurrRexxRun.IxProcName.Add(vId) ' name of proc
                    CurrRexxRun.IxProc.Add(CurrRexxRun.IntCode.Count() + 1) ' start of asm code
                    GenerateLinenumberIndication()
                    GetNextSymbol()
                    If (cSymb = Symbols.procsym) Then
                        CurrRexxRun.ProcNum = CurrRexxRun.ProcNum + 1
                        GenerateAsm(fct.lbl, Symbols.procsym, CurrRexxRun.ProcNum)
                    Else
                        GenerateAsm(fct.lbl, 0, 0)
                    End If
                ElseIf (cSymb = Symbols.eql) Then  ' assign
                    i = SourceNameIndexPosition(vId, tpSymbol.tpVariable, DefVars)
                    If DefVars.Kind <> tpSymbol.tpVariable Then
                        SigError(111)
                    End If
                    GetNextSymbol()
                    Condition() 'Expression()
                    If vId.substring(vId.length - 1) = "." Then
                        GenerateAsm(fct.stoc, 0, i)
                    Else
                        GenerateAsm(fct.sto, 0, i)
                    End If
                    TestSymbolExpected(Symbols.semicolon, 118)
                ElseIf (cSymb = Symbols.semicolon) Then  ' variabel command
                    i = SourceNameIndexPosition(vId, tpSymbol.tpVariable, DefVars)
                    GenerateAsm(fct.lod, tpSymbol.tpVariable, i)
                    GenerateAsm(fct.exc, CurrRexxRun.iRc, 0)
                ElseIf cSymb = Symbols.ident Or cSymb = Symbols.txtsym Or cSymb = Symbols.nbrsym Or cSymb = Symbols.lparen Then  ' command in form of expression
                    i = SourceNameIndexPosition(vId, tpSymbol.tpVariable, DefVars)
                    GenerateAsm(fct.lod, tpSymbol.tpVariable, i)
                    If CharBefId = " " Then
                        GenerateAsm(fct.lod, tpSymbol.tpConstant, StoreLiteral(" "))
                        GenerateAsm(fct.opr, 0, 8) ' concat
                    End If
                    Expression()
                    GenerateAsm(fct.opr, 0, 8) ' concat
                    GenerateAsm(fct.exc, CurrRexxRun.iRc, 0)
                    TestSymbolExpected(Symbols.semicolon, 118)
                End If
            ElseIf (cSymb = Symbols.selectsym) Then  ' select when ...
                ToggleKeywordIsActive("WHEN", True)
                ToggleKeywordIsActive("OTHERWISE", True)
                GetNextSymbol()
                jmpc = ""
                SkipOverSemicolon()
                While (cSymb = Symbols.whensym)
                    GenerateLinenumberIndication()
                    GetNextSymbol()
                    Condition()
                    SkipOverSemicolon()
                    If (cSymb <> Symbols.thensym) Then
                        SigError(114)
                    Else
                        GetNextSymbol()
                        GenerateAsm(fct.jcf, 0, 0) ' jmp if false
                        cx0 = CurrRexxRun.IntCode.Count()
                        SkipOverSemicolon()
                        CompileOneStatement()
                        GenerateAsm(fct.jmp, 0, 0) ' jmp if true
                        jmpc = jmpc & CStr(CurrRexxRun.IntCode.Count()) & " "
                        GenerateJumpAddress(cx0, 1)
                    End If
                End While
                If (cSymb = Symbols.otherwisesym) Then
                    GenerateLinenumberIndication()
                    GetNextSymbol()
                    SkipOverSemicolon()
                    While (cSymb <> Symbols.endsym And cSymb <> Symbols.endprog)
                        CompileOneStatement()
                    End While
                End If
                ToggleKeywordIsActive("WHEN", False)
                ToggleKeywordIsActive("OTHERWISE", False)
                If (cSymb <> Symbols.endsym) Then
                    SigError(115)
                Else
                    GetNextSymbol()
                End If
                While (jmpc.Length() > 0)
                    GenerateJumpAddress(CInt(NxtWordFromStr(jmpc)), 1)
                End While
            ElseIf (cSymb = Symbols.ifsym) Then  ' if ...
                GetNextSymbol()
                Condition()
                GenerateAsm(fct.jcf, 0, 0) ' jmp if false
                cx0 = CurrRexxRun.IntCode.Count()
                SkipOverSemicolon()
                If (cSymb = Symbols.thensym) Then
                    GetNextSymbol()
                    SkipOverSemicolon()
                    CompileOneStatement()
                    SkipOverSemicolon()
                    If (cSymb = Symbols.elsesym) Then
                        GenerateAsm(fct.jmp, 0, 0) ' jmp if true
                        cx1 = CurrRexxRun.IntCode.Count()
                        GetNextSymbol()
                        SkipOverSemicolon()
                        GenerateJumpAddress(cx0, 1)
                        CompileOneStatement()
                        SkipOverSemicolon()
                        GenerateJumpAddress(cx1, 1)
                    Else
                        GenerateJumpAddress(cx0, 1)
                    End If
                Else
                    SkipTo(114)
                End If
            ElseIf (cSymb = Symbols.dosym) Then ' do ..............
                Dim TypeDo, RetAdr As Integer
                ActivateDOstatKeywords(True)
                sSrcLine = CurrRexxRun.SrcLine ' backup to previous position in file
                sSrcPos = CurrRexxRun.SrcPos
                GetNextSymbol()
                nDo = nDo + 1
                LoopVar = CStr(nDo)
                j0 = StoreLiteral("0") ' num literal 0
                j1 = StoreLiteral("1") ' num literal 1
                codeUntil = ""
                RetAdr = CurrRexxRun.IntCode.Count()
                TypeDo = 0
                If cSymb = Symbols.semicolon Then ' simple do
                    TypeDo = 1
                ElseIf cSymb = Symbols.ident And cId = "FOREVER" Then  '  do forever loop
                    TypeDo = 2
                    GetNextSymbol()
                    RetAdr = CurrRexxRun.IntCode.Count() + 1
                ElseIf cSymb = Symbols.ident Then  '  do i    or   do i=
                    i = SourceNameIndexPosition(cId, tpSymbol.tpVariable, DefVars)
                    LoopVar = cId
                    GetNextSymbol()
                    If cSymb = Symbols.eql Then ' do i=.... 
                        TypeDo = 3
                        GetNextSymbol()
                        Expression()
                        GenerateAsm(fct.sto, 0, i) ' initial value
                        wsby = False
                        wsfor = False
                        wsto = False
                        While (cSymb = Symbols.tosym Or cSymb = Symbols.forsym Or cSymb = Symbols.bysym)
                            j = SourceNameIndexPosition(Left(cId, 1).ToLower() & " " & CStr(nDo), tpSymbol.tpVariable, DefVars)
                            svs = cSymb
                            GetNextSymbol()
                            Expression()
                            GenerateAsm(fct.sto, 0, j) ' final value
                            If (svs = Symbols.tosym) Then
                                wstoi = j
                                wsto = True
                            ElseIf (svs = Symbols.forsym) Then
                                wsfori = j
                                wsfor = True
                                jf = SourceNameIndexPosition("n " & CStr(nDo), tpSymbol.tpVariable, DefVars)
                                GenerateAsm(fct.lod, tpSymbol.tpConstant, j1)
                                GenerateAsm(fct.sto, 0, jf) ' For count = 1
                            ElseIf (svs = Symbols.bysym) Then
                                wsbyi = j
                                wsby = True
                            End If
                        End While
                        If wsto Then 'initial value exceeds end value?
                            GenerateAsm(fct.lod, tpSymbol.tpVariable, wstoi) ' to
                            GenerateAsm(fct.lod, tpSymbol.tpVariable, i) ' LVn  
                            If wsby Then
                                GenerateAsm(fct.lod, tpSymbol.tpVariable, wsbyi) ' by
                                GenerateAsm(fct.lod, tpSymbol.tpConstant, j0) ' 0
                                GenerateAsm(fct.opr, 0, 16) ' by < 0? 
                                GenerateAsm(fct.jct, 0, CurrRexxRun.IntCode.Count() + 4)
                                GenerateAsm(fct.opr, 0, 19) ' to <= LV?
                                GenerateAsm(fct.jmp, 0, CurrRexxRun.IntCode.Count() + 3)
                                GenerateAsm(fct.opr, 0, 17) ' to >= LV?
                            Else
                                GenerateAsm(fct.opr, 0, 19) ' to <= LV?
                            End If
                            GenerateAsm(fct.jcf, 0, 0)
                            Leave.Add(LoopVar)
                            LeaveX.Add(CurrRexxRun.IntCode.Count())
                        End If
                        If (wsfor) Then ' at least for 1?
                            GenerateAsm(fct.lod, tpSymbol.tpConstant, j1) ' 1
                            GenerateAsm(fct.lod, tpSymbol.tpVariable, wsfori) ' max
                            GenerateAsm(fct.opr, 0, 18) ' if for > 1 then
                            GenerateAsm(fct.jct, 0, 0) ' jump if false
                            Leave.Add(LoopVar)
                            LeaveX.Add(CurrRexxRun.IntCode.Count())
                        End If
                        RetAdr = CurrRexxRun.IntCode.Count() + 1
                    Else  ' do identifier 
                        TypeDo = 4
                        CurrRexxRun.SrcPos = sSrcPos ' backup to before identifier to be able to create an expression
                        CurrRexxRun.SrcLine = sSrcLine
                        cChara = " "c
                        ncChar = ""
                        GetNextSymbol() '  re-read identifier
                        Expression()
                        LoopVar = "LV " & CStr(nDo)
                        i = SourceNameIndexPosition(LoopVar, tpSymbol.tpVariable, DefVars)
                        GenerateAsm(fct.sto, 0, i) ' LVn = start
                        GenerateAsm(fct.lod, tpSymbol.tpVariable, i) ' lv <= 0?
                        GenerateAsm(fct.lod, tpSymbol.tpConstant, j0)
                        GenerateAsm(fct.opr, 0, 17) ' <=
                        GenerateAsm(fct.jct, 0, 0)
                        cx0 = CurrRexxRun.IntCode.Count()
                        RetAdr = CurrRexxRun.IntCode.Count()
                    End If
                ElseIf cSymb <> Symbols.whilesym And cSymb <> Symbols.untilsym Then ' do number
                    TypeDo = 4
                    Condition()
                    LoopVar = "LV " & CStr(nDo)
                    i = SourceNameIndexPosition(LoopVar, tpSymbol.tpVariable, DefVars)
                    GenerateAsm(fct.sto, 0, i) ' LVn = start
                    GenerateAsm(fct.lod, tpSymbol.tpVariable, i) ' lv <= 0?
                    GenerateAsm(fct.lod, tpSymbol.tpConstant, j0)
                    GenerateAsm(fct.opr, 0, 17) ' <=
                    GenerateAsm(fct.jct, 0, 0)
                    cx0 = CurrRexxRun.IntCode.Count()
                    RetAdr = CurrRexxRun.IntCode.Count()
                End If
                If cSymb = Symbols.whilesym Then  '  do ..... while loop
                    If TypeDo = 0 Then TypeDo = 5
                    GetNextSymbol()
                    Condition()
                    GenerateAsm(fct.jcf, 0, 0)
                    Leave.Add(LoopVar)
                    LeaveX.Add(CurrRexxRun.IntCode.Count())
                End If
                Dim GotUntil As Boolean, cPrevTmpCode As Integer
                If cSymb = Symbols.untilsym Then  '  do ....... until loop
                    If TypeDo = 0 Then TypeDo = 6
                    GetNextSymbol()
                    GenTemp = True
                    cPrevTmpCode = tmpIntCode.Count()
                    Condition()
                    GenerateAsm(fct.opr, 0, 11) ' not
                    GenTemp = False
                    GotUntil = True
                Else
                    GotUntil = False
                End If
                DoBlock(LoopVar)
                If TypeDo > 1 Then
                    'en de staarten
                    If GotUntil Then  '  do ....... until  
                        PrevTmpCode = cPrevTmpCode
                        CompileDoUntil(codeUntil, LoopVar)
                    End If
                    If TypeDo = 4 Then ' do n
                        i = SourceNameIndexPosition(LoopVar, tpSymbol.tpVariable, DefVars)
                        GenerateAsm(fct.lod, tpSymbol.tpVariable, i)
                        GenerateAsm(fct.lod, tpSymbol.tpConstant, j1) ' 1
                        GenerateAsm(fct.opr, 0, 3) ' minus
                        GenerateAsm(fct.sto, 0, i) ' LVn = LVn - 1
                        GenerateAsm(fct.lod, tpSymbol.tpVariable, i) ' LVn <= 0?
                        GenerateAsm(fct.lod, tpSymbol.tpConstant, j0) ' 0
                        GenerateAsm(fct.opr, 0, 17) ' <=
                        GenerateJumpAddress(cx0, 2) ' if initial value <= 0
                    ElseIf TypeDo = 5 Then ' do while: only jmmp retadr
                    ElseIf TypeDo = 6 Then ' do until: only jmmp retadr
                    ElseIf TypeDo = 3 Then
                        If wsto Then
                            i = SourceNameIndexPosition(LoopVar, tpSymbol.tpVariable, DefVars)
                            GenerateAsm(fct.lod, tpSymbol.tpVariable, wstoi) ' to
                            GenerateAsm(fct.lod, tpSymbol.tpVariable, i) ' LVn  
                            If wsby Then
                                GenerateAsm(fct.lod, tpSymbol.tpVariable, wsbyi) ' by
                            Else
                                GenerateAsm(fct.lod, tpSymbol.tpConstant, j1) ' incr = 1
                            End If
                            GenerateAsm(fct.opr, 0, 2) ' +
                            GenerateAsm(fct.sto, 0, i) ' LVn  
                            GenerateAsm(fct.lod, tpSymbol.tpVariable, i) ' lv
                            If wsby Then
                                GenerateAsm(fct.lod, tpSymbol.tpVariable, wsbyi) ' by
                                GenerateAsm(fct.lod, tpSymbol.tpConstant, j0) ' 0
                                GenerateAsm(fct.opr, 0, 16) ' by < 0? 
                                GenerateAsm(fct.jct, 0, CurrRexxRun.IntCode.Count() + 4)
                                GenerateAsm(fct.opr, 0, 19) ' to <= LV?
                                GenerateAsm(fct.jmp, 0, CurrRexxRun.IntCode.Count() + 3)
                                GenerateAsm(fct.opr, 0, 17) ' to >= LV?
                            Else
                                GenerateAsm(fct.opr, 0, 19) ' to <= LV?
                            End If
                            GenerateAsm(fct.jcf, 0, 0)
                            Leave.Add(LoopVar)
                            LeaveX.Add(CurrRexxRun.IntCode.Count())
                        Else ' not limited by TO
                            GenerateAsm(fct.lod, tpSymbol.tpVariable, i) ' LVn  
                            If wsby Then
                                GenerateAsm(fct.lod, tpSymbol.tpVariable, wsbyi) ' by
                            Else
                                GenerateAsm(fct.lod, tpSymbol.tpConstant, j1) ' incr = 1
                            End If
                            GenerateAsm(fct.opr, 0, 2) ' +
                            GenerateAsm(fct.sto, 0, i) ' LVn  
                        End If ' if wsto
                        If (wsfor) Then
                            GenerateAsm(fct.lod, tpSymbol.tpVariable, jf) ' for count
                            GenerateAsm(fct.lod, tpSymbol.tpConstant, j1) ' 1
                            GenerateAsm(fct.opr, 0, 2) ' plus
                            GenerateAsm(fct.sto, 0, jf) ' save count
                            GenerateAsm(fct.lod, tpSymbol.tpVariable, jf) ' current
                            GenerateAsm(fct.lod, tpSymbol.tpVariable, wsfori) ' max
                            GenerateAsm(fct.opr, 0, 18) ' >
                            GenerateAsm(fct.jct, 0, 0) ' jump if false
                            Leave.Add(LoopVar)
                            LeaveX.Add(CurrRexxRun.IntCode.Count())
                        End If
                    End If
                    GenerateAsm(fct.jmp, 0, RetAdr)
                End If
                GenerateLinenumberIndication()
                i = Leave.Count()
                While i > 0
                    If CStr(Leave.Item(i)) = "" Or CStr(Leave.Item(i)) = LoopVar Then
                        GenerateJumpAddress(CInt(LeaveX.Item(i)), 1)
                        Leave.Remove(i)
                        LeaveX.Remove(i)
                    End If
                    i = i - 1
                End While
                ActivateDOstatKeywords(False)
            ElseIf (cSymb = Symbols.callsym) Then ' call
                GetNextSymbol()
                i = SourceNameIndexPosition(cId, tpSymbol.tpProcedure, DefVars)
                If DefVars.Kind <> tpSymbol.tpProcedure Then SigError(105)
                GetNextSymbol()
                DoCallOrFunction(i, True)
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.procsym) Then ' procedure
                CurrRexxRun.IdExpose = New Collection
                CurrRexxRun.IdExposeStk.Add(CurrRexxRun.IdExpose)
                GetNextSymbol()
                CurrRexxRun.IdExpose.Add("RESULT")
                CurrRexxRun.IdExpose.Add("RC")
                CurrRexxRun.IdExpose.Add("SIGL")
                If (cSymb = Symbols.exposesym) Then
                    GetNextSymbol()
                    While (cSymb = Symbols.ident)
                        CurrRexxRun.IdExpose.Add(cId)
                        GetNextSymbol()
                        If cSymb = Symbols.comma Then GetNextSymbol() ' names separated by comma, not official rexx!
                    End While
                End If
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.saysym) Then ' say .............
                GetNextSymbol()
                If cSymb = Symbols.semicolon Then
                    GenerateAsm(fct.lod, tpSymbol.tpConstant, StoreLiteral(""))
                Else
                    Expression()
                End If
                GenerateAsm(fct.say, 0, 0)
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.exitsym Or cSymb = Symbols.retsym) Then ' exit ... return
                vSymb = cSymb
                GetNextSymbol()
                If (cSymb = Symbols.semicolon) Then
                    GenerateAsm(fct.lod, tpSymbol.tpConstant, 1) ' load literal "0" , always 1st in list
                Else
                    Expression()
                End If
                GenerateAsm(fct.sto, 0, CurrRexxRun.iRes)
                If (vSymb = Symbols.exitsym) Then
                    GenerateAsm(fct.exi, 0, 1)
                Else
                    GenerateAsm(fct.ret, 0, 1)
                End If
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.tracesym) Then  ' trace ? o/n/i/r
                Dim tracInt As Integer = 0
                GetNextSymbol()
                If (cSymb = Symbols.questmk) Then
                    GetNextSymbol()
                    tracInt = 1
                End If
                If Abbrev(cId, "NORMAL") Or Abbrev(cId, "OFF") Then
                    GenerateAsm(fct.tra, tracInt, 1)
                Else
                    If Abbrev(cId, "LABELS") Then
                        GenerateAsm(fct.tra, tracInt, 2)
                    ElseIf Abbrev(cId, "RESULTS") Then
                        GenerateAsm(fct.tra, tracInt, 3)
                    ElseIf Abbrev(cId, "INTERMEDIATES") Then
                        GenerateAsm(fct.tra, tracInt, 4)
                    ElseIf Abbrev(cId, "SAY,3") Then ' trace SAY statements in file
                        GenerateAsm(fct.tra, tracInt, 5)
                    Else
                        SigError(112)
                    End If
                End If
                GetNextSymbol()
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.argsym) Then  '   arg value etc
                GenerateAsm(fct.upp, 0, 1)
                argGen(fct.arg)
            ElseIf (cSymb = Symbols.parsesym) Then  ' parse lower upper arg value etc
                ActivatePARSEstatKeywords(True)
                GetNextSymbol()
                If (cSymb = Symbols.lowersym) Then
                    GetNextSymbol()
                    GenerateAsm(fct.upp, 0, 2)
                End If
                If (cSymb = Symbols.uppersym) Then
                    GetNextSymbol()
                    GenerateAsm(fct.upp, 0, 1)
                End If
                If (cSymb = Symbols.argsym) Then
                    argGen(fct.arg)
                ElseIf (cSymb = Symbols.pullsym) Then
                    argGen(fct.pul)
                ElseIf (cSymb = Symbols.varsym) Then
                    GetNextSymbol()
                    i = SourceNameIndexPosition(cId, tpSymbol.tpVariable, DefVars)
                    GenerateAsm(fct.lod, tpSymbol.tpVariable, i)
                    argGen(fct.pvr)
                ElseIf (cSymb = Symbols.valuesym) Then
                    GetNextSymbol()
                    Expression()
                    argGen(fct.pvl)
                Else
                    SigError(119)
                End If
                ActivatePARSEstatKeywords(False)
            ElseIf (cSymb = Symbols.uppersym Or cSymb = Symbols.lowersym) Then ' Upper/lower
                svs = cSymb
                GetNextSymbol()
                i = SourceNameIndexPosition(cId, tpSymbol.tpVariable, DefVars)
                If (svs = Symbols.lowersym) Then
                    GenerateAsm(fct.upc, i, 0)
                Else
                    GenerateAsm(fct.upc, i, 1)
                End If
                GetNextSymbol()
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.addresym) Then  ' parse upper arg value etc
                GetNextSymbol()
                If (cSymb = Symbols.ident Or cSymb = Symbols.txtsym) Then
                    i = StoreLiteral(cId)
                    GetNextSymbol()
                    If cSymb <> Symbols.semicolon Then
                        GenerateAsm(fct.adr, 0, i) ' temp address
                    Else
                        GenerateAsm(fct.adr, 1, i) ' permanent
                        GetNextSymbol()
                    End If
                Else
                    SigError(118)
                End If
            ElseIf (cSymb = Symbols.txtsym) Then  ' literal: command
                Expression()
                GenerateAsm(fct.exc, CurrRexxRun.iRc, 0)
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.nopsym) Then  ' nop
                GetNextSymbol()
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.pullsym) Then  ' pull
                GenerateAsm(fct.upp, 0, 1)
                argGen(fct.pul)
            ElseIf (cSymb = Symbols.pushsym) Then  ' push
                GetNextSymbol()
                Expression()
                GenerateAsm(fct.stk, 0, 0)
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.queuesym) Then  ' queue
                GetNextSymbol()
                Expression()
                GenerateAsm(fct.stk, 1, 0)
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.leavesym Or cSymb = Symbols.itersym) Then  ' leave/iter
                svs = cSymb
                GetNextSymbol()
                If (cSymb = Symbols.semicolon) Then
                    cId = ""
                ElseIf (cSymb = Symbols.ident) Then
                    GetNextSymbol()
                End If
                If (svs = Symbols.leavesym) Then
                    GenerateAsm(fct.jmp, 0, 0)
                    Leave.Add(cId)
                    LeaveX.Add(CurrRexxRun.IntCode.Count())
                Else
                    GenerateAsm(fct.jmp, 0, 0)
                    Itre.Add(cId)
                    ItreX.Add(CurrRexxRun.IntCode.Count())
                End If
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.signalsym) Then  ' signal on novalue  | signal label
                GetNextSymbol()
                If (cSymb = Symbols.ident And (cId = "ON" Or cId = "OFF")) Then
                    vId = cId
                    GetNextSymbol()
                    If (cSymb = Symbols.ident And cId = "NOVALUE") Then
                        GetNextSymbol()
                    End If
                    If vId = "ON" Then
                        Dim nrVars = CurrRexxRun.IdName.Count()
                        i = SourceNameIndexPosition(cId, tpSymbol.tpProcedure, DefVars)
                        If i > nrVars Then ' added now
                            CurrRexxRun.IdName.Remove(i)
                            i = 0
                        End If
                        If DefVars.Kind <> tpSymbol.tpProcedure Then SigError(105) ' if defined now, will be corrected when inserting labels
                        GenerateAsm(fct.sig, 1, i)
                    Else
                        GenerateAsm(fct.sig, 0, 0)
                    End If
                    If cSymb <> Symbols.semicolon Then
                        GetNextSymbol()
                    End If
                Else ' goto label
                    If (cSymb <> Symbols.ident) Then
                        SigError(105)
                    Else
                        i = SourceNameIndexPosition(cId, tpSymbol.tpProcedure, DefVars)
                        If DefVars.Kind <> tpSymbol.tpProcedure Then SigError(105)
                        GetNextSymbol()
                    End If
                    GenerateAsm(fct.cal, -1, i)
                End If
                TestSymbolExpected(Symbols.semicolon, 118)
            ElseIf (cSymb = Symbols.dropsym) Then  ' drop
                GetNextSymbol()
                While (cSymb <> Symbols.semicolon)
                    i = SourceNameIndexPosition(cId, tpSymbol.tpVariable, DefVars)
                    If Right(cId, 1) <> "." Then
                        GenerateAsm(fct.drp, 0, i)
                    Else
                        GenerateAsm(fct.drp, 1, i)
                    End If
                    GetNextSymbol()
                End While
            ElseIf (cSymb = Symbols.itpsym) Then  ' interpret
                GetNextSymbol()
                If cSymb <> Symbols.semicolon Then
                    Expression()
                    GenerateAsm(fct.itp, 0, 0)
                End If
                TestSymbolExpected(Symbols.semicolon, 118)
            Else
                TestSymbolExpected(Symbols.semicolon, 118)
            End If
        End If
    End Sub
    Private Sub ActivateDOstatKeywords(ByRef sw As Boolean)
        ToggleKeywordIsActive("BY", sw)
        ToggleKeywordIsActive("FOR", sw)
        ToggleKeywordIsActive("TO", sw)
        ToggleKeywordIsActive("UNTIL", sw)
        ToggleKeywordIsActive("WHILE", sw)
    End Sub
    Private Sub ActivatePARSEstatKeywords(ByRef sw As Boolean)
        ToggleKeywordIsActive("VAR", sw)
        ToggleKeywordIsActive("VALUE", sw)
        ToggleKeywordIsActive("WITH", sw)
    End Sub
    Private Sub DoBlock(ByVal LoopVar As String)
        ActivateDOstatKeywords(False)
        While (cSymb <> Symbols.endsym And cSymb <> Symbols.endprog)
            CompileOneStatement()
        End While
        If (cSymb <> Symbols.endsym) Then
            SigError(115)
        Else
            GenerateLinenumberIndication()
            GetNextSymbol()
            If cSymb = Symbols.ident Then
                If cId <> LoopVar Then
                    SigError(116)
                End If
                GetNextSymbol()
            End If
            SkipOverSemicolon(False)
        End If
        Dim i As Integer
        i = Itre.Count()
        While (i > 0)
            If CStr(Itre.Item(i)) = "" Or CStr(Itre.Item(i)) = LoopVar Then
                GenerateJumpAddress(CInt(ItreX.Item(i)), 1)
                Itre.Remove(i)
                ItreX.Remove(i)
            End If
            i = i - 1
        End While
    End Sub
    Private Sub CompileDoUntil(ByVal cod As String, ByVal LoopVar As String) ' gen until code
        Dim asm As AsmStatement
        Dim i, c As Integer
        c = CurrRexxRun.IntCode.Count()
        If tmpIntCode.Count() > 0 Then
            i = 0
            For Each asm In tmpIntCode
                i = i + 1
                If i > PrevTmpCode Then
                    If asm.f = fct.jmp Or asm.f = fct.jct Or asm.f = fct.jcf Then
                        asm.a = asm.a + c - PrevTmpCode
                    End If
                    GenerateAsm(asm.f, asm.l, asm.a)
                    tmpIntCode.Remove(PrevTmpCode + 1)
                End If
            Next asm
            GenerateAsm(fct.jcf, 0, 0)
            Leave.Add(LoopVar)
            LeaveX.Add(CurrRexxRun.IntCode.Count())
        End If
    End Sub
    Private Sub Expression(Optional ByVal restExpression As Boolean = False)
        ExpressionPlusMin(restExpression)
        While (cSymb = Symbols.ident Or cSymb = Symbols.concat Or cSymb = Symbols.txtsym Or cSymb = Symbols.nbrsym Or cSymb = Symbols.lparen)
            If (cSymb = Symbols.concat) Then
                GetNextSymbol()
            Else
                If CharBefId = " " Then
                    GenerateAsm(fct.lod, tpSymbol.tpConstant, StoreLiteral(" "))
                    GenerateAsm(fct.opr, 0, 8) ' concat with " "
                End If
            End If
            ExpressionPlusMin(restExpression)
            GenerateAsm(fct.opr, 0, 8) ' concat
        End While
    End Sub
    Private Sub ExpressionPlusMin(ByRef restExpression As Boolean)
        Dim addop As Symbols
        If Not restExpression AndAlso (cSymb = Symbols.plus Or cSymb = Symbols.minus Or cSymb = Symbols.notsym) Then
            addop = cSymb
            GetNextSymbol()
            ExpressionMultDiv(restExpression)
            If (addop = Symbols.minus) Then
                GenerateAsm(fct.opr, 0, 1)
            ElseIf (addop = Symbols.notsym) Then
                GenerateAsm(fct.opr, 0, 11)
            End If
        Else
            ExpressionMultDiv(restExpression)
        End If
        While (cSymb = Symbols.plus Or cSymb = Symbols.minus)
            addop = cSymb
            GetNextSymbol()
            ExpressionMultDiv(restExpression)
            If (addop = Symbols.minus) Then
                GenerateAsm(fct.opr, 0, 3)
            Else
                GenerateAsm(fct.opr, 0, 2)
            End If
        End While
    End Sub
    Private Sub ExpressionMultDiv(ByRef restExpression As Boolean)
        Dim mulop As Symbols
        ExpressionPowr(restExpression)
        While (cSymb = Symbols.times Or cSymb = Symbols.slash Or cSymb = Symbols.idiv Or cSymb = Symbols.moddiv)
            mulop = cSymb
            GetNextSymbol()
            ExpressionPowr(restExpression)
            If (mulop = Symbols.times) Then
                GenerateAsm(fct.opr, 0, 4)
            ElseIf (mulop = Symbols.slash) Then
                GenerateAsm(fct.opr, 0, 5)
            ElseIf (mulop = Symbols.moddiv) Then
                GenerateAsm(fct.opr, 0, 6)
            Else
                GenerateAsm(fct.opr, 0, 9)
            End If
        End While
    End Sub
    Private Sub ExpressionPowr(ByRef restExpression As Boolean)
        Dim LeadMin As Boolean
        If Not restExpression Then
            Factor(restExpression)
        Else
            restExpression = False ' operand is alreay on stack
        End If
        While (cSymb = Symbols.powr)
            GetNextSymbol()
            If cSymb = Symbols.plus Then GetNextSymbol()
            If cSymb = Symbols.minus Then
                LeadMin = True
                GetNextSymbol()
            End If
            Factor(restExpression)
            If LeadMin Then GenerateAsm(fct.opr, 0, 1)
            GenerateAsm(fct.opr, 0, 7)
        End While
    End Sub
    Private Sub SkipTo(ByRef e As Integer)
        SigError(e)
        While cSymb <> Symbols.semicolon And cSymb <> Symbols.endprog
            GetNextSymbol()
        End While
    End Sub
    Private Sub DoCallOrFunction(ByRef ixProcName As Integer, ByRef typeIsCall As Boolean)
        Dim NrValues, vi, j As Integer
        Dim ip As Integer
        Dim Pars As New Collection
        Dim ParsN As New Collection
        Dim closeSym As Symbols
        NrValues = 0
        iParSeqNr += 1000
        Pars.Clear()
        If cSymb = Symbols.lparen Then
            GetNextSymbol()
            closeSym = Symbols.rparen
        Else
            closeSym = Symbols.semicolon
        End If
        While (cSymb <> closeSym And cSymb <> Symbols.semicolon And cSymb <> Symbols.endprog)
            NrValues = NrValues + 1
            If cSymb <> Symbols.comma Then ' not an empty parameter
                ProcessCallParm(NrValues, vi, NrValues, ixProcName)
                Pars.Add(vi) ' position of parameter id in names
                ParsN.Add(iCurrParSeqNr) ' seq nr of parm  
            Else
                Pars.Add(-1)
                ParsN.Add(-1)
            End If
            If cSymb = Symbols.comma Then
                GetNextSymbol()
                If cSymb = closeSym Then
                    NrValues = NrValues + 1
                    vi = StoreLiteral("") '  literal ""
                    GenerateAsm(fct.lod, tpSymbol.tpConstant, vi)
                    Pars.Add(-1)
                    ParsN.Add(-1)
                End If
            End If
        End While
        If cSymb = Symbols.rparen Then GetNextSymbol()
        GenerateAsm(fct.cal, NrValues, ixProcName)
        j = 0
        For Each ip In Pars
            j = j + 1
            GenerateAsm(fct.cll, CInt(ParsN.Item(j)), ip)
        Next ip
        If Not typeIsCall Then GenerateAsm(fct.lod, tpSymbol.tpVariable, CurrRexxRun.iRes)
        iParSeqNr -= 1000
    End Sub
    Private Sub ProcessCallParm(ByVal n As Integer, ByRef i As Integer, ByVal iParStat As Integer, ixProcName As Integer)
        Dim pn As String
        iCurrParSeqNr = iParSeqNr + iParStat - 1
        If iMaxParSeqNr < iCurrParSeqNr Then iMaxParSeqNr = iCurrParSeqNr
        pn = "P " & CStr(iCurrParSeqNr)
        i = SourceNameIndexPosition(pn, tpSymbol.tpVariable, DefVars)
        Expression()
        'achteraf 1 Of 0, Als procedure
        GenerateAsm(fct.sto, ixProcName, i) ' store final value of parameter n in next level
    End Sub
    Private Sub Factor(ByRef restExpression As Boolean)
        Dim i As Integer
        Dim cIdx, CharAfterId As String
        If Not (cSymb = Symbols.ident Or cSymb = Symbols.txtsym Or cSymb = Symbols.nbrsym Or cSymb = Symbols.lparen) Then
            SkipTo(103)
        End If
        If (cSymb = Symbols.ident) Then
            '                         a space between identifier and ( has a meaning:
            cIdx = cId              ' x(...)  is a procedurecall to x
            CharAfterId = cChara    ' x (...) means: concatenate x with expression
            GetNextSymbol()
            i = SourceNameIndexPosition(cIdx, tpSymbol.tpUnknown, DefVars)
            If DefVars.Kind = -1 Then
                If CharAfterId = "(" Then
                    DefVars.Kind = tpSymbol.tpProcedure
                Else
                    DefVars.Kind = tpSymbol.tpVariable
                End If
            End If
            If CharAfterId <> "(" Then
                If cIdx.substring(cIdx.length - 1) = "." Then
                    GenerateAsm(fct.lodc, tpSymbol.tpVariable, i)
                Else
                    GenerateAsm(fct.lod, tpSymbol.tpVariable, i)
                End If
            Else
                If DefVars.Kind <> tpSymbol.tpProcedure Then
                    Dim Bltin As Boolean = False
                    Dim j As Integer
                    For j = 1 To UBound(RexxFunctions, 1)
                        If Mid(RexxFunctions(j), 12) = DefVars.Id Then
                            Bltin = True
                        End If
                    Next
                    If Not Bltin Then SigError(105)
                End If
                DoCallOrFunction(i, False)
            End If

        ElseIf (cSymb = Symbols.txtsym Or cSymb = Symbols.nbrsym) Then
            If cSymb = Symbols.txtsym And (cChara = "X" Or cChara = "x") Then ' hex string
                cIdx = cId
                GetNextSymbol()
                cId = X2C(cIdx)
            ElseIf cSymb = Symbols.txtsym And (cChara = "B" Or cChara = "b") Then ' binary string
                cIdx = cId
                GetNextSymbol()
                cId = B2C(cIdx)
            End If
            i = StoreLiteral(cId)
            GenerateAsm(fct.lod, tpSymbol.tpConstant, i)
            GetNextSymbol()
        ElseIf (cSymb = Symbols.lparen) Then
            GetNextSymbol()
            Expression()
            If (cSymb <> Symbols.rparen) Then
                SigError(106)
            Else
                GetNextSymbol()
            End If
        End If
    End Sub
    Private Sub Condition()
        ConditionOr()
        While (cSymb = Symbols.ors)
            GetNextSymbol()
            ConditionOr()
            GenerateAsm(fct.opr, 0, 20) ' or
        End While
    End Sub
    Private Sub ConditionOr()
        ConditionAnd()
        While (cSymb = Symbols.ands)
            GetNextSymbol()
            ConditionAnd()
            GenerateAsm(fct.opr, 0, 21) ' and
        End While
    End Sub
    Private Sub ConditionAnd()
        Dim nBrOpen As Integer
        If cSymb = Symbols.notsym Then
            GetNextSymbol()
            ConditionAnd()
            GenerateAsm(fct.opr, 0, 11)
        ElseIf cSymb = Symbols.lparen Then
            nBrOpen += 1
            GetNextSymbol()
            Condition()
            If cSymb = Symbols.rparen Then
                nBrOpen -= 1
                GetNextSymbol()
                ' the operand continues with more expression elements, the first element is already processed
                If (cSymb = Symbols.plus Or cSymb = Symbols.minus Or cSymb = Symbols.times Or cSymb = Symbols.idiv Or cSymb = Symbols.moddiv Or cSymb = Symbols.slash Or cSymb = Symbols.powr) Then
                    Expression(True)
                End If
                GenerateCondOperand()
            Else
                If nBrOpen > 0 Then SigError(119)
            End If
        Else
            Expression()
            If cSymb = Symbols.rparen And nBrOpen > 0 Then ' else pass ) to higher level
                nBrOpen -= 1
                GetNextSymbol()
            End If
            If (cSymb = Symbols.plus Or cSymb = Symbols.minus Or cSymb = Symbols.times Or cSymb = Symbols.idiv Or cSymb = Symbols.moddiv Or cSymb = Symbols.slash Or cSymb = Symbols.powr) Then
                Expression(True)
            End If
            GenerateCondOperand()
            If cSymb = Symbols.rparen And nBrOpen > 0 Then ' else pass ) to higher level
                nBrOpen -= 1
                GetNextSymbol()
            End If
        End If
    End Sub
    Sub GenerateCondOperand()
        If (cSymb = Symbols.eql Or cSymb = Symbols.eqlstr Or cSymb = Symbols.lss Or cSymb = Symbols.lssstr Or cSymb = Symbols.leq Or cSymb = Symbols.leqstr Or cSymb = Symbols.gtr Or cSymb = Symbols.gtrstr Or cSymb = Symbols.geq Or cSymb = Symbols.geqstr Or cSymb = Symbols.neq Or cSymb = Symbols.neqstr) Then
            Dim relop As Symbols = cSymb
            GetNextSymbol()
            Expression()
            If (relop = Symbols.eql) Then
                GenerateAsm(fct.opr, 0, 13)
            ElseIf (relop = Symbols.neq) Then
                GenerateAsm(fct.opr, 0, 15)
            ElseIf (relop = Symbols.lss) Then
                GenerateAsm(fct.opr, 0, 16)
            ElseIf (relop = Symbols.leq) Then
                GenerateAsm(fct.opr, 0, 17)
            ElseIf (relop = Symbols.gtr) Then
                GenerateAsm(fct.opr, 0, 18)
            ElseIf (relop = Symbols.geq) Then
                GenerateAsm(fct.opr, 0, 19)
            ElseIf (relop = Symbols.eqlstr) Then
                GenerateAsm(fct.opr, 0, 13 + strDelta)
            ElseIf (relop = Symbols.neqstr) Then
                GenerateAsm(fct.opr, 0, 15 + strDelta)
            ElseIf (relop = Symbols.lssstr) Then
                GenerateAsm(fct.opr, 0, 16 + strDelta)
            ElseIf (relop = Symbols.leqstr) Then
                GenerateAsm(fct.opr, 0, 17 + strDelta)
            ElseIf (relop = Symbols.gtrstr) Then
                GenerateAsm(fct.opr, 0, 18 + strDelta)
            ElseIf (relop = Symbols.geqstr) Then
                GenerateAsm(fct.opr, 0, 19 + strDelta)
            End If

        End If
    End Sub
    Private Const strDelta = 9
    Private Sub ToggleKeywordIsActive(ByRef s As String, ByRef b As Boolean)
        Dim w As RexxWord
        w = DirectCast(RexxWords.Item(s), RexxWord)
        w.Active = b
    End Sub
    Private Sub TestSymbolExpected(ByRef s As Symbols, ByRef n As Integer)
        If (cSymb <> s) Then
            MsgBox(SysMsg(127) & " " & SymsStr(CInt(s)) & " " & SysMsg(128) & " " & SymsStr(CInt(cSymb)), MsgBoxStyle.OkOnly)
            SkipTo(n)
        Else
            GetNextSymbol()
        End If
    End Sub
    Private Sub GenerateAsm(ByVal op As fct, ByVal p1 As Integer, ByVal p2 As Integer)
        Dim s As New AsmStatement
        s.f = op
        s.l = p1
        s.a = p2
        If GenTemp Then
            tmpIntCode.Add(s)
        Else
            CurrRexxRun.IntCode.Add(s)
        End If
    End Sub
    Private Sub GenerateLinenumberIndication()
        Static lastLnr As Integer
        If lastLnr <> CurrRexxRun.SrcLine Then
            GenerateAsm(fct.lin, CurrRexxRun.SrcLine, 0)
        End If
        lastLnr = CurrRexxRun.SrcLine
    End Sub
    Private Sub GenerateJumpAddress(ByVal cx As Integer, Optional ByVal incr As Integer = 0)
        Dim Code As AsmStatement
        Dim j As Integer
        j = CurrRexxRun.IntCode.Count() + incr
        Code = DirectCast(CurrRexxRun.IntCode.Item(cx), AsmStatement)
        Code.a = j
    End Sub
    Private Sub GenerateJumpAddressTemporary(ByVal cx As Integer, Optional ByVal incr As Integer = 0)
        Dim Code As AsmStatement
        Dim j As Integer
        j = tmpIntCode.Count() + incr
        Code = DirectCast(tmpIntCode.Item(cx), AsmStatement)
        Code.a = j
    End Sub
    Private Sub SkipOverSemicolon(Optional ByVal gLnr As Boolean = True)
        If cSymb = Symbols.semicolon Then GetNextSymbol()
        If gLnr Then GenerateLinenumberIndication()
    End Sub
    Dim LineTransit As Boolean = False
    Private Sub GetNextChar()
        If Not CurrRexxRun.InInterpret Then
            GetCh1(CurrRexxRun.Source)
        Else
            GetCh1(CurrRexxRun.ItpSource)
        End If
    End Sub
    Private Sub GetCh1(ByRef Source As Collection)
        If ncChar.Length() > 0 Then
            cChara = CChar(ncChar)
            ncChar = ""
        Else
            LineTransit = False
            CurrRexxRun.SrcPos = CurrRexxRun.SrcPos + 1
            Dim sr As LineOfSource = DirectCast(Source.Item(CurrRexxRun.SrcLine), LineOfSource)
            If CurrRexxRun.SrcPos >= sr.Text.Length Then
                If CurrRexxRun.SrcLine + 1 > Source.Count Then
                    eofRexxFile = True
                    cChara = " "c
                Else
                    CurrRexxRun.SrcLine = CurrRexxRun.SrcLine + 1
                    CurrRexxRun.SrcPos = 0
                    sr = DirectCast(Source.Item(CurrRexxRun.SrcLine), LineOfSource)
                    ' Debug.WriteLine(sr.Text)
                    cChara = sr.Text(CurrRexxRun.SrcPos)
                    LineTransit = True
                End If
            Else
                cChara = sr.Text(CurrRexxRun.SrcPos)
            End If
            If cChara = ","c AndAlso CurrRexxRun.SrcPos = sr.Text.Length - 1 Then ' continuation char
                CurrRexxRun.SrcLine = CurrRexxRun.SrcLine + 1
                CurrRexxRun.SrcPos = 0
                sr = DirectCast(Source.Item(CurrRexxRun.SrcLine), LineOfSource)
                cChara = sr.Text(CurrRexxRun.SrcPos)
                LineTransit = True
            End If
        End If
    End Sub
    Private Sub GetNextSymbol()
        Dim och As Char, pass As Integer
        Dim cRwrd As RexxWord
        Dim inCmt As Boolean
        If eofRexxFile Then
            cSymb = Symbols.endprog
        Else
            CharBefId = CharAftId
            While (cChara = " "c)
                GetNextChar()
            End While
            If InStr(1, sAtoZS, cChara) > 0 Then
                cId = ComposeComplexSymbol(sAtoZS_0to9).ToUpper(CultInf)
                While (cChara = "."c)
                    GetNextChar()
                    cId = cId & "." & ComposeComplexSymbol(sAtoZS_0to9).ToUpper(CultInf)
                End While
                If RexxWords.Contains(cId) Then
                    cRwrd = DirectCast(RexxWords.Item(cId), RexxWord)
                    If cRwrd.Active Then
                        cSymb = cRwrd.Sym
                    Else
                        cSymb = Symbols.ident
                    End If
                Else
                    cSymb = Symbols.ident
                End If
            ElseIf InStr(1, s0to9, cChara) > 0 Then
                cId = ComposeComplexSymbol(s0to9)
                cSymb = Symbols.nbrsym
                If (cChara = "."c) Then
                    GetNextChar()
                    If InStr(1, s0to9, cChara) > 0 Then
                        cId = cId & "." & ComposeComplexSymbol(s0to9)
                    End If
                End If
                If cChara = "e"c Or cChara = "E"c Then
                    GetNextChar()
                    If InStr(1, s0to9pm, cChara) > 0 Then
                        cId = cId & "E" & ComposeComplexSymbol(s0to9pm)
                    End If
                End If
            ElseIf (cChara = "'"c Or cChara = """"c) Then
                och = cChara
                cId = ""
                cSymb = Symbols.txtsym
                pass = 0
                While (cChara = och And Not eofRexxFile)
                    pass += 1
                    If pass > 1 Then cId = cId & och
                    GetNextChar()
                    While (cChara <> och And Not eofRexxFile)
                        cId = cId & cChara
                        prChara = cChara
                        GetNextChar()
                        If LineTransit AndAlso prChara = ";" Then
                            SigError(120)
                            Exit Sub
                        End If
                    End While
                    If eofRexxFile Then SigError(120)
                    GetNextChar()
                End While
            ElseIf (cChara = ">"c Or cChara = "<"c) Then
                If (cChara = ">"c) Then
                    cSymb = Symbols.gtr
                Else
                    cSymb = Symbols.lss
                End If
                GetNextChar()
                If (cChara = "="c) Then
                    If (cSymb = Symbols.gtr) Then
                        cSymb = Symbols.geq
                    Else
                        cSymb = Symbols.leq
                    End If
                    GetNextChar()
                ElseIf (cSymb = Symbols.lss And cChara = ">"c) Then
                    cSymb = Symbols.neq
                    GetNextChar()
                ElseIf (cSymb = Symbols.gtr And cChara = "<"c) Then
                    cSymb = Symbols.neq
                    GetNextChar()
                ElseIf (cSymb = Symbols.gtr And cChara = ">"c) Then
                    cSymb = Symbols.gtrstr
                    GetNextChar()
                    If cChara = "="c Then
                        cSymb = Symbols.geqstr
                        GetNextChar()
                    End If
                ElseIf (cSymb = Symbols.lss And cChara = "<"c) Then
                    cSymb = Symbols.lssstr
                    GetNextChar()
                    If cChara = "="c Then
                        cSymb = Symbols.leqstr
                        GetNextChar()
                    End If
                End If
            ElseIf (cChara = "*"c) Then
                cSymb = Symbols.times
                GetNextChar()
                If (cChara = "*"c) Then
                    cSymb = Symbols.powr
                    GetNextChar()
                End If
                If (cChara = "/"c) Then
                    cSymb = Symbols.comclose
                    GetNextChar()
                End If
            ElseIf (cChara = "/"c) Then
                cSymb = Symbols.slash
                GetNextChar()
                If (cChara = "/"c) Then
                    cSymb = Symbols.moddiv
                    GetNextChar()
                ElseIf (cChara = "*"c) Then
                    cSymb = Symbols.comopen
                    GetNextChar()
                End If
            ElseIf (cChara = "|"c) Then
                cSymb = Symbols.ors
                GetNextChar()
                If (cChara = "|"c) Then
                    cSymb = Symbols.concat
                    GetNextChar()
                End If
            ElseIf (cChara = "="c) Then
                cSymb = Symbols.eql
                GetNextChar()
                If (cChara = "="c) Then
                    cSymb = Symbols.eqlstr
                    GetNextChar()
                End If
            ElseIf (cChara = "^"c Or cChara = "!"c Or cChara = "\"c) Then
                cSymb = Symbols.notsym
                GetNextChar()
                If (cChara = "="c) Then
                    cSymb = Symbols.neq
                    GetNextChar()
                    If (cChara = "="c) Then
                        cSymb = Symbols.neqstr
                        GetNextChar()
                    End If
                ElseIf (cChara = "<"c) Then
                    cSymb = Symbols.geq
                    GetNextChar()
                    If (cChara = "<"c) Then
                        cSymb = Symbols.geqstr
                        GetNextChar()
                    End If
                ElseIf (cChara = ">"c) Then
                    cSymb = Symbols.leq
                    GetNextChar()
                    If (cChara = ">"c) Then
                        cSymb = Symbols.leqstr
                        GetNextChar()
                    End If
                End If
            ElseIf RexxWords.Contains(cChara) Then
                cRwrd = DirectCast(RexxWords.Item(cChara), RexxWord)
                cSymb = cRwrd.Sym
                GetNextChar()
            Else
                cSymb = Symbols.ident
                SigError(103)
                GetNextChar()
            End If
            CharAftId = cChara
            If (cSymb = Symbols.comopen) Then
                inCmt = True
                While inCmt
                    While (cChara <> "*"c And Not eofRexxFile)
                        GetNextChar()
                    End While
                    If Not eofRexxFile Then
                        GetNextChar()
                        If cChara = "/"c Then
                            inCmt = False
                            GetNextChar()
                        End If
                    Else
                        inCmt = False
                    End If
                End While
                If (cSymb = Symbols.endprog) Then
                    SigError(107)
                Else
                    GetNextSymbol()
                End If
            End If
        End If
#If DEBUG Then
        codesym()
        '  Debug.WriteLine("symbol " + asy)
#End If
    End Sub
    Private Function ComposeComplexSymbol(ByRef chars As String) As String
        ' compose a symbol
        Dim ccs As String = ""
        If cChara = "'" Or cChara = """" Then
            Dim clChar As Char = cChara
            GetNextChar()
            While cChara <> clChar AndAlso Not eofRexxFile
                ccs = ccs & cChara
                GetNextChar()
            End While
            GetNextChar()
        Else
            While InStr(chars, cChara) > 0 AndAlso Not eofRexxFile
                ccs = ccs & cChara
                GetNextChar()
            End While
        End If
        ComposeComplexSymbol = ccs.ToUpper
    End Function
#If DEBUG Then
    ' procedure for debug only, call in immediate window to see int-code
    Private Sub dumpCode(Optional ByVal st As Integer = 1, Optional ByVal ei As Integer = 9999999)
        dumpCodex(CurrRexxRun.IntCode, st, ei)
    End Sub
    Private Sub dumpCodet(Optional ByVal st As Integer = 1, Optional ByVal ei As Integer = 9999999)
        dumpCodex(tmpIntCode, st, ei)
    End Sub
    Private Sub dumpCodex(ByVal intcode As Collection, ByVal st As Integer, ByVal ei As Integer)
        Dim i As Integer
        Dim asm As New AsmStatement
        If intcode.Count() < ei Then ei = intcode.Count()
        For i = st To ei
            asm = DirectCast(intcode.Item(i), AsmStatement)
            If asm.f = fct.lin Then
                Dim s As LineOfSource = DirectCast(CurrRexxRun.Source.Item(asm.l), LineOfSource)
                Debug.Print("     " & s.Text)
            End If
            Debug.Print(i & " " & DumpStr(asm))
        Next
    End Sub
    Private Function DumpStr(ByVal asm As AsmStatement) As String
        Dim s, a, l As String
        a = CStr(asm.a)
        l = CStr(asm.l)
        Select Case CInt(asm.f)
            Case fct.opr : s = "opr"
                Select Case asm.a
                    Case 1 : a = "_"
                    Case 2 : a = "+"
                    Case 3 : a = "-"
                    Case 4 : a = "*"
                    Case 5 : a = "/"
                    Case 6 : a = "//"
                    Case 7 : a = "**"
                    Case 8 : a = "||"
                    Case 9 : a = "%"
                    Case 11 : a = "^"
                    Case 13 : a = "="
                    Case 14 : a = "=="
                    Case 15 : a = "<>"
                    Case 16 : a = "<"
                    Case 17 : a = "<="
                    Case 18 : a = ">"
                    Case 19 : a = ">="
                    Case 20 : a = "|"
                    Case 21 : a = "&"
                End Select
            Case fct.lod : s = "lod"
                Select Case asm.l
                    Case tpSymbol.tpVariable : l = "V"
                    Case tpSymbol.tpConstant : l = "L"
                    Case tpSymbol.tpProcedure : l = "P"
                End Select
            Case fct.sto : s = "sto"
            Case fct.jmp : s = "jmp"
            Case fct.jbr : s = "jbr"
            Case fct.jcf : s = "jcf"
            Case fct.lin : s = "lin"
            Case fct.drp : s = "drp"
            Case fct.tra : s = "tra"
            Case fct.cll : s = "cll"
            Case fct.adr : s = "adr"
            Case fct.cal : s = "cal"
            Case fct.ret : s = "ret"
            Case fct.exi : s = "exi"
            Case fct.lbl : s = "lbl"
            Case fct.cle : s = "cle"
            Case fct.arg : s = "arg"
            Case fct.upp : s = "upp"
            Case fct.parp : s = "parp"
            Case fct.parc : s = "parc"
            Case fct.parv : s = "parv"
            Case fct.parl : s = "parl"
            Case fct.parh : s = "parh"
            Case fct.pul : s = "pul"
            Case fct.pvr : s = "pvr"
            Case fct.pvl : s = "pvl"
            Case fct.stk : s = "stk"
            Case fct.say : s = "say"
            Case fct.upc : s = "upc"
            Case fct.exc : s = "exc"
            Case fct.sig : s = "sig"
            Case fct.jct : s = "jct"
            Case fct.itp : s = "itp"
            Case Else
                s = "???"
        End Select
        Return (s & " " & a & " " & l)
    End Function
    ' procedure for debug only, called in debug code
    Private Sub codesym()
        If cSymb = Symbols.comopen Then asy = "comopen"
        If cSymb = Symbols.comclose Then asy = "comclose"
        If cSymb = Symbols.ident Then asy = "ident"
        If cSymb = Symbols.plus Then asy = "plus"
        If cSymb = Symbols.minus Then asy = "minus"
        If cSymb = Symbols.times Then asy = "times"
        If cSymb = Symbols.slash Then asy = "slash"
        If cSymb = Symbols.idiv Then asy = "idiv"
        If cSymb = Symbols.notsym Then asy = "notsym"
        If cSymb = Symbols.powr Then asy = "powr"
        If cSymb = Symbols.moddiv Then asy = "moddiv"
        If cSymb = Symbols.eql Then asy = "eql"
        If cSymb = Symbols.eqlstr Then asy = "eqlstr"
        If cSymb = Symbols.neq Then asy = "neq"
        If cSymb = Symbols.neqstr Then asy = "neqstr"
        If cSymb = Symbols.lss Then asy = "lss"
        If cSymb = Symbols.lssstr Then asy = "lssstr"
        If cSymb = Symbols.leq Then asy = "leq"
        If cSymb = Symbols.leqstr Then asy = "leqstr"
        If cSymb = Symbols.gtr Then asy = "gtr"
        If cSymb = Symbols.gtrstr Then asy = "gtrstr"
        If cSymb = Symbols.geq Then asy = "geq"
        If cSymb = Symbols.geqstr Then asy = "geqstr"
        If cSymb = Symbols.concat Then asy = "concat"
        If cSymb = Symbols.ors Then asy = "ors"
        If cSymb = Symbols.ands Then asy = "ands"
        If cSymb = Symbols.lparen Then asy = "lparen"
        If cSymb = Symbols.rparen Then asy = "rparen"
        If cSymb = Symbols.comma Then asy = "comma"
        If cSymb = Symbols.semicolon Then asy = "semicolon"
        If cSymb = Symbols.colon Then asy = "colon"
        If cSymb = Symbols.becomes Then asy = "becomes"
        If cSymb = Symbols.txtsym Then asy = "txtsym"
        If cSymb = Symbols.nbrsym Then asy = "nbrsym"
        If cSymb = Symbols.callsym Then asy = "callsym"
        If cSymb = Symbols.procsym Then asy = "procsym"
        If cSymb = Symbols.retsym Then asy = "retsym"
        If cSymb = Symbols.endprog Then asy = "endprog"
        If cSymb = Symbols.exitsym Then asy = "exitsym"
        If cSymb = Symbols.exposesym Then asy = "exposesym"
        If cSymb = Symbols.argsym Then asy = "argsym"
        If cSymb = Symbols.parsesym Then asy = "parsesym"
        If cSymb = Symbols.uppersym Then asy = "uppersym"
        If cSymb = Symbols.tracesym Then asy = "tracesym"
        If cSymb = Symbols.questmk Then asy = "questmk"
        If cSymb = Symbols.saysym Then asy = "saysym"
        If cSymb = Symbols.ifsym Then asy = "ifsym"
        If cSymb = Symbols.thensym Then asy = "thensym"
        If cSymb = Symbols.elsesym Then asy = "elsesym"
        If cSymb = Symbols.dosym Then asy = "dosym"
        If cSymb = Symbols.endsym Then asy = "endsym"
        If cSymb = Symbols.leavesym Then asy = "leavesym"
        If cSymb = Symbols.itersym Then asy = "itersym"
        If cSymb = Symbols.tosym Then asy = "tosym"
        If cSymb = Symbols.forsym Then asy = "forsym"
        If cSymb = Symbols.bysym Then asy = "bysym"
        If cSymb = Symbols.whilesym Then asy = "whilesym"
        If cSymb = Symbols.untilsym Then asy = "untilsym"
        If cSymb = Symbols.dropsym Then asy = "dropsym"
        If cSymb = Symbols.nopsym Then asy = "nopsym"
        If cSymb = Symbols.pullsym Then asy = "pullsym"
        If cSymb = Symbols.pushsym Then asy = "pushsym"
        If cSymb = Symbols.queuesym Then asy = "queuesym"
        If cSymb = Symbols.signalsym Then asy = "signalsym"
        If cSymb = Symbols.selectsym Then asy = "selectsym"
        If cSymb = Symbols.whensym Then asy = "whensym"
        If cSymb = Symbols.addresym Then asy = "addresym"
        If cSymb = Symbols.otherwisesym Then asy = "otherwisesym"
        If cSymb = Symbols.varsym Then asy = "varsym"
        If cSymb = Symbols.valuesym Then asy = "valuesym"
        If cSymb = Symbols.withsym Then asy = "withsym"
        If cSymb = Symbols.lowersym Then asy = "lowersym"
    End Sub
    Sub dumpVars()
        Debug.WriteLine("Defined")
        Dim n As Int16 = 0
        For Each V As DefVariable In CurrRexxRun.IdName
            n += 1
            Debug.WriteLine(CStr(n) + " '" & V.Id & "' " & V.Kind)
        Next
        Debug.WriteLine("Runtime")
        n = 0
        For Each V As VariabelRun In CurrRexxRun.RuntimeVars
            n += 1
            Debug.WriteLine(CStr(n) + " '" & V.Id & "' " & V.Level & " |" & V.IdValue & "|")
        Next
    End Sub
#End If
    Private Function CreateNameOfWorkfile() As String
        Dim i As Integer, WrkFileName As String = ""
        For i = 1 To 999
            WrkFileName = Environ("TEMP")
            If WrkFileName = "" Then WrkFileName = Environ("TMP")
            If WrkFileName = "" Then WrkFileName = "C:"
            WrkFileName = WrkFileName & "\InterpretSource" & CStr(i) & ".rex"
            If Not File.Exists(WrkFileName) Then
                Return WrkFileName
            End If
        Next
        Return Nothing
    End Function
    Private Sub AddLineToSaybuffer(ByVal s As String, ByVal LastShow As Boolean) ' terminal box
        If RexxHandlesSay Then
#If DEBUG Then
            Debug.Print(s)
#End If
#If CreRexxLogFile Then
            Logg("say """ & s & """")
#End If
            If TracingSay Then
                sayFile.WriteLine(s)
            End If
            Dim m2 As String
            If (lSay > 0) Then
                m2 = SayLine(lSay)
                If Left(m2, 4) = "--> " Then lSay = lSay - 1 ' Answer to input request
            End If
            If Not LastShow And lSay = UBound(SayLine, 1) - 1 Then
                If MsgBox(ComposeSayFromBuffer, MsgBoxStyle.OkCancel, "Say") = MsgBoxResult.Cancel Then
                    CancRexx = True
                End If
                lSay = 0
            End If
            If s.Length() > 0 Or Not LastShow Then
                lSay = lSay + 1
                SayLine(lSay) = s
            End If
            If LastShow And lSay > 0 Then
                MsgBox(ComposeSayFromBuffer, MsgBoxStyle.OkOnly, "Say")
                lSay = 0
            End If
        Else
            RaiseEvent doSay(s)
        End If
    End Sub
    Private Function ComposeSayFromBuffer() As String
        Dim i As Integer
        ComposeSayFromBuffer = ""
        For i = 1 To lSay
            ComposeSayFromBuffer = ComposeSayFromBuffer & SayLine(i) & vbLf
        Next
        ComposeSayFromBuffer = Translate(ComposeSayFromBuffer, Chr(0) & Chr(9), Chr(32) & Chr(32))
    End Function
    Private Function StoreLiteral(ByVal literal As String) As Integer
        ' store a literal in lit-pool and return it's index
        Dim l As String
        StoreLiteral = 0
        For Each l In CurrRexxRun.TxtValue
            StoreLiteral = StoreLiteral + 1
            If l = literal Then
                Exit Function
            End If
        Next l
        CurrRexxRun.TxtValue.Add(literal)
        StoreLiteral = CurrRexxRun.TxtValue.Count()
    End Function
    Public Function SourceNameIndexPosition(ByVal Name As String, ByVal Kind As tpSymbol, ByRef Variable As DefVariable) As Integer
        ' position of variable in list of defined sourcenames
        Dim i As Integer
        SourceNameIndexPosition = 0
        i = 0
        For Each Variable In CurrRexxRun.IdName
            SourceNameIndexPosition = SourceNameIndexPosition + 1
            If Variable.Id = Name Then
                i = 1
                Exit For
            End If
        Next Variable
        If i = 0 Then
            Variable = New DefVariable
            Variable.Id = Name
            Variable.Kind = Kind
            CurrRexxRun.IdName.Add(Variable)
            SourceNameIndexPosition = CurrRexxRun.IdName.Count()
        End If
    End Function
    Public Function ExecuteRexxScript(ByVal params As String) As Integer
        'run file
        If CurrRexxRun.CompRc = 0 Then
            CallDepth += 1
            If CallDepth = 1 Then
                CurrRexxRun.TraceLevel = 0
            End If
            CurrRexxRun.InExecution = True
            ExecuteIntCode(params)
            CurrRexxRun.InExecution = False
            If CancRexx Then CallDepth = 1 ' abort all
            If CallDepth = 1 And lSay > 0 Then
                AddLineToSaybuffer("", True)
                If TracingSay Then
                    sayFile.Close()
                    TracingSay = False
                End If
            End If
            CallDepth -= 1
        Else
            CurrRexxRun.Rc = 255
        End If
        If CurrRexxRun.InInterpret Then
            While CurrRexxRun.IntCode.Count > EntriesInIntcode
                CurrRexxRun.IntCode.Remove(CurrRexxRun.IntCode.Count)
            End While
        Else
            If CallDepth = 0 Then
                Dim str As aStream ' opened by STREAM statement
                For Each str In Streams
                    Dim fi As New FileInfo(str.FileStr.Name)
                    If CloseStream(fi.Name) = "ERROR" Then
                        CloseStream(fi.FullName)
                    End If
                Next
                Streams.Clear()
            End If
        End If
        If Not CurrRexxRun.InInterpret Then CurrRexxRun.RuntimeVars.Clear()
        Return CurrRexxRun.Rc
    End Function
    Private Sub FillRexxWord(ByVal s As String, ByVal Sym As Symbols, Optional ByVal Active As Boolean = True)
        Dim w As New RexxWord
        w.Word = s
        w.Sym = Sym
        w.Active = Active
        RexxWords.Add(w, s)
    End Sub
    Private Sub FillRexxWords()
        Dim i As Integer
        FillRexxWord("ADDRESS", Symbols.addresym)
        SymsStr(Symbols.addresym) = "Address"
        FillRexxWord("ARG", Symbols.argsym)
        SymsStr(Symbols.argsym) = "Arg"
        FillRexxWord("BY", Symbols.bysym, False)
        SymsStr(Symbols.bysym) = "By"
        FillRexxWord("CALL", Symbols.callsym)
        SymsStr(Symbols.callsym) = "Call"
        FillRexxWord("DO", Symbols.dosym)
        SymsStr(Symbols.dosym) = "Do"
        FillRexxWord("DROP", Symbols.dropsym)
        SymsStr(Symbols.dropsym) = "Drop"
        FillRexxWord("ELSE", Symbols.elsesym)
        SymsStr(Symbols.elsesym) = "Else"
        FillRexxWord("END", Symbols.endsym)
        SymsStr(Symbols.endsym) = "End"
        FillRexxWord("EXIT", Symbols.exitsym)
        SymsStr(Symbols.exitsym) = "Exit"
        FillRexxWord("EXPOSE", Symbols.exposesym)
        SymsStr(Symbols.exposesym) = "Expose"
        FillRexxWord("FOR", Symbols.forsym, False)
        SymsStr(Symbols.forsym) = "For"
        FillRexxWord("IF", Symbols.ifsym)
        SymsStr(Symbols.ifsym) = "If"
        FillRexxWord("INTERPRET", Symbols.itpsym)
        SymsStr(Symbols.itersym) = "Iterate"
        FillRexxWord("ITERATE", Symbols.itersym)
        SymsStr(Symbols.itpsym) = "Interpret"
        FillRexxWord("LEAVE", Symbols.leavesym)
        SymsStr(Symbols.leavesym) = "Leave"
        FillRexxWord("LOWER", Symbols.lowersym)
        SymsStr(Symbols.lowersym) = "Lower"
        FillRexxWord("NOP", Symbols.nopsym)
        SymsStr(Symbols.nopsym) = "NOP"
        FillRexxWord("OTHERWISE", Symbols.otherwisesym, False)
        SymsStr(Symbols.otherwisesym) = "Otherwise"
        FillRexxWord("PARSE", Symbols.parsesym)
        SymsStr(Symbols.parsesym) = "Parse"
        FillRexxWord("PROCEDURE", Symbols.procsym)
        SymsStr(Symbols.procsym) = "Procedure"
        FillRexxWord("PULL", Symbols.pullsym)
        SymsStr(Symbols.pullsym) = "Pull"
        FillRexxWord("PUSH", Symbols.pushsym)
        SymsStr(Symbols.pushsym) = "Push"
        FillRexxWord("QUEUE", Symbols.queuesym)
        SymsStr(Symbols.queuesym) = "Queue"
        FillRexxWord("RETURN", Symbols.retsym)
        SymsStr(Symbols.retsym) = "Return"
        FillRexxWord("SAY", Symbols.saysym)
        SymsStr(Symbols.saysym) = "Say"
        FillRexxWord("SELECT", Symbols.selectsym)
        SymsStr(Symbols.selectsym) = "Select"
        FillRexxWord("SIGNAL", Symbols.signalsym)
        SymsStr(Symbols.signalsym) = "Signal"
        FillRexxWord("THEN", Symbols.thensym)
        SymsStr(Symbols.thensym) = "Then"
        FillRexxWord("TO", Symbols.tosym, False)
        SymsStr(Symbols.tosym) = "To"
        FillRexxWord("TRACE", Symbols.tracesym)
        SymsStr(Symbols.tracesym) = "Trace"
        FillRexxWord("UNTIL", Symbols.untilsym, False)
        SymsStr(Symbols.untilsym) = "Until"
        FillRexxWord("UPPER", Symbols.uppersym)
        SymsStr(Symbols.uppersym) = "Upper"
        FillRexxWord("VALUE", Symbols.valuesym, False)
        SymsStr(Symbols.valuesym) = "Value"
        FillRexxWord("VAR", Symbols.varsym, False)
        SymsStr(Symbols.varsym) = "Var"
        FillRexxWord("WHEN", Symbols.whensym, False)
        SymsStr(Symbols.whensym) = "When"
        FillRexxWord("WHILE", Symbols.whilesym, False)
        SymsStr(Symbols.whilesym) = "While"
        FillRexxWord("WITH", Symbols.withsym, False)
        SymsStr(Symbols.withsym) = "With"
        FillRexxWord("+", Symbols.plus)
        SymsStr(Symbols.plus) = "+"
        FillRexxWord("-", Symbols.minus)
        SymsStr(Symbols.minus) = "-"
        FillRexxWord("*", Symbols.times)
        SymsStr(Symbols.times) = "*"
        SymsStr(Symbols.comclose) = "*/"
        SymsStr(Symbols.powr) = "**"
        FillRexxWord("/", Symbols.slash)
        SymsStr(Symbols.slash) = "/"
        SymsStr(Symbols.moddiv) = "//"
        SymsStr(Symbols.comopen) = "/*"
        FillRexxWord("%", Symbols.idiv)
        SymsStr(Symbols.idiv) = "%"
        FillRexxWord("!", Symbols.notsym)
        SymsStr(Symbols.notsym) = "!"
        FillRexxWord("=", Symbols.eql)
        SymsStr(Symbols.eql) = "="
        SymsStr(Symbols.eqlstr) = "=="
        FillRexxWord("<", Symbols.lss)
        SymsStr(Symbols.lss) = "<"
        SymsStr(Symbols.lssstr) = "<<"
        SymsStr(Symbols.leq) = "<="
        SymsStr(Symbols.leqstr) = "<<="
        SymsStr(Symbols.neq) = "\="
        SymsStr(Symbols.neqstr) = "\=="
        FillRexxWord(">", Symbols.gtr)
        SymsStr(Symbols.gtr) = ">"
        SymsStr(Symbols.gtrstr) = ">>"
        SymsStr(Symbols.geq) = ">="
        SymsStr(Symbols.geqstr) = ">>="
        FillRexxWord("|", Symbols.ors)
        SymsStr(Symbols.ors) = "|"
        SymsStr(Symbols.concat) = "||"
        FillRexxWord("&", Symbols.ands)
        SymsStr(Symbols.ands) = "&"
        FillRexxWord("(", Symbols.lparen)
        SymsStr(Symbols.lparen) = "("
        FillRexxWord(")", Symbols.rparen)
        SymsStr(Symbols.rparen) = ")"
        FillRexxWord(",", Symbols.comma)
        SymsStr(Symbols.comma) = ","
        FillRexxWord(";", Symbols.semicolon)
        SymsStr(Symbols.semicolon) = ";"
        FillRexxWord(":", Symbols.colon)
        SymsStr(Symbols.colon) = ":"
        SymsStr(Symbols.becomes) = ":="
        FillRexxWord(".", Symbols.period)
        SymsStr(Symbols.period) = "."
        FillRexxWord("?", Symbols.questmk)
        SymsStr(Symbols.questmk) = "?"
        SymsStr(Symbols.endprog) = "End Program"
        SymsStr(Symbols.ident) = "Identifier"
        SymsStr(Symbols.nbrsym) = "Integer"
        SymsStr(Symbols.txtsym) = "Text"

        i = 0 '                        "nnxxxxxxxxx name of routine
        '                               nn = min. nr of parameters
        '                               each x = possible parameter, S=string, I=integer, R=Real  
        i = i + 1 : RexxFunctions(i) = "02SSI      ABBREV"
        i = i + 1 : RexxFunctions(i) = "01R        ABS"
        i = i + 1 : RexxFunctions(i) = "01S        B2X"
        i = i + 1 : RexxFunctions(i) = "01S        C2X"
        i = i + 1 : RexxFunctions(i) = "01SII      CHARIN"
        i = i + 1 : RexxFunctions(i) = "02SSII     CHAROUT"
        i = i + 1 : RexxFunctions(i) = "01S        CHARS"
        i = i + 1 : RexxFunctions(i) = "00S        CLIPBOARD"
        i = i + 1 : RexxFunctions(i) = "02SSS      COMPARE"
        i = i + 1 : RexxFunctions(i) = "02SS       CONTAINS"
        i = i + 1 : RexxFunctions(i) = "02SI       COPIES"
        i = i + 1 : RexxFunctions(i) = "01SS       DATATYPE"
        i = i + 1 : RexxFunctions(i) = "00S        DATE"
        i = i + 1 : RexxFunctions(i) = "02SII      DELSTR"
        i = i + 1 : RexxFunctions(i) = "00         EXTERNALS"
        i = i + 1 : RexxFunctions(i) = "01RIIII    FORMAT"
        i = i + 1 : RexxFunctions(i) = "02SS       INDEX"
        i = i + 1 : RexxFunctions(i) = "02SSIIS    INSERT"
        i = i + 1 : RexxFunctions(i) = "02SSS      JOIN"
        i = i + 1 : RexxFunctions(i) = "02SSI      LASTPOS"
        i = i + 1 : RexxFunctions(i) = "02SIS      LEFT"
        i = i + 1 : RexxFunctions(i) = "01S        LENGTH"
        i = i + 1 : RexxFunctions(i) = "01SII      LINEIN"
        i = i + 1 : RexxFunctions(i) = "01SSII     LINEOUT"
        i = i + 1 : RexxFunctions(i) = "01SS       LINES"
        i = i + 1 : RexxFunctions(i) = "02SSIIS    OVERLAY"
        i = i + 1 : RexxFunctions(i) = "02SSI      POS"
        i = i + 1 : RexxFunctions(i) = "00         QUEUED"
        i = i + 1 : RexxFunctions(i) = "00III      RANDOM"
        i = i + 1 : RexxFunctions(i) = "03SSS      REGEXP"
        i = i + 1 : RexxFunctions(i) = "01S        REVERSE"
        i = i + 1 : RexxFunctions(i) = "02SIS      RIGHT"
        i = i + 1 : RexxFunctions(i) = "01RI       ROUND"
        i = i + 1 : RexxFunctions(i) = "01R        SIGN"
        i = i + 1 : RexxFunctions(i) = "02SSS      SPLIT"
        i = i + 1 : RexxFunctions(i) = "02SSS      STREAM"
        i = i + 1 : RexxFunctions(i) = "01SSS      STRIP"
        i = i + 1 : RexxFunctions(i) = "02SII      SUBSTR"
        i = i + 1 : RexxFunctions(i) = "00S        TIME"
        i = i + 1 : RexxFunctions(i) = "01SSSS     TRANSLATE"
        i = i + 1 : RexxFunctions(i) = "01RI       TRUNC"
        i = i + 1 : RexxFunctions(i) = "01SSS      VALUE"
        i = i + 1 : RexxFunctions(i) = "02SSSI     VERIFY"
        i = i + 1 : RexxFunctions(i) = "02SI       WORD"
        i = i + 1 : RexxFunctions(i) = "01S        WORDS"
        i = i + 1 : RexxFunctions(i) = "01S        X2B"
        i = i + 1 : RexxFunctions(i) = "01S        X2C"
        i = i + 1 : RexxFunctions(i) = "00SS       XRANGE"

    End Sub
    Private Sub SigError(ByVal nr As Integer)
        Dim sr As LineOfSource = DirectCast(CurrRexxRun.Source.Item(CurrRexxRun.SrcLine), LineOfSource)
        SigErrorF(nr, sr.Text)
    End Sub
    Private Sub SigErrorF(ByVal nr As Integer, ByRef s As String) ' error with text iso sourcelinr
        Dim x As Long
        nErr = nErr + 1
        RcComp = 16
        Dim em As String = SysMsg(nr) + " " + RexxFileName + vbLf + s
        If nr = 124 AndAlso RexxPathElements.Length > 0 Then
            em = em + "Rexx path:" + vbLf
            For i As Integer = 1 To RexxPathElements.Length
                If RexxPathElements(i - 1) <> "" Then
                    em = em + Path.GetFullPath(RexxPathElements(i - 1)) + vbLf
                End If
            Next
        End If
        x = MsgBox(em, MsgBoxStyle.OkCancel)
        If x = MsgBoxResult.Cancel Then
            cSymb = Symbols.endprog
            CancRexx = True
            RaiseEvent doCancel()
        End If
    End Sub
    Private Function SysMsg(ByVal i As Integer) As String
        Dim s As String
        If SysMessages.Count = 0 Then
            ReadSysMsg(ExecutablePath & "\" & "system messages.txt")
        End If
        s = "SYSMSG" & CStr(i)
        If SysMessages.Contains(s) Then
            SysMsg = CStr(SysMessages.Item(s))
        Else
            SysMsg = s & " not defined in 'system messages.txt'"
        End If
    End Function
    Private Sub ReadSysMsg(ByVal Filename As String)
        Dim w As String
        Try
            Using sr As StreamReader = New StreamReader(Filename)
                Dim line As String
                ' Read and display the lines from the file until the end 
                ' of the file is reached.
                line = sr.ReadLine()
                While Not (line Is Nothing)
                    w = NxtWordFromStr(line)
                    If w.Length() > 6 AndAlso w.Substring(0, 6) = "SYSMSG" Then
                        SysMessages.Add(line, w)
                    End If
                    line = sr.ReadLine()
                End While
                sr.Close()
            End Using
        Catch E As Exception
            MsgBox("File: " & Filename & " not found, cancelling program", MsgBoxStyle.Exclamation)
            RaiseEvent doCancel()
        End Try
    End Sub
    Private Function NxtWordFromStr(ByRef s As String, Optional ByVal Def As String = "", Optional ByVal sep As String = " ") As String ' modifies s .............
        ' get next word from string, and strip from string s
        Dim i As Integer
        s = s.TrimStart()
        i = InStr(1, s, sep)
        If i = 0 Then
            NxtWordFromStr = s
            s = ""
        Else
            NxtWordFromStr = s.Substring(0, i - 1)
            s = s.Substring(i)
        End If
        If NxtWordFromStr = "" Then If Not IsNothing(Def) Then NxtWordFromStr = Def
        NxtWordFromStr = NxtWordFromStr.ToUpper(CultInf)
    End Function
    Dim LineExecuting As Integer = 0
    Private Sub ExecuteIntCode(ByRef CommandParm As String)
        Dim asm As New AsmStatement
        Dim asmp1 As New AsmStatement
        Dim i, j As Integer
        Dim cL, k, cA As Integer
        Dim cF As fct
        Dim en, n, m, m2 As String
        Dim CompNum As Boolean
        Dim num As Double
        ' Dim lv As Integer
        Dim num2 As Double
        Dim LRes As Integer
        Dim rtSym As Symbols
        Dim l2, l1, l3 As Integer
        Dim s As String
        Dim ps(5) As String ' max 5 parameters for builtin routines
        Dim pi(5) As Integer
        Dim pr(5) As Double
        Dim pm(5) As Boolean
        Dim nParMin As Integer
        Dim nParMax As Integer
        Dim ExtRoutine As Boolean = False
#If Not DEBUG Then
        Try
#End If
        en = ""
        n = ""
        If CurrRexxRun.InInterpret Then ' extend compiled code with new code
            IntPp = EntriesInIntcode + 1
        Else
            CurrRexxRun.RuntimeVars.Clear()
            CurrRexxRun.CallStack.Clear()
            Dim MainElem As New CallElem ' first callstack item is MAIN without exposed variables
            MainElem.ProcNum = 0
            MainElem.Exposes = Nothing
            CurrRexxRun.CallStack.Add(MainElem)
            StoreVar(CurrRexxRun.iRes, "", 0, "", "")
            StoreVar(CurrRexxRun.iRc, "0", 0, "", "")
            StoreVar(CurrRexxRun.iSigl, "0", 0, "", "")
            IntPp = 1
            CurrRexxRun.ProcNum = 0 ' not if in interpret !!!!!
            CurrRexxRun.sigNovalue = False
            CurrRexxRun.InteractiveTracing = 0
            If CommandParm.StartsWith(ExtRoutineTagstring) Then
                ExtRoutine = True
            End If
        End If
        Dim CommandLine, CommandPrm As String
        While IntPp <= CurrRexxRun.IntCode.Count()
            asm = DirectCast(CurrRexxRun.IntCode.Item(IntPp), AsmStatement)
            cF = asm.f
            cL = asm.l
            cA = asm.a
#If DEBUG Then
            asrc = "L:" + CStr(IntPp) + " " & DumpStr(asm) + " " + CStr(Stack.Count)
            Logg(asrc)
            'If (CurrRexxRun.TraceLevel > 2) Then Debug.WriteLine(asrc)
#End If
            Select Case cF
                Case fct.lin
                    SrcLstLin = cL
                    If cA = 1 Then
                        CurrRexxRun.TracingResume = CurrRexxRun.TraceLevel
                        CurrRexxRun.InteractiveTracing = 1
                        CurrRexxRun.TraceLevel = 3
                    End If
                    LineExecuting = cL
                    If (CurrRexxRun.TraceLevel > 2) Then traceLin(cL)
                Case fct.lod
                    Select Case cL
                        Case tpSymbol.tpVariable
                            Dim cintPP As Integer = IntPp
                            m = GetVarNV(cA, en, n, IntPp)
                            If cintPP = IntPp Then ' in case of "NOVALUE" trapped, there is no value and execution continues elsewhere
                                Stack.Add(m)
                            End If
                        Case tpSymbol.tpConstant
                            m = GetLit(cA)
                            Stack.Add(m)
                    End Select
                Case fct.opr
                    If (cA = 1) Then ' negate
                        m = FromStack()
                        num = StrFl(m)
                        Stack.Add(CStr(-num))
                    ElseIf (cA = 11) Then  ' not
                        m = FromStack()
                        num = StrFl(m)
                        If (num = 1) Then
                            num = 0
                        Else
                            num = 1
                        End If
                        Stack.Add(CStr(num))
                    ElseIf cA = 8 Then  ' concat
                        m = FromStack()
                        m2 = FromStack() & m
                        Stack.Add(m2)
                    ElseIf (cA >= 13 And cA <= 19) Or (cA >= 13 + strDelta And cA <= 19 + strDelta) Then  ' log. operators
                        m2 = FromStack()
                        m = FromStack()
                        LRes = 0
                        CompNum = False
                        If Not (cA >= 13 + strDelta And cA <= 19 + strDelta) Then ' not strict
                            If (IsNum(m2)) Then
                                num2 = NumY
                                If (IsNum(m)) Then
                                    num = NumY
                                    CompNum = True
                                End If
                            End If
                            If (CompNum) Then
                                Select Case cA ' numeric compare
                                    Case 13 ' =
                                        If (num = num2) Then LRes = 1
                                    Case 14 ' =
                                        If (num = num2) Then LRes = 1
                                    Case 15 ' <>
                                        If (num <> num2) Then LRes = 1
                                    Case 16 ' <
                                        If (num < num2) Then LRes = 1
                                    Case 17 ' <=
                                        If (num <= num2) Then LRes = 1
                                    Case 18 ' >
                                        If (num > num2) Then LRes = 1
                                    Case 19 ' >=
                                        If (num >= num2) Then LRes = 1
                                End Select
                            Else ' string compare  
                                m = m.Trim()
                                m2 = m2.Trim()
                                Select Case cA
                                    Case 13 ' =
                                        If (m2 = m) Then LRes = 1
                                    Case 15 ' \=
                                        If (m <> m2) Then LRes = 1
                                    Case 16 ' <
                                        If (m < m2) Then LRes = 1
                                    Case 17 ' <=
                                        If (m <= m2) Then LRes = 1
                                    Case 18 ' >
                                        If (m > m2) Then LRes = 1
                                    Case 19 ' >=
                                        If (m >= m2) Then LRes = 1
                                End Select
                            End If
                        Else ' strict string compare
                            Select Case cA
                                Case 13 + strDelta ' ==
                                    If (m2 = m) Then LRes = 1
                                Case 15 + strDelta ' \==
                                    If (m <> m2) Then LRes = 1
                                Case 16 + strDelta ' <<
                                    If (m < m2) Then LRes = 1
                                Case 17 + strDelta ' <<=
                                    If (m <= m2) Then LRes = 1
                                Case 18 + strDelta ' >>
                                    If (m > m2) Then LRes = 1
                                Case 19 + strDelta ' >>=
                                    If (m >= m2) Then LRes = 1
                            End Select
                        End If
                        Stack.Add(CStr(LRes))
                    Else ' arithm operator
                        m2 = FromStack()
                        m = FromStack()
                        num2 = StrFl(m2)
                        num = StrFl(m)
                        Select Case cA
                            Case 2 ' add
                                num = num + num2
                            Case 3 ' minus
                                num = num - num2
                            Case 4 ' mult
                                num = num * num2
                            Case 5, 9, 6 ' (int) divide
                                If (num2 = 0) Then
                                    num = 0
                                    RunError(9, "")
                                Else
                                    If cA = 6 Then
                                        num = num Mod num2
                                    Else
                                        num = num / num2
                                        If (cA = 9) Then
                                            num = Fix(num)
                                        End If
                                    End If
                                End If
                            Case 7 ' powr
                                num = num ^ num2
                            Case 20 ' or
                                If (num = 1 Or num2 = 1) Then
                                    num = 1
                                Else
                                    num = 0
                                End If
                            Case 21 ' and
                                If (num = 1 And num2 = 1) Then
                                    num = 1
                                Else
                                    num = 0
                                End If
                        End Select
                        If Not DecimalSepPt Then ' if ,
                            Stack.Add(CStr(num).Replace(","c, "."c))
                        Else
                            Stack.Add(CStr(num))
                        End If
                    End If
                Case fct.sto
                    m = FromStack()
                    StoreVar(cA, m, k, en, n, cL)
                Case fct.jmp
                    If (CurrRexxRun.TraceLevel > 2) Then
                        TracePLin(cA)
                    End If
                    IntPp = cA - 1
                Case fct.jcf
                    m = FromStack()
                    If m <> "1" Then
                        If (CurrRexxRun.TraceLevel > 2) Then
                            TracePLin(cA)
                        End If
                        IntPp = cA - 1
                    End If
                Case fct.jct
                    m = FromStack()
                    If m = "1" Then
                        If (CurrRexxRun.TraceLevel > 2) Then
                            TracePLin(cA)
                        End If
                        IntPp = cA - 1
                    End If
                Case fct.jbr ' builtin routines
                    nParMin = CInt(Left(RexxFunctions(cA), 2))
                    nParMax = nParMin
                    While nParMax <= 10 And Mid(RexxFunctions(cA), 3 + nParMax, 1) <> " "
                        nParMax = nParMax + 1
                    End While
                    For i = 1 To nParMax
                        pm(i) = False
                        pi(i) = 0
                        pr(i) = 0
                    Next
                    For i = 1 To cL
                        asmp1 = DirectCast(CurrRexxRun.IntCode.Item(IntPp + i), AsmStatement) ' fct.cll statement
                        If asmp1.a <> -1 Then ' not empty
                            VariaRuns = GetNmFunc(asmp1.a, n, en, k)
                            m = VariaRuns.IdValue
                            pm(i) = True
                        Else
                            m = ""
                            pm(i) = False
                        End If
                        Select Case Mid(RexxFunctions(cA), 2 + i, 1)
                            Case "S"
                                ps(i) = m
                            Case "I"
                                If pm(i) Then pi(i) = StrInt(m)
                            Case "R"
                                If pm(i) Then pr(i) = StrFl(m)
                        End Select
                    Next
                    IntPp = IntPp + cL
                    m = ""
                    Select Case Mid(RexxFunctions(cA), 12)
                        Case "SUBSTR"
                            If pi(2) < 1 Then pi(2) = 1
                            If pi(3) < 0 Then pi(3) = 0
                            If cL = 3 Then
                                m = Mid(ps(1), pi(2), pi(3)).PadRight(pi(3), " "c)
                            Else
                                m = Mid(ps(1), pi(2))
                            End If
                        Case "LEFT"
                            If pi(2) < 0 Then pi(2) = 0
                            If ps(1).Length() >= pi(2) Then
                                m = Left(ps(1), pi(2))
                            Else
                                If pm(3) Then
                                    m = ps(1).PadRight(pi(2), ps(3)(0))
                                Else
                                    m = ps(1).PadRight(pi(2), " "c)
                                End If
                            End If
                        Case "RIGHT"
                            If pi(2) < 0 Then pi(2) = 0
                            If ps(1).Length() >= pi(2) Then
                                m = Right(ps(1), pi(2))
                            Else
                                If pm(3) Then
                                    m = ps(1).PadLeft(pi(2), ps(3)(0))
                                Else
                                    m = ps(1).PadLeft(pi(2), " "c)
                                End If
                            End If
                        Case "LENGTH"
                            m = CStr(ps(1).Length())
                        Case "COPIES"
                            m = ""
                            For i = 1 To pi(2)
                                m = m & ps(1)
                            Next
                        Case "STRIP"
                            m = ps(1)
                            If cL < 2 Then ps(2) = "B"
                            If cL < 3 Then ps(3) = " "
                            If ps(2) = "L" Or ps(2) = "B" Then
                                While m.Length() > 0 And Left(m, 1) = ps(3)
                                    m = Mid(m, 2)
                                End While
                            End If
                            If ps(2) = "T" Or ps(2) = "B" Then
                                While m.Length() > 0 And Right(m, 1) = ps(3)
                                    m = Left(m, m.Length() - 1)
                                End While
                            End If
                        Case "INDEX"
                            m = CStr(InStr(ps(1), ps(2)))
                        Case "CONTAINS"
                            m = "0"
                            If ps(2).EndsWith(".") Then
                                For Each v As VariabelRun In CurrRexxRun.RuntimeVars
                                    If v.IdValue = ps(1) AndAlso v.Id.StartsWith(ps(2)) Then
                                        m = v.Id.Substring(ps(2).Length)
                                    End If
                                Next
                            End If
                        Case "POS"
                            If cL < 3 Then pi(3) = 1
                            m = CStr(InStr(pi(3), ps(2), ps(1)))
                        Case "COMPARE"
                            If cL < 3 Then ps(3) = " "
                            m = "0"
                            If ps(1) <> ps(2) Then
                                l1 = ps(1).Length()
                                l2 = ps(2).Length()
                                l3 = 0
                                For i = 1 To Math.Max(l1, l2)
                                    If i <= l1 And i <= l2 Then
                                        If Mid(ps(1), i, 1) <> Mid(ps(2), i, 1) Then l3 = i
                                    ElseIf i <= l1 Then
                                        If Mid(ps(1), i, 1) <> ps(3) Then l3 = i
                                    Else
                                        If Mid(ps(2), i, 1) <> ps(3) Then l3 = i
                                    End If
                                    If l3 <> 0 Then
                                        m = CStr(l3)
                                        Exit For
                                    End If
                                Next
                            End If
                        Case "VERIFY" ' (toVerify,verification,M/No match,start)
                            If cL < 4 Then pi(4) = 1
                            m = "0"
                            If Not pm(3) OrElse ps(3).ToUpper(CultInf) <> "M" Then ps(3) = "N"
                            For i = pi(4) To ps(1).Length()  ' N is like in Pl/1
                                j = InStr(1, ps(2), Mid(ps(1), i, 1))
                                If (j = 0 And ps(3) = "N") Or (j > 0 And ps(3) = "M") Then
                                    m = CStr(i)
                                    Exit For
                                End If
                            Next
                        Case "WORD" ' word(s,n)
                            m = Word(ps(1), pi(2))
                        Case "WORDS"
                            m = CStr(Words(ps(1)))
                        Case "QUEUED"
                            m = CStr(QStack.Count())
                        Case "EXTERNALS"
                            m = "0"
                        Case "XRANGE"
                            If cL < 1 OrElse Not pm(1) Then ps(1) = Chr(0)
                            If cL < 2 OrElse Not pm(2) Then ps(2) = Chr(255)
                            m = xRange(Asc(ps(1)), Asc(ps(2)))
                        Case "ABS"
                            m = CStr(System.Math.Abs(pr(1)))
                        Case "X2B"
                            m = C2B(X2C(ps(1)))
                        Case "B2X"
                            m = C2X(B2C(ps(1)))
                        Case "X2C"
                            m = X2C(ps(1))
                        Case "C2X"
                            m = C2X(ps(1))
                        Case "ABBREV"
                            If cL < 3 Then pi(3) = ps(2).Length()
                            If Left(ps(1), pi(3)) = Left(ps(2), pi(3)) Then
                                m = "1"
                            Else
                                m = "0"
                            End If
                        Case "TRANSLATE"
                            If cL < 2 Then
                                ps(2) = sAtoZcap
                                ps(3) = sAtoZlow
                            Else
                                If cL < 2 Then ps(2) = xRange(0, 255)
                                If Not pm(3) Then ps(3) = ""
                            End If
                            If cL < 4 Then ps(4) = " "
                            While ps(2).Length() < ps(3).Length() : ps(2) = ps(2) & ps(4) : End While
                            m = Translate(ps(1), ps(3), ps(2))
                        Case "TRUNC"
                            If cL < 2 Then pi(2) = 0
                            num2 = pr(1)
                            For i = 1 To pi(2)
                                num2 = num2 * 10
                            Next
                            num2 = Fix(num2)
                            For i = 1 To pi(2)
                                num2 = num2 / 10
                            Next
                            m = CStr(num2).Replace(","c, "."c)
                            If pm(2) AndAlso pi(2) > 0 Then
                                i = InStr(m, ".")
                                If i = 0 Then
                                    m = m & ".0"
                                    i = InStr(m, ".")
                                End If
                                i = m.Length() - i
                                If i < pi(2) Then
                                    m = m & "".PadRight(pi(2) - i, "0"c)
                                End If
                            End If
                        Case "TIME"
                            Dim tv As String = Format(TimeOfDay, "HH:mm:ss")
                            Dim hh, mm, ss As Integer
                            hh = CInt(tv.Substring(0, 2))
                            mm = CInt(tv.Substring(3, 2))
                            ss = CInt(tv.Substring(6, 2))
                            If cL = 0 Then
                                m = tv
                            Else
                                Dim RstTimer As Double
                                Select Case ps(1).ToUpper(CultInf)
                                    Case "C"
                                        Dim tvs As String = Format(TimeOfDay, "hh:mm:ss")
                                        If hh > 12 Then
                                            m = tvs.Substring(0, 5) & "pm"
                                        Else
                                            m = tvs.Substring(0, 5) & "am"
                                        End If
                                    Case "E"
                                        m = Translate(CStr(CDbl(CStr(VB6.Timer())) - RstTimer), ",", ".")
                                    Case "H"
                                        m = Format(TimeOfDay, "HH")
                                    Case "L"
                                        num2 = VB6.Timer()
                                        num2 = (num2 - Fix(num2)) * 1000.0#
                                        i = CInt(Fix(num2))
                                        m = VB6.Format(TimeOfDay, "HH:mm:ss") & "." & CStr(i)
                                    Case "M"
                                        m = CStr(hh * 60 + mm)
                                    Case "N"
                                        m = tv
                                    Case "R"
                                        m = Translate(CStr(CDbl(CStr(VB6.Timer())) - RstTimer), ",", ".")
                                        RstTimer = VB6.Timer()
                                    Case "S"
                                        m = CStr((hh * 60 + mm) * 60 + ss)
                                End Select
                            End If
                        Case "DATE"
                            If cL = 0 Then
                                m = Format(Today, "d MMMM yyyy")
                            Else
                                Select Case ps(1).ToUpper(CultInf)
                                    Case "B"
                                        m = CStr(DateDiff(VB6.DateInterval.Day, System.DateTime.FromOADate(0), Now))
                                    Case "C"
                                        m = CStr(DateDiff(VB6.DateInterval.Day, DateSerial(0, 1, 1), Now))
                                    Case "D"
                                        m = CStr(DateDiff(VB6.DateInterval.Day, DateSerial(Year(Now), 1, 1), Now))
                                    Case "E"
                                        m = Format(Today, "dd/MM/yy")
                                    Case "J"
                                        m = CStr(CInt(Format(Today, "yy")) * 1000 + DateDiff(VB6.DateInterval.Day, DateSerial(Year(Now), 1, 1), Now))
                                    Case "L"
                                        m = Format(Today, "d MMMM yyyy")
                                    Case "M"
                                        m = Format(Today, "MMMM")
                                    Case "N"
                                        m = Format(Today, "dd MMM yyyy")
                                    Case "O"
                                        m = Format(Today, "yy/MM/dd")
                                    Case "S"
                                        m = Format(Today, "yyyyMMdd")
                                    Case "U"
                                        m = Format(Today, "MM/dd/yy")
                                    Case "W"
                                        m = Format(Today, "dddd")
                                End Select
                            End If
                        Case "FORMAT"
                            If pm(2) Then
                                s = "0".PadLeft(pi(2), "#"c)
                                l1 = pi(2)
                            Else
                                m = CStr(pr(1))
                                If Not DecimalSepPt Then m = Translate(m, ".,", ",.")
                                i = InStr(m, ".")
                                If i > 0 Then
                                    l1 = i - 1
                                Else
                                    l1 = m.Length()
                                End If
                                s = "0".PadLeft(l1, "#"c)
                            End If
                            If pm(3) Then
                                If pi(3) > 0 Then
                                    s = s & "." & "".PadRight(pi(3), "0"c)
                                    l1 = l1 + 1 + pi(3)
                                End If
                            Else
                                m = CStr(pr(1))
                                If Not DecimalSepPt Then m = Translate(m, ".,", ",.")
                                i = InStr(m, ".")
                                If i > 0 Then
                                    i = m.Length() - i
                                    l1 = l1 + 1 + i
                                    s = s & "." & "".PadRight(i, "0"c)
                                End If
                            End If
                            If pr(1) < 0 Then l1 += 1
                            m = Format(pr(1), s).Replace(","c, "."c)
                            m = Right(Space(l1) & m, l1)
                        Case "INSERT"
                            If Not pm(3) Then pi(3) = 0
                            If Not pm(4) Then pi(4) = ps(1).Length()
                            If Not pm(5) Then ps(5) = " "
                            While ps(1).Length() < pi(4) : ps(1) = ps(1) & ps(5) : End While ' str to insert
                            While ps(2).Length() < pi(3) : ps(2) = ps(2) & ps(5) : End While ' base str
                            m = Mid(ps(2), 1, pi(3)) & ps(1) & Mid(ps(2), pi(3) + 1)
                        Case "OVERLAY"
                            If Not pm(3) Then pi(3) = 1
                            If Not pm(4) Then pi(4) = ps(1).Length()
                            If Not pm(5) Then ps(5) = " "
                            While ps(1).Length() < pi(4) : ps(1) = ps(1) & ps(5) : End While ' str to overlay
                            While ps(2).Length() < (pi(3) + pi(4) - 1) : ps(2) = ps(2) & ps(5) : End While ' base str
                            m = Mid(ps(2), 1, pi(3) - 1) & ps(1) & Mid(ps(2), pi(3) + pi(4))
                        Case "RANDOM"
                            Select Case cL
                                Case 0
                                    pi(1) = 0
                                    pi(2) = 999
                                Case 1
                                    pi(2) = pi(1)
                                    pi(1) = 0
                                Case 2, 3
                                    If Not pm(1) Then pi(1) = 0
                                    If Not pm(2) Then pi(2) = 999
                            End Select
                            If Not pm(3) Then
                                num2 = Rnd()
                            Else
                                num2 = Rnd(pi(3))
                            End If
                            num2 = num2 * (pi(1) - pi(2)) + pi(2)
                            m = CStr(Int(num2))
                        Case "LASTPOS"
                            If Not pm(3) Then pi(3) = ps(2).Length()
                            ps(2) = Mid(ps(2), 1, pi(3))
                            m = CStr(InStrRev(ps(2), ps(1)))
                        Case "REVERSE"
                            m = ps(1)
                            l1 = m.Length()
                            For i = 1 To CInt(l1 / 2)
                                s = Mid(m, l1 - i + 1, 1)
                                Mid(m, l1 - i + 1, 1) = Mid(m, i, 1)
                                Mid(m, i, 1) = s
                            Next
                        Case "DELSTR"
                            If Not pm(3) Then pi(3) = ps(1).Length() - pi(2) + 1
                            m = Mid(ps(1), 1, pi(2) - 1) & Mid(ps(1), pi(2) + pi(3))
                        Case "SPLIT"
                            If Not pm(3) Then ps(3) = " "
                            Dim st() As String = ps(2).Split(ps(3))
                            Dim cvr As New DefVariable
                            For i = 1 To st.Length
                                j = SourceNameIndexPosition(ps(1) & "." & CStr(i), Rexx.tpSymbol.tpUnknown, cvr)
                                StoreVar(j, st(i - 1), k, en, n) ' new value
                            Next
                            m = CStr(st.Length)
                        Case "JOIN"
                            If Not pm(3) Then ps(3) = " "
                            Dim cvr As New DefVariable
                            Dim nl As Integer = SourceNameIndexPosition(ps(1) & ".0", Rexx.tpSymbol.tpUnknown, cvr)
                            Int32.TryParse(GetVar(nl, en, n), nl)
                            m = ""
                            For i = 1 To nl
                                j = SourceNameIndexPosition(ps(1) & "." & CStr(i), Rexx.tpSymbol.tpUnknown, cvr)
                                m = m + GetVar(j, en, n)
                                If i < nl Then m += ps(3)
                            Next
                        Case "ROUND"
                            If cL < 2 Then pi(2) = 0
                            m = CStr(Math.Round(pr(1), pi(2)))
                            If Not DecimalSepPt Then m = Translate(m, ".,", ",.")
                        Case "SIGN"
                            If pr(1) = 0 Then
                                m = "0"
                            ElseIf pr(1) > 0 Then
                                m = "1"
                            Else
                                m = "-1"
                            End If
                        Case "DATATYPE"
                            If cL = 1 Then
                                If IsNum(ps(1)) Then
                                    m = "NUM"
                                Else
                                    m = "CHAR"
                                End If
                            Else
                                m = "1"
                                Select Case ps(2).ToUpper(CultInf)
                                    Case "A", "B", "L", "M", "U", "X"
                                        s = ""
                                        ps(2) = ps(2).ToUpper(CultInf)
                                        If ps(2) = "A" Then s = sAtoZ_0to9
                                        If ps(2) = "M" Then s = sAtoZ
                                        If ps(2) = "B" Then s = "01 "
                                        If ps(2) = "L" Then s = "abcdefghijklmnopqrstuvwxyz"
                                        If ps(2) = "U" Then s = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
                                        If ps(2) = "X" Then s = "ABCDEF0123456789 "
                                        If ps(1).Length() = 0 Then m = "0"
                                        For i = 1 To ps(1).Length()
                                            If InStr(1, s, Mid(ps(1), i, 1)) = 0 Then
                                                m = "0"
                                                Exit For
                                            End If
                                        Next
                                    Case "N"
                                        If Not IsNum(ps(1)) Then
                                            m = "0"
                                        End If
                                    Case "W"
                                        If IsNum(ps(1)) Then
                                            If NumY <> CInt(NumY) Then
                                                m = "0"
                                            End If
                                        Else
                                            m = "0"
                                        End If
                                End Select
                            End If
                        Case "VALUE"
                            CurrRexxRun.IdExpose = DirectCast(CurrRexxRun.IdExposeStk.Item(CurrRexxRun.ProcNum + 1), Collection)
                            If Not pm(3) Then
                                i = SourceNameIndexPosition(ps(1).ToUpper(CultInf), tpSymbol.tpUnknown, DefVars)
                                m = GetVar(i, en, n)
                                If cL > 1 Then
                                    StoreVar(i, ps(2), k, en, n) ' new value
                                End If
                            Else
                                m = Environ(ps(1))
                                If IsNothing(m) Then m = ""
                                If pm(2) Then Environ(ps(1) & "=" & ps(2))
                            End If
                        Case "STREAM"
                            Dim str As New aStream
                            Try
                                If ps(2) = "S" Or ps(2) = "D" Then ' give info
                                    If Streams.Contains(ps(1)) Then
                                        str = DirectCast(Streams.Item(ps(1)), aStream)
                                        If str.ErrMsg = "" Then
                                            m = "READY"
                                        Else
                                            m = "NOTREADY "
                                            If ps(2) = "D" Then m = m & str.ErrMsg
                                        End If
                                    Else
                                        m = "NOTREADY"
                                    End If
                                Else ' exec command
                                    m = Word(ps(3), 1)
                                    Select Case m
                                        Case "OPEN"
                                            Dim rw As String = "RW"
                                            m = Word(ps(3), 2)
                                            If m = "READ" Or m = "WRITE" Then
                                                rw = m.Substring(0, 1)
                                                m = Word(ps(3), 3) ' TEXT or BINARY
                                            End If
                                            m = OpenStream(ps(1), rw)
                                        Case "CLOSE"
                                            m = CloseStream(ps(1))
                                        Case "SEEK"
                                            Dim ofs As Integer
                                            str = DirectCast(Streams.Item(ps(1)), aStream)
                                            str.ErrMsg = ""
                                            m = Word(ps(3), 2)
                                            ofs = CInt(m.Substring(1))
                                            If m(0) = "="c Then
                                                str.FileStr.Seek(ofs, SeekOrigin.Begin)
                                            ElseIf m(0) = "<"c Then
                                                str.FileStr.Seek(str.FileStr.Length - ofs, SeekOrigin.Begin)
                                            ElseIf m(0) = "-"c Then
                                                str.FileStr.Seek(str.FileStr.Position - ofs, SeekOrigin.Begin)
                                            ElseIf m(0) = "+"c Then
                                                str.FileStr.Seek(str.FileStr.Position + ofs, SeekOrigin.Begin)
                                            End If
                                            str.ReadPos = str.FileStr.Position
                                            str.WritePos = str.FileStr.Position
                                            m = CStr(str.ReadPos)
                                        Case "QUERY"
                                            m2 = Word(ps(3), 2)
                                            Select Case m2
                                                Case "EXIST"
                                                    If File.Exists(ps(1)) Then
                                                        m = Path.GetFullPath(ps(1))
                                                    Else
                                                        m = ""
                                                        str.ErrMsg = "FILE NOT FOUND"
                                                    End If
                                                Case "SIZE"
                                                    If File.Exists(ps(1)) Then
                                                        Dim inforf As System.IO.FileInfo
                                                        inforf = My.Computer.FileSystem.GetFileInfo(ps(1))
                                                        m = CStr(inforf.Length)
                                                    Else
                                                        m = "ERROR"
                                                        str.ErrMsg = "FILE NOT FOUND"
                                                    End If
                                                Case "DATETIME"
                                                    If File.Exists(ps(1)) Then
                                                        Dim inforf As System.IO.FileInfo
                                                        inforf = My.Computer.FileSystem.GetFileInfo(ps(1))
                                                        m = CStr(inforf.LastWriteTime)
                                                    Else
                                                        m = "ERROR"
                                                        str.ErrMsg = "FILE NOT FOUND"
                                                    End If
                                            End Select
                                    End Select
                                End If
                            Catch e As Exception
                                m = "ERROR"
                                str.ErrMsg = e.Message
                            End Try
                        Case "LINES"
                            Dim str As New aStream, rp As Long
                            If Not Streams.Contains(ps(1)) Then
                                m = OpenStream(ps(1), "R")
                            Else
                                m = ""
                            End If
                            If m <> "ERROR" Then
                                Dim tCnt As String
                                If pm(2) Then
                                    tCnt = ps(2)
                                Else
                                    tCnt = "C" ' C=Count N=Report EOF
                                End If
                                str = DirectCast(Streams.Item(ps(1)), aStream)
                                Dim returnCount As Integer
                                returnCount = 0 ' no lines found
                                If tCnt = "C" Then ' count nr of lines
                                    rp = str.ReadPos
                                    Dim crlf As Integer
                                    While str.ReadPos < str.FileStr.Length()
                                        s = StreamReadLine(str, crlf)
                                        str.ReadPos += s.Length() + crlf
                                        returnCount = returnCount + 1
                                        If CancRexx Then
                                            Exit While
                                        End If
                                    End While
                                    str.ReadPos = rp
                                Else ' 1 if still input te be read
                                    If str.ReadPos < str.FileStr.Length() Then
                                        returnCount = 1
                                    End If
                                End If
                                m = CStr(returnCount)
                            Else
                                m = "0"
                            End If
                        Case "LINEIN"
                            If Not Streams.Contains(ps(1)) Then
                                m = OpenStream(ps(1), "R")
                            Else
                                m = ""
                            End If
                            If m <> "ERROR" Then
                                Dim str As aStream, crlf As Integer, nLns As Integer
                                str = DirectCast(Streams.Item(ps(1)), aStream)
                                If pm(2) Then GotoStreamLine(str, pi(2)) ' start at line
                                If pm(3) Then
                                    nLns = pi(3)
                                Else
                                    nLns = 1
                                End If
                                m = ""
                                For i = 1 To nLns
                                    If i > 1 Then m = m & vbCrLf
                                    s = StreamReadLine(str, crlf)
                                    m = m & s
                                    str.ReadPos += s.Length() + crlf
                                    If CancRexx Then
                                        Exit For
                                    End If
                                Next
                            End If
                        Case "LINEOUT"
                            If Not Streams.Contains(ps(1)) Then
                                m = OpenStream(ps(1), "W")
                            Else
                                m = ""
                            End If
                            If m <> "ERROR" Then
                                Dim str As aStream
                                str = DirectCast(Streams.Item(ps(1)), aStream)
                                If Not str.FileStr.CanWrite Then
                                    CloseStream(ps(1))
                                    m = OpenStream(ps(1), "W")
                                    str = DirectCast(Streams.Item(ps(1)), aStream)
                                End If
                                If m <> "ERROR" Then
                                    If Not pm(2) And Not pm(3) Then
                                        m = CloseStream(ps(1))
                                    Else
                                        If pm(3) Then
                                            GotoStreamLine(str, pi(3)) ' start at line
                                            str.WritePos = str.ReadPos
                                        End If
                                        m = StreamWriteLine(str, ps(2), True)
                                        str.WritePos += ps(2).Length() + 2
                                    End If
                                End If
                            End If
                        Case "CHARS"
                            If Not Streams.Contains(ps(1)) Then
                                m = OpenStream(ps(1), "R")
                            Else
                                m = ""
                            End If
                            If m <> "ERROR" Then
                                Dim str As aStream
                                str = DirectCast(Streams.Item(ps(1)), aStream)
                                m = CStr(str.FileStr.Length - str.FileStr.Position)
                            End If
                        Case "CHARIN"
                            If Not Streams.Contains(ps(1)) Then
                                m = OpenStream(ps(1), "R")
                            Else
                                m = ""
                            End If
                            If m <> "ERROR" Then
                                Dim str As aStream, mc As Integer
                                str = DirectCast(Streams.Item(ps(1)), aStream)
                                If pm(2) Then
                                    str.ReadPos = pi(2) - 1
                                End If
                                If pm(3) Then
                                    mc = pi(3)
                                Else
                                    mc = 1
                                End If
                                str.FileStr.Position = str.ReadPos
                                m = StreamReadChar(str, mc)
                                str.ReadPos += m.Length()
                            End If
                        Case "CHAROUT"
                            If Not Streams.Contains(ps(1)) Then
                                m = OpenStream(ps(1), "W")
                            Else
                                m = ""
                            End If
                            If m <> "ERROR" Then
                                Dim str As aStream, mc As Integer
                                str = DirectCast(Streams.Item(ps(1)), aStream)
                                If Not str.FileStr.CanWrite Then
                                    CloseStream(ps(1))
                                    m = OpenStream(ps(1), "W")
                                    str = DirectCast(Streams.Item(ps(1)), aStream)
                                End If
                                If m <> "ERROR" Then
                                    If Not pm(2) And Not pm(3) Then
                                        m = CloseStream(ps(1))
                                    Else
                                        If pm(3) Then
                                            str.WritePos = pi(3) - 1
                                        End If
                                        If pm(4) Then
                                            mc = pi(4)
                                        Else
                                            mc = ps(2).Length
                                        End If
                                        m = StreamWriteLine(str, ps(2).Substring(0, mc), False)
                                        str.WritePos += mc
                                    End If
                                End If
                            End If
                        Case "REGEXP"
                            Dim mc As MatchCollection
                            Dim rVar As String = ps(1)
                            Dim rData As String = ps(2)
                            Dim rExpr As String = ps(3)
                            Dim cvr As DefVariable = Nothing
                            Dim regExpre As New Regex(rExpr)
                            mc = regExpre.Matches(rData)
                            Dim nv As Integer = 0
                            For Each mt As Match In mc
                                If mt.Value.Length > 0 Then
                                    nv += 1
                                    i = SourceNameIndexPosition(rVar & "." & CStr(nv), Rexx.tpSymbol.tpUnknown, cvr)
                                    StoreVar(i, mt.Value, k, en, n) ' new value
                                End If
                            Next
                            i = SourceNameIndexPosition(rVar & ".0", Rexx.tpSymbol.tpUnknown, cvr)
                            StoreVar(i, CStr(nv), k, en, n) ' new value
                                If nv > 0 Then
                                    m = "1"
                                Else
                                    m = "0"
                                End If
                            Case "CLIPBOARD"
                                If Not pm(1) Then
                                    m = My.Computer.Clipboard.GetText()
                                Else
                                    My.Computer.Clipboard.SetText(ps(1))
                                End If
                        End Select
                        StoreVar(CurrRexxRun.iRes, m, k, en, n) ' result
                Case fct.say
                    If CurrRexxRun.InInteractive Then
                        MsgBox(FromStack, MsgBoxStyle.OkOnly, "say")
                    Else
                        AddLineToSaybuffer(FromStack, False)
                    End If
                Case fct.adr
                    n = GetLit(cA)
                    If (cL > 0) Then CurrRexxRun.AddressS = n
                    CurrRexxRun.CAddress = n
                Case fct.cal
                    If (CurrRexxRun.TraceLevel >= 2) Then
                        TracePLin(cA)
                    End If
                    SaveRegs()
                    IntPp = cA
                    asm = DirectCast(CurrRexxRun.IntCode.Item(IntPp), AsmStatement) ' intpp -> lin, intPp+1 -> lbl
                    If asm.f = fct.lin Then
                        IntPp += 1
                        asm = DirectCast(CurrRexxRun.IntCode.Item(IntPp), AsmStatement) ' intpp -> lin, intPp+1 -> lbl
                    End If
                    rtSym = DirectCast(asm.l, Symbols)
                    If (rtSym = Symbols.procsym) Then
                        CurrRexxRun.CallLevel += 1
                        CurrRexxRun.ProcNum = asm.a
                        Dim cse As New CallElem
                        cse.ProcNum = asm.a
                        cse.Exposes = DirectCast(CurrRexxRun.IdExposeStk(asm.a + 1), Collection)
                        cse.InternDepth = 0
                        CurrRexxRun.CallStack.Add(cse)
                    Else
                        Dim cse As CallElem = DirectCast(CurrRexxRun.CallStack(CurrRexxRun.CallStack.Count), CallElem)
                        cse.InternDepth += 1
                    End If
                    asm = DirectCast(CurrRexxRun.IntCode.Item(IntPp - 1), AsmStatement)
                    If asm.f = fct.lin Then ' also trace previous sourceline
                        IntPp -= 1
                    End If
                Case fct.exi, fct.ret ' exit/return
                    Dim cse As CallElem = DirectCast(CurrRexxRun.CallStack(CurrRexxRun.CallStack.Count), CallElem)
                    If (CurrRexxRun.CallStack.Count = 1 AndAlso cse.InternDepth = 0) OrElse cF = fct.exi Then
                        If ExtRoutine Then ' result from external routine may be alphanumeric
                            m = GetVar((CurrRexxRun.iRes), en, n)
                            CurrRexxRun.Result = m
                            CurrRexxRun.Rc = 0
                        Else
                            m = GetVar((CurrRexxRun.iRes), en, n)
                            CurrRexxRun.Rc = CInt(StrFl(m))
                        End If
                        IntPp = CurrRexxRun.IntCode.Count()
                    Else
                        i = RestRegs()
                        cse.InternDepth -= 1
                        If cse.InternDepth < 0 Then ' return to calling block
                            CurrRexxRun.CallStack.Remove(CurrRexxRun.CallStack.Count())
                            CurrRexxRun.CallLevel -= 1
                            Dim nDel As Integer = 0
                            Dim nVar As Integer = CurrRexxRun.RuntimeVars.Count() - i
                            For j = 1 To nVar
                                Dim rtv As VariabelRun = DirectCast(CurrRexxRun.RuntimeVars(i + j - nDel), VariabelRun)
                                If rtv.Level > CurrRexxRun.CallLevel Then
                                    CurrRexxRun.RuntimeVars.Remove(i + j - nDel)
                                    nDel += 1
                                Else
                                    rtv.ArrayIndex -= nDel
                                End If
                            Next
                        End If
                        asm = DirectCast(CurrRexxRun.IntCode.Item(IntPp), AsmStatement)
                        IntPp = IntPp + asm.l
                    End If
                        '  End If
                Case fct.exc ' external command
                    m = FromStack()
                    Logg("EXECUTE: " & m)
                    DoExc(m)
                Case fct.cle ' external rex
                    CommandLine = GetLit(cA)
                    ' compose parameterstring to be passed to routine to be split up by ARGS
                    m = ExtRoutineTagstring
                    m2 = ""
                    For i = 1 To cL
                        asmp1 = DirectCast(CurrRexxRun.IntCode.Item(IntPp + i), AsmStatement)
                        VariaRuns = GetNm(asmp1.a, n, en, k)
                        m = m & m2 & VariaRuns.IdValue
                        m2 = X2C(ExtRoutineParmSep)
                    Next
                    CommandPrm = m
                    SaveRegs()
                    Dim savCurrRexxRun As RexxCompData = CurrRexxRun
                    CurrRexxRun = New RexxCompData
                    Dim rc As Integer
                    Dim result As String = ""
                    rc = CompileRexxScript(CommandLine)
                    If rc = 0 Then
                        rc = ExecuteRexxScript(CommandPrm)
                        result = CurrRexxRun.Result
                    End If
                    CurrRexxRun = savCurrRexxRun
                    savCurrRexxRun = Nothing
                    RestRegs()
                    StoreVar(CurrRexxRun.iRc, CStr(rc), k, en, n)
                    StoreVar(CurrRexxRun.iRes, result, k, en, n)
                    NrStepsExecuted += 1000 'to respond quicker to interrupts
                    IntPp = IntPp + cL
                Case fct.tra
                    If CurrRexxRun.InteractiveTracing <> 1 Then
                        If cA = 5 Then ' start or stop trace of say
                            If Not TracingSay Then
                                sayFile = File.CreateText(RexxFileName & ".Say.Log.txt") ' new name for each rexxfile
                                TracingSay = True
                            Else
                                sayFile.Close()
                                TracingSay = False
                            End If
                        Else
                            CurrRexxRun.InteractiveTracing = cL
                            If (CurrRexxRun.TraceLevel <= 2 And cA > 2) Or CurrRexxRun.InteractiveTracing = 1 Then
                                CurrRexxRun.TraceLevel = cA
                                i = SrcLstLin
                                traceLin(i)
                            Else
                                CurrRexxRun.TraceLevel = cA
                            End If
                        End If
                    Else
                        CurrRexxRun.TracingResume = cA
                    End If
                Case fct.itp
                    MemorySource = FromStack()
                    CurrRexxRun.InInterpret = True
                    SaveRegs()
                    If CompileRexxScript("In memory source") = 0 Then
                        ExecuteRexxScript("")
                    End If
                    RestRegs()
                    CurrRexxRun.InInterpret = False
                Case fct.lbl
                    If (CurrRexxRun.TraceLevel = 2) Then
                        TracePLin(IntPp)
                    End If
                Case fct.sig
                    If cL = 1 Then
                        CurrRexxRun.sigNovalue = True
                        CurrRexxRun.sigLabel = asm.a
                    Else
                        CurrRexxRun.sigNovalue = False
                    End If
                Case fct.drp
                    If (cL <> 1) Then
                        VariaRuns = GetNm(cA, n, en, k)
                        If k > 0 Then
                            CurrRexxRun.RuntimeVars.Remove(k)
                            For kl As Integer = k To CurrRexxRun.RuntimeVars.Count
                                VariaRuns = DirectCast(CurrRexxRun.RuntimeVars(kl), VariabelRun)
                                VariaRuns.ArrayIndex -= 1
                            Next
                        End If
                    Else
                        n = DirectCast(CurrRexxRun.IdName.Item(cA), DefVariable).Id
                        i = n.Length()
                        k = 1
                        For Each VR As VariabelRun In CurrRexxRun.RuntimeVars
                            If Mid(VR.Id, 1, i) = n Then
                                CurrRexxRun.RuntimeVars.Remove(k)
                                For kl As Integer = k To CurrRexxRun.RuntimeVars.Count
                                    VariaRuns = DirectCast(CurrRexxRun.RuntimeVars(kl), VariabelRun)
                                    VariaRuns.ArrayIndex -= 1
                                Next
                            Else
                                k = k + 1
                            End If
                        Next VR
                    End If
                Case fct.upp ' parse UPPER ...
                    UpCase = (cA = 1)
                    LwCase = (cA = 2)
                Case fct.arg ' parse arg
                    DoParse(CommandParm) ' internal values, false = external parameter
                Case fct.pul ' parse pull
                    DoParse("")
                Case fct.pvl ' parse value
                    DoParse("")
                Case fct.pvr ' parse var
                    DoParse("")
                Case fct.stk ' push/queue
                    m = FromStack()
                    If cL = 0 Or QStack.Count() = 0 Then
                        QStack.Add(m) ' 0 = push
                    Else
                        QStack.Add(m, , 1) ' 1 = queue
                    End If
                Case fct.upc 'upper/lower
                    Dim cintPP As Integer = IntPp
                    m = GetVarNV(cL, en, n, IntPp)
                    If IntPp = cintPP Then  ' in case of "NOVALUE" trapped, there is no value and execution continues elsewhere
                        If cA = 1 Then
                            m = m.ToUpper(CultInf)
                        Else
                            m = m.ToLower(CultInf)
                        End If
                        StoreVar(cL, m, k, en, n)
                    End If
                Case Else
                    MsgBox("Internal error in REXX interpretor: " & CStr(cF) & " not implemented yet.")
            End Select
            NrStepsExecuted += 1
            If NrStepsExecuted > 30000 Then
                RaiseEvent doStep()
            End If
            If CancRexx Then
                Exit While
            End If
            IntPp = IntPp + 1
        End While
#If Not DEBUG Then
        Catch e As Exception
            CurrRexxRun.Rc = 256
            MsgBox("Internal error in REXX interpretor: " & Err.Description & vbCrLf & "Current ReXXfile is: " & RexxFileName & vbCrLf & "Current executing line is: " & CStr(SrcLstLin) & vbCrLf & ".Net Full error description: " & vbCrLf & Err.GetException().ToString)
        End Try
#End If
    End Sub
    Sub DoExc(m As String)
        Dim en As String = "", n As String = ""
        Dim k As Integer
        If (CurrRexxRun.CAddress = "") Then CurrRexxRun.CAddress = CurrRexxRun.AddressS
        If (CurrRexxRun.CAddress = "") Then CurrRexxRun.CAddress = "XEDIT"
        Logg("""" & CurrRexxRun.CAddress.ToUpper(CultInf) & """")
        Dim savLenStack = Stack.Count
        SaveRegs()
        Dim savRun As RexxCompData = CurrRexxRun
        Dim e As New RexxEvent
        RaiseEvent doCmd(CurrRexxRun.CAddress.ToUpper(CultInf), m, e)
        CurrRexxRun = savRun
        savRun = Nothing
        While Stack.Count > savLenStack
            RestRegs()
        End While
        CurrRexxRun.Rc = e.rc
        StoreVar(CurrRexxRun.iRc, CStr(CurrRexxRun.Rc), k, en, n)
        If CurrRexxRun.Rc < 0 Then
            AddLineToSaybuffer("Cmd: " & m, False)
            AddLineToSaybuffer(" RC = " & CStr(CurrRexxRun.Rc), True)
        End If
        CurrRexxRun.CAddress = ""
    End Sub
    Private Sub SaveRegs()
        ' Debug.WriteLine("Save regs " + IntPp.ToString + " " + Stack.Count.ToString)
        Stack.Add(CurrRexxRun.SrcLine) ' interpret ruins
        Stack.Add(CurrRexxRun.InteractiveTracing)
        Stack.Add(CurrRexxRun.TraceLevel)
        Stack.Add(CurrRexxRun.RuntimeVars.Count())
        Stack.Add(CurrRexxRun.ProcNum)
        Stack.Add(IntPp)
    End Sub
    Private Function RestRegs() As Integer
        IntPp = CInt(FromStack())
        CurrRexxRun.ProcNum = CInt(FromStack())
        Dim i As Integer = CInt(FromStack())
        CurrRexxRun.TraceLevel = CInt(FromStack())
        CurrRexxRun.InteractiveTracing = CInt(FromStack())
        CurrRexxRun.SrcLine = CInt(FromStack())
        'Debug.WriteLine("Rest regs " + IntPp.ToString + " " + Stack.Count.ToString)
        Return i
    End Function
    Private Function OpenStream(ByVal fn As String, ByVal rw As String) As String
        Dim m As String
        If Streams.Contains(fn) Then
            m = "ERROR"
            Dim str As aStream
            str = DirectCast(Streams.Item(fn), aStream)
            str.ErrMsg = "Already Open"
        Else
            m = "READY"
            Dim str As New aStream
            Try
                If rw = "R" Then
                    str.FileStr = New FileStream(fn, FileMode.Open, FileAccess.Read)
                Else
                    str.FileStr = New FileStream(fn, FileMode.OpenOrCreate, FileAccess.ReadWrite)
                End If
                str.OpenClosed = True
                str.ReadPos = 0
                str.WritePos = str.FileStr.Length()
                Streams.Add(str, fn)
            Catch e As Exception
                m = "ERROR"
                str.ErrMsg = e.Message
            End Try
        End If
        Return m
    End Function
    Private Function StreamReadLine(ByVal str As aStream, ByRef CrLf As Integer) As String
        Dim s As String = ""
        Dim goOn As Boolean = True
        str.ErrMsg = ""
        Try
            str.FileStr.Position = str.ReadPos
            Dim ch As Integer = str.FileStr.ReadByte()
            If ch = -1 Then Return ("")
            Dim ch2 As Integer
            CrLf = 0
            While goOn And str.FileStr.Position() <= str.FileStr.Length()
                If ch = 13 Or ch = 10 Then
                    goOn = False
                    CrLf = 1
                    If str.FileStr.Position() < str.FileStr.Length() Then
                        ch2 = str.FileStr.ReadByte()
                    Else
                        ch2 = 0
                    End If
                    If (ch2 = 10 Or ch2 = 13) And ch <> ch2 Then
                        CrLf = 2
                    End If
                Else
                    s = s & Chr(ch)
                    If str.FileStr.Position() < str.FileStr.Length() Then
                        ch = str.FileStr.ReadByte()
                    Else
                        goOn = False
                    End If
                End If
                If CancRexx Then
                    Exit While
                End If
            End While
        Catch e As Exception
            str.ErrMsg = e.Message
        End Try
        Return s
    End Function
    Private Function StreamReadChar(ByVal str As aStream, ByVal nCh As Integer) As String
        Dim s As String = ""
        Dim goOn As Boolean = True
        Dim iCh As Integer
        str.ErrMsg = ""
        Try
            str.FileStr.Position = str.ReadPos
            Dim ch As Integer
            If nCh > 0 Then
                ch = str.FileStr.ReadByte()
            Else
                goOn = False
            End If
            While goOn And str.FileStr.Position() <= str.FileStr.Length()
                s = s & Chr(ch)
                iCh += 1
                If str.FileStr.Position() < str.FileStr.Length() And iCh < nCh Then
                    ch = str.FileStr.ReadByte()
                Else
                    goOn = False
                End If
                If CancRexx Then
                    Exit While
                End If
            End While
        Catch e As Exception
            str.ErrMsg = e.Message
        End Try
        Return s
    End Function
    Private Function StreamWriteLine(ByVal str As aStream, ByVal s As String, ByVal CrLf As Boolean) As String
        Dim m As String = "0"
        Dim maxChars As Integer = s.Length()
        If CrLf Then maxChars += 2
        Dim buf(maxChars) As Byte
        If CrLf Then
            buf = System.Text.Encoding.Default.GetBytes(s & vbCrLf)
        Else
            buf = System.Text.Encoding.Default.GetBytes(s)
        End If
        Try
            str.FileStr.Position = str.WritePos
            str.FileStr.Write(buf, 0, maxChars)
        Catch e As Exception
            m = "ERROR"
            str.ErrMsg = e.Message
        End Try
        Return m
    End Function
    Private Sub GotoStreamLine(ByVal str As aStream, ByVal linenr As Integer)
        Dim i As Integer = 1 ' linenr to be read
        Dim crlf As Integer, s As String
        str.ReadPos = 0
        str.FileStr.Position = str.ReadPos
        While str.ReadPos < str.FileStr.Length() And i < linenr
            s = StreamReadLine(str, crlf)
            str.ReadPos += s.Length() + crlf
            i += 1
            If CancRexx Then
                Exit While
            End If
        End While
    End Sub
    Private Function CloseStream(ByVal fn As String) As String
        Dim m As String
        If Not Streams.Contains(fn) Then
            m = "ERROR"
        Else
            Dim str As aStream
            str = DirectCast(Streams.Item(fn), aStream)
            Try
                str.FileStr.Close()
                str.FileStr.Dispose()
                Streams.Remove(fn)
                m = "READY"
            Catch e As Exception
                m = "ERROR"
                str.ErrMsg = e.Message
            End Try
        End If
        Return m
    End Function
    Private Function FromStack() As String
        Dim s As String
        'If Stack.Count = 0 Then
        '    s = ""
        '    Return s
        'End If
        s = CStr(Stack.Item(Stack.Count()))
        Stack.Remove(Stack.Count())
        Return s
    End Function
    Private Function GetLit(ByRef a As Integer) As String
        GetLit = CStr(CurrRexxRun.TxtValue.Item(a))
    End Function
    Private NovalueDetect As Boolean
    Private Function GetVarNV(ByRef VarPosition As Integer, ByRef ExeName As String, ByRef RexxName As String, ByRef intPP As Integer) As String
        NovalueDetect = False
        GetVarNV = GetVar(VarPosition, ExeName, RexxName)
        If NovalueDetect Then
            If CurrRexxRun.sigLabel = 0 Then
                RunError(2, RexxName)
            Else
                CurrRexxRun.sigNovalue = False ' don't signal in a signal trap
                StoreVar(CurrRexxRun.iSigl, CStr(SrcLstLin), 0, "SIGL", "SIGL")
                intPP = CurrRexxRun.sigLabel - 1
            End If
        End If
    End Function
    Public Function GetVar(ByRef VarPosition As Integer, ByRef ExeName As String, ByRef RexxName As String) As String
        Dim k As Integer
        Dim VarName As String
        DefVars = DirectCast(CurrRexxRun.IdName.Item(VarPosition), DefVariable)
        VarName = DefVars.Id
        RexxName = VarName
        ExeName = SubstIndices(VarName)
        GetVar = VarName.ToUpper
        'Dim dbg As String
        k = VarIndex(ExeName, False) ' don't create if not exists
        If k > 0 Then
            Dim rtv As VariabelRun = DirectCast(CurrRexxRun.RuntimeVars(k), VariabelRun)
            GetVar = rtv.IdValue
            ' dbg = "lod " + ExeName + " |" + rtv.IdValue + "| x: " + CStr(k)
        Else
            Dim i As Integer = VarName.IndexOf("."c)
            If i > -1 And i <> VarName.Length() - 1 Then
                VarPosition = SourceNameIndexPosition(VarName.Substring(0, i + 1), tpSymbol.tpVariable, DefVars)
                If VarPosition > 0 Then
                    Dim valu As String = GetVar(VarPosition, (ExeName), (RexxName)) ' stem
                    GetVar = valu
                    'dbg = "lod " + ExeName + " |" + valu + "| x: " + CStr(k)
                End If
            Else
                If CurrRexxRun.sigNovalue Then
                    NovalueDetect = True
                End If
            End If
        End If
        If (CurrRexxRun.TraceLevel > 3) Then
#If Not DEBUG Then
            If Mid(ExeName, 2, 1) <> " " Then
#End If
            traceVar("v", RexxName, GetVar)
#If Not DEBUG Then
            End If
#End If
        End If
        Logg("Get: " & RexxName & "=|" & GetVar & "|")
    End Function
    Private Function SubstIndices(VarName As String) As String
        Dim i, k As Integer
        Dim m As String
        Dim Var As VariabelRun
        Dim nameParts() As String = VarName.Split("."c)
        If nameParts.Length() > 1 Then
            For i = 1 To nameParts.Length() - 1
                m = nameParts(i)
                k = VarIndex(m, False)
                If k > 0 Then
                    Var = DirectCast(CurrRexxRun.RuntimeVars(k), VariabelRun)
                    Dim Val As String = Var.IdValue
                    If (CurrRexxRun.TraceLevel > 3) Then traceVar("v", m, Val)
                    nameParts(i) = Val
                End If
            Next
            VarName = [String].Join("."c, nameParts)
        End If
        Return VarName
    End Function
    Public Sub StoreVar(ByVal VarPosition As Integer, ByVal VarValue As String, ByRef VarExePosition As Integer, ByRef ExeName As String, ByRef RexxName As String, Optional nextLev As Integer = 0)
        Dim k As Integer
        Dim VarName As String
        Dim Var As VariabelRun
        DefVars = DirectCast(CurrRexxRun.IdName.Item(VarPosition), DefVariable)
        VarName = DefVars.Id
        RexxName = VarName
        ExeName = SubstIndices(VarName)
        If nextLev = 0 Then
            k = VarIndex(ExeName, True) ' create if not exists
        Else
            Dim RunLvl As Integer = CurrRexxRun.CallStack.Count - 1
            If RunLvl = -1 Then RunLvl = 0
            k = VarIndex(ExeName, True, RunLvl + nextLev) ' create if not exists
        End If
        Var = DirectCast(CurrRexxRun.RuntimeVars(k), VariabelRun)
        'Dim dbg As String = "sto " + VarName + " " + CStr(Var.Level) + " : |" + VarValue + "| " + " x:" + CStr(k)
        Var.IdValue = VarValue
        VarExePosition = k
        If (CurrRexxRun.TraceLevel >= 3) Then
#If Not DEBUG Then
            If Mid(VarName, 2, 1) <> " " Then ' not internal variable
#End If
            traceVar("r", VarName, VarValue)
#If Not DEBUG Then
            End If
#End If
        End If
        Logg("Assign: " & VarName & "=|" & VarValue & "|")
    End Sub
    Private Function VarIndex(VarName As String, Create As Boolean, Optional RunLvlSpec As Integer = -1) As Integer
        Dim Retval As Integer = 0
        Dim RunLvl As Integer = RunLvlSpec
        If RunLvl = -1 Then RunLvl = CurrRexxRun.CallStack.Count - 1
        If RunLvl = -1 Then RunLvl = 0
        'Dim RunLvlp As Integer = RunLvl
        Dim key As String = CStr(RunLvl) & VarName
        If CurrRexxRun.RuntimeVars.Contains(key) Then
            Dim Var As VariabelRun = DirectCast(CurrRexxRun.RuntimeVars(key), VariabelRun)
            Retval = Var.ArrayIndex
        Else
            If IsExposed(VarName, RunLvl) Then
                Retval = VarIndex(VarName, Create, RunLvl - 1) ' create if not exists 
            Else
                Retval = 0
            End If
        End If
        If Retval = 0 AndAlso Create Then
            VariaRuns = New VariabelRun
            VariaRuns.Id = VarName
            VariaRuns.Level = RunLvl
            VariaRuns.IdValue = VarName
            VariaRuns.ArrayIndex = CurrRexxRun.RuntimeVars.Count + 1
            CurrRexxRun.RuntimeVars.Add(VariaRuns, CStr(RunLvl) & VarName)
            Retval = CurrRexxRun.RuntimeVars.Count
        End If
        Return Retval
    End Function
    Private Function IsExposed(Varname As String, RunLvl As Integer) As Boolean
        Dim cse As CallElem = Nothing
        If RunLvl >= CurrRexxRun.CallStack.Count Then
            Return False ' parameter for next call level
        Else
            cse = DirectCast(CurrRexxRun.CallStack(RunLvl + 1), CallElem)
            If Not cse.Exposes Is Nothing Then
                For Each s As String In cse.Exposes
                    If s = Varname Then
                        Return True
                    End If
                Next
            End If
            Dim i As Integer = Varname.IndexOf("."c)
            If i = -1 Or i = Varname.Length() - 1 Then
                Return False
            End If
            Return IsExposed(Varname.Substring(0, i + 1), RunLvl) ' var.
        End If
    End Function
    Private Function GetNm(ByVal a As Integer, ByRef n As String, ByRef en As String, ByRef k As Integer) As VariabelRun
        DefVars = DirectCast(CurrRexxRun.IdName.Item(a), DefVariable)
        k = VarIndex(DefVars.Id, False) ' don't create if not exists
        If k = 0 Then
            Return Nothing
        Else
            Return DirectCast(CurrRexxRun.RuntimeVars(k), VariabelRun)
        End If
    End Function
    Private Function GetNmFunc(ByVal a As Integer, ByRef n As String, ByRef en As String, ByRef k As Integer) As VariabelRun
        DefVars = DirectCast(CurrRexxRun.IdName.Item(a), DefVariable)
        k = VarIndex(DefVars.Id, False, -1) ' don't create if not exists
        If k = 0 Then
            Return Nothing
        Else
            Return DirectCast(CurrRexxRun.RuntimeVars(k), VariabelRun)
        End If
    End Function
    Private Sub RunError(ByRef n As Integer, ByRef s As String)
        Dim se As String = ""
        If LineExecuting > 0 Then
            se = CStr(LineExecuting) + " " + GetSLin(LineExecuting)
        End If
        If MsgBox(SysMsg(200 + n) + " '" + s + "'" + vbCrLf + se, MsgBoxStyle.OkCancel) = MsgBoxResult.Cancel Then
            RaiseEvent doCancel()
            CancRexx = True
        End If
    End Sub
    Private Function GetSLin(ByRef SrcLineNr As Integer) As String
        Dim lin As LineOfSource = DirectCast(CurrRexxRun.Source(SrcLineNr), LineOfSource)
        Return lin.Text
    End Function
    Private Sub traceLab(ByRef SrcLine As String)
        traceStr(">l>     " & SrcLine)
    End Sub
    Private Sub traceLin(ByRef h As Integer)
        If Not CurrRexxRun.InInterpret Then ' don't trace interpret compiled source
            traceStr("*-* " & CStr(h) & " " & GetSLin(h))
        End If
    End Sub
    Private Sub traceStr(ByRef SrcLine As String)
        AddLineToSaybuffer(SrcLine, False)
        If CurrRexxRun.InteractiveTracing = 1 Then
            Dim s As String = myInputBox(ComposeSayFromBuffer() + vbCrLf + "Interactive", "Enter statement or ?", "") + "      "
            lSay = 0
            s = s.TrimStart
            Do While s.Trim <> ""
                Dim sUpper As String = s.ToUpper.Trim
                If sUpper = "OFF" Then
                    CurrRexxRun.InteractiveTracing = 0
                    CurrRexxRun.TraceLevel = CurrRexxRun.TracingResume
                    s = ""
                ElseIf sUpper = "?" Then
                    MsgBox("Enter a any valid REXX statement, to modify variables or the execution of the script" & vbCrLf &
                           "Enter OFF to terminate interactive trace and resume execution" & vbCrLf &
                           "Enter L linenumber [linenumberTo] to list the source" & vbCrLf &
                           "Enter B linenumber to set a break at the given line" & vbCrLf &
                           "Enter C linenumber to remove the break" & vbCrLf &
                           "Enter G linenumber to resume execution at the given line" & vbCrLf &
                           "  (Take care not to jump into loops or routines)" & vbCrLf &
                           "Press only Enter to continue interactive trace" & vbCrLf)
                    s = myInputBox("Interactive", "Enter statement or ?", "")
                ElseIf sUpper.Length > 2 AndAlso (sUpper(0) = "B"c Or sUpper(0) = "C"c Or sUpper(0) = "G"c) Then
                    Dim nl As Integer
                    Try
                        nl = sUpper.IndexOf(" ")
                        nl = CInt(sUpper.Substring(nl))
                    Catch ex As Exception
                    End Try
                    Dim asm As AsmStatement
                    Dim cL, cA As Integer
                    Dim cF As fct
                    For lIntPP As Integer = 1 To CurrRexxRun.IntCode.Count() - 1
                        asm = DirectCast(CurrRexxRun.IntCode.Item(lIntPP), AsmStatement)
                        cF = asm.f
                        cL = asm.l
                        cA = asm.a
                        If cF = fct.lin Then
                            If cL = nl Then
                                If sUpper(0) = "G"c Then
                                    IntPp = lIntPP - 1
                                    Exit Sub
                                ElseIf sUpper(0) = "B"c Then
                                    asm.a = 1
                                Else
                                    asm.a = 0
                                End If
                            End If
                        End If
                    Next
                    s = myInputBox("Interactive", "Enter statement or ?", "")
                ElseIf sUpper.Length > 2 AndAlso (sUpper(0) = "L"c) Then
                    Dim il, nl, ml As Integer
                    Try
                        il = sUpper.IndexOf(" ")
                        sUpper = sUpper.Substring(il + 1)
                        il = sUpper.IndexOf(" ")
                        If il > 0 Then
                            nl = CInt(sUpper.Substring(0, il))
                            ml = CInt(sUpper.Substring(il))
                        Else
                            nl = CInt(sUpper)
                            ml = nl
                        End If
                    Catch ex As Exception
                    End Try
                    s = ""
                    If nl < 1 Then nl = 1
                    If ml > CurrRexxRun.Source.Count Then
                        ml = CurrRexxRun.Source.Count
                    End If
                    For il = nl To ml
                        s = s & CStr(il) & ": " & GetSLin(il) & vbCrLf
                    Next
                    s = myInputBox(s & "Interactive", "Enter statement or ?", "")
                Else
                    Dim wfn As String
                    wfn = CreateNameOfWorkfile()
                    Dim sw As New StreamWriter(wfn)
                    sw.Write("/* */ " & s & ";")
                    sw.Close()
                    CurrRexxRun.InInterpret = True
                    CurrRexxRun.InInteractive = True
                    Dim sTrace As Boolean
                    sTrace = RexxTrace
                    RexxTrace = False
                    SaveRegs()
                    If CompileRexxScript(wfn) = 0 Then
                        ExecuteRexxScript("")
                    End If
                    RestRegs()
                    RexxTrace = sTrace
                    CurrRexxRun.InInterpret = False
                    CurrRexxRun.InInteractive = False
                    Try
                        File.Delete(wfn)
                    Catch ex As Exception
                    End Try
                    s = myInputBox(s + vbCrLf + "Interactive debug", "Enter statement or ?", "")
                End If
            Loop
            lSay = 0
        End If
    End Sub
    Public Sub traceVar(ByRef t As String, ByRef n As String, ByRef v As String)
        traceStr(">" & t & ">     " & n & " = " & v)
    End Sub
    Private Sub TracePLin(ByVal i As Integer) ' trace source after jmp, jcf, cal, lbl
        ' lin is not always accurate
        Dim asm As AsmStatement
        asm = DirectCast(CurrRexxRun.IntCode.Item(i), AsmStatement)
        If asm.f <> fct.lin Then ' trace previous sourceline
            asm = DirectCast(CurrRexxRun.IntCode.Item(i - 1), AsmStatement)
            While asm.f <> fct.lin And i > 1
                asm = DirectCast(CurrRexxRun.IntCode.Item(i), AsmStatement)
                i = i - 1
            End While
            If asm.f = fct.lin Then
                If SrcLstLin <> asm.l Then
                    traceLin(asm.l)
                    SrcLstLin = i + 1
                End If
            End If
        End If
    End Sub
    Private Sub argGen(ByRef arg As fct) ' gen arg code for Parse template and a "par?" line for each variable to be assigned a value
        Dim fromWhichParameter, n, i As Integer
        Dim previousPar As fct
        Dim prSym As Symbols
        Dim code0 As AsmStatement
        GenerateAsm(arg, 0, 0)
        code0 = DirectCast(CurrRexxRun.IntCode.Item(CurrRexxRun.IntCode.Count()), AsmStatement)
        GetNextSymbol()
        previousPar = Nothing
        n = 0
        fromWhichParameter = 1
        While (cSymb <> Symbols.semicolon)
            n = n + 1
            If (cSymb <> Symbols.comma And cSymb <> Symbols.ident And cSymb <> Symbols.period) Then
                If (previousPar <> fct.parv) Then
                    GenerateAsm(fct.parv, fromWhichParameter, 0) ' placeholder
                    n = n + 1
                End If
            End If
            If (cSymb = Symbols.comma) Then
                fromWhichParameter = fromWhichParameter + 1
                n = n - 1
                previousPar = Nothing
            ElseIf (cSymb = Symbols.ident) Then
                i = SourceNameIndexPosition(cId, tpSymbol.tpVariable, DefVars)
                GenerateAsm(fct.parv, fromWhichParameter, i)
                previousPar = fct.parv
            ElseIf (cSymb = Symbols.period) Then
                GenerateAsm(fct.parv, fromWhichParameter, 0)
                previousPar = fct.parv
            ElseIf (cSymb = Symbols.plus Or cSymb = Symbols.minus) Then
                prSym = cSymb
                GetNextSymbol()
                If (cSymb = Symbols.nbrsym) Then
                    If (prSym = Symbols.plus) Then
                        GenerateAsm(fct.parp, fromWhichParameter, StrInt(cId))
                    Else
                        GenerateAsm(fct.parp, fromWhichParameter, -StrInt(cId))
                    End If
                Else
                    SigError(108) ' exp. number
                End If
                previousPar = fct.parp
            ElseIf (cSymb = Symbols.nbrsym) Then
                GenerateAsm(fct.parc, fromWhichParameter, StrInt(cId))
                previousPar = fct.parc
            ElseIf (cSymb = Symbols.txtsym) Then
                i = StoreLiteral(cId)
                GenerateAsm(fct.parl, fromWhichParameter, i)
                previousPar = fct.parl
            ElseIf (cSymb = Symbols.lparen) Then
                GetNextSymbol()
                If (cSymb <> Symbols.ident) Then SigError(119)
                i = SourceNameIndexPosition(cId, tpSymbol.tpVariable, DefVars)
                GenerateAsm(fct.parh, fromWhichParameter, i)
                previousPar = fct.parh
                GetNextSymbol()
                If (cSymb <> Symbols.rparen) Then SigError(119)
            Else
                SigError(119)
            End If
            GetNextSymbol()
        End While
        code0.l = fromWhichParameter
        code0.a = n
        TestSymbolExpected(Symbols.semicolon, 118)
    End Sub
    Private Sub DoParse(ByRef CommandParm As String)
        Dim asmParse, asmCallingParm, currentToken, followingToken As AsmStatement, asmCallingLine As AsmStatement = Nothing
        Dim CallStackElement As CallElem = Nothing
        Dim k, l2, l1, l3, i As Integer
        Dim TypeOfNextParm As fct
        'Dim sProcNum, sCallLevel As Integer
        Dim m As String = ""
        Dim n As String = ""
        Dim en As String = ""
        Dim m2, m3 As String
        Dim ExtRoutParm As New Collection
        Dim MainParmIsRout As Boolean = False
        l1 = 0
        asmParse = DirectCast(CurrRexxRun.IntCode.Item(IntPp), AsmStatement)
        If (asmParse.f = fct.arg) Then ' check nr arguments passed
            CallStackElement = DirectCast(CurrRexxRun.CallStack(CurrRexxRun.CallStack.Count), CallElem)
            If Not (CurrRexxRun.CallStack.Count = 1 AndAlso CallStackElement.InternDepth = 0) Then ' caller and routine in same script?
                asmCallingLine = DirectCast(CurrRexxRun.IntCode.Item(CInt(Stack.Item(Stack.Count()))), AsmStatement) ' cal statement  
            End If
        End If
        While l1 < asmParse.l
            l1 = l1 + 1
            If (asmParse.f = fct.arg) Then
                If CurrRexxRun.CallStack.Count = 1 AndAlso CallStackElement.InternDepth = 0 Then ' parm of MAIN
                    If l1 = 1 Then
                        m = CommandParm ' external parameter 
                        If CommandParm.StartsWith(ExtRoutineTagstring) Then
                            MainParmIsRout = True
                            Dim spls As String = X2C(ExtRoutineParmSep)
                            Dim lp As Integer = ExtRoutineTagstring.Length
                            For p As Integer = lp - 1 To m.Length - spls.Length
                                If m.Substring(p, spls.Length) = spls Then
                                    ExtRoutParm.Add(m.Substring(lp, p - lp))
                                    lp = p + spls.Length
                                End If
                            Next
                            ExtRoutParm.Add(m.Substring(lp))
                        End If
                    Else
                        m = ""
                    End If
                ElseIf (l1 <= asmCallingLine.l) Then  ' enough parameters on CALL
                    asmCallingParm = DirectCast(CurrRexxRun.IntCode.Item(CInt(Stack.Item(Stack.Count())) + l1), AsmStatement) ' cll statement of corr. parameter
                    If asmCallingParm.a <> -1 Then
                        VariaRuns = GetNm(asmCallingParm.a, n, en, k)
                        If k = 0 Then
                            m = ""
                        Else
                            m = VariaRuns.IdValue
                        End If
                    Else
                        m = ""
                    End If
                Else
                    m = ""
                End If
            ElseIf (asmParse.f = fct.pul) Then
                If QStack.Count() = 0 Then
                    If RexxHandlesSay Then
                        If (lSay > 0) Then
                            m = SayLine(lSay)
                            lSay = lSay - 1
                        Else
                            m = ""
                        End If
                        If lSay > 0 Then
                            AddLineToSaybuffer("", True)
                        End If
                        m = myInputBox(m, "REXX Pull", "")
                        If myInputBoxCancelled Then
                            ' cancelled
                            CancRexx = True
                        End If
                        AddLineToSaybuffer("--> " & m, False)
                    Else
                        RaiseEvent doPull(m)
                    End If
                Else
                    m = CStr(QStack.Item(QStack.Count()))
                    QStack.Remove(QStack.Count())
                End If
            ElseIf (asmParse.f = fct.pvl Or asmParse.f = fct.pvr) Then
                If (l1 = 1) Then
                    m = FromStack()
                Else
                    m = ""
                End If
            End If
            If (UpCase) Then m = m.ToUpper(CultInf)
            l3 = 1 ' next startpos in string m
            For l2 = 1 To asmParse.a ' per token to be parsed
                currentToken = DirectCast(CurrRexxRun.IntCode.Item(IntPp + l2), AsmStatement)
                followingToken = DirectCast(CurrRexxRun.IntCode.Item(IntPp + l2 + 1), AsmStatement)
                If currentToken.l = l1 Then
                    If MainParmIsRout Then
                        If l1 > ExtRoutParm.Count Then
                            m = ""
                        Else
                            m = ExtRoutParm(l1)
                            If (UpCase) Then m = m.ToUpper(CultInf)
                        End If
                    End If
                    If currentToken.f = fct.parv Then ' parse value 
                        TypeOfNextParm = 0
                        If followingToken.l = currentToken.l Then
                            If followingToken.f = fct.parl Or followingToken.f = fct.parc Or followingToken.f = fct.parp Or followingToken.f = fct.parh Or followingToken.f = fct.parv Then
                                TypeOfNextParm = followingToken.f
                            End If
                        End If
                        If (TypeOfNextParm = fct.parl Or TypeOfNextParm = fct.parh) Then
                            m2 = Mid(m, l3, m.Length() - l3 + 1)
                            If (TypeOfNextParm = fct.parl) Then
                                m3 = GetLit((followingToken.a))
                            Else
                                m3 = GetVar((followingToken.a), en, n)
                            End If
                            If LwCase Then
                                i = InStr(1, m2.ToLower(CultInf), m3.ToLower(CultInf))
                            Else
                                i = InStr(1, m2, m3)
                            End If
                            If (i = 0) Then i = m2.Length() + 1
                            m2 = Mid(m, l3, i - 1)
                            l3 = l3 + i - 1 + m3.Length()
                        ElseIf (TypeOfNextParm = fct.parc Or TypeOfNextParm = fct.parp) Then
                            If (TypeOfNextParm = fct.parc) Then
                                i = followingToken.a
                            Else
                                i = l3 + followingToken.a
                            End If
                            If (i > l3) Then
                                m2 = Mid(m, l3, i - l3)
                            Else
                                m2 = Mid(m, l3, m.Length() - l3 + 1)
                            End If
                            l3 = i
                        Else ' = parv  or  last
                            m2 = Mid(m, l3, m.Length() - l3 + 1)
                            If (TypeOfNextParm = fct.parv Or TypeOfNextParm = fct.parp) Then
                                m3 = m2.TrimStart()
                                l3 = l3 + (m2.Length() - m3.Length())
                                i = InStr(1, m3, " ")
                                If (i = 0) Then i = m3.Length() + 1
                                l3 = l3 + i
                                If (TypeOfNextParm = fct.parp) Then l3 = l3 + followingToken.a
                                m2 = Mid(m3, 1, i - 1)
                            Else
                                l3 = m.Length() + 1
                            End If
                        End If
                        If (l3 < 1) Then
                            l3 = 1
                        ElseIf (l3 > (m.Length() + 1)) Then
                            l3 = m.Length() + 1
                        End If
                        If (currentToken.a > 0) Then ' . does not store
                            StoreVar(currentToken.a, m2, k, en, n)
                        End If
                    End If
                End If
            Next
        End While
        IntPp = IntPp + asmParse.a ' skip par...
        UpCase = False
    End Sub
    Private Function StrInt(ByVal s As String) As Integer
        If Not IsNum(s) Then
            RunError(1, s)
        End If
        StrInt = CInt(NumY)
    End Function
    Private Function StrFl(ByVal s As String) As Double
        If Not IsNum(s) Then
            RunError(1, s)
        End If
        StrFl = NumY
    End Function
    Private Function IsNum(ByVal s As String) As Boolean
        If Not DecimalSepPt Then
            Dim j As Integer
            j = InStr(1, s, ".")
            If j > 0 Then Mid(s, j, 1) = ","
        End If
        If IsNumeric(s) Then
            NumY = CDbl(s)
            Return True
        End If
        Return False
    End Function
    Private Function Words(ByVal s As String, Optional ByVal sep As String = " ") As Integer
        Dim i, j As Integer
        If s.Trim().Length() > 0 Then
            For i = 1 To 9999999
                words1(s, j, sep)
                Words = Words + 1
                If j = 0 Then
                    Exit Function
                End If
            Next
        End If
        Words = 0
    End Function
    Private Function Word(ByVal s As String, ByVal f As Integer, Optional ByVal sep As String = " ") As String
        Dim i, j As Integer
        Word = ""
        For i = 1 To f - 1
            words1(s, j, sep)
            If j = 0 Then
                Exit Function
            End If
        Next
        Word = words1(s, j, sep)
    End Function
    Private Function words1(ByRef s As String, ByRef j As Integer, Optional ByVal sep As String = " ") As String
        ' get first (or only) word from string
        While s.Length() > 0 And Left(s, 1) = sep
            s = Mid(s, 2)
        End While
        While s.Length() > 0 And Right(s, 1) = sep
            s = Mid(s, 1, s.Length() - 1)
        End While
        j = InStr(1, s, sep)
        If j <> 0 Then
            words1 = Left(s, j - 1)
            s = Mid(s, j + 1)
        Else
            words1 = s
        End If
    End Function
    Private Function SubWord(ByVal s As String, ByVal f As Integer, Optional ByVal l As Integer = 999999, Optional ByVal sep As String = " ") As String
        Dim i, j As Integer
        SubWord = ""
        For i = 1 To f - 1
            words1(s, j, sep)
            If j = 0 Then
                Exit Function
            End If
        Next
        l = l + f - 1
        For i = f To l
            SubWord = SubWord & words1(s, j, sep) & " "
            If j = 0 Then
                Exit For
            End If
        Next
        SubWord = SubWord.TrimEnd()
    End Function
    Private Function Translate(ByVal s As String, ByRef froms As String, ByRef tos As String) As String
        Dim i, l, p As Integer
        l = s.Length()
        p = 1
        For i = 1 To l
            p = InStr(froms, Mid(s, i, 1))
            If p > 0 Then
                s = Left(s, i - 1) & Mid(tos, p, 1) & Mid(s, i + 1)
            End If
        Next
        Translate = s
    End Function
    Private Function xRange(ByVal st As Integer, ByVal en As Integer) As String
        Dim i As Integer, s As String
        s = ""
        If en >= st Then
            s = Space(en - st + 1)
            For i = st To en
                Mid(s, i - st + 1, 1) = Chr(i)
            Next
        Else
            s = xRange(st, 255) & xRange(0, en)
        End If
        Return s
    End Function
    Private Function C2X(ByVal x As String) As String
        Dim c1, i, l, c2 As Integer
        Dim ccc As String
        Dim ic As Integer
        Dim s As String
        C2X = ""
        s = "0123456789ABCDEF"
        l = x.Length()
        For i = 1 To l
            ccc = Mid(x, i, 1)
            ic = Asc(ccc)
            c1 = ic \ 16
            c2 = ic And &HFS
            C2X = C2X & Mid(s, c1 + 1, 1) & Mid(s, c2 + 1, 1)
        Next
    End Function
    Private Function C2B(ByVal x As String) As String
        Dim j, i, l As Integer
        Dim ccc As String
        Dim ic, c1 As Integer
        Dim s As String
        C2B = ""
        l = x.Length()
        For i = 1 To l
            ccc = Mid(x, i, 1)
            ic = Asc(ccc)
            s = "        "
            For j = 1 To 8
                c1 = ic Mod 2
                ic = ic \ 2
                Mid(s, 9 - j, 1) = CStr(c1)
            Next
            C2B = C2B & s
        Next
    End Function
    Private Function Abbrev(ByVal inptStr As String, ByVal RefStr As String, Optional ByVal MinL As Integer = 1) As Boolean
        If inptStr.Length() >= MinL AndAlso inptStr.Length() <= RefStr.Length() AndAlso RefStr.Substring(0, inptStr.Length()) = inptStr Then
            Return True
        End If
        Return False
    End Function
    Private Function X2C(ByVal x As String) As String
        Dim h, i, l, j As Integer
        Dim ccc, res As String
        Dim ic As Integer
        x = x.ToUpper(CultInf)
        Dim y As String = ""
        For ii As Integer = x.Length - 1 To 0 Step -1
            If (x(ii) >= "0"c And x(ii) <= "9"c Or x(ii) >= "A"c And x(ii) <= "F"c) Then
                y = x(ii) & y
            ElseIf x(ii) <> " "c Then
                SigError(121)
            End If
        Next
        x = y
        l = x.Length()
        If l Mod 2 > 0 Then
            x = "0" & x
            l = x.Length()
        End If
        res = ""
        j = 1
        For i = 1 To l
            If (j = 1) Then h = 0
            ccc = Mid(x, i, 1)
            If ccc <> " " Then
                If (ccc >= "0" And ccc <= "9") Then
                    ic = Asc(ccc) - 48
                ElseIf (ccc >= "A" And ccc <= "F") Then
                    ic = Asc(ccc) - 55
                End If
                If (j = 1) Then
                    h = h + ic * 16
                Else
                    h = h + ic
                    res = res & Chr(h)
                End If
                j = 3 - j
            End If
        Next
        Return res
    End Function
    Private Function B2C(ByVal x As String) As String
        Dim h, i, l, j As Integer
        Dim ccc, res As String
        Dim ic As Integer
        x = x.ToUpper(CultInf)
        Dim y As String = ""
        For ii As Integer = x.Length - 1 To 0 Step -1
            If (x(ii) = "0"c Or x(ii) = "1"c) Then
                y = x(ii) & y
            ElseIf x(ii) <> " "c Then
                SigError(121)
            End If
        Next
        x = y
        l = x.Length()
        Dim l1 As Integer = l Mod 8
        If l1 > 0 Then
            x = "0000000".Substring(0, 8 - l1) + x
            l = x.Length()
        End If
        res = ""
        j = 0
        h = 0
        For i = 1 To l
            ccc = Mid(x, i, 1)
            If ccc <> " " Then
                j = j + 1
                ic = Asc(ccc) - 48
                h = h * 2 + ic
                If j = 8 Then
                    res = res & Chr(h)
                    j = 0
                    h = 0
                End If
            End If
        Next
        Return res
    End Function
    Private Function ReadSource(src As Collection, longName As String) As Integer
        If longName <> "In memory source" Then
            ReadSource = ReadSourceFile(src, longName)
        Else
            Dim s As New LineOfSource
            s.Text = MemorySource & ";"
            s.LineNr = 1
            src.Add(s)
            ReadSource = 0
        End If
    End Function
    Private Function ReadSourceFile(src As Collection, Filename As String) As Integer
        ' read file in ASCII or UNICODE and store source in collection src
        Dim rc As Integer = 0, IsUnicode As Boolean = False
        Dim RexxSourceFile As FileStream
        Dim ssd As SourceLine
        Dim ch As Integer, vCh As Integer
        Dim iFile, lFile As Long
        Dim IsUnicodeh As Boolean = False
        Dim nSkip As Integer, lNr As Integer
        Dim SourceList As New Collection
        Try
            RexxSourceFile = New FileStream(Filename, FileMode.Open, FileAccess.Read)
            lFile = RexxSourceFile.Length()
            IsUnicode = False
            ssd = New SourceLine
            ssd.SrcStart = 1
            nSkip = 0
            For iFile = 1 To lFile
                ch = RexxSourceFile.ReadByte()
                If nSkip > 0 Then
                    nSkip -= 1
                Else
                    If iFile = 1 AndAlso ch = 63 AndAlso CurrEdtSession.EncodingType = "8"c Then
                        ssd.SrcStart = 2 ' skip ?: BOM indicator for windows utf8 file encoder
                    End If
                    If iFile = 1 AndAlso ch = &HFF Then
                        IsUnicodeh = True
                    ElseIf iFile = 2 AndAlso IsUnicodeh AndAlso ch = &HFE Then
                        IsUnicode = True
                        ssd.SrcStart = 3
                    Else
                        If (ch = 10 Or ch = 13) Then
                            If (vCh = 10 And ch = 13 Or vCh = 13 And ch = 10) Then
                                ch = 0 ' Lf after CR or Cr after LF has no meaning
                                If IsUnicode Then
                                    nSkip = 1
                                End If
                                ssd.SrcStart += (nSkip + 1)
                            Else
                                ssd.SrcLength = iFile - ssd.SrcStart
                                SourceList.Add(ssd)
                                If IsUnicode Then nSkip = 1
                                ssd = New SourceLine
                                ssd.SrcStart = iFile + 1 + nSkip
                            End If
                        End If
                    End If
                    vCh = ch
                End If
            Next
            ssd.SrcLength = iFile - ssd.SrcStart
            SourceList.Add(ssd)
            lNr = 0
            For Each ssd In SourceList
                Dim s As New LineOfSource
                s.Text = ReadOneRexxLine(ssd, IsUnicode, RexxSourceFile)
                If s.Text.Length > 0 AndAlso s.Text(s.Text.Length - 1) = "," Then
                Else
                    s.Text += ";"
                End If
                s.LineNr = lNr
                lNr += 1
                src.Add(s)
            Next
        Catch E As Exception
            rc = 16
        End Try
        Return rc
    End Function
    Private Function ReadOneRexxLine(ByVal ssd As SourceLine, IsUnicode As Boolean, RexxSourceFile As FileStream) As String
        Dim value As String = ""
        If ssd.SrcLength > 0 Then
            Dim buf(ssd.SrcLength - 1) As Byte
            RexxSourceFile.Seek(ssd.SrcStart - 1, SeekOrigin.Begin)
            RexxSourceFile.Read(buf, 0, ssd.SrcLength)
            If IsUnicode Then
                Dim enc As System.Text.Encoding = New System.Text.UnicodeEncoding(False, True, True)
                value = enc.GetString(buf)
            Else
                value = System.Text.Encoding.UTF8.GetString(buf)
                ' value = System.Text.Encoding.Default.GetString(buf)
            End If
        End If
        Return value
    End Function
    Private myInputBoxCancelled As Boolean
    Friend Function myInputBox(Ask As String, Optional tit As String = "", Optional resp As String = "")
        InputBx.InpTit = tit
        InputBx.OutResp = resp
        InputBx.InpSay = Ask
        Dim s As String = ""
        InputBx.ShowDialog()
        myInputBoxCancelled = Not InputBx.OKBut
        If InputBx.OKBut Then
            s = InputBx.OutResp
        End If
        Return s
    End Function
End Class
Friend Class LineOfSource
    Friend LineNr As Integer
    Friend Text As String
End Class
Friend Class CallElem
    Friend ProcNum As Integer    ' nr in source of proc
    Friend InternDepth As Integer    ' internal call depth
    Friend Exposes As Collection ' strings of names to expose
End Class
Friend Class RexxWord
    Friend Word As String
    Friend Sym As Rexx.Symbols
    Friend Active As Boolean
End Class
Public Class DefVariable
    Friend Id As String ' variable name Soure  
    Friend Kind As Rexx.tpSymbol ' Procedure / variable
End Class
Friend Class VariabelRun
    Friend Id As String ' variable name execution-time
    Friend IdValue As String ' execution value
    Friend Level As Integer '  call nesting level 
    Friend ArrayIndex As Integer '  seq.nr. in collections
    Friend Kind As Rexx.tpSymbol ' Procedure / variable
End Class
Friend Class AsmStatement
    Friend f As Rexx.fct ' opcode
    Friend l As Integer ' length operand
    Friend a As Integer ' address operand
End Class
Friend Class aStream
    Friend FileStr As FileStream
    Friend OpenClosed As Boolean ' true=open
    Friend ReadPos As Long
    Friend WritePos As Long
    Friend ErrMsg As String
End Class
Friend Class InterpretFromRexx
    Public WithEvents Rx As New Rexx
    Public Sub New(ByVal f As String, ByVal p As String, ByVal st As RexxCompData)
        Rx.CurrRexxRun = st
        If Rx.CompileRexxScript(f) = 0 Then
            Rx.ExecuteRexxScript(p)
        End If
    End Sub
End Class
Public Class RexxCompData
    Friend IntCode As New Collection ' executable code
    Friend SrcLine As Integer ' position in source where compiling
    Friend SrcPos As Integer
    Friend TxtValue As New Collection ' literals
    Friend IxProcName As New Collection ' procnames of encountered procedures
    Friend IxProc As New Collection ' indexes of encountered procedures
    Friend IdName As New Collection ' variable names in source.
    Public IdExpose As Collection ' exposed names of current procedure
    Public IdExposeStk As New Collection ' currRexxRun.Stack of IdExpose's per procedurelevel
    Friend Rc, CompRc, iRc, iRes, iSigl As Integer
    Friend Result As String = "" ' return value external routine
    Friend RuntimeVars As New Collection ' variable names and values in execution.
    Public ProcNum As Integer '  0 = main programma, 1 2 3 4 5 .. = local procedure
    Friend vProcNum As Integer
    Friend CallLevel, vCallLevel As Integer ' levels in execute
    Friend CAddress As String = "", AddressS As String = ""
    Friend TraceLevel As Integer
    Friend InteractiveTracing, TracingResume As Integer
    Friend sigNovalue As Boolean, sigLabel As Integer
    Friend InInterpret As Boolean
    Friend InInteractive As Boolean = False
    Friend InExecution As Boolean = False

    Friend Source As New Collection
    Friend ItpSource As New Collection
    Friend CallStack As New Collection

    Friend fileName As String
    Friend fileTstamp As Date
    Friend UseStamp As Date
    Protected Overrides Sub Finalize()
        TxtValue.Clear() ' literals
        For Each exp As Collection In IdExposeStk
            exp.Clear()
        Next
        IdExposeStk.Clear() ' currRexxRun.Stack of IdEexpose per procedure
        IxProcName.Clear() ' procnames of encountered procedures
        IxProc.Clear() ' indexes of encountered procedures
        IdName.Clear() ' variable names and charact in source.
        'RuntimeVars.Clear() ' variable names and charact in execution.
        CallStack.Clear()
        MyBase.Finalize()
    End Sub
End Class
Public Class RexxEvent
    Inherits EventArgs
    Public rc As Integer
    Sub New()
        MyBase.New()
        rc = 0
    End Sub
End Class

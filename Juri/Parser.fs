module Juri.Internal.Parser

open System
open Juri.Internal.LanguageModel
open LanguageModel
open ParserCombinators
open CoreLib

type IndentationType =
    | Tabs
    | Spaces
    | Unknown

type JuriContext =
    {
        IndentationType : IndentationType
        IndentationStack : int list
        Line : int
        Functions : Identifier list
    }
    static member Default =
        {
            IndentationType = Unknown
            IndentationStack = [0]
            Line = 1
            Functions = createEnvWithCoreLibFunctions () |> Map.toList |> List.map fst
        }



let private ws =
    many (pchar ' ' <|> pchar '\t')

let newline = createNewline<JuriContext> () |> updateContext (fun _ c -> { c with Line = c.Line + 1 })
let EOS = createEOS<JuriContext>()
let newlineEOS = either newline EOS




let digit =
        "0123456789"
        |> Set |> anyOf

let integer =
    many1 digit 
    //|>> fun cs -> cs |> List.map string |> List.reduce ( + ) |> int
    |>> (String.Concat >> int)
    |> deferr "Listenelemente müssen mit einer Ganzahl Indexiert werden."

let private identifier =

    let identifierStart =
        '_' :: ['a'..'z'] @ ['A'..'Z']
        |> Set |> anyOf

    let identifierTail =
        '_' :: ['a'..'z'] @ ['A'..'Z'] @ ['0'..'9']
        |> Set |> anyOf

    identifierStart .>>. (many identifierTail)
    .>> ws
    |>> fun (c, cs) -> c :: cs |> String.Concat |> Identifier
    |> deferr "Es wurde ein Identifier erwartet."



let private listIdentifier =

    let identifierStart = pchar ':' 

    identifierStart .>>. identifier
    .>> ws
    |>> fun (prefix, Identifier rest) -> (string prefix) + rest |> Identifier
    |> deferr "Es wurde ein ListIdentifier erwartet."




let private operator =

    let operatorChar =
        ['+'; '-'; '*'; '/'; '>'; '<'; '.'; '='; '!'; '%']
        |> Set |> anyOf

    many1 operatorChar
    |> satisfies (fun opChars _ -> opChars <> ['='])
    .>> ws
    |>> (String.Concat >> BinaryOperator)
    
let operatorPrecedence (BinaryOperator str) =
    let charMapper = function
        | '=' | '<' | '>' | '!' -> 2
        | '+' | '-' -> 1
        | '*' | '/' | '%' -> 0
        | _ -> 0
    str
    |> Seq.map charMapper
    |> Seq.max
    
let operatorComparison =
    let isComparisonOperator op _ = operatorPrecedence op = 2
    operator |> satisfies isComparisonOperator

let operatorSum =
    let isSumOperator op _ = operatorPrecedence op = 1
    operator |> satisfies isSumOperator

let operatorProduct =
    let isProductOperator op _ = operatorPrecedence op = 0
    operator |> satisfies isProductOperator

let private operatorNot =
    pstring "not " .>> ws
    |>> BinaryOperator
    
let private operatorAnd =
    pstring "and " .>> ws
    |>> BinaryOperator
    
let private operatorOr =
    pstring "or " .>> ws
    |>> BinaryOperator



// keywords and control chars
let eq =
    pchar '=' .>> ws
    
let rangeOperator =
    pstring "to" .>> ws
    
let jinit =
    pstring "init" .>> ws
    
let jbreak =
    pstring "break" .>> ws
    
let jreturn =
    pstring "return" .>> ws
    
let iterate =
    pstring "iterate" .>> ws

let jtimes =
    pstring "times" .>> ws
    
let jas =
    pstring "as" .>> ws

let ifloop =
    pstring "if" .>> ws
    
let jthen =
    pstring "then" .>> ws

let repeat =
    optional (pstring "repeat") .>> ws |>> (function | [] -> false | _ -> true)

let jfun =
    pstring "fun" .>> ws

let binary =
    pstring "operator" .>> ws

let openParen =
    pchar '(' .>> ws

let closingParen =
    pchar ')' .>> ws |> deferr "Es fehlt eine schließende Klammer"

let openBracket =
    pchar '[' .>> ws |> deferr "Es fehlt eine öffnende Klammer"

let closingBracket =
    pchar ']' .>> ws |> deferr "Es fehlt eine schließende Klammer"
    
let private jskip =
    pstring "skip" .>>. ws
    
let private jcry =
    pstring "cry" .>>. ws
    
    
    
// expressions
let private expression, expressionImpl = createParserForwarder ()
let private singleExpression, singleExpressionImpl = createParserForwarder ()



// list expressions
let private listReference =
    listIdentifier
    |>> ListReference
    
let private listLiteral = 
    openBracket >>. (many expression) .>> closingBracket
    |>> LiteralList

let private range =
    expression .>> rangeOperator
    .>>. (expression |> failAsFatal)
    |>> Range

let private charList =
    pchar '''
    >>. many (anyBut (set [''']))
    .>> (pchar ''' |> failAsFatal)
    .>> ws
    |>> (fun cs -> cs |> List.map (int >> float >> LiteralNumber) |> LiteralList)

let private nakedListExpression =
    [ range
      listLiteral
      charList
      listReference ]
    |> choice

let private listExpression =
    let parenthesizedListExpression =
        openParen >>. nakedListExpression .>> closingParen
    either nakedListExpression parenthesizedListExpression


// parameter
let private parameter =
    let listReference = listExpression |>> Pointer
    let value = expression |>> Value
    either listReference value


let private number = 

    let punctuation = pchar '.'
    let sign = pchar '-' <|> pchar '+'

    (optional sign) .>>. (many1 digit) .>>. (optional punctuation) .>>. (many digit)
    |>> fun (((s,ds),p),ds2) -> s@ds@p@ds2 |> List.map string |> List.reduce ( + ) |> float |> LiteralNumber
    .>> ws
    |> deferr "Eine Zahl wurde erwartet."



let private parenthesizedExpression =
    openParen
    >>. expression
    .>> (closingParen |> failAsFatal)
    |>> ParenthesizedExpression
    


let private variableReference =
    identifier
    |>> VariableReference



let private functionCall =
    identifier |> satisfies (fun id c -> List.contains id c.Functions)
    .>>. (many parameter)
    |>> FunctionCall



let private listLength =
    pchar '?' >>. listIdentifier
    |>> ListLength
    
    
    
let private listAccess =
    let reference =
        listIdentifier |>> ListReference
    let other =
        pchar ':'
        .>> ws
        >>. listExpression
    (parenthesizedExpression <|> number <|> variableReference <|> functionCall <|> listLength)
    .>> ws
    .>>. either reference other
    |>> fun (index, id) -> ListAccess (id, index)



let private logicalNegation =
    operatorNot .>>. expression
    |>> Unary


    
// Binary Expressions :O
let private listToTree (single, chain): Expression =
    let rec traverse left rest =
        match rest with
        | [] -> left
        | (op, right) :: tail -> Binary (op, left, traverse right tail)
    traverse single chain

let private product =
    singleExpression
    .>>. many (operatorProduct .>>. singleExpression)
    |>> listToTree 
let private sum =
    product .>>. many (operatorSum .>>. product)
    |>> listToTree
    
let private comparison =
    sum .>>. many (operatorComparison .>>. sum)
    |>> listToTree
    
let private logicalProduct =
    comparison .>>. many (operatorAnd .>>. comparison)
    |>> listToTree
    
let private logicalSum =
    logicalProduct .>>. many (operatorOr .>>. logicalProduct)
    |>> listToTree


singleExpressionImpl.Value <-
    [
        logicalNegation
        functionCall
        listAccess
        variableReference
        listLength
        number
        parenthesizedExpression
    ]
    |> choice
    

expressionImpl.Value <-
    either logicalSum singleExpression
    .>> ws
    |> deferr "Es wird ein Ausdruck erwartet."



// argument
let argument =
    let valueArgument = identifier |>> ValueArgument
    let listPointer = listIdentifier |>> PointerArgument
    either listPointer valueArgument



// instructions
let private instruction, instructionImpl = createParserForwarder ()

let addLastLineToResult inst context = inst, context.Line - 1
let addThisLineToResult inst context = inst, context.Line

let emptyLines =
    let commentLine =
        ws >>. pchar '#' .>> (AsUntilB (anyChar()) newlineEOS)
        |>> ignore
    let empty =
        ws >>. newline
        |>> ignore
    many (either commentLine empty)
    
let singleLineStatementEnding parser =
    parser
    ||>> addThisLineToResult
    .>> newlineEOS .>> emptyLines



let private codeblock =

    let countTabsAndSpaces (chars: char list) =
        let mutable tabs = 0
        let mutable spaces = 0
        chars |> List.iter (function | '\t' -> tabs <- tabs + 1 | ' ' -> spaces <- spaces + 1 | _ -> ())
        (tabs, spaces)


    let indentation =
        fun (stream: CharStream<JuriContext>) ->
            let posStart = stream.GetPosition()
            match run (ws |>> countTabsAndSpaces) stream with
            | Failure (m,e) -> Failure (m,e)
            | Fatal (m,e)   -> Fatal (m,e)
            | Success ((tabs,spaces),c,p) ->
                match (tabs, spaces, c.IndentationType) with
                | (t,0,Unknown) ->
                    let newContext = {stream.GetContext() with IndentationType = Tabs}
                    stream.SetPosition(p)
                    Success (t,newContext,p)
                | (t,0,Tabs) ->
                    stream.SetPosition(p)
                    Success (t,c,p)
                | (0,s,Unknown) ->
                    let newContext = {stream.GetContext() with IndentationType = Spaces}
                    stream.SetPosition(p)
                    Success (s,newContext,p)
                | (0,s,Spaces) ->
                    stream.SetPosition(p)
                    Success (s,c,p)
                | (t,s,_) ->
                    let errorMark = (posStart, t + s)
                    Fatal ("Tabs und Leerzeichen dürfen nicht gemischt werden.", Some errorMark)
        |> Parser


    let codeblockHead =
        indentation .>>. instruction
        |> satisfies (fun (level,_) c -> level > c.IndentationStack.Head)
        |> updateContext (fun (level,_) c -> {c with IndentationStack = level :: c.IndentationStack})
        |>> snd

    let codeblockTail =
        indentation |> satisfies (fun level c -> level = c.IndentationStack.Head)
        >>. instruction
    
    codeblockHead .>>. (many codeblockTail)
    |>> join2
    |> updateContext (fun _ c -> {c with IndentationStack = c.IndentationStack.Tail})
    |> deferr "Es fehlt ein Codeblock."




let private instructionExpression =
    expression
    |>> Expression
    |> singleLineStatementEnding



let private assignment =
    identifier
    .>> eq
    .>>. (expression |> failAsFatal)
    |>> fun (id, exp) -> Assignment (id, exp)
    |> singleLineStatementEnding



let private listAssignment =
    listIdentifier
    .>> eq
    .>>. listExpression
    |>> ListAssignment
    |> singleLineStatementEnding
    
    
    
let private listInitialisationWithCode = // has to be tried before listInitialisationWithValue in the parsing order
    listIdentifier
    .>> eq
    .>> jinit
    .>>. (expression |> failAsFatal)
    .>> jas
    .>>. (identifier |> failAsFatal)
    ||>> addThisLineToResult
    .>> newline .>> emptyLines
    .>>. (codeblock |> failAsFatal)
    |>> fun ((((id, size), indexName), line), body) -> (ListInitialisationWithCode (id, size, indexName, body)), line
    
    
    
let private listInitialisationWithValue =
    listIdentifier
    .>> eq
    .>> jinit
    .>>. (expression |> failAsFatal)
    .>>. (expression |> failAsFatal)
    |>> fun ((id, size), value) -> ListInitialisationWithValue (id, size, value)
    |> singleLineStatementEnding



let private listElementAssignment =
    (number <|> variableReference <|> parenthesizedExpression)
    .>>. listIdentifier
    .>> eq
    .>>. (expression |> failAsFatal)
    |>> fun ((index, id), exp) -> ListElementAssignment (id, index, exp)
    |> singleLineStatementEnding 



let private listIteration =
    iterate
    >>. listExpression
    .>> (jas |> failAsFatal)
    .>>. identifier
    ||>> addThisLineToResult
    .>> newline .>> emptyLines
    .>>. (codeblock |> failAsFatal)
    |>> fun (((list, elementName), line), body) -> (ListIteration (list, elementName, body)), line
 


let private iteration =
    iterate
    >>. expression
    .>> (jtimes |> failAsFatal)
    ||>> addThisLineToResult
    .>> newline .>> emptyLines
    .>>. (codeblock |> failAsFatal)
    |>> fun ((repetitions, line), body) -> (Iteration (repetitions, body)), line
    


let private functionDefinition =
    jfun
    >>. (identifier |> failAsFatal)
    |> updateContext (fun id c -> {c with Functions = id :: c.Functions})
    .>>. ((many argument) |> failAsFatal)
    ||>> addThisLineToResult
    .>> newline .>> emptyLines
    .>>. (codeblock |> failAsFatal)
    |>> fun (((id, argNames), line), body) -> (FunctionDefinition (id, argNames, body)), line



let private binaryOperatorDefinition =
    binary
    >>. (operator |> failAsFatal)
    .>>. (argument |> failAsFatal)
    .>>. (argument |> failAsFatal)
    ||>> addThisLineToResult
    .>> newline .>> emptyLines
    .>>. (codeblock |> failAsFatal)
    |>> fun ((((op, left), right), line), body) -> (OperatorDefinition (op, left, right, body)), line



let private loop =
    ifloop
    >>. (expression |> failAsFatal)
    .>>. repeat
    ||>> addThisLineToResult
    .>> newline .>> emptyLines
    .>>. (codeblock |> failAsFatal)
    |>> fun (((con, rep), line), body) -> (Loop (con, rep, body)), line
    
    
    
let private conditionWithSingleStatement = // has to be parsed before loop
    ifloop
    >>. (expression |> failAsFatal)
    .>> jthen
    .>>. (instruction |> failAsFatal)
    |>> fun (con, statement) -> Loop (con, false, [statement])
    ||>> addLastLineToResult



let private breakStatement =
    jbreak
    |>> (fun _ -> Break)
    |> singleLineStatementEnding
    


let private returnStatement =
    jreturn
    >>. (expression |> failAsFatal)
    |>> Return
    |> singleLineStatementEnding



let private skipStatement =
    jskip
    |>> (fun _ -> Skip)
    |> singleLineStatementEnding



let private cryStatement =
    jcry >>. listExpression
    |>> Cry
    |> singleLineStatementEnding
    


instructionImpl.Value <-
    [
        binaryOperatorDefinition
        conditionWithSingleStatement 
        loop
        functionDefinition
        assignment 
        listAssignment 
        listInitialisationWithCode
        listInitialisationWithValue 
        listElementAssignment 
        listIteration
        iteration
        breakStatement 
        returnStatement
        skipStatement
        cryStatement
        instructionExpression
    ]
    |> choice



let juriProgram=
    emptyLines
    >>. many1 instruction
    .>> emptyLines
    .>> (EOS |> failAsFatal)



let parseProgram (text: char seq) =
    let stream = CharStream(text, JuriContext.Default)
    let parsingResult = stream.RunParser(juriProgram)
    match parsingResult with
    | Failure (m,e) ->
        //stream.PrintError(m,e)
        ()
    | Fatal (m,e) ->
        //stream.PrintError(m,e)
        ()
    | Success(r,_,_) ->
        //printfn "%A" r
        //printfn "%A" (stream.GetContext())
        ()
    parsingResult

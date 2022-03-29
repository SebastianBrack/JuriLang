module Juri.Internal.Interpreter

open System
open Juri.Internal.LanguageModel
open Juri.Internal.OutputWriter
open Juri.Internal.Runtime
open Runtime



// helper functions
let private prependToList xs x = x::xs
let private appendToList xs x = xs@[x]
let private dropResults xs x = ()

let private accumulateResults accumulationStrategy results elem =
    match results, elem with
    | Ok _, Error msg -> Error msg
    | Error msg, Error msg2 -> Error (msg + " " + msg2)
    | Error msg, Ok _ -> Error msg
    | Ok xs, Ok x -> Ok (accumulationStrategy xs x)


let private checkFunctionSignature
        (expectedArgs: Parameter list)
        (givenArgs: GivenArgument list) =
    let matchesType expected given =
        match expected, given with
        | ValueArgument _, Value _ -> Ok ()
        | PointerArgument _, Pointer _ -> Ok ()
        | ValueArgument id, Pointer _ -> Error $"Parameter {id} -> übergeben: Pointer - erwartet: einen berechenbaren Wert!"
        | PointerArgument id, Value _ -> Error $"Parameter {id} -> übergeben: berechenbarer Wert - erwartet: Pointer!"
    if expectedArgs.Length <> givenArgs.Length then
        Error $"Diese Funktion erwartet %i{expectedArgs.Length} Argumente - es wurden aber %i{givenArgs.Length} übergeben."
    else
        List.map2 matchesType expectedArgs givenArgs
        |> List.reduce (accumulateResults dropResults)
        
        
let private checkArgumentsAreOnlyValues args : InterpreterResult<Expression list> =
    let checker = function
        | Value expression -> Ok expression
        | Pointer id -> Error $"{id} ist eine Referenz auf eine Liste. Es wird aber ein Wert erwartet."
    args
    |> List.map checker
    |> List.fold (accumulateResults appendToList) (Ok [])


// computation and evaluation -------------
let rec private computeLoop
        (con: Expression)
        (rep: bool)
        (body: JuriProgram)
        (outputWriter: IOutputWriter)
        (state: ComputationState) : InterpreterResult<ComputationState> =
    
    match eval outputWriter state con with
    | Error e       -> Error e
    | Ok 0.         -> Ok state
    | Ok _ when rep ->
            match (compute body outputWriter state) with
            | Error e -> Error e
            | Ok nextState when nextState.ReturnFlag = true -> Ok nextState
            | Ok nextState when nextState.BreakFlag = true -> Ok {nextState with BreakFlag = false}
            | Ok nextState -> computeLoop con rep body outputWriter nextState
    | Ok _ -> (compute body outputWriter state)


and private computeAssignment
        (id, exp)
        (outputWriter: IOutputWriter)
        (state: ComputationState) : InterpreterResult<ComputationState> =
    
    let env = state.Environment
    let addVariableToState x =
        let newEnv = env |> Map.add id (Variable x)
        Ok {state with LastExpression = None; Environment = newEnv}
    match (env.TryFind id) with
    | None | Some (Variable _) -> eval outputWriter state exp >>= addVariableToState
    | _ -> Error $"{id} ist keine Variable und kann keinen Wert zugewiesen bekommen."
    
    
and private computeListAssignment
        (id, expressions)
        (outputWriter: IOutputWriter)
        (state: ComputationState) : InterpreterResult<ComputationState> =
        
    let env = state.Environment
    let addListToState (xs: float list) =
        let newEnv = env |> Map.add id (List (List.toArray xs))
        Ok {state with LastExpression = None; Environment = newEnv}
    match (env.TryFind id) with
    | None | Some (List _) ->
        evalList outputWriter state expressions 
        >>= addListToState
    | _ -> Error $"{id} ist keine Liste und kann keine entsprechenden Werte zugewiesen bekommen."
    
    
    
and private computeListAssignmentWithRange
        (id, lowerBoundExpression, upperBoundExpression)
        (outputWriter: IOutputWriter)
        (state: ComputationState) : InterpreterResult<ComputationState> =
        
    let env = state.Environment
    match (env.TryFind id) with
    | None | Some (List _) ->
        let lowerEvalResult = eval outputWriter state lowerBoundExpression
        let upperEvalResult = eval outputWriter state upperBoundExpression
        match (lowerEvalResult, upperEvalResult) with
        | Ok low, Ok up ->
            let newEnv = env |> Map.add id (List [|low..up|])
            Ok {state with LastExpression = None; Environment = newEnv}
        | Error msg, _ -> Error msg
        | _, Error msg -> Error msg
    | _ -> Error $"{id} ist keine Liste und kann keine entsprechenden Werte zugewiesen bekommen."
    
    
    
and private computeListInitialisationWithValue
        (listName, sizeExpression, valueExpression)
        (outputWriter: IOutputWriter)
        (state: ComputationState) : InterpreterResult<ComputationState> =
    
    let env = state.Environment
    match (env.TryFind listName) with
    | None | Some (List _) ->
        let sizeEvalResult = eval outputWriter state sizeExpression
        let valueEvalResult = eval outputWriter state valueExpression
        match (sizeEvalResult, valueEvalResult) with
        | Ok size, Ok value ->
            let newList = Array.create (int size) value
            let newEnv = env |> Map.add listName (List newList)
            Ok {state with LastExpression = None; Environment = newEnv}
        | Error msg, _ -> Error msg
        | _, Error msg -> Error msg
    | _ -> Error $"{id} ist keine Liste und kann keine entsprechenden Werte zugewiesen bekommen."
    
            
            
and private computeListInitialisationWithCode
        (listName, sizeExpression, indexName, generatorCode) 
        (outputWriter: IOutputWriter)
        (state: ComputationState) : InterpreterResult<ComputationState> =
    
    let env = state.Environment
    let generateListElement i state =
        let generatorResult =
            computeAssignment (indexName, LiteralNumber i) outputWriter state
            >>= compute generatorCode outputWriter
        match generatorResult with
        | Error msg -> Error $"Fehler beim Generieren des Listenelements: {msg}"
        | Ok {LastExpression = None} -> Error $"Fehler beim Generieren des Listenelements: Der Generatorcode hat keinen Wert zurückgegeben."
        | Ok {LastExpression = Some x} -> Ok x
    match (env.TryFind listName) with
    | None | Some (List _) ->
        let sizeEvalResult = eval outputWriter state sizeExpression
        match sizeEvalResult with
        | Ok size ->
            if size < 0. then
                Error $"Es kann keine List der Länge {size} erstellt werden."
            else
                let newList : float array = Array.create (int size) 0.
                let mutable i = 0
                let mutable cancelFlag = false
                let mutable generationError = Error ""
                while i < (int size) && not cancelFlag do
                    let element = generateListElement i state
                    match element with
                    | Error msg ->
                        cancelFlag <- true
                        generationError <- Error msg
                    | Ok x ->
                        newList[i] <- x
                    i <- i+1
                if cancelFlag then
                    generationError
                else
                    let newEnv = env |> Map.add listName (List newList)
                    Ok {
                        LastExpression = None
                        Environment = newEnv
                        BreakFlag = false
                        ReturnFlag = false }
        | Error msg -> Error msg
    | _ -> Error $"{id} ist keine Liste und kann keine entsprechenden Werte zugewiesen bekommen."
    
and private computeListElementAssignment
        (id, indexExpression, valueExpression)
        (outputWriter: IOutputWriter)
        (state: ComputationState) : InterpreterResult<ComputationState> =
    let env = state.Environment
    match (env.TryFind id) with
    | Some (List xs) ->
        let indexEvalResult = eval outputWriter state indexExpression
        let valueEvalResult = eval outputWriter state valueExpression
        match (indexEvalResult, valueEvalResult ) with
        | Ok index, Ok value ->
            let trueIndex = if index < 0.0 then xs.Length + int index else int index
            if trueIndex >= 0 && trueIndex < xs.Length then
                xs[trueIndex] <- value
            Ok {state with LastExpression = Some value; Environment = env}
        | Error msg, _ -> Error msg
        | _, Error msg -> Error msg
    | _ -> Error $"{id} ist keine Liste."
    

and private computeFunctionDefinition
        (id, argNames, body)
        (state: ComputationState) : InterpreterResult<ComputationState> =
    
    let lastExp, env = state.LastExpression, state.Environment
    let addFunctionToState () =
        let newEnv = env |> Map.add id (CustomFunction (argNames, body))
        Ok {state with LastExpression = lastExp; Environment = newEnv}
    match (env.TryFind id) with
    | Some _ -> Error $"Der Name {id} wird bereits verwendet und kann nicht neu definiert werden."
    | None -> addFunctionToState ()


and private computeListIteration
        (listExpression, valueName, loopBody)
        (outputWriter: IOutputWriter)
        (state: ComputationState) : InterpreterResult<ComputationState> =
    
    let env = state.Environment
    let computeIteration state value =
        computeAssignment (valueName, LiteralNumber value) outputWriter state
        >>= compute loopBody outputWriter
    let rec iterate state pos (xs: float array) =
        if pos = xs.Length then
            Ok state
        else
            match computeIteration state xs[pos] with
            | Error e -> Error e
            | Ok newState -> iterate { newState with BreakFlag = false; ReturnFlag = false } (pos+1) xs
    evalListExpression outputWriter state listExpression
    >>= iterate state 0


and compute
        (program: JuriProgram)
        (outputWriter: IOutputWriter)
        (state: ComputationState) : InterpreterResult<ComputationState> =
    
    match program with
    | [] -> Ok state
    | _ when state.BreakFlag || state.ReturnFlag -> Ok state
    | (instruction, line) :: tail ->
        let mutable discardTail = false
        let computationResult =
            match instruction with
            | Break ->
                discardTail <- true
                Ok {state with BreakFlag = true}
            | Return exp ->
                match eval outputWriter state exp with
                | Ok x ->
                    discardTail <- true
                    Ok {state with ReturnFlag = true; LastExpression = Some x}
                | Error e ->
                    Error e
            | Expression exp ->
                match eval outputWriter state exp with
                | Ok x -> Ok {state with LastExpression = Some x}
                | Error e -> Error e
            | Assignment (id,exp) ->
                computeAssignment (id, exp) outputWriter state
            | FunctionDefinition (id, argNames, body) ->
                computeFunctionDefinition (id, argNames, body) state
            | Loop (con, rep, body) ->
                computeLoop con rep body outputWriter state
            | OperatorDefinition (BinaryOperator opName, leftName, rightName, body) ->
                computeFunctionDefinition (Identifier opName, [leftName; rightName], body) state
            | ListAssignment(listName, expressions) ->
                computeListAssignment (listName, expressions) outputWriter state
            | ListAssignmentWithRange (listName, lowerBound, upperBound) ->
                computeListAssignmentWithRange (listName, lowerBound, upperBound) outputWriter state
            | ListInitialisationWithValue (listName, size, value) ->
                computeListInitialisationWithValue (listName, size, value) outputWriter state
            | ListInitialisationWithCode(listName, size, indexName, instructions) ->
                computeListInitialisationWithCode (listName, size, indexName, instructions) outputWriter state
            | ListElementAssignment(identifier, indexExpression, valueExpression) ->
                computeListElementAssignment (identifier, indexExpression, valueExpression) outputWriter state
            | Iteration(listExpression, elementName, loopBody) ->
                computeListIteration (listExpression, elementName, loopBody) outputWriter state
        
        match computationResult with
        | Ok newState -> compute tail outputWriter newState
        | Error msg ->
            outputWriter.WriteERR(msg, line)
            Error msg


and private eval
        (outputWriter: IOutputWriter)
        (state: ComputationState)
        (exp: Expression) : InterpreterResult<float> =
    
    let env = state.Environment
    
    match exp with
    | LiteralNumber x -> Ok x
    | VariableReference id ->
        match (Map.tryFind id env) with
        | Some (Variable x) -> Ok x
        | Some _ -> Error $"{id} ist keine Variable"
        | None -> Error $"Der Verweis auf %A{id} konnt nicht aufgelöst werden."
    | ListAccess (id, indexExpression) ->
        match (Map.tryFind id env) with
        | Some (List xs) ->
            eval outputWriter state indexExpression
            >>= (int >> Ok)
            >>= evalListAccess xs
        | Some _ -> Error $"{id} ist keine Liste."
        | None -> Error $"Die Liste {id} existiert nicht."
    | ListLength id ->
        match (Map.tryFind id env) with
        | Some (List xs) -> Ok xs.Length
        | _ -> Error $"Die Liste {id} existiert nicht."
    | FunctionCall (id, args) ->
        match (Map.tryFind id env) with
        | Some (ProvidedFunction f) ->
            checkArgumentsAreOnlyValues args
            >>= evalList outputWriter state
            >>= (f outputWriter)
        | Some (CustomFunction (argNames, body)) ->
            evalCustomFunction (argNames, body) args outputWriter state
        | Some _ -> Error $"{id} ist keine Funktion."
        | None -> Error $"Der Verweis auf %A{id} konnte nicht aufgelöst werden."
    | Binary (BinaryOperator op, left, right) ->
        match (Map.tryFind (Identifier op) env) with
        | Some (ProvidedFunction f) ->
            evalList outputWriter state [left;right]
            >>= (f outputWriter)
        | Some (CustomFunction (argNames, body)) ->
            evalCustomFunction (argNames, body) [Value left; Value right] outputWriter state
        | Some _ -> Error $"{id} ist kein Operator."
        | None -> Error $"Der Operator %A{op} ist nicht definiert"
    | ParenthesizedExpression expression ->
        eval outputWriter state expression


and private evalListAccess
        (list: float array)
        (index: int) : InterpreterResult<float> =
    
    let trueIndex =
        if index < 0
            then list.Length + index
            else index
    if trueIndex >= 0 && trueIndex < list.Length
        then Ok list[trueIndex]
        else Error $"Der Index {index} liegt ausserhalb des Umfangs der Liste."


and private evalList
        (outputWriter: IOutputWriter)
        (state: ComputationState) 
        (expList: Expression list) : InterpreterResult<float list> =
    
    expList
    |> List.map (eval outputWriter state)
    |> List.fold (accumulateResults appendToList) (Ok [])


and private evalCustomFunction
        (parameter, body)
        (givenArgs : GivenArgument list)
        (outputWriter: IOutputWriter)
        (state: ComputationState) : InterpreterResult<float> =
    let env = state.Environment
    match checkFunctionSignature parameter givenArgs with
    | Error msg -> Error msg
    | Ok () ->
        let functionFilter _ = function | CustomFunction _ | ProvidedFunction _ -> true | _ -> false
        let argumentMapper = function
            | Pointer id ->
                match Map.tryFind id env with
                | Some (List ns) -> Ok (List ns)
                | _ -> Error $"{id} verweist auf eine nicht Existierende Liste."
            | Value expression ->
                match eval outputWriter state expression with
                | Ok x -> Ok (Variable x)
                | Error msg -> Error msg
                
        let evaluatedParams =
            givenArgs
            |> List.map argumentMapper
            |> List.fold (accumulateResults appendToList) (Ok [])
            
        match evaluatedParams with
        | Error msg -> Error msg
        | Ok args ->
            let parameterNames = parameter |> List.map (function |ValueArgument id -> id |PointerArgument id -> id)
            let scopedVariables = args |> List.zip parameterNames
            let scopedFunctions = env |> Map.filter functionFilter |> Map.toList
            let functionEnv = Map (scopedVariables @ scopedFunctions)
            let functionComputationState = {
                LastExpression = None
                Environment = functionEnv
                BreakFlag = false
                ReturnFlag = false }
            let returnValue = compute body outputWriter functionComputationState
            match returnValue with
            | Error e -> Error e
            | Ok {LastExpression = None} -> Error "Die Funktion hat keinen Wert zurückgegeben"
            | Ok {LastExpression = Some x} -> Ok x
            
            
            
and private evalListExpression
        (outputWriter: IOutputWriter)
        (state: ComputationState)
        (exp: ListExpression) : InterpreterResult<float array> =
    
    let env = state.Environment
    match exp with
    | ListReference id -> 
        match (env.TryFind id) with
        | Some (List xs) -> Ok xs
        | _ -> Error $"{id} ist keine Liste."
    | Range (low, high) ->
        let lowerEvalResult = eval outputWriter state low
        let upperEvalResult = eval outputWriter state high
        match (lowerEvalResult, upperEvalResult) with
        | Ok low, Ok up -> Ok [|low..up|]
        | Error msg, _ -> Error msg
        | _, Error msg -> Error msg
    | LiteralList expressions ->
        evalList outputWriter state expressions
        |> Runtime.map List.toArray
        
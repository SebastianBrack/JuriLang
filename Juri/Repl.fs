module Juri.Internal.Repl

open Interpreter
open CoreLib
open OutputWriter
open Runtime
open Parser
open ParserCombinators



let rec private repl (outputWriter: IOutputWriter) (parserContext: JuriContext) (state: ComputationState) =
    let mutable userInput = ""
    let mutable line = stdin.ReadLine()
    while not (line.EndsWith(";")) do
        userInput <- userInput + line + "\n"
        line <- stdin.ReadLine()
    userInput <- userInput + line.TrimEnd(';') + "\n"
    let stream = CharStream(userInput, parserContext)
    match stream.RunParser(juriProgram) with
    | Success (r,c,_) ->
        compute r outputWriter state
        >>= repl outputWriter c
    | Failure (msg, _) ->
        let oldContext = stream.GetContext()
        outputWriter.WriteERR(msg, stream.GetContext().Line)
        repl outputWriter oldContext state 
    | Fatal (msg, _) ->
        let oldContext = stream.GetContext()
        outputWriter.WriteERR(msg, stream.GetContext().Line)
        repl outputWriter oldContext state



let startRepl () =
    let outputWriter = ConsoleWriter()
    let initialState = { ComputationState.Default with Environment = createEnvWithCoreLibFunctions () }
    let initialParserContext = JuriContext.Default
    repl outputWriter initialParserContext initialState 
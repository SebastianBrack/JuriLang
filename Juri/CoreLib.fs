module Juri.Internal.CoreLib

open System
open Runtime
open LanguageModel

let private buildinAdd : ProvidedFunction = fun _ args -> args |> List.reduce ( + ) |> Ok
let private buildinMul : ProvidedFunction = fun _ args -> args |> List.reduce ( * ) |> Ok
let private buildinSub : ProvidedFunction = fun _ args -> args |> List.reduce ( - ) |> Ok
let private buildinDiv : ProvidedFunction = fun _ args -> args |> List.reduce ( / ) |> Ok




let private buildinEquals : ProvidedFunction =
    fun _ args ->
        match args with
        | [] | [_] -> Error (sprintf "Diese Funktion erwartet mindestens 2 Argumente - es wurden aber %i übergeben" args.Length)
        | head :: tail ->
            if List.forall ((=)head) tail
                then Ok 1.
                else Ok 0.

let private buildinInBoundarys : ProvidedFunction =
    fun _ args ->
        match args with
        | [a;b]   -> if a < b then Ok 1. else Ok 0.
        | [a;b;c] -> if a <= b && b < c then Ok 1. else Ok 0.
        | _       -> Error (sprintf "Diese Funktion erwartet 2 oder 3 Argumente - es wurden aber %i übergeben." args.Length)

let private buildinPrint : ProvidedFunction =
    fun out args ->
        args
        |> List.map (fun x -> $"{x} ")
        |> String.Concat
        |> out.WriteSTD
        Ok 0.

let private buildinBier : ProvidedFunction =
    fun out args ->
        out.WriteSTD "NIMM VERDAMMT NOCHMAL DIE HÄNDE WEG VON MEINEM BIER DU FICKSCHNITZEL SIND ZU VIEL JUUUNGE"
        Ok 0.


let private buildinJunge : ProvidedFunction =
    fun out args ->
        args
        |> List.map (fun x -> $"{x} ")
        |> String.Concat
        |> out.WriteSTD
        Ok 0.
        
let private buildinPrintNewline : ProvidedFunction =
    let join separator a b = a + separator + b
    fun out args ->
        args
        |> List.map (fun x -> $"{x}")
        |> List.reduce (join " ")
        |> sprintf "%s\n"
        |> out.WriteSTD
        Ok 0.

let private buildinPrintChar : ProvidedFunction =
    fun out args ->
        args
        |> List.map (fun x -> x |> int |> char)
        |> String.Concat
        |> out.WriteSTD
        Ok 0.

let private buildinPrintCharNewline : ProvidedFunction =
    fun out args ->
        args
        |> List.map (fun x -> x |> int |> char)
        |> String.Concat
        |> sprintf "%s\n"
        |> out.WriteSTD
        Ok 0.

let private buildinInput : ProvidedFunction =
    fun _ args ->
        args
        |> List.map char 
        |> String.Concat
        |> printf "%s"

        try
            stdin.ReadLine()
            |> float
            |> Ok
        with
        | _ -> Error $"Das ist doch keine Zahl Juuuuuunge!"

let private buildinRandom : ProvidedFunction =
    fun _ args ->
        let rand = Random()
        match args with
        | [a]     -> rand.Next(0, int a) |> float |> Ok
        | [a;b]   -> rand.Next(int a, int b) |> float |> Ok
        | _       -> Error (sprintf "Diese Funktion erwartet 1 oder 2 Argumente - es wurden aber %i übergeben." args.Length)

let private buildinFactorial : ProvidedFunction =
    fun _ args ->
        let factorial n =
            let rec loop i acc =
                match i with
                | 0 | 1 -> acc
                | _ -> loop (i-1) (acc * i)
            loop n 1
        match args with
        | [a]     -> factorial(int a) |> float |> Ok
        | _       -> Error (sprintf "Diese Funktion erwartet 1 Argument - es wurden aber %i übergeben." args.Length)

let private buildinSqrt : ProvidedFunction =
    fun _ args ->
        match args with
        | [a]     -> sqrt a |> float |> Ok
        | _       -> Error (sprintf "Diese Funktion erwartet 1  Argument - es wurden aber %i übergeben." args.Length)

let private buildinFloor : ProvidedFunction =
    fun _ args ->
        match args with
        | [a]     -> floor a |> float |> Ok
        | _       -> Error (sprintf "Diese Funktion erwartet 1  Argument - es wurden aber %i übergeben." args.Length)

let private buildinCeil : ProvidedFunction =
    fun _ args ->
        match args with
        | [a]     -> ceil a |> float |> Ok
        | _       -> Error (sprintf "Diese Funktion erwartet 1  Argument - es wurden aber %i übergeben." args.Length)


let private buildinRound : ProvidedFunction =
    fun _ args ->
        match args with
        | [a]     -> round a |> float |> Ok
        | _       -> Error (sprintf "Diese Funktion erwartet 1  Argument - es wurden aber %i übergeben." args.Length)

let private buildinPI : ProvidedFunction =
    fun _ args ->
        match args with
        | [a]     -> Math.PI |> float |> Ok
        | _       -> Error (sprintf "Diese Funktion erwartet 1  Argument - es wurden aber %i übergeben." args.Length)

let private buildinTime : ProvidedFunction =
    fun _ args ->
        match args with
        | [] -> DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds() |> float |> Ok
        | _  -> Error (sprintf "Diese Funktion erwartet 0 Argumente - es wurden aber %i übergeben." args.Length)
        

let private buildinQuickMath : ProvidedFunction =
    let test x =
        if x <= 20 then "+"
        elif x >= 20 && x < 40 then "*"
        elif x >= 40 && x < 60 then "-"
        elif x >= 60 && x < 80 then "/"
        elif x >= 80 && x < 100 then "%"
        else "**"
        
    fun _ args ->
        let rand = Random()
        let decider = rand.Next(0,120)
        //+ * - / % ** ??
        let op = test(decider)
       
        match args with
        | [a;b]   -> 
            match op with 
            | "+" -> a + b  |> float |> Ok
            | "*" -> a * b  |> float |> Ok
            | "-" -> a - b  |> float |> Ok
            | "/" -> a / b  |> float |> Ok
            | "%" -> a % b  |> float |> Ok
            | "**"-> a ** b |> float |> Ok
            | _ -> Error (sprintf "Du bist ein Otto")
        | _     -> Error (sprintf "Diese Funktion erwartet 2 Argumente - es wurden aber %i übergeben." args.Length)


let private argError n = Error (sprintf "Diese Funktion erwartet 2 Argumente - es wurden aber %i übergeben" n)

let private plus : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] -> Ok (l + r)
        | _      -> argError args.Length

let private minus : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] -> Ok (l - r)
        | _      -> argError args.Length

let private star : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] -> Ok (l * r)
        | _      -> argError args.Length

let private slash : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] -> Ok (l / r)
        | _      -> argError args.Length

let private lesser : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] ->
            if l < r
                then Ok 1.
                else Ok 0.
        | _ -> argError args.Length

let private juri : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] ->
            let rnd = Random(Environment.TickCount)
            if rnd.Next(100) > 50
                then Ok 1.
                else Ok 0.
        | _ -> argError args.Length

let private greater : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] ->
            if l > r
                then Ok 1.
                else Ok 0.
        | _ -> argError args.Length

let private equalsEquals : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] ->
            if l = r
                then Ok 1.
                else Ok 0.
        | _ -> argError args.Length

let private bangEquals : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] ->
            if l <> r
                then Ok 1.
                else Ok 0.
        | _ -> argError args.Length

let private lesserEquals : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] ->
            if l <= r
                then Ok 1.
                else Ok 0.
        | _ -> argError args.Length

let private greaterEquals : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] ->
            if l >= r
                then Ok 1.
                else Ok 0.
        | _ -> argError args.Length

let private modulo : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] -> Ok (l % r)
        | _      -> argError args.Length

let private intDivision : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] -> (l/ r) |> int |> float |> Ok
        | _      -> argError args.Length

let private pow : ProvidedFunction =
    fun _ args ->
        match args with
        | [l; r] -> Ok (l ** r)
        | _      -> argError args.Length



let createEnvWithCoreLibFunctions () : Environment =
    Map [
        (Identifier "add", ProvidedFunction buildinAdd)
        (Identifier "mul", ProvidedFunction buildinMul)
        (Identifier "sub", ProvidedFunction buildinSub)
        (Identifier "div", ProvidedFunction buildinDiv)
        (Identifier "bnd", ProvidedFunction buildinInBoundarys)
        (Identifier "eq", ProvidedFunction buildinEquals)
        (Identifier "print", ProvidedFunction buildinPrint)
        (Identifier "printn", ProvidedFunction buildinPrintNewline)
        (Identifier "printc", ProvidedFunction buildinPrintChar)
        (Identifier "printcn", ProvidedFunction buildinPrintCharNewline)
        (Identifier "juuunge", ProvidedFunction buildinJunge)
        (Identifier "bier", ProvidedFunction buildinBier)
        (Identifier "input", ProvidedFunction buildinInput)
        (Identifier "rand", ProvidedFunction buildinRandom)
        (Identifier "sqrt", ProvidedFunction buildinSqrt)
        (Identifier "quickmath", ProvidedFunction buildinQuickMath)
        (Identifier "factorial", ProvidedFunction buildinFactorial)
        (Identifier "floor", ProvidedFunction buildinFloor)
        (Identifier "ceil", ProvidedFunction buildinCeil)
        (Identifier "round", ProvidedFunction buildinRound)
        (Identifier "PI", ProvidedFunction buildinPI)
        (Identifier "time", ProvidedFunction buildinTime)
        (Identifier "+", ProvidedFunction plus)
        (Identifier "-", ProvidedFunction minus)
        (Identifier "*", ProvidedFunction star)
        (Identifier "/", ProvidedFunction slash)
        (Identifier "==", ProvidedFunction equalsEquals)
        (Identifier "!=", ProvidedFunction bangEquals)
        (Identifier "<", ProvidedFunction lesser)
        (Identifier ">", ProvidedFunction greater)
        (Identifier "<=", ProvidedFunction lesserEquals)
        (Identifier ">=", ProvidedFunction greaterEquals)
        (Identifier "%", ProvidedFunction modulo)
        (Identifier "//", ProvidedFunction intDivision)
        (Identifier "**", ProvidedFunction pow)
        (Identifier "??", ProvidedFunction juri)
        ]
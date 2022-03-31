module Juri.Internal.LanguageModel


type Identifier = Identifier of string
type BinaryOperator = BinaryOperator of string
type Expression =
    | LiteralNumber of float
    | ListAccess of Identifier * Expression
    | ListLength of Identifier
    | VariableReference of Identifier
    | FunctionCall of functionName: Identifier * parameter: GivenArgument list
    | Binary of operator: BinaryOperator * left: Expression * right: Expression
    | Unary of operator: BinaryOperator * Expression
    | ParenthesizedExpression of Expression
    
and GivenArgument =
    | Value of Expression
    | Pointer of ListExpression
    
and ListExpression =
    | LiteralList of Expression list
    | Range of lower: Expression * upper: Expression
    | ListReference of Identifier
    
type Parameter =
    | ValueArgument of Identifier
    | PointerArgument of Identifier

type Instruction =
    | Expression of Expression
    | Assignment of variableName: Identifier * value: Expression
    | ListAssignment of listName: Identifier * values: ListExpression
    | ListInitialisationWithValue of listName: Identifier * size: Expression * value: Expression
    | ListInitialisationWithCode of listName: Identifier * size: Expression * indexName: Identifier * body: JuriProgram
    | ListElementAssignment of  listName: Identifier * index: Expression * value: Expression
    | FunctionDefinition of functionName: Identifier * arguments: Parameter list * functionBody: JuriProgram
    | Loop of condition: Expression * repeat: bool * loopBody: JuriProgram
    | Iteration of list: ListExpression * elementName: Identifier * loopBody: JuriProgram
    | OperatorDefinition of operator: BinaryOperator * leftArg: Parameter * rightArg: Parameter * functionBody: JuriProgram
    | Break
    | Return of Expression

and JuriProgram = (Instruction * int) list
using Juri.Internal;
using Microsoft.FSharp.Collections;

namespace WebInterpreter;
    
public class JuriOutput
{
    public FSharpList<string> Standard { get; set; }
    public Output.ErrorMessage[] Error { get; set; }
    public FSharpList<string> Meta { get; set; }
    

    public JuriOutput(Output.InterpreterOutputStreams streams)
    {
        Standard = streams.Standard.ReadToEnd();
        Error = streams.Error.ReadToEnd().ToArray();
        Meta = streams.MetaInfo.ReadToEnd();
    }
}
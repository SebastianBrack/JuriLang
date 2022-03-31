using Juri.Internal;
using Microsoft.FSharp.Collections;

namespace WebInterpreter.Controllers;

using Microsoft.AspNetCore.Mvc;



[ApiController]
[Route("/interpret")]
public class JuriCompilerController : Controller
{
    [HttpGet]
    public async Task<JuriOutput> InterpretCode([FromQuery]string code)
    {
        var codeBytes = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(code);
        var codeText = System.Text.Encoding.UTF8.GetString(codeBytes);

        var interpreter = new Juri.Api.Interpreter();
        
        interpreter.ParseJuriProgram(codeText);
        
        if (interpreter.ParsingOk())
        {
            interpreter.ExecuteProgram();
        }

        await Task.CompletedTask;
        return new JuriOutput(interpreter.GetOutputStreams());
    }
}
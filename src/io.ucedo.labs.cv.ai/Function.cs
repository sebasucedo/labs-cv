using Amazon.Lambda.Core;
using io.ucedo.labs.cv.ai.domain;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace io.ucedo.labs.cv.ai;

public class Function
{
    private readonly Generator _generator;

    public Function()
    {
        _generator = new Generator();
    }

    public async Task<string> FunctionHandler(string input, ILambdaContext context)
    {
        var key = input.Replace(" ", "%20").Replace("\u0026", "&");

        var html = await _generator.Generate(key) ?? Constants.SHRUGGIE;

        return html;
    }
}

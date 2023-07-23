using Amazon.Lambda.Core;
using io.ucedo.labs.cv.ai.domain;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

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
        var key = input.Replace(" ", "%20");

        var html = await _generator.Generate(key) ?? Constants.SHRUGGIE;

        return html;
    }
}

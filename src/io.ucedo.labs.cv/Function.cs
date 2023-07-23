using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using io.ucedo.labs.cv.domain;
using System;
using System.Net;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace io.ucedo.labs.cv;

public class Function
{
    readonly HtmlGenerator _generator;
    public Function()
    {
        _generator = new HtmlGenerator();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            string key = request.QueryStringParameters != null ?
                                    string.Join("&", request.QueryStringParameters.Select(kvp => kvp.Key + "=" + kvp.Value)).Replace(" ", "%20") :
                                    Constants.DEFAULT; 

            //string queryString = Uri.EscapeDataString(string.Join("&", request.QueryStringParameters.Select(kvp => kvp.Key + "=" + kvp.Value)));

            var generated = await _generator.GenerateHtml(key);

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)generated.StatusCode,
                Body = generated.Content,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "text/html" }
                }
            };
            return response;
        }
        catch (Exception ex)
		{
            LambdaLogger.Log($"Exception: {ex.Message}\r\n{ex.StackTrace}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"<html>{Constants.SHRUGGIE}</html>",
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "text/html" }
                }
            };
        }
    }

}

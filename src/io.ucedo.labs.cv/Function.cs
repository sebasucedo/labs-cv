using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using io.ucedo.labs.cv.domain;
using System.Net;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace io.ucedo.labs.cv;

public class Function
{
    
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
		try
		{
            var html = await HtmlGenerator.GenerateHtml();

            HttpStatusCode statusCode = HttpStatusCode.OK; //or HttpStatusCode.Created when not cached

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)statusCode,
                Body = html,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "text/html" }
                }
            };
            return response;
        }
        catch (Exception ex)
		{
            LambdaLogger.Log($"{ex.Message}\r\n{ex.StackTrace}");
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

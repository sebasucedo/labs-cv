using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using DotLiquid;
using io.ucedo.labs.cv.domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Lambda;
using System.Net;

namespace io.ucedo.labs.cv;

public class HtmlGenerator
{
    const string DATA_URL = "https://cdn.ucedo.io/cv/raw.json";
    const string ENVIRONMENT_SOCKET_URL = "api_gateway_websocket";
    const string ENVIRONMENT_ARN_AI_LAMBDA = "arn_ai_lambda";

    readonly CacheManager _cacheManager;

    public HtmlGenerator()
    {
        _cacheManager = new CacheManager(new DynamoDBContext(new AmazonDynamoDBClient()));
    }

    public async Task<GenerateResponse> GenerateHtml(string key)
    {
        var html = await GetBasic();

        var response = new GenerateResponse();
        var body = await _cacheManager.Get(key);
        if (string.IsNullOrEmpty(body))
        {
            body = await GetBody(key);
            response.StatusCode = HttpStatusCode.Created;
            await Generate(key);
        }

        html = SetBody(html, body);
        response.Content = html;

        return response;
    }

    private static async Task<string> GetBody(string queryString)
    {
        var socketUrl = System.Environment.GetEnvironmentVariable(ENVIRONMENT_SOCKET_URL) ?? string.Empty;
        var websocketUrl = socketUrl + (queryString == Constants.DEFAULT ? string.Empty : "?" + queryString);

        string templateContent = await File.ReadAllTextAsync("./views/body.liquid");

        Template template = Template.Parse(templateContent);
        var templarParameters = Hash.FromAnonymousObject(new { queryString, websocketUrl });
        var html = template.Render(templarParameters);

        return html;
    }

    private static string SetBody(string html, string body)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        HtmlNode bodyNode = doc.DocumentNode.SelectSingleNode("//body");
        bodyNode.InnerHtml = body;
        html = doc.DocumentNode.OuterHtml;
        return html;
    }

    private static async Task<string> GetBasic()
    {
        var name = await GetName();
        var html = await GetHtmlFromTemplate(name);

        return html;
    }

    private static async Task<string> GetName()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(DATA_URL);
        var json = await response.Content.ReadAsStringAsync();

        using JsonDocument document = JsonDocument.Parse(json);

        JsonElement root = document.RootElement;
        if (root.TryGetProperty("name", out JsonElement nameElement))
        {
            string nombre = nameElement.GetString() ?? string.Empty;
            return nombre;
        }

        return string.Empty;
    }

    private static async Task<string> GetHtmlFromTemplate(string name)
    {
        string templateContent = await File.ReadAllTextAsync("./views/index.liquid");

        Template template = Template.Parse(templateContent);
        var templarParameters = Hash.FromAnonymousObject(new { name });
        var html = template.Render(templarParameters);

        return html;
    }

    private static async Task Generate(string queryString)
    {
        var lambdaCliente = new AmazonLambdaClient();

        var functionArn = System.Environment.GetEnvironmentVariable(ENVIRONMENT_ARN_AI_LAMBDA);
        var payload = JsonSerializer.Serialize(queryString);

        var invokeRequest = new InvokeRequest
        {
            FunctionName = functionArn,
            InvocationType = InvocationType.Event,
            Payload = payload,
        };

        try
        {
            var response = await lambdaCliente.InvokeAsync(invokeRequest);

            if (response.HttpStatusCode != HttpStatusCode.Accepted)
                LambdaLogger.Log($"Call to {functionArn} failed with status code: {response.HttpStatusCode}.");

            LambdaLogger.Log($"Call to {functionArn}, payload: {payload}.");
        }
        catch (Exception ex)
        {
            LambdaLogger.Log($"Exception calling {functionArn} failed with message: {ex.Message}.\r\n{ex.StackTrace}");
        }
    }

}

using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using io.ucedo.labs.cv.ai.domain;
using DotLiquid;
using io.ucedo.labs.cv.ai.openai;
using System.Collections.Specialized;
using System.Web;
using Amazon.DynamoDBv2.Model;
using HtmlAgilityPack;

namespace io.ucedo.labs.cv.ai;

public class Generator
{
    const string DATA_URL = "https://cdn.ucedo.io/cv/raw.json";

    private readonly OpenAI _openAI;
    private readonly CacheManager _cacheManager;

    public Generator()
    {
        var openAiKey = Environment.GetEnvironmentVariable("openai_api_key") ?? string.Empty;

        _openAI = new OpenAI(openAiKey);
        _cacheManager = new CacheManager(new DynamoDBContext(new AmazonDynamoDBClient()));
    }

    public async Task<string?> Generate(string key)
    {
        LambdaLogger.Log($"Begin {nameof(Generate)}(key: {key})");

        var data = await GetData();
        var parameters = GetParameters(key);
        var dataAI = await GetDataFromOpenAI(data, parameters);

        var html = await GetHtmlFromTemplate(dataAI);

        html = await ReplaceBodyFromOpenAI(html, parameters);

        html = await ReplaceProfilePictureFromOpenAI(html, parameters.As);
        
        await Save(key, html);

        LambdaLogger.Log($"End {nameof(Generate)}(key: {key})");
        return html;
    }

    private async Task Save(string key, string html)
    {
        try
        {
            await _cacheManager.Add(key, html);
        }
        catch (Exception ex)
        {
            LambdaLogger.Log($"Error when persisting to DynamoDB. {ex.Message}");
        }
    }

    private static Dictionary<string, string> ParseQueryString(string queryString)
    {
        NameValueCollection queryParameters = HttpUtility.ParseQueryString(queryString);
        return queryParameters.AllKeys.ToDictionary(k => k, k => queryParameters[k]);
    }

    private static InputParameters GetParameters(string queryString)
    {
        var parameters = new InputParameters();

        if (queryString == Constants.DEFAULT)
            return parameters;

        var queryStringParameters = ParseQueryString(queryString);
        if (queryStringParameters == null || queryStringParameters.Count == 0)
            return parameters;

        var p = queryStringParameters;

        parameters.QueryString = queryString;

        var @as = nameof(InputParameters.As).ToLower();
        if (p.ContainsKey(@as))
            parameters.As = p[@as];

        var language = nameof(InputParameters.Language).ToLower();
        if (p.ContainsKey(language))
            parameters.Language = p[language];

        var format = nameof(InputParameters.Format).ToLower();
        if (p.ContainsKey(format))
            parameters.Format = p[format];

        return parameters;
    }

    private static async Task<Data> GetData()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(DATA_URL);
        var json = await response.Content.ReadAsStringAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var data = JsonSerializer.Deserialize<Data>(json, options);
        if (data == null)
            throw new NullReferenceException(json.ToString());

        return data;
    }

    private static async Task<string> GetHtmlFromTemplate(Data data)
    {
        string templateContent = await File.ReadAllTextAsync("./views/body.liquid");

        Template template = Template.Parse(templateContent);
        var templarParameters = Hash.FromAnonymousObject(data);
        var html = template.Render(templarParameters);

        return html;
    }

    public async Task<Data> GetDataFromOpenAI(Data data, InputParameters inputParameters)
    {
        LambdaLogger.Log("Begin OpenAI data");

        var drawnUpAs = string.Empty;
        if (!string.IsNullOrEmpty(inputParameters.As))
            drawnUpAs = $", drawn up as if it were {inputParameters.As} but without saying that it is {inputParameters.As}";

        foreach (var experience in data.Experiences.AsEnumerable().Reverse())
        {
            var experiencePrompt = Prompts.GetExperiencePrompt(experience.Company, experience.Title, experience.Age, experience.Description, drawnUpAs);
            experience.Description = await _openAI.SendChatCompletionRequest(experiencePrompt);
        }

        var aboutPrompt = Prompts.GetAboutPrompt(data.About, drawnUpAs);
        data.About = await _openAI.SendChatCompletionRequest(aboutPrompt);

        LambdaLogger.Log("End OpenAI data");
        return data;
    }

    public async Task<string> ReplaceBodyFromOpenAI(string html, InputParameters inputParameters)
    {
        if (inputParameters.Language == Constants.DEFAULT_LANGUAGE && inputParameters.Format != Constants.DEFAULT)
        {
            LambdaLogger.Log("OpenAI will not process body");
            return html;
        }

        LambdaLogger.Log("Begin OpenAI body");

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        if (inputParameters.Language != Constants.DEFAULT_LANGUAGE)
        {
            var tasks = new List<Task>
            {
                TranslateContentById(doc, inputParameters.Language, "basicInfo"),
                TranslateContentById(doc, inputParameters.Language, "about"),
                TranslateContentById(doc, inputParameters.Language, "experience"),
                TranslateContentById(doc, inputParameters.Language, "grey")
            };

            await Task.WhenAll(tasks);
        }

        html = doc.DocumentNode.OuterHtml;

        LambdaLogger.Log("End OpenAI body");
        return html;
    }

    private async Task TranslateContentById(HtmlDocument doc, string language, string id)
    {
        HtmlNode nodeAbout = doc.GetElementbyId(id);
        if (nodeAbout == null)
            return;

        var aboutContent = nodeAbout.InnerHtml;
        var prompt = $"Give me the same html code but with content translated to {language} language: {aboutContent}";
        aboutContent = await _openAI.SendSingleChatCompletionRequest(prompt);
        if (!string.IsNullOrEmpty(aboutContent))
            nodeAbout.InnerHtml = aboutContent;
    }

    public async Task<string> ReplaceProfilePictureFromOpenAI(string html, string @as)
    {
        if (string.IsNullOrEmpty(@as))
            return html;

        var imagePath = "./files/profile_web.png";
        var maskPath = "./files/profile_web_mask.png";
        var prompt = Prompts.GetProfilePicturePrompt(@as);
        var response = await _openAI.SendImagesEditsRequest(prompt, imagePath, maskPath);
        if (response == null || !response.data.Any())
            return html;

        var profilePictureUrl = response.data.First().url;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        HtmlNode nodePicture = doc.GetElementbyId("profilePicture");
        var background_image = $"background-image: url('{profilePictureUrl}');";
        nodePicture.SetAttributeValue("style", background_image);
        html = doc.DocumentNode.OuterHtml;

        return html;
    }
}

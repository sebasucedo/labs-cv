using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using DotLiquid;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using io.ucedo.labs.cv.ai.domain;
using io.ucedo.labs.cv.ai.openai;
using static io.ucedo.labs.cv.ai.domain.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace io.ucedo.labs.cv.ai;

public class Generator
{
    const string DATA_URL = "https://cdn.ucedo.io/cv/raw.json";
    const string OPENAI_URL = "https://api.openai.com/";
    const string CDN_URL = "https://cdn.ucedo.io/";

    private readonly OpenAI _openAI;
    private readonly CacheManager _cacheManager;
    private readonly S3Manager _s3Manager;

    public Generator()
    {
        var openAiKey = Environment.GetEnvironmentVariable("openai_api_key") ?? string.Empty;
        IHttpClientFactory httpClientFactory = GetHttpClientFactory(openAiKey);
        _openAI = new OpenAI(httpClientFactory);
        _cacheManager = new CacheManager(new DynamoDBContext(new AmazonDynamoDBClient()));

        var bucketName = "my-personal-public-info";
        var awsAccessKey = Environment.GetEnvironmentVariable("aws_access_key") ?? string.Empty;
        var awsSecretKey = Environment.GetEnvironmentVariable("aws_secret_key") ?? string.Empty;
        _s3Manager = new S3Manager(bucketName, awsAccessKey, awsSecretKey);
    }

    private static IHttpClientFactory GetHttpClientFactory(string openAiKey)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(Constants.OPENAI_CLIENT_NAME, client =>
        {
            client.BaseAddress = new Uri(OPENAI_URL);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);
            client.Timeout = TimeSpan.FromSeconds(90);
        });
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        if (httpClientFactory == null)
            throw new NullReferenceException(nameof(httpClientFactory));

        return httpClientFactory;
    }

    public async Task<string?> Generate(string key)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        LambdaLogger.Log($"Begin {nameof(Generate)}(key: {key})");

        var parameters = GetParameters(key);
        var data = await GetData(parameters.Datasource);

        //var dataAI = await GetDataFromOpenAI(data, parameters);
        var dataAI = await GetDataFromOpenAIParallel(data, parameters);

        var html = await GetHtmlFromTemplate(dataAI);

        html = await ReplaceBodyFromOpenAI(html, parameters);

        html = await ReplaceProfilePictureFromOpenAI(html, parameters.As);

        stopwatch.Stop();

        await Save(key, html);

        LambdaLogger.Log($"End {nameof(Generate)}(key: {key}) - Duration:{stopwatch.ElapsedMilliseconds} milliseconds");
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

    public static IDictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new Dictionary<string, string>();
        var regex = new Regex(@"(?<key>\w+)=((?<value>'[^']*')|(?<value>[^&]*))");
        var matches = regex.Matches(queryString);

        foreach (Match match in matches)
        {
            var key = match.Groups["key"].Value;
            var value = match.Groups["value"].Value;

            result[key] = value.Trim('\''); 
        }

        return result;
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

        var datasource = nameof(InputParameters.Datasource).ToLower();
        if (p.ContainsKey(datasource))
            parameters.Datasource = p[datasource];

        return parameters;
    }

    private static async Task<Data> GetData(string datasource)
    {
        using var httpClient = new HttpClient();

        var dataUrl = datasource == Constants.DEFAULT ? DATA_URL : datasource;
        var response = await httpClient.GetAsync(dataUrl);
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
            experience.Description = await _openAI.SendChatCompletionRequest(experiencePrompt, 500);
        }

        var aboutPrompt = Prompts.GetAboutPrompt(data.About, drawnUpAs);
        data.About = await _openAI.SendChatCompletionRequest(aboutPrompt);

        LambdaLogger.Log("End OpenAI data");
        return data;
    }

    public async Task<Data> GetDataFromOpenAIParallel(Data data, InputParameters inputParameters)
    {
        LambdaLogger.Log("Begin OpenAI data");

        var drawnUpAs = string.Empty;
        if (!string.IsNullOrEmpty(inputParameters.As))
            drawnUpAs = $", drawn up as if it were {inputParameters.As} but without saying that it is {inputParameters.As}";

        var tasks = data.Experiences.AsParallel().Select(x => { return SetExperienceFromOpenAI(x, drawnUpAs); });
        await Task.WhenAll(tasks);

        var aboutPrompt = Prompts.GetAboutPrompt(data.About, drawnUpAs);
        data.About = await _openAI.SendSingleChatCompletionRequest(aboutPrompt);

        LambdaLogger.Log("End OpenAI data");
        return data;
    }

    private async Task SetExperienceFromOpenAI(Experience experience, string drawnUpAs)
    {
        var experiencePrompt = Prompts.GetExperiencePrompt(experience.Company, experience.Title, experience.Age, experience.Description, drawnUpAs);
        experience.Description = await _openAI.SendSingleChatCompletionRequest(experiencePrompt, 500);
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
                TranslateContentById(doc, inputParameters.Language, "experience", true),
                TranslateContentById(doc, inputParameters.Language, "grey")
            };

            await Task.WhenAll(tasks);
        }

        html = doc.DocumentNode.OuterHtml;

        LambdaLogger.Log("End OpenAI body");
        return html;
    }

    private async Task TranslateContentById(HtmlDocument doc, string language, string id, bool isPosibleLargeText = false)
    {
        HtmlNode nodeAbout = doc.GetElementbyId(id);
        if (nodeAbout == null)
            return;

        var aboutContent = nodeAbout.InnerHtml;
        string response = string.Empty;

        var parsed = isPosibleLargeText ? ParseText(aboutContent, "</li>", 2) : new[] { aboutContent };
        foreach (var p in parsed)
        {
            var prompt = $"Give me the same html code but with content translated to {language} language: {p}";
            response += await _openAI.SendSingleChatCompletionRequest(prompt);
        }

        if (!string.IsNullOrEmpty(response))
            nodeAbout.InnerHtml = response;
    }

    static IEnumerable<string> ParseText(string text, string separator, int groupSize)
    {

        var parts = text.Split(new[] { separator }, StringSplitOptions.None).ToList();
        for (var i = 0; i < parts.Count - 1; i++)
            parts[i] = parts[i] + separator;

        List<string> combined = new();

        for (int i = 0; i < parts.Count; i += groupSize)
        {
            int endIndex = Math.Min(i + groupSize, parts.Count);
            combined.Add(string.Join(string.Empty, parts.GetRange(i, endIndex - i)));
        }

        return combined;
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

        var path = await SaveFileFromUrl(profilePictureUrl);
        if (!string.IsNullOrEmpty(path))
            profilePictureUrl = CDN_URL + path;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        HtmlNode nodePicture = doc.GetElementbyId("profilePicture");
        var background_image = $"background-image: url('{profilePictureUrl}');";
        nodePicture.SetAttributeValue("style", background_image);
        html = doc.DocumentNode.OuterHtml;

        return html;
    }

    public async Task<string> SaveFileFromUrl(string url)
    {
        using HttpClient httpClient = new();
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                byte[] contenido = await response.Content.ReadAsByteArrayAsync();
                var path = await _s3Manager.Upload(contenido);
                return path;
            }
        }
        catch (Exception ex)
        {
            LambdaLogger.Log($"Error getting file.\r\n{ex.Message}\r\n{ex.StackTrace}");
        }
        return string.Empty;
    }
}

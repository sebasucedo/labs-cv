using io.ucedo.labs.cv.ai.domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace io.ucedo.labs.cv.ai.openai;

public class OpenAI
{
    const string URL_CHAT_COMPLETIONS = "v1/chat/completions";
    const string URL_IMAGE_EDIT = "v1/images/edits";

    private readonly IHttpClientFactory _httpClientFactory;

    public string Model { get; set; } = "gpt-3.5-turbo";
    public double Temperature { get; set; } = 0.7;
    public string SystemRoleContent { get; set; } = "You are a helpful assistant";

    private readonly List<Message> _messages = new();

    public OpenAI(IHttpClientFactory httpClientFactory, string systemRole)
    {
        if (!string.IsNullOrEmpty(systemRole))
            SystemRoleContent = $"You are {systemRole}.";
        _httpClientFactory = httpClientFactory;

        _messages.Add(new Message { role = "system", content = systemRole });
    }
    public OpenAI(IHttpClientFactory httpClientFactory) : this(httpClientFactory, string.Empty) { }

    public async Task<string> SendChatCompletionRequest(string userPrompt, int maxTokens = Constants.MAX_TOKENS)
    {
        var client = _httpClientFactory.CreateClient(Constants.OPENAI_CLIENT_NAME);

        _messages.Add(new Message { role = "user", content = userPrompt });

        var requestData = new
        {
            model = Model,
            messages = _messages.ToArray(),
            temperature = Temperature,
            max_tokens = maxTokens
        };

        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(URL_CHAT_COMPLETIONS, content);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadFromJsonAsync<ChatResponse>();

            var responseMessage = responseBody?.choices.FirstOrDefault()?.message.content ?? string.Empty;
            _messages.Add(new Message { role = "assistant", content = responseMessage });
            return responseMessage;
        }

        throw new Exception($"Error: {response.StatusCode}");
    }

    public async Task<string> SendSingleChatCompletionRequest(string prompt)
    {
        var client = _httpClientFactory.CreateClient(Constants.OPENAI_CLIENT_NAME);

        var requestData = new
        {
            model = Model,
            messages = new[]
                    {
                        new { role = "system", content = SystemRoleContent },
                        new { role = "user", content = prompt }
                    },
            temperature = Temperature,
        };

        var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(URL_CHAT_COMPLETIONS, content);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadFromJsonAsync<ChatResponse>();

            var responseMessage = responseBody?.choices.FirstOrDefault()?.message.content ?? string.Empty;
            return responseMessage;
        }

        throw new Exception($"Error: {response.StatusCode}");

    }

    public async Task<ImageResponse?> SendImagesEditsRequest(string userPrompt, string imagePath, string maskPath)
    {
        var client = _httpClientFactory.CreateClient(Constants.OPENAI_CLIENT_NAME);

        byte[] imageData = File.ReadAllBytes(imagePath);
        byte[] maskData = File.ReadAllBytes(maskPath);

        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent(imageData), "image", "profile_web.png" },
            { new ByteArrayContent(maskData), "mask", "profile_web_mask.png" },
            { new StringContent(userPrompt), "prompt" },
            { new StringContent("1"), "n" },
            { new StringContent("512x512"), "size" }
        };

        var response = await client.PostAsync(URL_IMAGE_EDIT, content);

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadFromJsonAsync<ImageResponse>();
            return responseBody;
        }

        throw new Exception($"Error: {response.StatusCode}");
    }

}
